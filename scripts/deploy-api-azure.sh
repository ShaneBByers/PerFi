#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_SCRIPT="${ROOT}/scripts/publish-api-azure.sh"
ZIP_PATH="${ROOT}/artifacts/perfi-api-azure.zip"
RESOURCE_GROUP="per-fi-api-rg"
APP_NAME="${AZURE_APP_NAME:-per-fi-api}"
SUBSCRIPTION="${AZURE_SUBSCRIPTION:-PerFiSubscription}"
JWT_KEYVAULT_NAME="${PERFI_JWT_KEYVAULT_NAME:-per-fi-key-vault}"
JWT_KEYVAULT_URI="${PERFI_JWT_KEYVAULT_URI:-https://${JWT_KEYVAULT_NAME}.vault.azure.net/}"
JWT_KEY_NAME="${PERFI_JWT_KEY_NAME:-per-fi-jwt-signing-key}"

usage() {
  cat <<'EOF'
Usage:
  ./scripts/deploy-api-azure.sh

Defaults:
  Resource group: per-fi-api-rg
  App Service name: per-fi-api
  Subscription: PerFiSubscription

Optional environment variables:
  PERFI_JWT_KEYVAULT_NAME  Key Vault name used to build the default KeyVaultUri (default: per-fi-kv).
  PERFI_JWT_KEYVAULT_URI   Override the full Key Vault URI used for JWT signing (default: derived from PERFI_JWT_KEYVAULT_NAME).
  PERFI_JWT_KEY_NAME       Key Vault key name used to sign JWTs (default: perfi-jwt-signing-key).
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

echo "Applying App Service settings for API..."
az webapp config appsettings set \
  --resource-group "$RESOURCE_GROUP" \
  --name "$APP_NAME" \
  --settings \
    "Jwt__KeyVaultUri=${JWT_KEYVAULT_URI}" \
    "Jwt__KeyName=${JWT_KEY_NAME}" \
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
echo "API URL: https://${APP_NAME}.azurewebsites.net"
echo "Login route: https://${APP_NAME}.azurewebsites.net/api/auth/login"
echo "JWT Key Vault URI: ${JWT_KEYVAULT_URI}"
echo "JWT Key name: ${JWT_KEY_NAME}"
