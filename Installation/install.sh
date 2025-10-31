#!/usr/bin/env bash
set -euo pipefail


# ================================================================================== #
# Buttonboard Host Setup                                                             #
# - Creates a deployment share via Samba                                             #
# - Installs Webmin                                                                  #
# - Installs Frontail (global npm) and wires a systemd service with highlight preset #
# ================================================================================== #


### ─────────────────────────── User Config ────────────────────────────
APP_NAME="buttonboard"                         # Your app/binary name
APP_DIR="/opt/${APP_NAME}"                     # Deployment target directory

# Samba share for deployments
SAMBA_SHARE_NAME="deploy"
SAMBA_USER="${SUDO_USER:-${USER}}"            # Default: current user
SAMBA_PASSWORD="buttonboard"                   # Change this in production
SAMBA_READONLY="no"                            # "yes" for read-only

# Frontail (log viewer)
FRONTAIL_PORT="9001"
FRONTAIL_THEME="dark"                          # "default" or "dark"
FRONTAIL_LINES="500"                           # Number of lines initially shown
FRONTAIL_BASE="${FRONTAIL_BASE:-/usr/local/lib/node_modules/frontail}"  # Autodetected if missing
LIVE_LOG_PATH="${APP_DIR}/logs/live.log"
FRONTAIL_SERVICE="/etc/systemd/system/frontail.service"

# VLC – kept here for future use (not used in this script yet)
VLC_USER="${SUDO_USER:-${USER}}"
VLC_HOME="$(getent passwd "${VLC_USER}" | cut -d: -f6)"
VLC_CFG_DIR="${VLC_HOME}/.config/vlc"
VLC_CFG_DST="${VLC_CFG_DIR}/vlcrc"


### ─────────────────────────── Logging ────────────────────────────────
log()  { echo -e "\033[1;32m[+] $*\033[0m"; }
warn() { echo -e "\033[1;33m[!] $*\033[0m"; }
err()  { echo -e "\033[1;31m[✗] $*\033[0m" >&2; }

trap 'err "Script failed at line $LINENO."' ERR


### ─────────────────────────── Host IP ────────────────────────────────
# Try to get the first IPv4 from hostname -I; fallback to ip/awk if needed.
PI_IP="$(hostname -I 2>/dev/null | awk "{print \$1}")"
if [[ -z "${PI_IP}" ]]; then
  PI_IP="$(ip -4 addr show scope global | awk "/inet /{print \$2}" | cut -d/ -f1 | head -n1 || true)"
fi
PI_IP="${PI_IP:-127.0.0.1}"


### ─────────────────────────── Helpers ────────────────────────────────
require_root() {
  [[ "${EUID}" -eq 0 ]] || { err "Please run this script with sudo/root."; exit 1; }
}

# Run apt-get install with common flags; call apt-get update beforehand once.
apt_updated="false"
apt_update_once() {
  if [[ "${apt_updated}" != "true" ]]; then
    log "Updating package index…"
    apt-get update -y
    apt_updated="true"
  fi
}
apt_install() {
  apt_update_once
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "$@"
}

file_has_block() { local f="$1" h="$2"; [[ -f "$f" ]] && grep -qF "$h" "$f"; }

append_block_if_missing() {
  local f="$1" h="$2" b="$3"
  if file_has_block "$f" "$h"; then
    log "Block already present in ${f}: ${h}"
  else
    log "Appending block to ${f}: ${h}"
    printf "\n%s\n" "$b" >> "$f"
  fi
}

ensure_dir_owned() {
  local path="$1" user="$2"
  mkdir -p "${path}"
  chown -R "${user}:${user}" "${path}"
}


### ─────────────────────── Prepare filesystem ─────────────────────────
prepare_fs() {
  log "Installing base packages…"
  apt_install ca-certificates curl gnupg lsb-release

  log "Preparing app directory…"
  ensure_dir_owned "${APP_DIR}" "${SAMBA_USER}"
  chmod 2775 "${APP_DIR}"
  log "App directory ready: ${APP_DIR}"

  log "Preparing logs for Frontail…"
  ensure_dir_owned "${APP_DIR}/logs" "${SAMBA_USER}"
  touch "${LIVE_LOG_PATH}"
  chown "${SAMBA_USER}:${SAMBA_USER}" "${LIVE_LOG_PATH}"
  log "Live log prepared: ${LIVE_LOG_PATH}"
}


### ─────────────────────────── Samba setup ────────────────────────────
setup_samba() {
  log "Installing and configuring Samba…"
  apt_install samba samba-common-bin

  ensure_dir_owned "${APP_DIR}" "${SAMBA_USER}"
  chmod 2775 "${APP_DIR}"

  # Backup smb.conf once
  if [[ -f /etc/samba/smb.conf && ! -f /etc/samba/smb.conf.bak ]]; then
    cp /etc/samba/smb.conf /etc/samba/smb.conf.bak
  fi

  local header="[# ${SAMBA_SHARE_NAME} share (managed by setup script)]"
  local block="
${header}
[${SAMBA_SHARE_NAME}]
   path = ${APP_DIR}
   browsable = yes
   writable = $( [[ "${SAMBA_READONLY}" == "yes" ]] && echo "no" || echo "yes" )
   read only = ${SAMBA_READONLY}
   valid users = ${SAMBA_USER}
   force user = ${SAMBA_USER}
   create mask = 0664
   directory mask = 2775
"
  append_block_if_missing "/etc/samba/smb.conf" "$header" "$block"

  # Create or update Samba user password
  if ! pdbedit -L | cut -d: -f1 | grep -qx "${SAMBA_USER}"; then
    log "Creating Samba user: ${SAMBA_USER}"
    printf '%s\n%s\n' "${SAMBA_PASSWORD}" "${SAMBA_PASSWORD}" | smbpasswd -a -s "${SAMBA_USER}"
  else
    log "Updating Samba password for user: ${SAMBA_USER}"
    printf '%s\n%s\n' "${SAMBA_PASSWORD}" "${SAMBA_PASSWORD}" | smbpasswd -s "${SAMBA_USER}"
  fi

  systemctl enable --now smbd nmbd >/dev/null 2>&1 || systemctl enable --now smbd
  systemctl restart smbd || true

  log "Samba share is ready."
  echo "  • Windows:        \\\\${PI_IP}\\${SAMBA_SHARE_NAME}"
  echo "  • macOS:          smb://${PI_IP}/${SAMBA_SHARE_NAME}"
  echo "  • Path:           ${APP_DIR}"
  echo "  • Samba User:     ${SAMBA_USER}"
  echo "  • Samba Password: ${SAMBA_PASSWORD}"
}


### ─────────────────────────── Webmin setup ───────────────────────────
install_webmin() {
  log "Installing Webmin (official repo)…"
  if ! apt-cache policy | grep -qi "download.webmin.com"; then
    curl -fsSL https://raw.githubusercontent.com/webmin/webmin/master/webmin-setup-repo.sh -o /tmp/webmin-setup-repo.sh
    bash /tmp/webmin-setup-repo.sh --stable --force
  else
    log "Webmin repo already configured."
  fi

  apt_install webmin
  systemctl enable --now webmin
  log "Webmin running at: https://${PI_IP}:10000/"
}


### ────────────────────────── Frontail setup ──────────────────────────
setup_frontail() {
  log "Installing Node.js + npm (for Frontail)…"
  if ! command -v node >/dev/null 2>&1; then
    apt_install nodejs npm
  else
    log "Node.js already present: $(node -v)"
  fi

  log "Installing frontail globally via npm…"
  npm install -g frontail >/dev/null 2>&1 || npm install -g frontail

  local FRONTAIL_BIN
  FRONTAIL_BIN="$(command -v frontail || true)"
  if [[ -z "${FRONTAIL_BIN}" ]]; then
    err "Frontail binary not found after installation."
    exit 1
  fi
  log "Frontail binary: ${FRONTAIL_BIN}"

  # Autodetect FRONTAIL_BASE if the provided/default path does not exist
  if [[ ! -d "${FRONTAIL_BASE}" ]]; then
    local npm_root
    npm_root="$(npm root -g 2>/dev/null || true)"
    if [[ -n "${npm_root}" && -d "${npm_root}/frontail" ]]; then
      FRONTAIL_BASE="${npm_root}/frontail"
    elif [[ -d "/usr/lib/node_modules/frontail" ]]; then
      FRONTAIL_BASE="/usr/lib/node_modules/frontail"
    fi
  fi
  log "Frontail base: ${FRONTAIL_BASE}"

  # Ensure log file is present and owned correctly (idempotent)
  ensure_dir_owned "$(dirname "${LIVE_LOG_PATH}")" "${SAMBA_USER}"
  touch "${LIVE_LOG_PATH}"
  chown "${SAMBA_USER}:${SAMBA_USER}" "${LIVE_LOG_PATH}"

  # Write highlight preset (upstream Frontail supports 'words' and 'lines' with inline CSS)
  log "Creating/Updating Frontail highlight preset…"
  install -d -m 0755 "${FRONTAIL_BASE}/preset"
  tee "${FRONTAIL_BASE}/preset/buttonboard.json" >/dev/null <<'JSON'
{
  "lines": {
    "[ERR]": "color: #ef5350; font-weight: 500;",
    "[WRN]": "color: #ffca28;",
    "[INF]": "color: #00e676;",
    "[DBG]": "color: #29b6f6;"
  }
}
JSON

  # Systemd service for Frontail
  log "Creating/Updating systemd service for Frontail…"
  tee "${FRONTAIL_SERVICE}" >/dev/null <<EOF
[Unit]
Description=Frontail live log viewer for ${APP_NAME}
After=network.target

[Service]
Type=simple
User=${SAMBA_USER}
WorkingDirectory=${APP_DIR}
Environment=NODE_OPTIONS=--no-deprecation
Environment=FORCE_COLOR=1
ExecStart=/bin/sh -lc '\
  ${FRONTAIL_BIN} \
    --disable-usage-stats \
    --ui-highlight \
    --ui-highlight-preset ${FRONTAIL_BASE}/preset/buttonboard.json \
    --theme ${FRONTAIL_THEME} \
    --port ${FRONTAIL_PORT} \
    --host 0.0.0.0 \
    --lines ${FRONTAIL_LINES} \
    ${LIVE_LOG_PATH}'
Restart=on-failure
RestartSec=2s

[Install]
WantedBy=multi-user.target
EOF

  systemctl daemon-reload
  systemctl enable --now frontail
  systemctl restart frontail || true

  log "Frontail running at: http://${PI_IP}:${FRONTAIL_PORT}/"
  echo "  • Tailing file:   ${LIVE_LOG_PATH}"
  echo "  • Theme:          ${FRONTAIL_THEME}"
  echo "  • Lines shown:    ${FRONTAIL_LINES}"
}


### ───────────────────────────── Main ─────────────────────────────────
main() {
  require_root
  log "Detected host IP: ${PI_IP}"
  prepare_fs
  setup_samba
  install_webmin
  setup_frontail

  cat <<SUMMARY

────────────────────────── Setup completed ──────────────────────────
• Deployment share:  //${PI_IP}/${SAMBA_SHARE_NAME}  →  ${APP_DIR}
  Copy your self-contained publish output into:
    ${APP_DIR}
  (make sure the binary is executable, e.g.: chmod +x ${APP_DIR}/BSolutions.Buttonboard.App)

• Webmin:            https://${PI_IP}:10000/  (self-signed)
• Frontail:          http://${PI_IP}:${FRONTAIL_PORT}/
  → Live view of:    ${LIVE_LOG_PATH}
• VLC Player:        http://${PI_IP}:8080/   (reserved; not configured here)

• App Start:         Start your app manually after deployment, e.g.:
  ${APP_DIR}/./BSolutions.Buttonboard.App
────────────────────────────────────────────────────────────────────
SUMMARY
}

main "$@"
