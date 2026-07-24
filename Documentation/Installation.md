# Buttonboard Installation

This guide describes how to prepare a Raspberry Pi and install the Buttonboard runtime environment.

## Raspberry Pi OS (64-bit, Trixie)

1. Flash **Raspberry Pi OS Trixie (64-bit)** to a microSD card using the [Raspberry Pi Imager](https://www.raspberrypi.com/software/).
2. Insert the card into the Raspberry Pi 5 and power it on.
3. Connect the Pi to **Wi-Fi** (via desktop GUI, `nmtui`, or `nmcli`) and enable **SSH**.
4. Update all system packages:

```bash
sudo apt update && sudo apt full-upgrade -y
```

Your base system is now ready.

## Install Buttonboard Runtime Environment

1. Copy all files from the **Installation** folder to your Raspberry Pi (for example via **SFTP**) and change permissions:

```bash
chmod +x install-buttonboard.sh
```

2. Run the installation script with root permissions:

```bash
sudo bash install-buttonboard.sh
```

The script will:

- Install all required dependencies
- Enable SSH and I2C on the Raspberry Pi (when `raspi-config` is available)
- Set up Webmin system administration tool (`https://[RASPBERRY-PI-IP]:10000`)
- Set up frontail for log monitoring (`http://[RASPBERRY-PI-IP]:9001`)
- Set up VLC player for media playback (`http://[RASPBERRY-PI-IP]:8080`)
- Configure a Samba shared folder for Buttonboard app deployment (user: `[Default User Name]`, password: `buttonboard`)

By default the app path is `/opt/buttonboard` on the Raspberry Pi and it is exposed as the network share `\\buttonboard\deploy`.
