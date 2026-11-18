#!/usr/bin/env bash
# Starts everything for the exercise. Safe to run again at any time.
cd "$(dirname "$0")" || exit 1
mkdir -p logs telemetry-output

export HOST_UID HOST_GID
HOST_UID="$(id -u)"
HOST_GID="$(id -g)"
if [ -n "$CODESPACE_NAME" ]; then
  export GRAFANA_ROOT_URL="https://${CODESPACE_NAME}-3000.${GITHUB_CODESPACES_PORT_FORWARDING_DOMAIN}/"
fi

echo "Starting Grafana and the telemetry collector (the first time can take a few minutes)..."
if ! docker compose up -d; then
  echo "❌ Couldn't start Grafana. Wait a minute and run 'bash start.sh' again."
  exit 1
fi

bash stop.sh --app-only >/dev/null 2>&1

# The practice AI model is a small Python program. It stands in for an
# outside AI provider, so it isn't part of the .NET code you work on.
start_python() {
  local name=$1 port=$2 dir=$3 module=$4
  nohup python -m uvicorn "$module:app" --app-dir "$dir" --port "$port" \
    --reload --reload-dir "$dir" >"logs/$name.log" 2>&1 &
  echo $! >"logs/$name.pid"
}

# The .NET services restart automatically whenever you save a change.
start_dotnet() {
  local name=$1 port=$2 project=$3
  ASPNETCORE_URLS="http://localhost:$port" DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true \
    DOTNET_WATCH_SUPPRESS_EMOJIS=1 \
    nohup dotnet watch run --project "$project" --no-hot-reload --non-interactive \
    >"logs/$name.log" 2>&1 &
  echo $! >"logs/$name.pid"
}

echo "Starting the practice app (the .NET services take about a minute to build)..."
start_python fake-llm 8003 services/fake_llm fake_llm_app
start_dotnet recommendations 8002 services/Recommendations
start_dotnet orders 8001 services/Orders

all_ok=true
for entry in "orders:8001" "recommendations:8002" "fake-llm:8003" "Grafana:3000"; do
  name=${entry%%:*}
  port=${entry##*:}
  path="/health"
  [ "$name" = "Grafana" ] && path="/api/health"
  ok=false
  for _ in $(seq 1 240); do
    if curl -sf "http://localhost:$port$path" >/dev/null 2>&1; then ok=true; break; fi
    sleep 1
  done
  if $ok; then
    echo "✅ $name is running"
  else
    echo "❌ $name didn't start. Look in logs/$name.log for the reason."
    all_ok=false
  fi
done

echo
if $all_ok; then
  echo "Everything is running. Open the PORTS tab and click the globe next to 'Grafana' to see traces."
  echo "When you save a change, the service rebuilds and restarts by itself (about 10-20 seconds)."
else
  echo "Something didn't start. Try 'bash start.sh' once more."
fi
