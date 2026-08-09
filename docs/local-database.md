# Local Database Setup (macOS)

This project uses SQL Server in Docker for local development.

## Prerequisites

1. Install Docker Desktop (or OrbStack).
2. Verify Docker is running.

## Configuration Check

Your SQL Server container password and API connection string password must match.

Current values in this repo:
1. Docker password in `docker-compose.yml`: `PerFiDBPassword!`
2. API password in `PerFi.API/appsettings.Development.json`: `YourStrong!Passw0rd`

Before running the API, update one of them so they are identical.

## Start SQL Server

From the repo root:

```bash
docker compose up -d
```

Check status:

```bash
docker compose ps
```

View logs:

```bash
docker compose logs -f sqlserver
```

## Run the API and Migrations

In Development, migrations are automatically applied on startup in `PerFi.API/Program.cs` via `dbContext.Database.Migrate()`.

Start the API:

```bash
dotnet run --project PerFi.API/PerFi.API.csproj
```

Optional manual migration apply:

```bash
dotnet dotnet-ef database update --project PerFi.Infrastructure/PerFi.Infrastructure.csproj --startup-project PerFi.API/PerFi.API.csproj
```

## Create Additional Migrations

After model changes:

```bash
dotnet dotnet-ef migrations add <MigrationName> --project PerFi.Infrastructure/PerFi.Infrastructure.csproj --startup-project PerFi.API/PerFi.API.csproj --output-dir Migrations
```

## Reset Local Database (Optional)

This removes local SQL data volume and recreates the database.

```bash
docker compose down -v
docker compose up -d
```

## Recommended Database UI

Recommended app: DBeaver Community Edition.

Connection settings:
1. Host: `localhost`
2. Port: `1433`
3. Database: `PerFi`
4. User: `sa`
5. Password: same value used in Docker and appsettings
6. Encrypt: off for local only, or trust server certificate
