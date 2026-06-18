# Deployment Guide

## Environment

- Runtime: ASP.NET Core `net10.0`
- Database: Supabase PostgreSQL
- Frontend: static files served from `wwwroot`

## Required Settings

Set these as environment variables on the host:

```powershell
$env:SUPABASE_POSTGRES_CONNECTION_STRING="Host=...;Database=postgres;Username=...;Password=...;Port=6543;SSL Mode=Require;Trust Server Certificate=true"
$env:QR_LIBRARY_JWT_SECRET="use-a-long-random-secret"
```

Optional JWT settings can be supplied in configuration:

- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:ExpiryMinutes`

## Database Deployment

1. Open Supabase SQL Editor.
2. Run `database/schema.sql`.
3. Seed departments, books, book copies, and initial students.
4. Promote staff users by setting `students.role` to `Admin` or `Librarian`.

## Application Deployment

1. Build:

```powershell
dotnet build .\dbms\library.csproj
```

2. Publish:

```powershell
dotnet publish .\dbms\library.csproj -c Release
```

3. Run behind HTTPS in production.
4. Configure the hosting platform to inject the database connection string and JWT secret.

## Smoke Test

- Register a student.
- Login and confirm a JWT token is returned.
- Issue `book-001`.
- Return `book-001`.
- Confirm history, fines, notifications, recommendations, and admin dashboard load.
