# Buttonboard Installation

> Prepare a Raspberry Pi and install the Buttonboard runtime in a few focused steps.

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

- ✅ Required dependencies
- ✅ SSH and I2C, if `raspi-config` is available
- ✅ Webmin: `https://[RASPBERRY-PI-IP]:10000`
- ✅ frontail: `http://[RASPBERRY-PI-IP]:9001`
- ✅ VLC: `http://[RASPBERRY-PI-IP]:8080`
- ✅ Samba deployment share

## Default Paths And Access

- App path: `/opt/buttonboard`
- Network share: `\\buttonboard\deploy`
- Samba user: `[Default User Name]`
- Samba password: `buttonboard`

> Tip for maintainers: keep placeholders like `[RASPBERRY-PI-IP]` and `[Default User Name]` up to date so the guide stays reusable and easy to adapt.
