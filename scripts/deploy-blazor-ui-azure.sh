#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_SCRIPT="${ROOT}/scripts/publish-blazor-ui-azure.sh"
PUBLISH_DIR="${ROOT}/artifacts/perfi-blazor-ui-publish"
RESOURCE_GROUP="per-fi-blazor-rg"
APP_NAME="${AZURE_APP_NAME:-per-fi-blazor-ui}"
SUBSCRIPTION="${AZURE_SUBSCRIPTION:-PerFiSubscription}"
BFF_BASE_URL="${PERFI_BFF_BASE_URL:-https://auth.per-fi.net}"
SWA_DEFAULT_HOSTNAME=""

usage() {
  cat <<'EOF'
Usage:
  ./scripts/deploy-blazor-ui-azure.sh

Defaults:
  Resource group: per-fi-blazor-rg
  Static Web App name: per-fi-blazor-ui
  Subscription: PerFiSubscription
  BFF base URL: https://auth.per-fi.net (Front Door custom domain)

Optional environment variables:
  PERFI_BFF_BASE_URL   Override API base URL written to UI appsettings.json (default: https://auth.per-fi.net).
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

if [[ ! -d "$PUBLISH_DIR" ]]; then
  echo "Publish artifact not found at $PUBLISH_DIR. Building it first..."
  "$PUBLISH_SCRIPT"
fi

if [[ -n "$SUBSCRIPTION" ]]; then
  echo "Setting Azure subscription: $SUBSCRIPTION"
  az account set --subscription "$SUBSCRIPTION"
fi

SWA_DEFAULT_HOSTNAME="$(az staticwebapp show \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "defaultHostname" \
  -o tsv 2>/dev/null || true)"

echo "Fetching deployment token for Static Web App '${APP_NAME}'..."
DEPLOYMENT_TOKEN="$(az staticwebapp secrets list \
  --name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --query "properties.apiKey" \
  -o tsv)"

if [[ -z "$DEPLOYMENT_TOKEN" ]]; then
  echo "Could not retrieve deployment token for Static Web App '${APP_NAME}' in resource group '${RESOURCE_GROUP}'." >&2
  exit 1
fi

APP_ROOT="${PUBLISH_DIR}/wwwroot"
APPSETTINGS_PATH="${APP_ROOT}/appsettings.json"

if [[ ! -d "$APP_ROOT" ]]; then
  echo "UI publish root not found at $APP_ROOT" >&2
  exit 1
fi

echo "Writing UI API base URL (${BFF_BASE_URL}) to ${APPSETTINGS_PATH}..."
cat > "$APPSETTINGS_PATH" <<EOF
{
  "Api": {
    "BaseUrl": "${BFF_BASE_URL}"
  }
}
EOF

# Prevent stale precompressed variants from overriding the updated appsettings.json.
rm -f "${APPSETTINGS_PATH}.br" "${APPSETTINGS_PATH}.gz"

echo "Deploying ${APP_ROOT} to Static Web App '${APP_NAME}'..."
npx -y @azure/static-web-apps-cli deploy "${APP_ROOT}" \
  --deployment-token "$DEPLOYMENT_TOKEN" \
  --app-name "$APP_NAME" \
  --resource-group "$RESOURCE_GROUP" \
  --env production

echo "Deployment complete."
if [[ -n "$SWA_DEFAULT_HOSTNAME" ]]; then
  echo "URL: https://${SWA_DEFAULT_HOSTNAME}"
else
  echo "URL: https://${APP_NAME}.azurestaticapps.net"
fi
echo "UI Api:BaseUrl: ${BFF_BASE_URL}"
