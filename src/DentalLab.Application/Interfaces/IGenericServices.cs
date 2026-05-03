using DentalLab.Application.Common;
using DentalLab.Application.DTOs;

namespace DentalLab.Application.Interfaces;

public interface IClinicService
{
    Task<PagedResult<ClinicDto>> GetAsync(QueryParameters parameters, CancellationToken ct = default);
    Task<ClinicDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ClinicDto> CreateAsync(CreateClinicDto dto, CancellationToken ct = default);
    Task<ClinicDto> UpdateAsync(int id, UpdateClinicDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IPatientService
{
    Task<PagedResult<PatientDto>> GetAsync(QueryParameters parameters, int? clinicId, CancellationToken ct = default);
    Task<PatientDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PatientDto> CreateAsync(CreatePatientDto dto, CancellationToken ct = default);
    Task<PatientDto> UpdateAsync(int id, UpdatePatientDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface ITechnicianService
{
    Task<IReadOnlyList<TechnicianDto>> GetAllAsync(CancellationToken ct = default);
    Task<TechnicianDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<TechnicianDto> CreateAsync(CreateTechnicianDto dto, CancellationToken ct = default);
    Task<TechnicianDto> UpdateAsync(int id, UpdateTechnicianDto dto, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}

public interface IDeliveryService
{
    Task<IReadOnlyList<DeliveryDto>> GetAllAsync(CancellationToken ct = default);
    Task<DeliveryDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<DeliveryDto>> GetByCaseAsync(int caseId, CancellationToken ct = default);
    Task<DeliveryDto> CreateAsync(CreateDeliveryDto dto, string? performedBy, CancellationToken ct = default);
    Task<DeliveryDto> UpdateStatusAsync(int id, UpdateDeliveryStatusDto dto, string? performedBy, CancellationToken ct = default);
}
