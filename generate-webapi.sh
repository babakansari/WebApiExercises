#!/usr/bin/env bash
# Generate an ASP.NET Core Web API from an OAS file using openapi-generator-cli.
# Usage: ./generate-webapi.sh <oas-file> [packageName] [outputDir]
#   e.g. ./generate-webapi.sh features/GroundEventOAS.yaml
#        -> package GroundEvent.Api, project in APIs/GroundEvent.Api/src
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

# Derive a PascalCase name from the file name, ignoring any directory:
# features/GroundEventOAS.yaml / ground-event.oas.yaml -> GroundEvent
BASE_NAME="$(basename "$OAS_FILE")"
BASE_NAME="${BASE_NAME%.*}"
BASE_NAME="$(printf '%s' "$BASE_NAME" | sed -E 's/[._-]?(oas|OAS|openapi|OpenApi|OpenAPI|swagger|Swagger)$//')"
FEATURE="$(printf '%s' "$BASE_NAME" | awk -F'[-_. ]+' '{ for (i = 1; i <= NF; i++) printf "%s%s", toupper(substr($i,1,1)), substr($i,2) }')"

# Package/namespace precedence: CLI argument > info.x-package-name in the OAS file > <Feature>.Api
OAS_PACKAGE_NAME="$(sed -nE 's/^[[:space:]]+x-package-name:[[:space:]]*["'"'"']?([A-Za-z0-9_.]+)["'"'"']?[[:space:]]*$/\1/p' "$OAS_FILE" | head -1)"
PACKAGE_NAME="${2:-${OAS_PACKAGE_NAME:-${FEATURE}.Api}}"
OUTPUT_DIR="${3:-APIs/${PACKAGE_NAME}}"

echo "OAS file : $OAS_FILE"
echo "Package  : $PACKAGE_NAME"
echo "Output   : $OUTPUT_DIR"

openapi-generator-cli validate -i "$OAS_FILE"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TEMPLATE_DIR="$SCRIPT_DIR/templates/aspnetcore"

# Controllers are generated as concrete classes with virtual methods (not abstract): ASP.NET Core
# does not register abstract controllers, so they would never show up in Swagger. The custom
# controller template makes each operation return the sample DTO from the OAS with its success
# status code, or throw NotImplementedException when the spec defines no response body. The sample
# is a real C# object: the custom model template gives every model a Sample property built from the
# OAS example values, so no JSON string is parsed at runtime.

# API versioning: the version segment of the OAS servers path (e.g. /api/v1 -> "api" + "1") drives
# Asp.Versioning. Controllers are routed on /<prefix>/v{version:apiVersion}/... and annotated with
# [ApiVersion]/[MapToApiVersion], so the version lives in the versioning framework rather than in a
# hard-coded path. A spec whose servers path carries no version segment falls back to that path as-is.
BASE_PATH="$(sed -nE 's#^[[:space:]]*-[[:space:]]*url:[[:space:]]*["'"'"']?[a-zA-Z]+://[^/]+(/[^"'"'"'[:space:]]*).*#\1#p' "$OAS_FILE" | head -1)"
BASE_PATH="${BASE_PATH%/}"
API_VERSION="$(printf '%s' "$BASE_PATH" | sed -nE 's#.*/v([0-9]+(\.[0-9]+)?)$#\1#p')"
if [[ -n "$API_VERSION" ]]; then
  ROUTE_PREFIX="${BASE_PATH%/v*}"
  [[ "$API_VERSION" == *.* ]] || API_VERSION="${API_VERSION}.0"
else
  ROUTE_PREFIX="$BASE_PATH"
  API_VERSION="1.0"
fi
# Swashbuckle document name must match the Asp.Versioning ApiExplorer group ('v'VVV format).
API_VERSION_GROUP="v${API_VERSION%.0}"
API_VERSION_MAJOR="${API_VERSION%%.*}"
API_VERSION_MINOR="${API_VERSION##*.}"

echo "Version  : $API_VERSION (routes ${ROUTE_PREFIX}/v{version:apiVersion}, OpenAPI doc $API_VERSION_GROUP)"

ADDITIONAL_PROPS="packageName=$PACKAGE_NAME,aspnetCoreVersion=10.0,buildTarget=program,operationResultTask=true,operationModifier=virtual,generateBody=true,nullableReferenceTypes=true,useSwashbuckle=true"
ADDITIONAL_PROPS="$ADDITIONAL_PROPS,apiVersionNumber=$API_VERSION,apiVersionGroup=$API_VERSION_GROUP,apiVersionMajor=$API_VERSION_MAJOR,apiVersionMinor=$API_VERSION_MINOR${ROUTE_PREFIX:+,apiRoutePrefix=$ROUTE_PREFIX}"

# A model named like a segment of the package namespace (e.g. model "GroundEvent" in package
# "GroundEvent.Api") is shadowed by that namespace inside the generated code (CS0118).
# Detect such models via a dry run and rename them to <Name>Model.
MODEL_FILES="$(openapi-generator-cli generate --dry-run -i "$OAS_FILE" -g aspnetcore -o "$OUTPUT_DIR" \
  --additional-properties="$ADDITIONAL_PROPS" 2>&1 \
  | sed -nE "s#.*/src/${PACKAGE_NAME//./\\.}/Models/([A-Za-z0-9_]+)\.cs\$#\1#p" | sort -u)"
if [[ -n "$MODEL_FILES" ]]; then
  ADDITIONAL_PROPS="$ADDITIONAL_PROPS,hasModels=true"
fi

MAPPING_ARGS=()
IFS='.' read -r -a NAMESPACE_SEGMENTS <<< "$PACKAGE_NAME"
for SEGMENT in "${NAMESPACE_SEGMENTS[@]}"; do
  if grep -qx "$SEGMENT" <<< "$MODEL_FILES"; then
    echo "Renaming model '$SEGMENT' -> '${SEGMENT}Model' (conflicts with namespace '$PACKAGE_NAME')"
    MAPPING_ARGS+=(--model-name-mappings "$SEGMENT=${SEGMENT}Model")
    rm -f "$OUTPUT_DIR/src/Models/$SEGMENT.cs"
  fi
done

openapi-generator-cli generate \
  -i "$OAS_FILE" \
  -g aspnetcore \
  -o "$OUTPUT_DIR" \
  -t "$TEMPLATE_DIR" \
  --additional-properties="$ADDITIONAL_PROPS" \
  ${MAPPING_ARGS[@]+"${MAPPING_ARGS[@]}"}

# The generator always nests the project in src/<packageName>/; flatten it to src/ and fix references.
NESTED_DIR="$OUTPUT_DIR/src/$PACKAGE_NAME"
cp -R "$NESTED_DIR/." "$OUTPUT_DIR/src/"
rm -rf "$NESTED_DIR"
for FILE in "$OUTPUT_DIR/$PACKAGE_NAME.sln" "$OUTPUT_DIR/build.sh" "$OUTPUT_DIR/build.bat" \
            "$OUTPUT_DIR/README.md" "$OUTPUT_DIR/src/Dockerfile" "$OUTPUT_DIR/.openapi-generator/FILES"; do
  [[ -f "$FILE" ]] || continue
  PKG="$PACKAGE_NAME" perl -pi -e 's#src([/\\])\Q$ENV{PKG}\E(?:[/\\]|\b)#src$1#g; s#src([/\\])(?=["\s]|$)#src#g' "$FILE"
done

echo "Done. Project: $OUTPUT_DIR/src/$PACKAGE_NAME.csproj"
