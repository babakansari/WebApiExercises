#!/usr/bin/env bash
# Run a generated API with hot reload.
# Usage: ./run.sh <Name> [extra dotnet watch run args]
#   e.g. ./run.sh GroundEvent  -> APIs/GroundEvent.Api/src/GroundEvent.Api.csproj
#        ./run.sh Hello        -> APIs/Hello.Api/src/Hello.Api.csproj

set -e

script_dir="$(cd "$(dirname "$0")" && pwd)"

if [[ $# -lt 1 || "$1" == "-h" || "$1" == "--help" ]]; then
  echo "Usage: $0 <Name> [extra dotnet watch run args]" >&2
  echo "Available APIs:" >&2
  for dir in "$script_dir"/APIs/*.Api; do
    [[ -d "$dir" ]] && echo "  $(basename "$dir" .Api)" >&2
  done
  exit 1
fi

name="${1%.Api}"
shift

package="$name.Api"
project="$script_dir/APIs/$package/src/$package.csproj"

if [[ ! -f "$project" ]]; then
  echo "Error: project not found: $project" >&2
  exit 1
fi

exec dotnet watch run --project "$project" --launch-profile OpenAPI "$@"
