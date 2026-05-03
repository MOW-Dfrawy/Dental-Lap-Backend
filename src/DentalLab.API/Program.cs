using DentalLab.API.Middleware;
using DentalLab.Application;
using DentalLab.Infrastructure;
using DentalLab.Infrastructure.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Dental Lab Workflow API",
        Version = "v1",
        Description = "Production-ready REST API for Dental Lab Workflow Management. " +
                      "Send the X-Role header (Admin, Technician, or Reception) on every request."
    });

    options.AddSecurityDefinition("RoleHeader", new OpenApiSecurityScheme
    {
        Description = "Role header. Supply: Admin, Technician, or Reception.",
        Name = "X-Role",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "RoleHeader"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DentalLabDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Dental Lab API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors();

app.MapControllers();


app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();
