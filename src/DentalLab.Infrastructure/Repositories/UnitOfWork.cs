using DentalLab.Domain.Entities;
using DentalLab.Domain.Interfaces;
using DentalLab.Infrastructure.Data;

namespace DentalLab.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DentalLabDbContext _context;

    public IRepository<Clinic> Clinics { get; }
    public IRepository<Patient> Patients { get; }
    public IRepository<Case> Cases { get; }
    public IRepository<CaseItem> CaseItems { get; }
    public IRepository<WorkflowStage> WorkflowStages { get; }
    public IRepository<CaseStageHistory> CaseStageHistories { get; }
    public IRepository<Technician> Technicians { get; }
    public IRepository<Invoice> Invoices { get; }
    public IRepository<Payment> Payments { get; }
    public IRepository<Delivery> Deliveries { get; }
    public IRepository<ActivityLog> ActivityLogs { get; }

    public UnitOfWork(DentalLabDbContext context)
    {
        _context = context;
        Clinics = new Repository<Clinic>(context);
        Patients = new Repository<Patient>(context);
        Cases = new Repository<Case>(context);
        CaseItems = new Repository<CaseItem>(context);
        WorkflowStages = new Repository<WorkflowStage>(context);
        CaseStageHistories = new Repository<CaseStageHistory>(context);
        Technicians = new Repository<Technician>(context);
        Invoices = new Repository<Invoice>(context);
        Payments = new Repository<Payment>(context);
        Deliveries = new Repository<Delivery>(context);
        ActivityLogs = new Repository<ActivityLog>(context);
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}
