#!/usr/bin/env bash
set -euo pipefail

# ═══════════════════════ Configuration ═══════════════════════
VLC_HTTP_PORT="8080"
VLC_HTTP_PASSWORD="videoplayer"

# Media folder
TARGET_USER="${SUDO_USER:-${USER}}"
TARGET_UID="$(id -u "${TARGET_USER}")"
TARGET_HOME="$(getent passwd "${TARGET_USER}" | cut -d: -f6)"
TARGET_GROUP="$(id -gn "${TARGET_USER}")"
MEDIA_DIR="${TARGET_HOME}/Videos"

# Samba
SAMBA_SHARE_NAME="Videos"

# VLC start command
VLC_CMD="vlc --intf dummy \
  --extraintf=http --http-host=0.0.0.0 --http-port=${VLC_HTTP_PORT} --http-password=${VLC_HTTP_PASSWORD} \
  --fullscreen --no-video-title-show --no-osd --sub-track=-1 --loop -- \
  ${MEDIA_DIR}/*.mp4"

# systemd user service
SERVICE_NAME="vlc-kiosk.service"
USER_SYSTEMD_DIR="${TARGET_HOME}/.config/systemd/user"
SERVICE_PATH="${USER_SYSTEMD_DIR}/${SERVICE_NAME}"

# ═══════════════════════ Helpers ═════════════════════════════
log() { echo -e "\033[1;32m[+] $*\033[0m"; }
err() { echo -e "\033[1;31m[✗] $*\033[0m" >&2; }
require_root() { [[ $EUID -eq 0 ]] || { err "Please run with sudo/root."; exit 1; }; }

# ═══════════════════════ Media Folder ═══════════════════════
create_media_dir() {
  log "Ensuring media folder exists: ${MEDIA_DIR}"
  mkdir -p "${MEDIA_DIR}"
  chown -R "${TARGET_USER}:${TARGET_GROUP}" "${MEDIA_DIR}"
  chmod 2775 "${MEDIA_DIR}" || true
}

# ═════════════ Samba Share (Authenticated, Videos Only) ═════
install_and_configure_samba() {
  log "Installing and configuring Samba (user: ${TARGET_USER}) for ${MEDIA_DIR}"

  apt-get update -y
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends samba samba-common-bin

  local SMB_CONF="/etc/samba/smb.conf"

  # Backup once
  if [[ ! -f "${SMB_CONF}.bak" ]]; then
    cp -a "${SMB_CONF}" "${SMB_CONF}.bak" || true
    log "Backup created: ${SMB_CONF}.bak"
  fi

  # Minimal config: ONLY the Videos share
  log "Rewriting ${SMB_CONF} to expose only the '${SAMBA_SHARE_NAME}' share..."
  cat > "${SMB_CONF}" <<EOF
[global]
   workgroup = WORKGROUP
   server string = Raspberry Pi Media Server
   security = user
   server min protocol = SMB2
   map to guest = Bad User
   guest account = nobody
   smb encrypt = required
   disable spoolss = yes
   load printers = no
   printing = bsd
   printcap name = /dev/null
   show add printer wizard = no
   log file = /var/log/samba/%m.log
   max log size = 1000

[${SAMBA_SHARE_NAME}]
   path = ${MEDIA_DIR}
   browseable = yes
   read only = no
   writable = yes
   guest ok = no
   public = no
   valid users = ${TARGET_USER}
   force user = ${TARGET_USER}
   force group = ${TARGET_GROUP}
   create mask = 0664
   directory mask = 2775
   follow symlinks = yes
   wide links = yes
   unix extensions = no
EOF

  # Set Samba password for TARGET_USER to VLC_HTTP_PASSWORD
  log "Setting Samba password for '${TARGET_USER}' from VLC_HTTP_PASSWORD..."
  (echo "${VLC_HTTP_PASSWORD}"; echo "${VLC_HTTP_PASSWORD}") | smbpasswd -a -s "${TARGET_USER}" >/dev/null
  smbpasswd -e "${TARGET_USER}" >/dev/null

  systemctl enable --now smbd nmbd 2>/dev/null || systemctl enable --now smbd || true
  systemctl restart smbd || true

  local HOST_IP
  HOST_IP="$(hostname -I 2>/dev/null | awk '{print $1}')"
  log "Samba share ready:"
  echo "  • Path:         \\\\${HOST_IP}\\${SAMBA_SHARE_NAME}"
  echo "  • Directory:    ${MEDIA_DIR}"
  echo "  • Username:     ${TARGET_USER}"
  echo "  • Password:     ${VLC_HTTP_PASSWORD}"
  echo "  • Config file:  ${SMB_CONF} (Backup: ${SMB_CONF}.bak)"
}

# ═══════════════════════ VLC Installation ═══════════════════
install_vlc() {
  log "Installing VLC..."
  apt-get update -y
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends vlc
}

# ═════ systemd User Service Creation & Activation (robust) ════
create_systemd_user_service() {
  log "Creating systemd user service at ${SERVICE_PATH}"
  mkdir -p "${USER_SYSTEMD_DIR}"
  cat > "${SERVICE_PATH}" <<EOF
[Unit]
Description=VLC Kiosk (Desktop User Service)
After=graphical-session.target
Wants=graphical-session.target

[Service]
Type=simple
Environment=DISPLAY=:0
Environment=XAUTHORITY=${TARGET_HOME}/.Xauthority
ExecStart=/bin/bash -lc '${VLC_CMD}'
Restart=on-failure
RestartSec=2

[Install]
WantedBy=default.target
EOF
  chown -R "${TARGET_USER}:${TARGET_GROUP}" "${TARGET_HOME}/.config"
  chmod 644 "${SERVICE_PATH}"
}

enable_user_service() {
  log "Enabling systemd user service..."

  # Keep the user manager running even without interactive login
  loginctl enable-linger "${TARGET_USER}" || true

  # Start user@UID manager and operate in that context (no need for DBUS/XDG env)
  machinectl --quiet shell "${TARGET_USER}@.host" /bin/true || true
  systemctl --user --machine="${TARGET_USER}@.host" daemon-reload
  systemctl --user --machine="${TARGET_USER}@.host" enable --now "${SERVICE_NAME}"

  local HOST_IP
  HOST_IP="$(hostname -I 2>/dev/null | awk '{print $1}')"

  log "Autostart configured."
  echo "  • Media folder:  ${MEDIA_DIR}"
  echo "  • VLC Web UI:    http://${HOST_IP}:${VLC_HTTP_PORT}/  (no username, password: ${VLC_HTTP_PASSWORD})"
  echo "  • Service check: systemctl --user --machine='${TARGET_USER}@.host' status '${SERVICE_NAME}'"
}

# ═══════════════════════ Main ═══════════════════════════════
main() {
  require_root
  create_media_dir
  install_and_configure_samba
  install_vlc
  create_systemd_user_service
  enable_user_service
}

main "$@"
