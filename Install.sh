#!/usr/bin/env bash
set -euo pipefail

### ─────────────────────────── User Config ────────────────────────────
APP_NAME="buttonboard"              # Your app/binary name
APP_DIR="/opt/${APP_NAME}"           # Target directory for deployment

# Samba share for deployments
SAMBA_SHARE_NAME="deploy"
SAMBA_USER="${SUDO_USER:-${USER}}"   # Default: current user
SAMBA_PASSWORD="buttonboard"         # Change for other password
SAMBA_READONLY="no"                  # "yes" for read-only

# Webmin (default port 10000)
WEBMIN_INFO_PORT="10000"

### ─────────────────────────── Logging ────────────────────────────────
log()  { echo -e "\033[1;32m[+] $*\033[0m"; }
warn() { echo -e "\033[1;33m[!] $*\033[0m"; }
err()  { echo -e "\033[1;31m[✗] $*\033[0m" >&2; }

### ─────────────────────────── Host IP ────────────────────────────────
# Same approach as in your other script: first IPv4 from hostname -I
PI_IP="$(hostname -I | awk '{print $1}')"

### ─────────────────────────── Helpers ────────────────────────────────
require_root() { [[ "${EUID}" -eq 0 ]] || { err "Please run this script with sudo/root."; exit 1; }; }
apt_install() { DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "$@"; }
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

### ─────────────────────── Prepare filesystem ─────────────────────────
prepare_fs() {
  log "Updating package index…"
  apt-get update -y
  log "Installing base packages…"
  apt_install ca-certificates curl gnupg lsb-release
  mkdir -p "${APP_DIR}"
  chown -R "${SAMBA_USER}:${SAMBA_USER}" "${APP_DIR}"
  log "App directory ready: ${APP_DIR}"
}

### ─────────────────────────── Samba setup ────────────────────────────
setup_samba() {
  log "Installing and configuring Samba…"
  apt_install samba samba-common-bin

  mkdir -p "${APP_DIR}"
  chown -R "${SAMBA_USER}:${SAMBA_USER}" "${APP_DIR}"
  chmod 2775 "${APP_DIR}"

  # Backup once
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

  # Create/ensure Samba user
  if ! pdbedit -L | cut -d: -f1 | grep -qx "${SAMBA_USER}"; then
    log "Creating Samba user: ${SAMBA_USER}"
    printf '%s\n%s\n' "${SAMBA_PASSWORD}" "${SAMBA_PASSWORD}" | smbpasswd -a -s "${SAMBA_USER}"
else
    log "Updating Samba password for user: ${SAMBA_USER}"
    printf '%s\n%s\n' "${SAMBA_PASSWORD}" "${SAMBA_PASSWORD}" | smbpasswd -s "${SAMBA_USER}"
fi

  systemctl enable --now smbd nmbd || systemctl enable --now smbd
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
  apt-get update -y
  apt_install webmin
  systemctl enable --now webmin
  log "Webmin running at: https://${PI_IP}:${WEBMIN_INFO_PORT}/"
}

### ───────────────────────────── Main ─────────────────────────────────
main() {
  require_root
  log "Detected host IP: ${PI_IP}"
  prepare_fs
  setup_samba
  install_webmin

  cat <<SUMMARY

────────────────────────── Setup completed ──────────────────────────
• Deployment share:  //${PI_IP}/${SAMBA_SHARE_NAME}  →  ${APP_DIR}
  Copy your self-contained publish output into:
    ${APP_DIR}
  (make sure the binary is executable, e.g.: chmod +x ${APP_DIR}/BSolutions.Buttonboard.App)

• Webmin:            https://${PI_IP}:${WEBMIN_INFO_PORT}/
  (accept self-signed certificate warning)

• App Start: Start your app manually after deployment, e.g.:
  ${APP_DIR}/./BSolutions.Buttonboard.App
────────────────────────────────────────────────────────────────────
SUMMARY
}

main "$@"
