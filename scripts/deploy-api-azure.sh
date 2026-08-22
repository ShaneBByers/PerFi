#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_SCRIPT="${ROOT}/scripts/publish-api-azure.sh"
ZIP_PATH="${ROOT}/artifacts/perfi-api-azure.zip"
RESOURCE_GROUP="per-fi-api-rg"
APP_NAME="${AZURE_APP_NAME:-per-fi-api}"
SUBSCRIPTION="${AZURE_SUBSCRIPTION:-PerFiSubscription}"

usage() {
  cat <<'EOF'
Usage:
  ./scripts/deploy-api-azure.sh

Defaults:
  Resource group: per-fi-api-rg
  App Service name: per-fi-api
  Subscription: PerFiSubscription
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

APP_NAME="${AZURE_APP_NAME:-per-fi-api}"
SUBSCRIPTION="${AZURE_SUBSCRIPTION:-PerFiSubscription}"

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
echo "API URL: https://${APP_NAME}.azurewebsites.net"
echo "Login route: https://${APP_NAME}.azurewebsites.net/api/auth/login"
