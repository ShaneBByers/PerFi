#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENVIRONMENT="${PERFI_MIGRATE_ENVIRONMENT:-Development}"

usage() {
  cat <<'EOF'
Usage:
  ./scripts/migrate-remote-db.sh

Applies pending EF Core migrations to the Azure SQL database configured for the
"Development" environment (per-fi-db-server.database.windows.net), using Active
Directory Default authentication - requires `az login` with DB access first.

Optional environment variables:
  PERFI_MIGRATE_ENVIRONMENT  ASPNETCORE_ENVIRONMENT used to resolve the connection
                             string (default: Development).
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

echo "Applying EF Core migrations (ASPNETCORE_ENVIRONMENT=${ENVIRONMENT})..."
ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}" \
  dotnet dotnet-ef database update \
    --project "${ROOT}/PerFi.Infrastructure/PerFi.Infrastructure.csproj" \
    --startup-project "${ROOT}/PerFi.API/PerFi.API.csproj"

echo "Migration complete."
