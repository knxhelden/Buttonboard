# Actions ⚡

Actions define what Buttonboard should **do** at a given time during a scene.  
They are referenced directly in each DSL scene step with the **`<action>`** keyword.

All actions follow the pattern:

```text
<time> <action> [target] [key=value ...]
```

Each action type may have specific arguments.  
Values with spaces must be quoted.

---

## 🎵 Audio Actions

### `audio.play`
Starts audio playback from a Squeezelite player controlled via Lyrion Music Server.

```text
00:00 audio.play player=Player1 url="http://example.local/media/intro.mp3"
```

**Arguments:**
- `player` *(string, required)* – target audio player (defined in `appsettings.json` → `Lyrion → Players`)
- `url` *(string, required)* – URL to an MP3 file

---

### `audio.pause`
Pauses or resumes playback on a Squeezelite player.

```text
10:00 audio.pause player=Player1
```

**Arguments:**
- `player` *(string, required)* – target audio player (defined in `appsettings.json` → `Lyrion → Players`)
- `paused` *(bool, optional, default=true)* – `true` pauses, `false` resumes

---

### `audio.volume`
Sets the playback volume on a Squeezelite player.

```text
00:05 audio.volume player=Player1 level=35
```

**Arguments:**
- `player` *(string, required)* – target audio player (defined in `appsettings.json` → `Lyrion → Players`)
- `level` *(int, required)* – volume level in percent (`0–100`)


## 🎬 Video Actions

### `video.next`
Skips to the next item in a VLC playlist.

```text
00:00 video.next player=Mediaplayer1
```

**Arguments:**
- `player` *(string, required)* – target VLC player (defined in `appsettings.json` → `VLC → Devices`)

---

### `video.pause`
Toggles pause/resume on a VLC player.

```text
00:10 video.pause player=Mediaplayer1
```

**Arguments:**
- `player` *(string, required)* – target VLC player (defined in `appsettings.json` → `VLC → Devices`)

---

### `video.playItem`
Plays a specific playlist entry by its 1-based position (`1 = first item`).

```text
00:00 video.playItem player=Mediaplayer1 position=1
```

**Arguments:**
- `player` *(string, required)* – target VLC player (defined in `appsettings.json` → `VLC → Devices`)
- `position` *(int, required)* – playlist position (starts with `1`)


## 💡 GPIO Actions

### Available LED IDs

| Logical Name           | GPIO Pin | Group       | Description                    |
| ---------------------- | -------- | ----------- | ------------------------------ |
| **ProcessRed1**        | 23       | Process     | Left red indicator             |
| **ProcessRed2**        | 22       | Process     | Center red indicator           |
| **ProcessRed3**        | 12       | Process     | Right red indicator            |
| **ProcessYellow1**     | 20       | Process     | Left yellow indicator          |
| **ProcessYellow2**     | 19       | Process     | Center yellow indicator        |
| **ProcessYellow3**     | 24       | Process     | Right yellow indicator         |
| **ProcessGreen1**      | 25       | Process     | Left green indicator           |
| **ProcessGreen2**      | 5        | Process     | Center green indicator         |
| **ProcessGreen3**      | 6        | Process     | Right green indicator          |
| **ButtonTopCenter**    | 16       | Button LEDs | Top button backlight           |
| **ButtonBottomLeft**   | 9        | Button LEDs | Bottom left button backlight   |
| **ButtonBottomCenter** | 26       | Button LEDs | Bottom center button backlight |
| **ButtonBottomRight**  | 10       | Button LEDs | Bottom right button backlight  |
| **SystemYellow**       | 17       | System      | System scenario indicator      |
| **SystemRed**          | 18       | System      | System error indicator         |

---

### `gpio.on`
Turns an LED **on**.

```text
00:00 gpio.on led=ButtonTopCenter
```

**Arguments:**
- `led` *(string, required)* – logical LED name (e.g., `ButtonTopCenter`, `ButtonBottomLeft`)

---

### `gpio.off`
Turns an LED **off**.

```text
00:00 gpio.off led=ButtonBottomLeft
```

**Arguments:**
- `led` *(string, required)* – logical LED name (e.g., `ButtonTopCenter`, `ButtonBottomLeft`)

---

### `gpio.blink`
Blinks **all configured LEDs** multiple times.

```text
00:00 gpio.blink count=3 intervalMs=100
```

**Arguments:**
- `count` *(int, optional, default=3)* – number of blink cycles
- `intervalMs` *(int, optional, default=100)* – on/off interval in milliseconds

**Note:**
- `gpio.blink` does **not** use a `led` argument. It currently blinks all LEDs.


## 📺 LCD Actions

LCD actions control the connected character display.  
They can clear the display, write text at the current or a specific cursor position, render one or two aligned lines, and control the backlight.

Supported alignment values are:
- `left`
- `center` / `centre`
- `right`

---

### `lcd.clear`
Clears the entire LCD display.

```text
00:00 lcd.clear
```

**Arguments:**
- none

---

### `lcd.write`
Writes text to the LCD at the current cursor position.  
Optionally, a cursor position can be set before writing.

```text
00:00 lcd.write text="Hello World"
```

With explicit cursor position:

```text
00:00 lcd.write row=1 column=3 text="Hello"
```

**Arguments:**
- `text` *(string, required)* – text to write
- `row` *(int, optional)* – target row index
- `column` *(int, optional)* – target column index

**Notes:**
- If `row` or `column` is provided, the cursor is moved before writing.
- If neither is provided, writing starts at the current cursor position.
- Missing `row` or `column` values default internally to `0` when cursor positioning is used.

---

### `lcd.line`
Writes text to a specific LCD row using optional alignment.

```text
00:00 lcd.line row=0 text="System Ready"
```

Centered text:

```text
00:00 lcd.line row=1 text="WELCOME" align=center
```

Right-aligned text without clearing the row first:

```text
00:00 lcd.line row=1 text="OK" align=right clearRow=false
```

**Arguments:**
- `row` *(int, required)* – target row index
- `text` *(string, required)* – text to display
- `align` *(string, optional, default=`left`)* – text alignment: `left`, `center` / `centre`, or `right`
- `clearRow` *(bool, optional, default=`true`)* – whether the row should be cleared before writing

---

### `lcd.lines`
Writes two lines to the LCD at once using a shared alignment.

```text
00:00 lcd.lines line1="Halloween Mode" line2="Starting..." align=center
```

Single-line usage with empty second line:

```text
00:00 lcd.lines line1="Ready"
```

**Arguments:**
- `line1` *(string, required)* – text for the first line
- `line2` *(string, optional, default=`""`)* – text for the second line
- `align` *(string, optional, default=`left`)* – text alignment: `left`, `center` / `centre`, or `right`

---

### `lcd.backlight`
Enables or disables the LCD backlight.

```text
00:00 lcd.backlight enabled=true
```

**Arguments:**
- `enabled` *(bool, required)* – `true` to enable backlight, `false` to disable


## 📡 MQTT Actions

### `mqtt.pub`
Publishes a message to an MQTT broker. The broker is defined in `appsettings.json` → `Mqtt`.

```text
00:00 mqtt.pub topic=buttonboard/test payload=ON
```

**Arguments:**
- `topic` *(string, required)* – MQTT topic
- `payload` *(string, optional, default=`ON`)* – message content
