# 🎛️ Buttonboard – Entertainment Controlling System

The **Buttonboard** is a flexible control platform built with **.NET** and optimized for the **Raspberry Pi**.
It enables the design and execution of interactive entertainment scenarios, ranging from private home projects to live shows and installations.

Key features include:

- 🎛️ Device control for audio and video outputs
- 🌐 IoT integration via MQTT and REST APIs
- ⚡ Lightweight deployment with self-contained binaries, no runtime installation required
- 🛠️ Designed for extensibility, making it easy to add new scenarios or device types

Typical use cases:

- Interactive Halloween or stage shows
- Smart home entertainment control
- Rapid prototyping of IoT-driven experiences

---

## 🔧 Hardware

- **Raspberry Pi 3 B+** running Raspberry Pi OS (64-bit, Bookworm)  
- **4 control buttons** with integrated status LEDs  
- **9-segment LED process bar** for visual progress indication  
- Dedicated **“System Ready”** and **“System Warning”** LEDs  
- **Custom enclosure** with mounting hardware for reliable installation

## ⚡ Circuit & Wiring

### Buttons

| Button                   | Function      | GPIO (BCM) | Pin (Board) |
|--------------------------|---------------|------------|-------------|
| Button 1 (Top Center)    | Start Scene 1 | GPIO 13    | Pin 33      |
| Button 2 (Bottom Left)   | Start Scene 2 | GPIO 27    | Pin 13      |
| Button 3 (Bottom Middle) | Start Scene 3 | GPIO 4     | Pin 7       |
| Button 4 (Bottom Right)  | Start Scene 4 | GPIO 21    | Pin 40      |

### LEDs

| LED                     | Farbe           | GPIO (BCM) | Pin (Board) | Voltage | El. Current | Resistor |
|-------------------------|-----------------|------------|-------------|---------|-------------|----------|
| LED 1 (Top Center)      | :green_circle:  | GPIO 16    | Pin 36      | 3.2V    | 20mA        | 2kΩ      |
| LED 2 (Bottom Left)     | :green_circle:  | GPIO 9     | Pin 21      | 3.2V    | 20mA        | 2kΩ      |
| LED 3 (Bottom Center)   | :green_circle:  | GPIO 26    | Pin 37      | 3.2V    | 20mA        | 2kΩ      |
| LED 4 (Bottom Right)    | :green_circle:  | GPIO 10    | Pin 19      | 3.2V    | 20mA        | 2kΩ      |
| LED 5 (Process 1)       | :red_circle:    | GPIO 23    | Pin 16      | 2.4V    | 20mA        | 120Ω     |
| LED 6 (Process 2)       | :red_circle:    | GPIO 22    | Pin 15      | 2.4V    | 20mA        | 120Ω     |
| LED 7 (Process 3)       | :red_circle:    | GPIO 12    | Pin 32      | 2.4V    | 20mA        | 120Ω     |
| LED 8 (Process 4)       | :yellow_circle: | GPIO 20    | Pin 38      | 2.4V    | 20mA        | 120Ω     |
| LED 9 (Process 5)       | :yellow_circle: | GPIO 19    | Pin 35      | 2.4V    | 20mA        | 120Ω     |
| LED 10 (Process 6)      | :yellow_circle: | GPIO 24    | Pin 18      | 2.4V    | 20mA        | 120Ω     |
| LED 11 (Process 7)      | :green_circle:  | GPIO 25    | Pin 22      | 3.2V    | 20mA        | 2kΩ      |
| LED 12 (Process 8)      | :green_circle:  | GPIO 5     | Pin 29      | 3.2V    | 20mA        | 2kΩ      |
| LED 13 (Process 9)      | :green_circle:  | GPIO 6     | Pin 31      | 3.2V    | 20mA        | 2kΩ      |
| LED 14 (System Ready)   | :green_circle:  | GPIO 17    | Pin 11      | 3.2V    | 20mA        | 2kΩ      |
| LED 15 (System Warning) | :yellow_circle: | GPIO 18    | Pin 12      | 2.4V    | 20mA        | 120Ω     |


***LED Series Resistor Calculation:***
`Resistor = (GPIO voltage – LED forward voltage) / LED current`

**Example:**  
`R = (3.3V – 2.0V) / 0.010A = 130Ω`

👉 In practice, use the next higher standard resistor value (e.g., **150Ω** or **220Ω**) to ensure safe operation.

ℹ️ Higher resistors were deliberately chosen to protect the GPIOs and to maintain the same brightness across all colors.

### LCD Display

**Display**: HD44780 1602 LCD Module Display Bundle with I2C Interface 2x16 Characters

| LCD-Display | GPIO (BCM)   | Pin (Board) |
|-------------|--------------|-------------|
| 5V          | 5V power     | Pin 4       |
| GND         | Ground       | Pin 6       |
| SDA         | GPIO 2 (SDA) | Pin 3       |
| SCL         | GPIO 3 (SCL) | Pin 5       |


## 📦 Installation Guide

### 📥 Raspberry Pi OS (64-bit, Trixie)

1. Flash **Raspberry Pi OS Trixie (64-bit)** to a microSD card using the [Raspberry Pi Imager](https://www.raspberrypi.com/software/).

2. Insert the card into the Raspberry Pi 5 and power it on.

3. Connect the Pi to **Wi-Fi** (via desktop GUI, `nmtui`, or `nmcli`) and enable **SSH**.

4. Update all system packages:

```bash
sudo apt update && sudo apt full-upgrade -y
```

✅ Your base system is now ready.

### 🌐 Install Buttonboard Runtime Environment

1. Copy all files from the **Installation** folder to your Raspberry Pi (e.g., via **SFTP**) and change permissions:

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
- Set up frontail for log monitoring: (`http://[RASPBERRY-PI-IP]:9001`)
- Set up VLC player for media playback: (`http://[RASPBERRY-PI-IP]:8080`)
- Configure Samba shared folder for buttonboard app deployment (User: `[Default User Name]` / Password: `buttonboard`)
  
*(By default the app path is `/opt/buttonboard` on the Raspberry Pi and exposes it as a network share `\\buttonboard\deploy`)*

---

## 📦 Deployment & Execution of the Buttonboard App

### 1. Deployment  

The application is built using a **Publish Profile** and then transferred to the Buttonboard.

![Publish Profile](./Images/deployment_01.png "Publish Profile")

ℹ️ Note:
The deployment is performed as a self-contained deployment. All required libraries and the .NET runtime are included in the application package — no separate .NET installation is required on the Raspberry Pi.

### 2. Execution

After transferring the files to the Raspberry Pi, set the **execute permission** on the application binary:

```bash
chmod +x /opt/buttonboard/BSolutions.Buttonboard.App
```

You can then start the application with:

```bash
/opt/buttonboard/./BSolutions.Buttonboard.App
```
