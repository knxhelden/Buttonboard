# Configuration ⚙️

Buttonboard uses a central configuration file named **`appsettings.json`**.  
It defines runtime behavior, integrations, device mappings, display settings, and logging.

---

## Configuration Sections

### `Application`
Controls overall runtime behavior.

| Key | Type | Description |
|-----|------|-------------|
| `OperationMode` | `"Real"` \| `"Simulated"` | Chooses between real integrations and mock implementations for selected services (currently OpenHAB + MQTT). |
| `DisableSceneOrder` | `bool` | If `true`, scenes can be triggered regardless of `RequiredStage`. |
| `ScenarioAssetsFolder` | `string` | Relative folder (under app base path) used by the scenario loader for `*.scene` and `*.json` assets. |

---

### `Serilog`
Defines logging output and verbosity.

Current runtime setup supports:
- Console sink
- Daily rolling compact JSON (`.clef`) file sink
- `live.log` text sink for live tailing (Frontail)

Example paths in current config:
- `/opt/buttonboard/logs/buttonboard-.clef`
- `/opt/buttonboard/logs/live.log`

---

### `Scenario`
Describes setup scene and the ordered scene/button mapping.

| Key | Description |
|-----|-------------|
| `Setup.Key` | Key of the setup scene (fallback/default: `setup`). |
| `Scenes` | List of scene mappings with `Key`, `TriggerButton`, and `RequiredStage`. |

`TriggerButton` must match the `Button` enum values:
- `TopCenter`
- `BottomLeft`
- `BottomCenter`
- `BottomRight`

Example:
```json
"Scenes": [
  { "Key": "scene1", "TriggerButton": "TopCenter", "RequiredStage": 0 },
  { "Key": "scene2", "TriggerButton": "BottomLeft", "RequiredStage": 1 }
]
```

---

### `OpenHAB`
Connection settings for the OpenHAB REST API.

| Key | Type | Description |
|-----|------|-------------|
| `BaseUri` | `string` | OpenHAB REST base URL (e.g. `http://192.168.20.20:8080/rest/`). |
| `Audio` | `object` | Dictionary of named audio player configs for OpenHAB item-based control. |

Each entry in `OpenHAB:Audio` uses:
- `Volume` *(int, 0–100, default=0)*
- `ControlItem` *(string)*
- `StreamItem` *(string)*
- `VolumeItem` *(string)*

> Hinweis: Die aktuelle Action-Runtime steuert Audio über `Lyrion` (`audio.*`), nicht über OpenHAB-Actions.

---

### `Lyrion`
Controls **Lyrion / Squeezebox** players for audio playback.

| Key | Type | Description |
|-----|------|-------------|
| `BaseUri` | `string` | Base address of the Lyrion server (e.g. `tcp://192.168.20.28:9090`). |
| `Username` / `Password` | `string` | Optional credentials for secured servers. |
| `Players` | `object` | Dictionary of logical player names to player IDs (MAC addresses). |

Example:
```json
"Lyrion": {
  "BaseUri": "tcp://192.168.20.28:9090",
  "Players": { "Halloween1": "b8:27:eb:75:e2:fa" }
}
```

---

### `VLC`
Configuration of VLC players used for video output.

| Key | Type | Description |
|-----|------|-------------|
| `Devices` | `object` | Dictionary of VLC devices. Key is the logical player name used by `video.*` actions. |

Each device entry:
- `BaseUri` *(string, required)*
- `Password` *(string, required)*

Example:
```json
"VLC": {
  "Devices": {
    "Videoplayer1": {
      "BaseUri": "http://videoplayer1:8080/",
      "Password": "videoplayer"
    }
  }
}
```

---

### `Lcd`
Settings for the HD44780-compatible I2C LCD display.

| Key | Type | Description |
|-----|------|-------------|
| `BusId` | `int` | I2C bus ID (valid range: `0–10`, default `1`). |
| `Address` | `int` | I2C address (valid range: `0x03–0x77`, default `0x27`). |
| `Columns` | `int` | Display width (valid range: `8–40`, default `16`). |
| `Rows` | `int` | Display height (valid range: `1–4`, default `2`). |
| `DefaultBacklight` | `bool` | Initial backlight state used during LCD initialization. |

---

### `Mqtt`
Defines MQTT broker connectivity, status topics, and reset-capable devices.

| Key | Description |
|-----|-------------|
| `Server` | MQTT broker host |
| `Port` | Broker port (default `1883`) |
| `Username` / `Password` | Broker credentials |
| `WillTopic` | Topic used for Last Will message (`offline`) |
| `OnlineTopic` | Topic used to announce `online` after connect |
| `Devices` | List of devices with optional startup/reset publish payload |

Device entry schema:
```json
{
  "Name": "Beacon 1",
  "Topic": "cmnd/bremus/entertainment/beaconcontroller1/POWER1",
  "Reset": "OFF"
}
```

Behavior:
- On MQTT connect, Buttonboard publishes retained `online` to `OnlineTopic`.
- Last Will is configured as retained `offline` on `WillTopic`.
- On scenario reset, each configured device with non-empty `Topic` and `Reset` receives its reset payload.
