using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Interfaces;

namespace DentalLab.Application.Services;

public class PatientService : IPatientService
{
    private readonly IUnitOfWork _uow;

    public PatientService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<PatientDto>> GetAsync(QueryParameters parameters, int? clinicId, CancellationToken ct = default)
    {
        var query = _uow.Patients.Query();

        if (clinicId.HasValue)
            query = query.Where(p => p.ClinicId == clinicId.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(p =>
                p.FullName.ToLower().Contains(s) ||
                (p.Phone != null && p.Phone.Contains(s)));
        }

        query = parameters.SortDescending
            ? query.OrderByDescending(p => p.CreatedAt)
            : query.OrderBy(p => p.FullName);

        var total = query.Count();
        var entities = query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();

        var clinicIds = entities.Select(e => e.ClinicId).Distinct().ToList();
        var clinics = (await _uow.Clinics.ListAsync(c => clinicIds.Contains(c.Id), ct))
            .ToDictionary(c => c.Id, c => c.Name);

        var items = entities.Select(p => MapToDto(p, clinics.GetValueOrDefault(p.ClinicId))).ToList();

        return new PagedResult<PatientDto>(items, total, parameters.Page, parameters.PageSize);
    }

    public async Task<PatientDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var p = await _uow.Patients.GetByIdAsync(id, ct);
        if (p == null) return null;
        var clinic = await _uow.Clinics.GetByIdAsync(p.ClinicId, ct);
        return MapToDto(p, clinic?.Name);
    }

    public async Task<PatientDto> CreateAsync(CreatePatientDto dto, CancellationToken ct = default)
    {
        var clinic = await _uow.Clinics.GetByIdAsync(dto.ClinicId, ct)
            ?? throw new NotFoundException("Clinic", dto.ClinicId);

        var entity = new Patient
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Phone = dto.Phone,
            Notes = dto.Notes,
            ClinicId = dto.ClinicId
        };
        await _uow.Patients.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(entity, clinic.Name);
    }

    public async Task<PatientDto> UpdateAsync(int id, UpdatePatientDto dto, CancellationToken ct = default)
    {
        var entity = await _uow.Patients.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Patient", id);

        if (dto.ClinicId != entity.ClinicId)
        {
            _ = await _uow.Clinics.GetByIdAsync(dto.ClinicId, ct)
                ?? throw new NotFoundException("Clinic", dto.ClinicId);
        }

        entity.FullName = dto.FullName;
        entity.DateOfBirth = dto.DateOfBirth;
        entity.Gender = dto.Gender;
        entity.Phone = dto.Phone;
        entity.Notes = dto.Notes;
        entity.ClinicId = dto.ClinicId;
        entity.UpdatedAt = DateTime.UtcNow;

        _uow.Patients.Update(entity);
        await _uow.SaveChangesAsync(ct);

        var clinic = await _uow.Clinics.GetByIdAsync(entity.ClinicId, ct);
        return MapToDto(entity, clinic?.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Patients.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Patient", id);

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Patients.Update(entity);
        await _uow.SaveChangesAsync(ct);
    }

    private static PatientDto MapToDto(Patient p, string? clinicName) => new()
    {
        Id = p.Id,
        FullName = p.FullName,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender,
        Phone = p.Phone,
        Notes = p.Notes,
        ClinicId = p.ClinicId,
        ClinicName = clinicName,
        CreatedAt = p.CreatedAt
    };
}
