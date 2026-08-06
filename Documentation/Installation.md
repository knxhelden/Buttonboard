# Buttonboard Installation

Prepare a Raspberry Pi and install the Buttonboard runtime in a few focused steps.

## Quick Start

### 1. Prepare Raspberry Pi OS

1. Flash **Raspberry Pi OS Trixie (64-bit)** to a microSD card with the [Raspberry Pi Imager](https://www.raspberrypi.com/software/).
2. Insert the card into the **Raspberry Pi 5** and power it on.
3. Connect the Pi to **Wi-Fi** and enable **SSH**.
4. Update the system:

```bash
sudo apt update && sudo apt full-upgrade -y
```

---

### 2. Test Sound Card

1. Connect the **external USB sound card** to the Raspberry Pi.
2. Verify that it is detected:
```bash
cat /proc/asound/cards
```
3. Note the card number and use it in the playback test:
```bash
speaker-test -D plughw:2,0 -c 2 -t wav
```

> Replace `2` with the card number shown for the USB sound card. Press `Ctrl+C` to stop the test.

---

### 3. Install Buttonboard

1. Copy the files from the `Installation` folder to the Raspberry Pi, for example via **SFTP**.
2. Make the script executable:

```bash
chmod +x install-buttonboard.sh
```

3. Start the installation:

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