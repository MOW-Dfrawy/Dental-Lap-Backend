using DentalLab.Application.DTOs;

namespace DentalLab.Application.Interfaces;

public interface IWorkflowService
{
    Task<IReadOnlyList<WorkflowStageDto>> GetStagesAsync(CancellationToken ct = default);
    Task<WorkflowStageDto?> GetStageByIdAsync(int id, CancellationToken ct = default);
    Task<WorkflowStageDto> CreateStageAsync(CreateWorkflowStageDto dto, CancellationToken ct = default);
    Task<WorkflowStageDto> UpdateStageAsync(int id, UpdateWorkflowStageDto dto, CancellationToken ct = default);
    Task DeleteStageAsync(int id, CancellationToken ct = default);

    Task<CaseStageHistoryDto> MoveCaseToStageAsync(int caseId, MoveCaseToStageDto dto, string? performedBy, CancellationToken ct = default);
    Task<IReadOnlyList<CaseStageHistoryDto>> GetCaseHistoryAsync(int caseId, CancellationToken ct = default);
    Task<int> GetTotalDurationMinutesAsync(int caseId, CancellationToken ct = default);
}
