using DentalLab.Domain.Entities;

namespace DentalLab.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Clinic> Clinics { get; }
    IRepository<Patient> Patients { get; }
    IRepository<Case> Cases { get; }
    IRepository<CaseItem> CaseItems { get; }
    IRepository<WorkflowStage> WorkflowStages { get; }
    IRepository<CaseStageHistory> CaseStageHistories { get; }
    IRepository<Technician> Technicians { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<Payment> Payments { get; }
    IRepository<Delivery> Deliveries { get; }
    IRepository<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
