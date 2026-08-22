#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_PATH="${ROOT}/PerFi.Blazor/PerFi.Blazor.csproj"
PUBLISH_DIR="${ROOT}/artifacts/perfi-blazor-ui-publish"
ZIP_PATH="${ROOT}/artifacts/perfi-blazor-ui-azure.zip"

mkdir -p "${ROOT}/artifacts"
rm -rf "${PUBLISH_DIR}" "${ZIP_PATH}"

dotnet publish "${PROJECT_PATH}" -c Release -o "${PUBLISH_DIR}" --nologo

(
  cd "${PUBLISH_DIR}"
  zip -r "${ZIP_PATH}" .
)

echo "Published Blazor UI to: ${PUBLISH_DIR}"
echo "Azure deploy zip: ${ZIP_PATH}"
ls -lh "${ZIP_PATH}"
