#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

RAW_DIR="./artifacts/coverage-raw"
REPORT_DIR="./artifacts/coverage-report"

rm -rf "$RAW_DIR" "$REPORT_DIR"

dotnet test PerFi.slnx --collect:"XPlat Code Coverage" --results-directory "$RAW_DIR"

dotnet tool restore

dotnet reportgenerator \
  -reports:"$RAW_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:Html\;TextSummary

cat "$REPORT_DIR/Summary.txt"
