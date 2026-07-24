# 🎞️ Mediaplayer – Kiosk Video Playback on Raspberry Pi

The **Mediaplayer** is a lean, VLC-based kiosk setup for **Raspberry Pi OS (Desktop)** that plays local videos in **fullscreen** and can be controlled via **HTTP**.

Key features:

- 🖥️ Fullscreen playback with a clean frame (no title overlays, no OSD)
- 🌐 Web control via VLC HTTP interface (`http://<PI-IP>:8080`) – **no username, password: `videoplayer`**
- ⚡ Autostart with a systemd **user** service (starts on desktop login)
- 📁 Folder-based playlist (drop files into a designated directory)

---

## 🔧 Hardware

- Raspberry Pi (HDMI display attached)
- Raspberry Pi OS (Desktop), 64-bit (Trixie or newer)
- microSD card (≥16 GB recommended)

---

## 📦 Installation Guide

### 📥 Raspberry Pi OS (Desktop)

1. Flash **Raspberry Pi OS (Desktop, 64-bit)** using the Raspberry Pi Imager.  
2. Boot the Pi, complete first-boot setup, connect to the network.  
3. Update the system:
   ```bash
   sudo apt update && sudo apt full-upgrade -y
   ```
4. Enable **SSH** and **VNC** for remote access:
   ```bash
   sudo raspi-config nonint do_ssh 0
   sudo raspi-config nonint do_vnc 0
   ```

### 🛠️ Install the Mediaplayer Runtime

1. Copy the installer script `install-mediaplayer.sh` to the Pi.  
2. Run the installer with root permissions:
   ```bash
   sudo bash install-mediaplayer.sh
   ```

The script will:

- Create the video directory (default): `~/Videos`  
- Configure a **Samba** share for the video directory: `\\<RASPBERRY-PI-IP>\Videos`  
  - **Username:** `<your-pi-user>` (e.g., `pi`)  
  - **Password:** `videoplayer` *(same as VLC HTTP password)*  
- Install **VLC**  
- Create & enable the **systemd user service** `vlc-kiosk.service` (autostart on desktop login)  
  - `loginctl enable-linger <user>` is used so the user manager stays active  
- Launch **VLC in kiosk mode** with HTTP control: `http://<RASPBERRY-PI-IP>:8080` → **no username, password: `videoplayer`**  
- Set up **Webmin** system administration tool (`https://<RASPBERRY-PI-IP>:10000`)

🛑 **Stop VLC Kiosk Mode:** Open VLC HTTP control and press **STOP**.

### 🎞️ Add Media Files

Place your video files in: `~/Videos`. Files are picked up on service start/restart (or after reboot).  
Default playback:
- Fullscreen, **no OSD/title**, **subtitles disabled**, **loop enabled**
- Files matched by `~/Videos/*.mp4` (extend the command for other formats)