using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Enums;
using DentalLab.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DentalLab.Application.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(IUnitOfWork uow, ILogger<WorkflowService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WorkflowStageDto>> GetStagesAsync(CancellationToken ct = default)
    {
        var stages = await _uow.WorkflowStages.ListAllAsync(ct);
        return stages.OrderBy(s => s.Order).Select(MapToDto).ToList();
    }

    public async Task<WorkflowStageDto?> GetStageByIdAsync(int id, CancellationToken ct = default)
    {
        var stage = await _uow.WorkflowStages.GetByIdAsync(id, ct);
        return stage == null ? null : MapToDto(stage);
    }

    public async Task<WorkflowStageDto> CreateStageAsync(CreateWorkflowStageDto dto, CancellationToken ct = default)
    {
        var stage = new WorkflowStage
        {
            Name = dto.Name,
            Description = dto.Description,
            Order = dto.Order,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            IsActive = true
        };
        await _uow.WorkflowStages.AddAsync(stage, ct);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(stage);
    }

    public async Task<WorkflowStageDto> UpdateStageAsync(int id, UpdateWorkflowStageDto dto, CancellationToken ct = default)
    {
        var stage = await _uow.WorkflowStages.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("WorkflowStage", id);

        stage.Name = dto.Name;
        stage.Description = dto.Description;
        stage.Order = dto.Order;
        stage.EstimatedDurationMinutes = dto.EstimatedDurationMinutes;
        stage.IsActive = dto.IsActive;
        stage.UpdatedAt = DateTime.UtcNow;

        _uow.WorkflowStages.Update(stage);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(stage);
    }

    public async Task DeleteStageAsync(int id, CancellationToken ct = default)
    {
        var stage = await _uow.WorkflowStages.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("WorkflowStage", id);

        var inUse = await _uow.CaseStageHistories.CountAsync(h => h.StageId == id, ct);
        if (inUse > 0)
        {
            // Soft-deactivate instead of hard-delete to preserve history
            stage.IsActive = false;
            stage.UpdatedAt = DateTime.UtcNow;
            _uow.WorkflowStages.Update(stage);
        }
        else
        {
            _uow.WorkflowStages.Remove(stage);
        }
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<CaseStageHistoryDto> MoveCaseToStageAsync(int caseId, MoveCaseToStageDto dto, string? performedBy, CancellationToken ct = default)
    {
        var caseEntity = await _uow.Cases.GetByIdAsync(caseId, ct)
            ?? throw new NotFoundException("Case", caseId);

        var newStage = await _uow.WorkflowStages.GetByIdAsync(dto.StageId, ct)
            ?? throw new NotFoundException("WorkflowStage", dto.StageId);

        if (!newStage.IsActive)
            throw new BusinessRuleException("Cannot move case to an inactive stage.");

        if (caseEntity.Status == CaseStatus.Cancelled || caseEntity.Status == CaseStatus.Delivered)
            throw new BusinessRuleException($"Cannot change stage of a case in status {caseEntity.Status}.");

        if (dto.TechnicianId.HasValue)
        {
            var tech = await _uow.Technicians.GetByIdAsync(dto.TechnicianId.Value, ct)
                ?? throw new NotFoundException("Technician", dto.TechnicianId.Value);
            if (!tech.IsActive)
                throw new BusinessRuleException("Cannot assign an inactive technician.");
        }

        // Close current open history record (if any)
        var openHistory = (await _uow.CaseStageHistories.ListAsync(
            h => h.CaseId == caseId && h.ExitedAt == null, ct)).FirstOrDefault();

        if (openHistory != null)
        {
            openHistory.ExitedAt = DateTime.UtcNow;
            openHistory.DurationMinutes = (int)(openHistory.ExitedAt.Value - openHistory.EnteredAt).TotalMinutes;
            _uow.CaseStageHistories.Update(openHistory);
        }

        // Create new history entry
        var history = new CaseStageHistory
        {
            CaseId = caseId,
            StageId = dto.StageId,
            TechnicianId = dto.TechnicianId ?? caseEntity.AssignedTechnicianId,
            EnteredAt = DateTime.UtcNow,
            Notes = dto.Notes
        };
        await _uow.CaseStageHistories.AddAsync(history, ct);

        // Update case
        caseEntity.CurrentStageId = dto.StageId;
        if (dto.TechnicianId.HasValue)
            caseEntity.AssignedTechnicianId = dto.TechnicianId.Value;

        if (caseEntity.StartedAt == null)
            caseEntity.StartedAt = DateTime.UtcNow;

        if (caseEntity.Status == CaseStatus.New)
            caseEntity.Status = CaseStatus.InProgress;

        caseEntity.UpdatedAt = DateTime.UtcNow;
        _uow.Cases.Update(caseEntity);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "MoveCaseStage",
            EntityType = nameof(Case),
            EntityId = caseId,
            Details = $"Case {caseEntity.CaseNumber} moved to stage '{newStage.Name}'.",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Case {CaseId} moved to stage {StageId} by {User}",
            caseId, dto.StageId, performedBy ?? "system");

        return new CaseStageHistoryDto
        {
            Id = history.Id,
            CaseId = history.CaseId,
            StageId = history.StageId,
            StageName = newStage.Name,
            TechnicianId = history.TechnicianId,
            EnteredAt = history.EnteredAt,
            ExitedAt = history.ExitedAt,
            DurationMinutes = history.DurationMinutes,
            Notes = history.Notes
        };
    }

    public async Task<IReadOnlyList<CaseStageHistoryDto>> GetCaseHistoryAsync(int caseId, CancellationToken ct = default)
    {
        var history = (await _uow.CaseStageHistories.ListAsync(h => h.CaseId == caseId, ct))
            .OrderBy(h => h.EnteredAt).ToList();

        var result = new List<CaseStageHistoryDto>();
        foreach (var h in history)
        {
            result.Add(new CaseStageHistoryDto
            {
                Id = h.Id,
                CaseId = h.CaseId,
                StageId = h.StageId,
                StageName = (await _uow.WorkflowStages.GetByIdAsync(h.StageId, ct))?.Name,
                TechnicianId = h.TechnicianId,
                TechnicianName = h.TechnicianId.HasValue
                    ? (await _uow.Technicians.GetByIdAsync(h.TechnicianId.Value, ct))?.FullName
                    : null,
                EnteredAt = h.EnteredAt,
                ExitedAt = h.ExitedAt,
                DurationMinutes = h.DurationMinutes,
                Notes = h.Notes
            });
        }
        return result;
    }

    public async Task<int> GetTotalDurationMinutesAsync(int caseId, CancellationToken ct = default)
    {
        var history = await _uow.CaseStageHistories.ListAsync(h => h.CaseId == caseId, ct);
        return history.Sum(h => h.DurationMinutes ?? 0);
    }

    private static WorkflowStageDto MapToDto(WorkflowStage s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        Order = s.Order,
        IsActive = s.IsActive,
        EstimatedDurationMinutes = s.EstimatedDurationMinutes
    };
}
