using DentalLab.Application.Common;
using DentalLab.Application.DTOs;
using DentalLab.Application.Interfaces;
using DentalLab.Domain.Entities;
using DentalLab.Domain.Enums;
using DentalLab.Domain.Interfaces;

namespace DentalLab.Application.Services;

public class DeliveryService : IDeliveryService
{
    private readonly IUnitOfWork _uow;

    public DeliveryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<DeliveryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var deliveries = await _uow.Deliveries.ListAllAsync(ct);
        var caseIds = deliveries.Select(d => d.CaseId).Distinct().ToList();
        var cases = (await _uow.Cases.ListAsync(c => caseIds.Contains(c.Id), ct))
            .ToDictionary(c => c.Id, c => c.CaseNumber);

        return deliveries
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => MapToDto(d, cases.GetValueOrDefault(d.CaseId)))
            .ToList();
    }

    public async Task<DeliveryDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var d = await _uow.Deliveries.GetByIdAsync(id, ct);
        if (d == null) return null;
        var c = await _uow.Cases.GetByIdAsync(d.CaseId, ct);
        return MapToDto(d, c?.CaseNumber);
    }

    public async Task<IReadOnlyList<DeliveryDto>> GetByCaseAsync(int caseId, CancellationToken ct = default)
    {
        var c = await _uow.Cases.GetByIdAsync(caseId, ct)
            ?? throw new NotFoundException("Case", caseId);

        var deliveries = await _uow.Deliveries.ListAsync(d => d.CaseId == caseId, ct);
        return deliveries
            .OrderByDescending(d => d.CreatedAt)
            .Select(d => MapToDto(d, c.CaseNumber))
            .ToList();
    }

    public async Task<DeliveryDto> CreateAsync(CreateDeliveryDto dto, string? performedBy, CancellationToken ct = default)
    {
        var c = await _uow.Cases.GetByIdAsync(dto.CaseId, ct)
            ?? throw new NotFoundException("Case", dto.CaseId);

        var delivery = new Delivery
        {
            CaseId = dto.CaseId,
            ScheduledDate = dto.ScheduledDate,
            RecipientName = dto.RecipientName,
            DeliveryAddress = dto.DeliveryAddress,
            Courier = dto.Courier,
            TrackingNumber = dto.TrackingNumber,
            Notes = dto.Notes,
            Status = dto.ScheduledDate.HasValue ? DeliveryStatus.Scheduled : DeliveryStatus.Pending
        };
        await _uow.Deliveries.AddAsync(delivery, ct);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "CreateDelivery",
            EntityType = nameof(Delivery),
            EntityId = delivery.Id,
            Details = $"Delivery created for case {c.CaseNumber}.",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        return MapToDto(delivery, c.CaseNumber);
    }

    public async Task<DeliveryDto> UpdateStatusAsync(int id, UpdateDeliveryStatusDto dto, string? performedBy, CancellationToken ct = default)
    {
        var delivery = await _uow.Deliveries.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Delivery", id);

        delivery.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.Notes))
            delivery.Notes = dto.Notes;

        switch (dto.Status)
        {
            case DeliveryStatus.OutForDelivery when delivery.DispatchedAt == null:
                delivery.DispatchedAt = DateTime.UtcNow;
                break;
            case DeliveryStatus.Delivered:
                delivery.DeliveredAt = DateTime.UtcNow;
                if (delivery.DispatchedAt == null)
                    delivery.DispatchedAt = DateTime.UtcNow;
                // Update related case status
                var c = await _uow.Cases.GetByIdAsync(delivery.CaseId, ct);
                if (c != null)
                {
                    c.Status = CaseStatus.Delivered;
                    c.CompletedAt = DateTime.UtcNow;
                    c.UpdatedAt = DateTime.UtcNow;
                    _uow.Cases.Update(c);
                }
                break;
        }

        delivery.UpdatedAt = DateTime.UtcNow;
        _uow.Deliveries.Update(delivery);

        await _uow.ActivityLogs.AddAsync(new ActivityLog
        {
            Action = "UpdateDeliveryStatus",
            EntityType = nameof(Delivery),
            EntityId = delivery.Id,
            Details = $"Delivery status changed to {dto.Status}.",
            PerformedBy = performedBy
        }, ct);

        await _uow.SaveChangesAsync(ct);

        var caseEntity = await _uow.Cases.GetByIdAsync(delivery.CaseId, ct);
        return MapToDto(delivery, caseEntity?.CaseNumber);
    }

    private static DeliveryDto MapToDto(Delivery d, string? caseNumber) => new()
    {
        Id = d.Id,
        CaseId = d.CaseId,
        CaseNumber = caseNumber,
        ScheduledDate = d.ScheduledDate,
        DispatchedAt = d.DispatchedAt,
        DeliveredAt = d.DeliveredAt,
        RecipientName = d.RecipientName,
        DeliveryAddress = d.DeliveryAddress,
        Courier = d.Courier,
        TrackingNumber = d.TrackingNumber,
        Status = d.Status,
        Notes = d.Notes
    };
}
