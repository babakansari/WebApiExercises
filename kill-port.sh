#!/usr/bin/env bash
# Kills the process(es) listening on the given TCP port.
# Usage: ./kill-port.sh <port> [-f]   (-f sends SIGKILL instead of SIGTERM)

set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Usage: $0 <port> [-f]" >&2
  exit 1
fi

port="$1"
signal="TERM"
[[ "${2:-}" == "-f" ]] && signal="KILL"

if ! [[ "$port" =~ ^[0-9]+$ ]] || (( port < 1 || port > 65535 )); then
  echo "Error: '$port' is not a valid port (1-65535)." >&2
  exit 1
fi

pids=$(lsof -ti "tcp:${port}" -sTCP:LISTEN 2>/dev/null || true)

if [[ -z "$pids" ]]; then
  echo "No process is listening on port ${port}."
  exit 0
fi

for pid in $pids; do
  name=$(ps -p "$pid" -o comm= 2>/dev/null || echo "unknown")
  echo "Killing PID ${pid} (${name}) on port ${port} with SIG${signal}"
  kill "-${signal}" "$pid"
done
