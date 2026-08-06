# Hardware

This page documents the required hardware components and the GPIO wiring used by the Buttonboard.

## Required Hardware

- **Raspberry Pi 5 Model B** running Raspberry Pi OS (64-bit, Trixie) ([Affilliate Link to Amazon](https://link.amazon/B000CqJ9d))
- **GPIO Expansion Breakout Board** for Raspberry Pi 5 ([Affilliate Link to Amazon](https://link.amazon/B0cnPFlSY))
- **External USB sound card** with Linux support  ([Affilliate Link to Amazon](https://link.amazon/B07t3DXxQ))
- **4 control buttons** with integrated status LEDs ([Affilliate Link to Amazon](https://link.amazon/B0c9oBth1))
- **9-segment LED process bar** for visual progress indication ([Affilliate Link to Amazon](https://link.amazon/B0ePVLgGw))
- Dedicated **"System Ready"** and **"System Warning"** LEDs
- **Custom enclosure** with mounting hardware for reliable installation

## Circuit & Wiring

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
| LED 1 (Top Center)      | :green_circle:  | GPIO 16    | Pin 36      | 3.2V    | 20mA        | 2 kOhm   |
| LED 2 (Bottom Left)     | :green_circle:  | GPIO 9     | Pin 21      | 3.2V    | 20mA        | 2 kOhm   |
| LED 3 (Bottom Center)   | :green_circle:  | GPIO 26    | Pin 37      | 3.2V    | 20mA        | 2 kOhm   |
| LED 4 (Bottom Right)    | :green_circle:  | GPIO 10    | Pin 19      | 3.2V    | 20mA        | 2 kOhm   |
| LED 5 (Process 1)       | :red_circle:    | GPIO 23    | Pin 16      | 2.4V    | 20mA        | 120 Ohm  |
| LED 6 (Process 2)       | :red_circle:    | GPIO 22    | Pin 15      | 2.4V    | 20mA        | 120 Ohm  |
| LED 7 (Process 3)       | :red_circle:    | GPIO 12    | Pin 32      | 2.4V    | 20mA        | 120 Ohm  |
| LED 8 (Process 4)       | :yellow_circle: | GPIO 20    | Pin 38      | 2.4V    | 20mA        | 120 Ohm  |
| LED 9 (Process 5)       | :yellow_circle: | GPIO 19    | Pin 35      | 2.4V    | 20mA        | 120 Ohm  |
| LED 10 (Process 6)      | :yellow_circle: | GPIO 24    | Pin 18      | 2.4V    | 20mA        | 120 Ohm  |
| LED 11 (Process 7)      | :green_circle:  | GPIO 25    | Pin 22      | 3.2V    | 20mA        | 2 kOhm   |
| LED 12 (Process 8)      | :green_circle:  | GPIO 5     | Pin 29      | 3.2V    | 20mA        | 2 kOhm   |
| LED 13 (Process 9)      | :green_circle:  | GPIO 6     | Pin 31      | 3.2V    | 20mA        | 2 kOhm   |
| LED 14 (Scenario Ready) | :yellow_circle: | GPIO 17    | Pin 11      | 3.2V    | 20mA        | 2 kOhm   |
| LED 15 (Scenario Error) | :red_circle:    | GPIO 18    | Pin 12      | 2.4V    | 20mA        | 120 Ohm  |

**LED Series Resistor Calculation:**
`Resistor = (GPIO voltage - LED forward voltage) / LED current`

**Example:**
`R = (3.3V - 2.0V) / 0.010A = 130 Ohm`

In practice, use the next higher standard resistor value, for example **150 Ohm** or **220 Ohm**, to ensure safe operation.

Higher resistors were deliberately chosen to protect the GPIOs and to maintain the same brightness across all colors.

### LCD Display

**Display**: HD44780 1602 LCD Module Display Bundle with I2C Interface 2x16 Characters

| LCD-Display | GPIO (BCM)   | Pin (Board) |
|-------------|--------------|-------------|
| 5V          | 5V power     | Pin 4       |
| GND         | Ground       | Pin 6       |
| SDA         | GPIO 2 (SDA) | Pin 3       |
| SCL         | GPIO 3 (SCL) | Pin 5       |
