using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Enums;
using DentalLab.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DentalLab.Application.Services;

public class CaseService : ICaseService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<CaseService> _logger;

    public CaseService(IUnitOfWork uow, ILogger<CaseService> logger)
    {
        _uow = uow;
        _logger = logger;
    }

    public async Task<PagedResult<CaseDto>> GetCasesAsync(CaseFilterDto filter, CancellationToken ct = default)
    {
        var query = _uow.Cases.Query();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(c =>
                c.CaseNumber.ToLower().Contains(s) ||
                c.Title.ToLower().Contains(s) ||
                (c.Description != null && c.Description.ToLower().Contains(s)));
        }

        if (filter.Status.HasValue)
            query = query.Where(c => c.Status == filter.Status.Value);

        if (filter.ClinicId.HasValue)
            query = query.Where(c => c.ClinicId == filter.ClinicId.Value);

        if (filter.TechnicianId.HasValue)
            query = query.Where(c => c.AssignedTechnicianId == filter.TechnicianId.Value);

        if (filter.PatientId.HasValue)
            query = query.Where(c => c.PatientId == filter.PatientId.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(c => c.ReceivedDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(c => c.ReceivedDate <= filter.ToDate.Value);

        // Sorting
        query = (filter.SortBy?.ToLower(), filter.SortDescending) switch
        {
            ("title", true) => query.OrderByDescending(c => c.Title),
            ("title", false) => query.OrderBy(c => c.Title),
            ("duedate", true) => query.OrderByDescending(c => c.DueDate),
            ("duedate", false) => query.OrderBy(c => c.DueDate),
            ("status", true) => query.OrderByDescending(c => c.Status),
            ("status", false) => query.OrderBy(c => c.Status),
            ("price", true) => query.OrderByDescending(c => c.Price),
            ("price", false) => query.OrderBy(c => c.Price),
            (_, true) => query.OrderByDescending(c => c.ReceivedDate),
            _ => query.OrderBy(c => c.ReceivedDate)
        };

        var totalCount = query.Count();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 20 : filter.PageSize;

        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList()
            .Select(MapToDto)
            .ToList();

        // Hydrate names for paged results
        await HydrateNamesAsync(items, ct);

        return new PagedResult<CaseDto>(items, totalCount, page, pageSize);
    }

    public async Task<CaseDto?> GetCaseByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Cases.GetByIdAsync(id, ct);
        if (entity == null) return null;

        var dto = MapToDto(entity);
        dto.Items = (await _uow.CaseItems.ListAsync(i => i.CaseId == id, ct))
            .Select(MapItemToDto).ToList();

        var historyEntities = (await _uow.CaseStageHistories.ListAsync(h => h.CaseId == id, ct))
            .OrderBy(h => h.EnteredAt).ToList();
        var historyDtos = new List<CaseStageHistoryDto>();
        foreach (var h in historyEntities)
        {
            historyDtos.Add(new CaseStageHistoryDto
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
        dto.StageHistory = historyDtos;

        await HydrateNamesAsync(new List<CaseDto> { dto }, ct);

        // total duration
        dto.TotalDurationMinutes = historyDtos.Sum(h => h.DurationMinutes ?? 0);

        return dto;
    }

    public async Task<CaseDto?> GetCaseByNumberAsync(string caseNumber, CancellationToken ct = default)
    {
        var list = await _uow.Cases.ListAsync(c => c.CaseNumber == caseNumber, ct);
        var entity = list.FirstOrDefault();
        if (entity == null) return null;
        return await GetCaseByIdAsync(entity.Id, ct);
    }

    public async Task<CaseDto> CreateCaseAsync(CreateCaseDto dto, string? performedBy, CancellationToken ct = default)
    {
        // Validate references
        var clinic = await _uow.Clinics.GetByIdAsync(dto.ClinicId, ct)
            ?? throw new NotFoundException("Clinic", dto.ClinicId);

        var patient = await _uow.Patients.GetByIdAsync(dto.PatientId, ct)
            ?? throw new NotFoundException("Patient", dto.PatientId);

        if (patient.ClinicId != clinic.Id)
            throw new BusinessRuleException("Patient does not belong to the specified clinic.");

        if (dto.AssignedTechnicianId.HasValue)
        {
            var tech = await _uow.Technicians.GetByIdAsync(dto.AssignedTechnicianId.Value, ct)
                ?? throw new NotFoundException("Technician", dto.AssignedTechnicianId.Value);
            if (!tech.IsActive)
                throw new BusinessRuleException("Cannot assign an inactive technician.");
        }

        var caseNumber = await GenerateCaseNumberAsync(ct);

        var entity = new Case
        {
            CaseNumber = caseNumber,
            Title = dto.Title,
            Description = dto.Description,
            ClinicId = dto.ClinicId,
            PatientId = dto.PatientId,
            AssignedTechnicianId = dto.AssignedTechnicianId,
            DueDate = dto.DueDate,
            Price = dto.Price,
            Priority = dto.Priority,
            ToothShade = dto.ToothShade,
            Notes = dto.Notes,
            Status = CaseStatus.New,
            ReceivedDate = DateTime.UtcNow
        };

        await _uow.Cases.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        // Add items
        foreach (var i in dto.Items)
        {
            await _uow.CaseItems.AddAsync(new CaseItem
            {
                CaseId = entity.Id,
                ProductName = i.ProductName,
                ToothNumber = i.ToothNumber,
                Material = i.Material,
                Shade = i.Shade,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Notes = i.Notes
            }, ct);
        }

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "CreateCase",
            EntityType = nameof(Case),
            EntityId = entity.Id,
            Details = $"Case {entity.CaseNumber} created.",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Case {CaseNumber} created (id={Id}) by {User}",
            entity.CaseNumber, entity.Id, performedBy ?? "system");

        return (await GetCaseByIdAsync(entity.Id, ct))!;
    }

    public async Task<CaseDto> UpdateCaseAsync(int id, UpdateCaseDto dto, string? performedBy, CancellationToken ct = default)
    {
        var entity = await _uow.Cases.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Case", id);

        if (dto.AssignedTechnicianId.HasValue && dto.AssignedTechnicianId != entity.AssignedTechnicianId)
        {
            var tech = await _uow.Technicians.GetByIdAsync(dto.AssignedTechnicianId.Value, ct)
                ?? throw new NotFoundException("Technician", dto.AssignedTechnicianId.Value);
            if (!tech.IsActive)
                throw new BusinessRuleException("Cannot assign an inactive technician.");
        }

        var previousStatus = entity.Status;

        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.AssignedTechnicianId = dto.AssignedTechnicianId;
        entity.DueDate = dto.DueDate;
        entity.Status = dto.Status;
        entity.Price = dto.Price;
        entity.Priority = dto.Priority;
        entity.ToothShade = dto.ToothShade;
        entity.Notes = dto.Notes;
        entity.UpdatedAt = DateTime.UtcNow;

        if (previousStatus != CaseStatus.InProgress && dto.Status == CaseStatus.InProgress && entity.StartedAt == null)
            entity.StartedAt = DateTime.UtcNow;

        if (previousStatus != CaseStatus.Delivered && dto.Status == CaseStatus.Delivered)
            entity.CompletedAt = DateTime.UtcNow;

        _uow.Cases.Update(entity);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "UpdateCase",
            EntityType = nameof(Case),
            EntityId = entity.Id,
            Details = $"Case {entity.CaseNumber} updated. Status: {previousStatus} -> {dto.Status}",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return (await GetCaseByIdAsync(entity.Id, ct))!;
    }

    public async Task DeleteCaseAsync(int id, string? performedBy, CancellationToken ct = default)
    {
        var entity = await _uow.Cases.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Case", id);

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Cases.Update(entity);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "DeleteCase",
            EntityType = nameof(Case),
            EntityId = entity.Id,
            Details = $"Case {entity.CaseNumber} deleted (soft).",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);
    }

    private async Task<string> GenerateCaseNumberAsync(CancellationToken ct)
    {
        var prefix = $"CS-{DateTime.UtcNow:yyyyMMdd}-";
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var todayCount = await _uow.Cases.CountAsync(c =>
            c.CreatedAt >= todayStart && c.CreatedAt < todayEnd, ct);

        var sequence = (todayCount + 1).ToString("D4");
        return prefix + sequence;
    }

    private async Task HydrateNamesAsync(List<CaseDto> dtos, CancellationToken ct)
    {
        if (dtos.Count == 0) return;

        var clinicIds = dtos.Select(d => d.ClinicId).Distinct().ToList();
        var patientIds = dtos.Select(d => d.PatientId).Distinct().ToList();
        var techIds = dtos.Where(d => d.AssignedTechnicianId.HasValue)
            .Select(d => d.AssignedTechnicianId!.Value).Distinct().ToList();
        var stageIds = dtos.Where(d => d.CurrentStageId.HasValue)
            .Select(d => d.CurrentStageId!.Value).Distinct().ToList();

        var clinics = (await _uow.Clinics.ListAsync(c => clinicIds.Contains(c.Id), ct)).ToDictionary(c => c.Id, c => c.Name);
        var patients = (await _uow.Patients.ListAsync(p => patientIds.Contains(p.Id), ct)).ToDictionary(p => p.Id, p => p.FullName);
        var techs = techIds.Count > 0
            ? (await _uow.Technicians.ListAsync(t => techIds.Contains(t.Id), ct)).ToDictionary(t => t.Id, t => t.FullName)
            : new Dictionary<int, string>();
        var stages = stageIds.Count > 0
            ? (await _uow.WorkflowStages.ListAsync(s => stageIds.Contains(s.Id), ct)).ToDictionary(s => s.Id, s => s.Name)
            : new Dictionary<int, string>();

        foreach (var d in dtos)
        {
            if (clinics.TryGetValue(d.ClinicId, out var cn)) d.ClinicName = cn;
            if (patients.TryGetValue(d.PatientId, out var pn)) d.PatientName = pn;
            if (d.AssignedTechnicianId.HasValue && techs.TryGetValue(d.AssignedTechnicianId.Value, out var tn))
                d.AssignedTechnicianName = tn;
            if (d.CurrentStageId.HasValue && stages.TryGetValue(d.CurrentStageId.Value, out var sn))
                d.CurrentStageName = sn;
        }
    }

    internal static CaseDto MapToDto(Case c) => new()
    {
        Id = c.Id,
        CaseNumber = c.CaseNumber,
        Title = c.Title,
        Description = c.Description,
        ClinicId = c.ClinicId,
        PatientId = c.PatientId,
        AssignedTechnicianId = c.AssignedTechnicianId,
        CurrentStageId = c.CurrentStageId,
        Status = c.Status,
        ReceivedDate = c.ReceivedDate,
        DueDate = c.DueDate,
        StartedAt = c.StartedAt,
        CompletedAt = c.CompletedAt,
        Price = c.Price,
        Priority = c.Priority,
        ToothShade = c.ToothShade,
        Notes = c.Notes
    };

    internal static CaseItemDto MapItemToDto(CaseItem i) => new()
    {
        Id = i.Id,
        CaseId = i.CaseId,
        ProductName = i.ProductName,
        ToothNumber = i.ToothNumber,
        Material = i.Material,
        Shade = i.Shade,
        Quantity = i.Quantity,
        UnitPrice = i.UnitPrice,
        LineTotal = i.LineTotal,
        Notes = i.Notes
    };
}
