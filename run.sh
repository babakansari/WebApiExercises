#!/usr/bin/env bash

set -e

script_dir="$(cd "$(dirname "$0")" && pwd)"

exec dotnet watch run --project "$script_dir/APIs/GroundEvent.Api/src/GroundEvent.Api.csproj" --launch-profile OpenAPI "$@"