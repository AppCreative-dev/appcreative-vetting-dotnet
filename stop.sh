#!/usr/bin/env bash
# Stops the practice app. Add --app-only to leave Grafana running.
cd "$(dirname "$0")" || exit 1

for name in orders recommendations fake-llm; do
  if [ -f "logs/$name.pid" ]; then
    kill "$(cat "logs/$name.pid")" 2>/dev/null
    rm -f "logs/$name.pid"
  fi
done
pkill -f "dotnet watch run --project services/" 2>/dev/null
pkill -f "dotnet-watch" 2>/dev/null
pkill -f "bin/Debug/net10.0/(Orders|Recommendations)" 2>/dev/null
pkill -f "uvicorn fake_llm_app:app" 2>/dev/null
pkill -f "multiprocessing.spawn" 2>/dev/null
sleep 1

if [ "$1" != "--app-only" ]; then
  docker compose down
fi
echo "Stopped."
