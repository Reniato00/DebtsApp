# Debt Manager

Blazor Server monolith for personal debt tracking with multi-user support.

## Stack

- **Frontend:** Blazor Server (Interactive Server)
- **Backend:** .NET 8, Dapper, SQL Server
- **Auth:** Custom `AuthenticationStateProvider` with BCrypt password hashing

## Project Structure

```
├── application/          Blazor Server UI (pages, components, auth)
├── domain/               Domain entities (Debt, Payment, User)
├── persistence/          Dapper repositories, queries, SQL scripts
├── services/             Business logic services + models
├── AGENTS.md             Architecture diagrams (Mermaid)
└── API.md                Legacy API reference
```

## Quick Start

1. Open `application/application.sln` in Visual Studio or VS Code
2. Ensure SQL Server Express is running (`localhost\SQLEXPRESS`)
3. Run `persistence/Sql/DatabaseConfig.sql` to create/update the database
4. Set `application` as startup project and run

## Key Features

- Debt CRUD with pagination and search
- Payment tracking with balance auto-update
- Dashboard with stats, upcoming payments, interest cost
- Payment calculator (snowball vs avalanche strategies)
- Registration with rate limiting (1/hour per email)
- Login rate limiting (5 attempts → 15min lockout)
- Session timeout (20 min inactivity)
- CSP headers on all responses

## Configuration

`application/appsettings.json`:
- `ConnectionStrings:DefaultConnection` — SQL Server connection
- `Cache:TtlMinutes` — Cache TTL (currently uses `Jwt:ExpirationMinutes`)
