#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="${ROOT}/artifacts/perfi-api-publish"
ZIP_PATH="${ROOT}/artifacts/perfi-api-azure.zip"

mkdir -p "${ROOT}/artifacts"
rm -rf "${PUBLISH_DIR}" "${ZIP_PATH}"

dotnet publish "${ROOT}/PerFi.API/PerFi.API.csproj" -c Release -o "${PUBLISH_DIR}" --nologo

(
  cd "${PUBLISH_DIR}"
  zip -r "${ZIP_PATH}" .
)

echo "Published API to: ${PUBLISH_DIR}"
echo "Azure deploy zip: ${ZIP_PATH}"
ls -lh "${ZIP_PATH}"
