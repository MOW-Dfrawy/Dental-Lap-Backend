# Dental Lab Workflow Management System

A production-ready Clean Architecture backend (REST API + Service Layer) for a dental lab, built with **.NET 8** and **SQLite**. Designed to power both a Windows Forms desktop client and a future web frontend.

---

## Features

- **Clean Architecture** (Domain / Application / Infrastructure / API)
- **SOLID** principles, Dependency Injection, Repository + Unit of Work
- **Entity Framework Core 8** (Code-First) with **SQLite** (zero-install)
- **Auto database creation** on first run (`EnsureCreated`) + seed data
- **Workflow tracking** with full stage history & per-stage time tracking
- **Auto-generated case numbers** (`CS-yyyyMMdd-####`)
- **Invoices + payments** with status auto-transitions
- **Delivery tracking**
- **Role-based authorization** (Admin / Technician / Reception via `X-Role` header)
- **Activity log** for audit trail
- **Global error handling** middleware
- **Swagger UI** for live API testing
- **CORS** open by default (lock down in production)
- **Pagination, filtering, sorting, validation**

---

## Project Structure

```
DentalLabSystem/
├── DentalLabSystem.sln
├── README.md
└── src/
    ├── DentalLab.Domain/                 # Entities, enums, repository contracts
    │   ├── Entities/
    │   ├── Enums/
    │   └── Interfaces/
    ├── DentalLab.Application/            # Business logic, DTOs, services
    │   ├── Common/                       # Result, exceptions, paging
    │   ├── DTOs/
    │   ├── Interfaces/
    │   ├── Services/
    │   └── DependencyInjection.cs
    ├── DentalLab.Infrastructure/         # EF Core, repositories, UoW
    │   ├── Data/                         # DbContext, DbInitializer
    │   ├── Repositories/                 # Generic Repository, UnitOfWork
    │   └── DependencyInjection.cs
    └── DentalLab.API/                    # ASP.NET Core REST API
        ├── Controllers/
        ├── Middleware/                   # ErrorHandling, Role auth filter
        ├── Program.cs
        └── appsettings.json
```

---

## Run Instructions

### Prerequisites
- .NET 8.0 SDK ([download](https://dotnet.microsoft.com/download/dotnet/8.0))

### Steps

```bash
# 1. Restore packages
cd DentalLabSystem
dotnet restore

# 2. Run the API
cd src/DentalLab.API
dotnet run
```

The API will:
1. Start on **http://localhost:5000**
2. Create `dentallab.db` (SQLite) automatically on first run
3. Seed default workflow stages, technicians, and one demo clinic
4. Open Swagger UI at **http://localhost:5000/swagger**

### Test the API

Open **http://localhost:5000/swagger** in your browser. Click **Authorize** in the top-right and set `X-Role` to one of:
- `Admin` — full access
- `Reception` — create/read for clinics, patients, cases, invoices, payments
- `Technician` — read cases, move stages

---

## Sample Requests

### Health check
```bash
curl http://localhost:5000/api/health
```

### Create a clinic
```bash
curl -X POST http://localhost:5000/api/clinics \
  -H "Content-Type: application/json" \
  -H "X-Role: Admin" \
  -d '{
    "name": "Bright Smile Dental",
    "contactPerson": "Dr. Khaled",
    "phone": "555-0123",
    "email": "info@brightsmile.test",
    "city": "Alexandria"
  }'
```

### Create a patient
```bash
curl -X POST http://localhost:5000/api/patients \
  -H "Content-Type: application/json" \
  -H "X-Role: Reception" \
  -d '{
    "fullName": "Ahmed Mostafa",
    "dateOfBirth": "1985-04-12",
    "gender": "Male",
    "phone": "555-9876",
    "clinicId": 1
  }'
```

### Create a case (auto-generates case number)
```bash
curl -X POST http://localhost:5000/api/cases \
  -H "Content-Type: application/json" \
  -H "X-Role: Reception" \
  -d '{
    "title": "Upper Crown #14",
    "description": "Zirconia crown for upper-left first molar",
    "clinicId": 1,
    "patientId": 1,
    "assignedTechnicianId": 1,
    "dueDate": "2026-04-15T00:00:00Z",
    "price": 350.00,
    "priority": "Normal",
    "toothShade": "A2",
    "items": [
      {
        "productName": "Zirconia Crown",
        "toothNumber": "14",
        "material": "Zirconia",
        "shade": "A2",
        "quantity": 1,
        "unitPrice": 350.00
      }
    ]
  }'
```

**Sample response:**
```json
{
  "id": 1,
  "caseNumber": "CS-20260404-0001",
  "title": "Upper Crown #14",
  "clinicName": "Bright Smile Dental",
  "patientName": "Ahmed Mostafa",
  "status": "New",
  "statusName": "New",
  "price": 350.00,
  "items": [ { "id": 1, "productName": "Zirconia Crown", "lineTotal": 350.00 } ]
}
```

### Move case to next workflow stage
```bash
curl -X POST http://localhost:5000/api/cases/1/move-stage \
  -H "Content-Type: application/json" \
  -H "X-Role: Technician" \
  -d '{
    "stageId": 2,
    "technicianId": 2,
    "notes": "Starting design phase"
  }'
```

### Get case with full history & time tracking
```bash
curl http://localhost:5000/api/cases/1 -H "X-Role: Admin"
curl http://localhost:5000/api/cases/1/total-duration -H "X-Role: Admin"
```

### List cases (filter + paginate)
```bash
curl "http://localhost:5000/api/cases?status=InProgress&page=1&pageSize=20&sortBy=duedate&sortDescending=false" \
  -H "X-Role: Admin"
```

### Create an invoice
```bash
curl -X POST http://localhost:5000/api/invoices \
  -H "Content-Type: application/json" \
  -H "X-Role: Reception" \
  -d '{
    "caseId": 1,
    "subtotal": 350.00,
    "taxAmount": 49.00,
    "dueDate": "2026-05-01T00:00:00Z",
    "notes": "Net 30"
  }'
```

### Record a payment
```bash
curl -X POST http://localhost:5000/api/payments \
  -H "Content-Type: application/json" \
  -H "X-Role: Reception" \
  -d '{
    "invoiceId": 1,
    "amount": 200.00,
    "method": "Card",
    "referenceNumber": "TXN-9991",
    "receivedBy": "Reception Desk"
  }'
```

### Schedule a delivery
```bash
curl -X POST http://localhost:5000/api/deliveries \
  -H "Content-Type: application/json" \
  -H "X-Role: Reception" \
  -d '{
    "caseId": 1,
    "scheduledDate": "2026-04-16T09:00:00Z",
    "recipientName": "Dr. Khaled",
    "deliveryAddress": "Bright Smile Dental, Alexandria",
    "courier": "Lab Driver",
    "trackingNumber": "DEL-0001"
  }'
```

### Mark delivery as delivered (auto-completes the case)
```bash
curl -X PUT http://localhost:5000/api/deliveries/1/status \
  -H "Content-Type: application/json" \
  -H "X-Role: Reception" \
  -d '{ "status": "Delivered", "notes": "Signed by Dr. Khaled" }'
```

---

## Endpoint Summary

| Method | Route | Roles |
|---|---|---|
| GET / POST / PUT / DELETE | `/api/clinics` | Admin, Reception (read: all roles) |
| GET / POST / PUT / DELETE | `/api/patients` | Admin, Reception (read: all roles) |
| GET / POST / PUT / DELETE | `/api/cases` | All roles (delete: Admin only) |
| POST | `/api/cases/{id}/move-stage` | Admin, Technician |
| GET | `/api/cases/{id}/history` | All roles |
| GET | `/api/cases/{id}/total-duration` | All roles |
| GET / POST / PUT / DELETE | `/api/workflow-stages` | Admin (read: all) |
| GET / POST / PUT / DELETE | `/api/technicians` | Admin (read: all) |
| GET / POST | `/api/invoices` | Admin, Reception |
| POST | `/api/payments` | Admin, Reception |
| GET / POST | `/api/deliveries` | Admin, Reception (read: all) |
| PUT | `/api/deliveries/{id}/status` | Admin, Reception |
| GET | `/api/health` | Public |

---

## WinForms Integration Tip

```csharp
using var http = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
http.DefaultRequestHeaders.Add("X-Role", "Reception");
http.DefaultRequestHeaders.Add("X-User", currentUser.Name);

var response = await http.GetFromJsonAsync<PagedResult<CaseDto>>("/api/cases?page=1&pageSize=20");
```

---

## Notes / Future Hardening

- Replace the `X-Role` header with **JWT bearer auth** when adding identity (this is the only file that needs changing: `RoleAuthorizationFilter`).
- Switch from `EnsureCreated()` to **EF Core migrations** when the schema starts evolving in production:
  ```bash
  dotnet ef migrations add InitialCreate -p src/DentalLab.Infrastructure -s src/DentalLab.API
  dotnet ef database update -p src/DentalLab.Infrastructure -s src/DentalLab.API
  ```
- For SQL Server LocalDB, change the connection string in `appsettings.json` and replace `UseSqlite` with `UseSqlServer` in `Infrastructure/DependencyInjection.cs` (and add the `Microsoft.EntityFrameworkCore.SqlServer` package).
- Tighten CORS for production deployment.
