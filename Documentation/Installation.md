# Buttonboard Installation

Prepare a Raspberry Pi and install the Buttonboard runtime in a few focused steps.

## Quick Start

### 1. Prepare Raspberry Pi OS

- 🖴 Flash **Raspberry Pi OS Trixie (64-bit)** to a microSD card with the [Raspberry Pi Imager](https://www.raspberrypi.com/software/).
- 🔌 Insert the card into the **Raspberry Pi 5** and power it on.
- 📶 Connect the Pi to **Wi-Fi** and enable **SSH**.
- ⬆️ Update the system:

```bash
sudo apt update && sudo apt full-upgrade -y
```

### 2. Install Buttonboard

- 📁 Copy the files from the `Installation` folder to the Raspberry Pi, for example via **SFTP**.
- 🔐 Make the script executable:

```bash
chmod +x install-buttonboard.sh
```

- ▶️ Start the installation:

```bash
sudo bash install-buttonboard.sh
```

## What The Script Sets Up

- ✅ Base packages: `ca-certificates`, `curl`, `gnupg`, `lsb-release`
- ✅ SSH service and I2C interface, if `raspi-config` is available
- ✅ Application directory `/opt/buttonboard`
- ✅ Log directory `/opt/buttonboard/logs` and live log file `/opt/buttonboard/logs/live.log`
- ✅ Samba deployment share `deploy` for `/opt/buttonboard`
- ✅ Webmin: `https://[RASPBERRY-PI-IP]:10000`
- ✅ Node.js and `npm` as prerequisites for frontail
- ✅ frontail including systemd service: `http://[RASPBERRY-PI-IP]:9001`
- ✅ Mosquitto MQTT broker on port `1883` including configured local user/password authentication

## Default Paths And Access

- App path: `/opt/buttonboard`
- Network share: `\\buttonboard\deploy`
- Samba user: `[DEFAULT USER NAME]`
- Samba password: `buttonboard`

## Raspberry Pi OS Update

The system can be cleanly updated using the following commands:

```bash
sudo apt update
sudo apt full-upgrade
sudo apt autoremove
sudo apt autoclean
sudo reboot
```