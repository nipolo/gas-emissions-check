#!/usr/bin/env bash
set -euo pipefail

REPO_URL="git@github.com:nipolo/gas-emissions-check.git"
TARGET_DIR="$HOME/gas-emissions-check"
PROJECT_PATH="$TARGET_DIR/src/SensorService/GEC.SensorService.csproj"
SERVICE_NAME="gasemissionscheck-sensor.service"
SERVICE_SRC="$TARGET_DIR/deployment/$SERVICE_NAME"
SERVICE_DST="/etc/systemd/system/$SERVICE_NAME"
PUBLISH_DIR="$TARGET_DIR/artifacts/publish/sensorservice"

echo "==> 1) Remove existing repo folder (if any)"
if [[ -d "$TARGET_DIR" ]]; then
  rm -rf "$TARGET_DIR"
fi

echo "==> 2) Clone repo via SSH into $TARGET_DIR"
git clone "$REPO_URL" "$TARGET_DIR"

echo "==> 3) Build + publish self-contained for Raspberry Pi OS 64-bit (linux-arm64)"
# Ensure dotnet exists
command -v dotnet >/dev/null 2>&1 || { echo "dotnet not found in PATH"; exit 1; }

mkdir -p "$PUBLISH_DIR"

dotnet restore "$PROJECT_PATH"

dotnet publish "$PROJECT_PATH" \
  -c Release \
  -r linux-arm64 \
  --self-contained true \
  -o "$PUBLISH_DIR" \
  /p:PublishSingleFile=true \
  /p:PublishTrimmed=false \
  /p:IncludeNativeLibrariesForSelfExtract=true

echo "==> Published to: $PUBLISH_DIR"

echo "==> 4) Remove existing systemd service (stop/disable + delete unit file if present)"
if systemctl list-unit-files | grep -q "^$SERVICE_NAME"; then
  # Stop if running (ignore failures)
  sudo systemctl stop "$SERVICE_NAME" || true
  sudo systemctl disable "$SERVICE_NAME" || true
fi

# Remove unit file if it exists in /etc/systemd/system
if [[ -f "$SERVICE_DST" ]]; then
  sudo rm -f "$SERVICE_DST"
fi

echo "==> 5) Install new service unit from repo"
if [[ ! -f "$SERVICE_SRC" ]]; then
  echo "ERROR: Service file not found at: $SERVICE_SRC"
  exit 1
fi

sudo cp "$SERVICE_SRC" "$SERVICE_DST"
sudo chmod 644 "$SERVICE_DST"

echo "==> 6) Reload systemd, enable service, and start it"
sudo systemctl daemon-reload
sudo systemctl enable "$SERVICE_NAME"
sudo systemctl restart "$SERVICE_NAME"

echo "==> 7) Show status"
systemctl status "$SERVICE_NAME" --no-pager -l

echo "==> Done."
