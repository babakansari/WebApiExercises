#!/usr/bin/env bash
# Generate an ASP.NET Core Web API from an OAS file using openapi-generator-cli.
# Usage: ./generate-webapi.sh <oas-file> [packageName] [outputDir]
#   e.g. ./generate-webapi.sh GroundEventOAS.yaml
#        -> package GroundEvent.Api, output APIs/GroundEvent.Api
set -euo pipefail

if [[ $# -lt 1 || "$1" == "-h" || "$1" == "--help" ]]; then
  echo "Usage: $0 <oas-file> [packageName] [outputDir]" >&2
  exit 1
fi

OAS_FILE="$1"
if [[ ! -f "$OAS_FILE" ]]; then
  echo "Error: OAS file not found: $OAS_FILE" >&2
  exit 1
fi

if ! command -v openapi-generator-cli >/dev/null 2>&1; then
  echo "Error: openapi-generator-cli not found on PATH (npm i -g @openapitools/openapi-generator-cli)" >&2
  exit 1
fi

# Derive a PascalCase name from the file: GroundEventOAS.yaml / ground-event.oas.yaml -> GroundEvent
BASE_NAME="$(basename "$OAS_FILE")"
BASE_NAME="${BASE_NAME%.*}"
BASE_NAME="$(printf '%s' "$BASE_NAME" | sed -E 's/[._-]?(oas|OAS|openapi|OpenApi|OpenAPI|swagger|Swagger)$//')"
FEATURE="$(printf '%s' "$BASE_NAME" | awk -F'[-_. ]+' '{ for (i = 1; i <= NF; i++) printf "%s%s", toupper(substr($i,1,1)), substr($i,2) }')"

PACKAGE_NAME="${2:-${FEATURE}.Api}"
OUTPUT_DIR="${3:-APIs/${PACKAGE_NAME}}"

echo "OAS file : $OAS_FILE"
echo "Package  : $PACKAGE_NAME"
echo "Output   : $OUTPUT_DIR"

openapi-generator-cli validate -i "$OAS_FILE"

openapi-generator-cli generate \
  -i "$OAS_FILE" \
  -g aspnetcore \
  -o "$OUTPUT_DIR" \
  --additional-properties=packageName="$PACKAGE_NAME",aspnetCoreVersion=10.0,buildTarget=program,operationIsAsync=true,operationResultTask=true,classModifier=abstract,operationModifier=abstract,nullableReferenceTypes=true,useSwashbuckle=true

echo "Done. Project: $OUTPUT_DIR/src/$PACKAGE_NAME/$PACKAGE_NAME.csproj"
