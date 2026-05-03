using DentalLab.Application.Common;
using DentalLab.Application.DTOs;

namespace DentalLab.Application.Interfaces;

public interface ICaseService
{
    Task<PagedResult<CaseDto>> GetCasesAsync(CaseFilterDto filter, CancellationToken ct = default);
    Task<CaseDto?> GetCaseByIdAsync(int id, CancellationToken ct = default);
    Task<CaseDto?> GetCaseByNumberAsync(string caseNumber, CancellationToken ct = default);
    Task<CaseDto> CreateCaseAsync(CreateCaseDto dto, string? performedBy, CancellationToken ct = default);
    Task<CaseDto> UpdateCaseAsync(int id, UpdateCaseDto dto, string? performedBy, CancellationToken ct = default);
    Task DeleteCaseAsync(int id, string? performedBy, CancellationToken ct = default);
}
