using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Interfaces;

namespace DentalLab.Application.Services;

public class TechnicianService : ITechnicianService
{
    private readonly IUnitOfWork _uow;

    public TechnicianService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<TechnicianDto>> GetAllAsync(CancellationToken ct = default)
    {
        var techs = await _uow.Technicians.ListAllAsync(ct);
        return techs.OrderBy(t => t.FullName).Select(MapToDto).ToList();
    }

    public async Task<TechnicianDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var t = await _uow.Technicians.GetByIdAsync(id, ct);
        return t == null ? null : MapToDto(t);
    }

    public async Task<TechnicianDto> CreateAsync(CreateTechnicianDto dto, CancellationToken ct = default)
    {
        var entity = new Technician
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Phone = dto.Phone,
            Specialty = dto.Specialty,
            IsActive = true
        };
        await _uow.Technicians.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<TechnicianDto> UpdateAsync(int id, UpdateTechnicianDto dto, CancellationToken ct = default)
    {
        var entity = await _uow.Technicians.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Technician", id);

        entity.FullName = dto.FullName;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Specialty = dto.Specialty;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _uow.Technicians.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Technicians.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Technician", id);

        entity.IsActive = false;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Technicians.Update(entity);
        await _uow.SaveChangesAsync(ct);
    }

    private static TechnicianDto MapToDto(Technician t) => new()
    {
        Id = t.Id,
        FullName = t.FullName,
        Email = t.Email,
        Phone = t.Phone,
        Specialty = t.Specialty,
        IsActive = t.IsActive
    };
}
