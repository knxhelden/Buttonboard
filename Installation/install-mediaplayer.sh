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
MEDIA_DIR="${TARGET_HOME}/Videos"

# Samba
SAMBA_SHARE_NAME="Videos"

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

apt_install() {
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends "$@"
}


# ═══════════════════════ Media Folder Creation ═══════════════════
create_media_dir() {
  log "Create media folder: ${MEDIA_DIR}"
  mkdir -p "${MEDIA_DIR}"
  chown -R "${TARGET_USER}:${TARGET_GROUP}" "${MEDIA_DIR}"
}

# ═══════════════════════ Samba Share ───────────────────────────────
install_and_configure_samba() {
  log "Installiere & konfiguriere Samba (guest writeable) für ${MEDIA_DIR}"

  # Paketinstallation (idempotent)
  apt-get update -y
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends samba samba-common-bin

  # Sicherstellen, dass Verzeichnis existiert und Rechte stimmen
  mkdir -p "${MEDIA_DIR}"
  chown -R "${TARGET_USER}:${TARGET_GROUP}" "${MEDIA_DIR}"
  # Set-GID, damit neue Dateien dieselbe Gruppe bekommen; schreibbar für owner+group
  chmod 2775 "${MEDIA_DIR}" || true

  local SMB_CONF="/etc/samba/smb.conf"
  # Backup nur einmal
  if [[ ! -f "${SMB_CONF}.bak" ]]; then
    cp -a "${SMB_CONF}" "${SMB_CONF}.bak" || true
    log "Backup erstellt: ${SMB_CONF}.bak"
  fi

  # --- Ensure global guest mapping ---
  # Insert "map to guest = Bad User" and "guest account = nobody" into [global] if missing
  if grep -q "^\[global\]" "${SMB_CONF}"; then
    # helper to add a directive under [global] if it does not exist
    add_global_directive() {
      local directive="$1"
      if ! grep -q "^[[:space:]]*${directive//\//\\/}" "${SMB_CONF}"; then
        log "Füge globale Samba-Direktive hinzu: ${directive}"
        # Insert directive immediately after the [global] header (first occurrence)
        awk -v d="${directive}" '
          BEGIN{added=0}
          {
            print $0
            if(!added && $0 ~ /^\[global\]/) {
              print "   " d
              added=1
            }
          }
        ' "${SMB_CONF}" > "${SMB_CONF}.tmp" && mv "${SMB_CONF}.tmp" "${SMB_CONF}"
      else
        log "Samba-Direktive bereits vorhanden: ${directive}"
      fi
    }

    add_global_directive "map to guest = Bad User"
    add_global_directive "guest account = nobody"
  else
    # Falls keine [global]-Sektion existiert (sehr unwahrscheinlich), anhängen
    log "Keine [global]-Sektion gefunden — Füge guest mapping am Ende der Datei an."
    cat >> "${SMB_CONF}" <<EOF

[global]
   map to guest = Bad User
   guest account = nobody
EOF
  fi

  # --- Add share block if missing ---
  if ! grep -q "^\[${SAMBA_SHARE_NAME}\]" "${SMB_CONF}"; then
    log "Trage Share [${SAMBA_SHARE_NAME}] in ${SMB_CONF} ein…"
    cat >> "${SMB_CONF}" <<EOF

[${SAMBA_SHARE_NAME}]
   path = ${MEDIA_DIR}
   browseable = yes
   read only = no
   writable = yes
   guest ok = yes
   public = yes
   force user = ${TARGET_USER}
   force group = ${TARGET_GROUP}
   create mask = 0664
   directory mask = 2775
   follow symlinks = yes
   wide links = yes
   unix extensions = no
EOF
  else
    # Wenn Share existiert, stelle sicher, dass es guest ok=yes und force user gesetzt hat
    log "Share [${SAMBA_SHARE_NAME}] existiert bereits — überprüfe/aktualisiere Attribute."
    # Ersetze / aktualisiere relevante Einstellungen robust mit awk (erste Vorkommen)
    awk -v share="${SAMBA_SHARE_NAME}" -v user="${TARGET_USER}" -v group="${TARGET_GROUP}" '
      BEGIN{inShare=0; done=0}
      {
        if ($0 ~ "^\\[" share "\\]") { inShare=1; print; next }
        if (inShare && $0 ~ "^\\[") { # Ende des Share-Blocks
          if(!done) {
            print "   guest ok = yes"
            print "   public = yes"
            print "   force user = " user
            print "   force group = " group
            print "   create mask = 0664"
            print "   directory mask = 2775"
            done=1
          }
          inShare=0
        }
        if(inShare) {
          # remove existing settings we will re-add to avoid duplicates
          if ($0 ~ "^[[:space:]]*(guest ok|public|force user|force group|create mask|directory mask)[[:space:]]*=") {
            next
          }
        }
        print
      }
      END {
        if(inShare && !done) {
          # EOF within share block -> append directives
          print "   guest ok = yes"
          print "   public = yes"
          print "   force user = " user
          print "   force group = " group
          print "   create mask = 0664"
          print "   directory mask = 2775"
        }
      }
    ' "${SMB_CONF}" > "${SMB_CONF}.tmp" && mv "${SMB_CONF}.tmp" "${SMB_CONF}"
  fi

  # Restart Samba
  systemctl enable --now smbd nmbd 2>/dev/null || systemctl enable --now smbd || true
  systemctl restart smbd || true

  local HOST_IP
  HOST_IP="$(hostname -I 2>/dev/null | awk '{print $1}')"
  log "Samba Guest-Share bereit: \\\\${HOST_IP}\\${SAMBA_SHARE_NAME}"
  echo "  • Pfad:     ${MEDIA_DIR}"
  echo "  • Schreibzugriff: ohne Authentifizierung (guest ok = yes)"
  echo "  • Dateibesitzer: wird erzwungen auf ${TARGET_USER}:${TARGET_GROUP}"
  echo "  • Samba-Konfig: ${SMB_CONF} (Backup: ${SMB_CONF}.bak)"
}



# ═══════════════════════ VLC Installation ══════════════════════
install_vlc() {
  log "Installiere VLC…"
  apt-get update -y
  DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends vlc
}

# ═══════════════ systemd User-Service Creation & Activation ═════
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
  echo "  • VLC Web-UI:     http://<PI-IP>:${HTTP_PORT}/  (User leer, Passwort: ${HTTP_PASSWORD})"
  echo "  • Logs:           journalctl --user -u ${SERVICE_NAME} -f"
}


# ══════════════════════ Webmin setup  ══════════════════════
install_webmin() {
  log "Installing Webmin (official repo)…"

  # Basis-Tools sicherstellen
  apt-get update -y
  apt_install curl ca-certificates

  # Webmin-Repo einrichten (idempotent)
  if ! apt-cache policy | grep -qi "download.webmin.com"; then
    curl -fsSL https://raw.githubusercontent.com/webmin/webmin/master/webmin-setup-repo.sh -o /tmp/webmin-setup-repo.sh
    bash /tmp/webmin-setup-repo.sh --stable --force
  else
    log "Webmin repo already configured."
  fi

  apt-get update -y
  apt_install webmin

  systemctl enable --now webmin || true

  # Ausgabe-Infos (Port ist standardmäßig 10000)
  local PI_IP WEBMIN_INFO_PORT
  PI_IP="$(hostname -I 2>/dev/null | awk '{print $1}')"
  WEBMIN_INFO_PORT="10000"
  log "Webmin running at: https://${PI_IP}:${WEBMIN_INFO_PORT}/"
}


# ═══════════════════════ main ══════════════════════════════════════
main() {
  require_root
  create_media_dir
  install_and_configure_samba
  install_vlc
  create_systemd_user_service
  enable_user_service
  install_webmin
}

main "$@"
