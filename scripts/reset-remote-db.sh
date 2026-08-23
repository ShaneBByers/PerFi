#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENVIRONMENT="${PERFI_CONSOLE_ENVIRONMENT:-Development}"

usage() {
  cat <<'EOF'
Usage:
  ./scripts/reset-remote-db.sh [--yes]

Permanently deletes ALL financial data (institutions, accounts, account types/groups,
snapshots, balances) from the database configured for the "Development" environment
(per-fi-db-server.database.windows.net). User accounts are not affected. Prompts for
confirmation unless --yes is passed.

Optional environment variables:
  PERFI_CONSOLE_ENVIRONMENT  Environment used to resolve the connection string
                             (default: Development).
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
  usage
  exit 0
fi

DOTNET_ENVIRONMENT="${ENVIRONMENT}" ASPNETCORE_ENVIRONMENT="${ENVIRONMENT}" \
  dotnet run --project "${ROOT}/PerFi.Console/PerFi.Console.csproj" -- reset-database "$@"
