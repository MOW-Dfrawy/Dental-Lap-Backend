using DentalLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DentalLab.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(DentalLabDbContext context)
    {
        await context.Database.EnsureCreatedAsync();
        await SeedAsync(context);
    }

    private static async Task SeedAsync(DentalLabDbContext context)
    {
        if (!await context.WorkflowStages.AnyAsync())
        {
            context.WorkflowStages.AddRange(
                new WorkflowStage { Name = "Received", Order = 1, EstimatedDurationMinutes = 30, IsActive = true, Description = "Case received from clinic" },
                new WorkflowStage { Name = "Design", Order = 2, EstimatedDurationMinutes = 120, IsActive = true, Description = "CAD/CAM design" },
                new WorkflowStage { Name = "Wax-up", Order = 3, EstimatedDurationMinutes = 180, IsActive = true, Description = "Wax modeling" },
                new WorkflowStage { Name = "Casting", Order = 4, EstimatedDurationMinutes = 240, IsActive = true, Description = "Metal casting / milling" },
                new WorkflowStage { Name = "Finishing", Order = 5, EstimatedDurationMinutes = 120, IsActive = true, Description = "Polishing and finishing" },
                new WorkflowStage { Name = "Quality Check", Order = 6, EstimatedDurationMinutes = 30, IsActive = true, Description = "QA inspection" },
                new WorkflowStage { Name = "Packaging", Order = 7, EstimatedDurationMinutes = 15, IsActive = true, Description = "Final packaging" }
            );
        }

        if (!await context.Technicians.AnyAsync())
        {
            context.Technicians.AddRange(
                new Technician { FullName = "Dr. Sarah Ahmed", Specialty = "Crown & Bridge", Email = "sarah@lab.test", IsActive = true },
                new Technician { FullName = "Mike Wilson", Specialty = "CAD/CAM", Email = "mike@lab.test", IsActive = true },
                new Technician { FullName = "Yusuf Hassan", Specialty = "Ceramics", Email = "yusuf@lab.test", IsActive = true }
            );
        }

        if (!await context.Clinics.AnyAsync())
        {
            context.Clinics.Add(new Clinic
            {
                Name = "Smile Dental Clinic",
                ContactPerson = "Dr. Omar",
                Phone = "555-0100",
                Email = "info@smiledental.test",
                City = "Cairo",
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }
}
