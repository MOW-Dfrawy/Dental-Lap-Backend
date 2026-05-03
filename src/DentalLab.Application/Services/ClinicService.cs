using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Interfaces;

namespace DentalLab.Application.Services;

public class ClinicService : IClinicService
{
    private readonly IUnitOfWork _uow;

    public ClinicService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<ClinicDto>> GetAsync(QueryParameters parameters, CancellationToken ct = default)
    {
        var query = _uow.Clinics.Query();

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            var s = parameters.Search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s) ||
                (c.City != null && c.City.ToLower().Contains(s)) ||
                (c.Phone != null && c.Phone.Contains(s)));
        }

        query = (parameters.SortBy?.ToLower(), parameters.SortDescending) switch
        {
            ("name", true) => query.OrderByDescending(c => c.Name),
            ("name", false) => query.OrderBy(c => c.Name),
            (_, true) => query.OrderByDescending(c => c.CreatedAt),
            _ => query.OrderBy(c => c.Name)
        };

        var total = query.Count();
        var items = query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList()
            .Select(MapToDto)
            .ToList();

        return new PagedResult<ClinicDto>(items, total, parameters.Page, parameters.PageSize);
    }

    public async Task<ClinicDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var c = await _uow.Clinics.GetByIdAsync(id, ct);
        return c == null ? null : MapToDto(c);
    }

    public async Task<ClinicDto> CreateAsync(CreateClinicDto dto, CancellationToken ct = default)
    {
        var entity = new Clinic
        {
            Name = dto.Name,
            ContactPerson = dto.ContactPerson,
            Phone = dto.Phone,
            Email = dto.Email,
            Address = dto.Address,
            City = dto.City,
            Notes = dto.Notes,
            IsActive = true
        };
        await _uow.Clinics.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task<ClinicDto> UpdateAsync(int id, UpdateClinicDto dto, CancellationToken ct = default)
    {
        var entity = await _uow.Clinics.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Clinic", id);

        entity.Name = dto.Name;
        entity.ContactPerson = dto.ContactPerson;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Address = dto.Address;
        entity.City = dto.City;
        entity.Notes = dto.Notes;
        entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _uow.Clinics.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return MapToDto(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _uow.Clinics.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Clinic", id);

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _uow.Clinics.Update(entity);
        await _uow.SaveChangesAsync(ct);
    }

    private static ClinicDto MapToDto(Clinic c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        ContactPerson = c.ContactPerson,
        Phone = c.Phone,
        Email = c.Email,
        Address = c.Address,
        City = c.City,
        Notes = c.Notes,
        IsActive = c.IsActive,
        CreatedAt = c.CreatedAt
    };
}
