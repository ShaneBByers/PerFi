#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_SCRIPT="${ROOT}/scripts/publish-blazor-bff-azure.sh"
ZIP_PATH="${ROOT}/artifacts/perfi-blazor-bff-azure.zip"
RESOURCE_GROUP="per-fi-blazor-rg"
APP_NAME="${AZURE_APP_NAME:-per-fi-blazor-bff}"
UI_APP_NAME="${PERFI_UI_APP_NAME:-per-fi-blazor-ui}"
SUBSCRIPTION="${AZURE_SUBSCRIPTION:-PerFiSubscription}"
API_APP_NAME="${PERFI_API_APP_NAME:-per-fi-api}"
API_RESOURCE_GROUP="${PERFI_API_RESOURCE_GROUP:-per-fi-api-rg}"
API_BASE_URL="${PERFI_API_BASE_URL:-https://api.per-fi.net}"
UI_ORIGIN="${PERFI_UI_ORIGIN:-https://www.per-fi.net}"
UI_FALLBACK_ORIGIN="${PERFI_UI_FALLBACK_ORIGIN:-}"

usage() {
  cat <<'EOF'
Usage:
  ./scripts/deploy-blazor-bff-azure.sh

Defaults:
  Resource group: per-fi-blazor-rg
  App Service name: per-fi-blazor-bff
  Subscription: PerFiSubscription
  API base URL: https://api.per-fi.net (Front Door custom domain)
  UI origin: https://www.per-fi.net (Front Door custom domain)

Optional environment variables:
  PERFI_API_APP_NAME   API App Service name used for hostname discovery.
  PERFI_API_RESOURCE_GROUP  API resource group used for hostname discovery.
  PERFI_API_BASE_URL   Override upstream API URL used by BFF (default: https://api.per-fi.net).
  PERFI_UI_APP_NAME    Static Web App name used to resolve the fallback default hostname.
  PERFI_UI_ORIGIN      Primary frontend origin allowed by BFF CORS (default: https://www.per-fi.net).
  PERFI_UI_FALLBACK_ORIGIN  Extra frontend origin allowed by BFF CORS; auto-discovered from the Static Web App's default *.azurestaticapps.net hostname if unset.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ ! -f "$PUBLISH_SCRIPT" ]]; then
  echo "Missing publish script: $PUBLISH_SCRIPT" >&2
  exit 1
fi

if [[ ! -f "$ZIP_PATH" ]]; then
  echo "Publish artifact not found at $ZIP_PATH. Building it first..."
  "$PUBLISH_SCRIPT"
fi

if [[ -n "$SUBSCRIPTION" ]]; then
  echo "Setting Azure subscription: $SUBSCRIPTION"
  az account set --subscription "$SUBSCRIPTION"
fi

if [[ -z "$UI_FALLBACK_ORIGIN" ]]; then
  UI_DEFAULT_HOSTNAME="$(az staticwebapp show \
    --name "$UI_APP_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --query "defaultHostname" \
    -o tsv 2>/dev/null || true)"

  if [[ -n "$UI_DEFAULT_HOSTNAME" ]]; then
    UI_FALLBACK_ORIGIN="https://${UI_DEFAULT_HOSTNAME}"
  fi
fi

echo "Applying App Service settings for BFF..."
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --settings \
    "PerFiApi__BaseUrl=${API_BASE_URL}" \
    "Cors__AllowedOrigins__0=${UI_ORIGIN}" \
    "Cors__AllowedOrigins__1=${UI_FALLBACK_ORIGIN}" \
    "ASPNETCORE_ENVIRONMENT=Production" \
    "ASPNETCORE_URLS=http://0.0.0.0:8080" \
    "WEBSITES_PORT=8080" \
  >/dev/null

echo "Deploying ${ZIP_PATH} to App Service '${APP_NAME}' in resource group '${RESOURCE_GROUP}'..."
az webapp deploy \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --src-path "$ZIP_PATH" \
  --type zip

echo "Restarting the App Service..."
az webapp restart \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME"

echo "Deployment complete."
echo "URL: https://${APP_NAME}.azurewebsites.net"
echo "BFF PerFiApi__BaseUrl: ${API_BASE_URL}"
echo "BFF allowed origin[0]: ${UI_ORIGIN}"
if [[ -n "$UI_FALLBACK_ORIGIN" ]]; then
  echo "BFF allowed origin[1] (fallback): ${UI_FALLBACK_ORIGIN}"
fi
