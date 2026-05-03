using DentalLab.Application.Interfaces;
using DentalLab.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DentalLab.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICaseService, CaseService>();
        services.AddScoped<IWorkflowService, WorkflowService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IClinicService, ClinicService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ITechnicianService, TechnicianService>();
        services.AddScoped<IDeliveryService, DeliveryService>();
        return services;
    }
}
