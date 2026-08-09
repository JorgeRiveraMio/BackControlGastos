#!/bin/sh
set -eu
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-10000}"
exec dotnet ControlGastos.Api.dll
