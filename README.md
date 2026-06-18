# QR-Based Digital Library System

ASP.NET Core Web API plus a student/staff-facing frontend for a QR-based digital library workflow. The project now covers Assignment 1 issuing, Assignment 2 return/fine/dashboard automation, and Assignment 3 JWT/RBAC, analytics, activity logging, multi-branch readiness, and rule-based recommendations.

## What The App Includes

- Student registration and login with BCrypt password hashing.
- JWT session tokens with `Student`, `Librarian`, and `Admin` roles.
- QR/manual book issuing and returning.
- Automatic overdue detection and fine calculation.
- Student borrowed-books, history, fines, notifications, and recommendations.
- Staff/admin dashboard with circulation metrics and analytics.
- Activity logs for login, issue, return, fine-related events, and failed attempts.
- Supabase PostgreSQL schema, indexes, views, and stored functions.
- Rate limiting for authentication and transaction endpoints.

## Project Structure

- `wwwroot/`: final product frontend.
- `Controllers/`: auth, transaction, student, department, and admin dashboard endpoints.
- `Services/`: PostgreSQL access, JWT creation/validation, dashboard analytics, and QR transaction services.
- `Contracts/`: request and response DTOs.
- `database/schema.sql`: complete PostgreSQL/Supabase schema and stored routines.
- `docs/Assignment2_3_Report.md`: report-ready Assignment 2 and 3 write-up.
- `docs/DeploymentGuide.md`: configuration and deployment notes.
- `docs/SecurityTestingReport.md`: security testing checklist and results template.

## Database Setup

1. Open the Supabase SQL Editor.
2. Run the full script in `database/schema.sql`.
3. Insert departments, students, books, and book copies.
4. Use standard book copy identifiers like `book-001`, `book-002`, etc.
5. To create staff access, update a registered user role:

```sql
update students set role = 'Admin' where university_id = '2024-CS-229';
update students set role = 'Librarian' where university_id = '2024-CS-190';
```

## Configuration

Use environment variables so secrets are not committed:

```powershell
$env:SUPABASE_POSTGRES_CONNECTION_STRING="Host=...;Database=postgres;Username=...;Password=...;Port=6543;SSL Mode=Require;Trust Server Certificate=true"
$env:QR_LIBRARY_JWT_SECRET="replace-with-a-long-random-secret-for-deployment"
```

Optional `appsettings.Development.json` keys:

```json
{
  "Jwt": {
    "Issuer": "QrDigitalLibrary",
    "Audience": "QrDigitalLibrary.Users",
    "ExpiryMinutes": "120"
  }
}
```

## Run

```powershell
dotnet restore
dotnet run --project .\dbms\library.csproj --launch-profile http
```

Open:

```text
http://localhost:5178/
```

## Core Endpoints

### Authentication

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/departments`

### Student and Transactions

- `POST /api/transactions/issue`
- `POST /api/transactions/return`
- `GET /api/students/me/borrowed-books`
- `GET /api/students/me/transactions`
- `GET /api/students/me/fines`
- `GET /api/students/me/notifications`
- `GET /api/students/me/recommendations`

### Admin and Analytics

- `GET /api/admin/dashboard/summary`
- `GET /api/admin/dashboard/analytics`

Admin endpoints require a JWT role of `Admin` or `Librarian`.

## Verification

```powershell
dotnet build .\dbms\library.csproj
dotnet build .\QrDigitalLibrary.slnx
```
