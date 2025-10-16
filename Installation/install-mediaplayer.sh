#!/usr/bin/env bash
set -euo pipefail

# ═══════════════════════ Configuration ═══════════════════════
HTTP_PORT="8080"
HTTP_PASSWORD="mediaplayer"

# Media folder
TARGET_USER="${SUDO_USER:-${USER}}"
TARGET_UID="$(id -u "${TARGET_USER}")"
TARGET_HOME="$(getent passwd "${TARGET_USER}" | cut -d: -f6)"
TARGET_GROUP="$(id -gn "${TARGET_USER}")"   # NEU: echte Primärgruppe
MEDIA_DIR="${TARGET_HOME}/Videos/horrorhouse"

# VLC start command
VLC_CMD="vlc --intf dummy \
  --extraintf=http --http-host=0.0.0.0 --http-port=${HTTP_PORT} --http-password=${HTTP_PASSWORD} \
  --fullscreen --no-video-title-show --no-osd --sub-track=-1 --loop -- \
  ${MEDIA_DIR}/*.mp4"

# systemd User-Service
SERVICE_NAME="vlc-kiosk.service"
USER_SYSTEMD_DIR="${TARGET_HOME}/.config/systemd/user"
SERVICE_PATH="${USER_SYSTEMD_DIR}/${SERVICE_NAME}"


# ═══════════════════════ Helpers ═══════════════════════════════════
log()  { echo -e "\033[1;32m[+] $*\033[0m"; }
warn() { echo -e "\033[1;33m[!] $*\033[0m"; }
err()  { echo -e "\033[1;31m[✗] $*\033[0m" >&2; }

require_root() { [[ $EUID -eq 0 ]] || { err "Please run with sudo/root."; exit 1; }; }


# ═══════════════════════ 1) Media Folder Creation ═══════════════════
create_media_dir() {
  log "Create media folder: ${MEDIA_DIR}"
  mkdir -p "${MEDIA_DIR}"
  chown -R "${TARGET_USER}:${TARGET_GROUP}" "${MEDIA_DIR}"
}

# ═══════════════════════ 2) VLC Installation ══════════════════════
install_vlc() {
  log "Installiere VLC…"
  apt-get update -y
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends vlc
}

# ═══════════════ 3) systemd User-Service Creation & Activation ═════
create_systemd_user_service() {
  log "Create systemd user service under ${SERVICE_PATH}"
  mkdir -p "${USER_SYSTEMD_DIR}"
  cat > "${SERVICE_PATH}" <<EOF
[Unit]
Description=VLC Kiosk (Desktop User Service)
After=graphical-session.target
Wants=graphical-session.target

[Service]
Type=simple
# Desktop-Session Display
Environment=DISPLAY=:0
Environment=XAUTHORITY=${TARGET_HOME}/.Xauthority
# Bash für Globbing (*.mp4 etc.)
ExecStart=/bin/bash -lc '${VLC_CMD}'
Restart=on-failure
RestartSec=2

[Install]
WantedBy=default.target
EOF
  chown "${TARGET_USER}:${TARGET_GROUP}" "${SERVICE_PATH}"
}

enable_user_service() {
  log "Enable systemd user service…"
  # Set XDG_RUNTIME_DIR so that systemctl --user works under sudo
  export XDG_RUNTIME_DIR="/run/user/${TARGET_UID}"
  mkdir -p "${XDG_RUNTIME_DIR}"
  chown "${TARGET_USER}:${TARGET_GROUP}" "${SERVICE_PATH}" || true

  # Daemon reload + enable + start in the context of the target user
  sudo -u "${TARGET_USER}" XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR}" systemctl --user daemon-reload
  sudo -u "${TARGET_USER}" XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR}" systemctl --user enable --now "${SERVICE_NAME}"

  log "Autostart active. Service: ${SERVICE_NAME}"
  echo "  • Media folder:   ${MEDIA_DIR}"
  echo "  • Web-UI:         http://<PI-IP>:${HTTP_PORT}/  (User leer, Passwort: ${HTTP_PASSWORD})"
  echo "  • Stop service:   systemctl --user stop ${SERVICE_NAME}"
  echo "  • Logs:           journalctl --user -u ${SERVICE_NAME} -f"
}

# ═══════════════════════ main ══════════════════════════════════════
main() {
  require_root
  create_media_dir
  install_vlc
  create_systemd_user_service
  enable_user_service
}

main "$@"
