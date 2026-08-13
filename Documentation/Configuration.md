# Configuration ⚙️

Buttonboard uses **`appsettings.json`** for global runtime and connection settings. The device
inventory is stored separately in **`hardware.json`** inside the selected scenario directory. This
keeps resets limited to the devices used by that scenario.

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

### `Audio`
Controls playback of local files through the Raspberry Pi's default ALSA/PulseAudio sound card.

| Key | Type | Description |
|-----|------|-------------|
| `MediaFolder` | `string` | Folder containing audio assets, relative to the application directory (default: `audio`). |
| `OutputDevice` | `string` | mpv audio device name; `auto` uses the system default. |
| `DefaultVolume` | `int` | Default playback volume from `0` to `100`. |

The installation script installs `mpv` and creates `/opt/buttonboard/audio`. Files may be organized
in subdirectories and are addressed relatively in scenes, for example `file="effects/thunder.ogg"`.

Available device names can be listed on the Raspberry Pi with `mpv --audio-device=help`. Typical
configurations are:

```json
"OutputDevice": "alsa/plughw:CARD=Headphones,DEV=0"
```

The example above commonly addresses the integrated analogue output on a Raspberry Pi 3. For a
USB sound card on a Raspberry Pi 5, a configuration can look like this:

```json
"OutputDevice": "alsa/plughw:CARD=Device,DEV=0"
```

Because ALSA card names depend on the installed hardware and OS image, the value returned by
`mpv --audio-device=help` should always be preferred over copying an example unchanged.

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

> Note: Lyrion continues to be controlled through `lyrion.*`. The `audio.*` actions are exclusively intended for the local sound card.

---

### `Lyrion`
Controls **Lyrion** players for audio playback.

| Key | Type | Description |
|-----|------|-------------|
| `BaseUri` | `string` | Base address of the Lyrion server (e.g. `tcp://192.168.20.28:9090`). |
| `Username` / `Password` | `string` | Optional credentials for secured servers. |
| `Players` | `object` | Scenario-specific dictionary of logical player names to player IDs (MAC addresses), configured in `hardware.json`. |

Example:
```json
"Lyrion": { "Players": { "Halloween1": "b8:27:eb:75:e2:fa" } }
```

---

### `VLC`
Configuration of VLC players used for video output.

| Key | Type | Description |
|-----|------|-------------|
| `Devices` | `object` | Scenario-specific dictionary of VLC devices in `hardware.json`. The key is the logical player name used by `video.*` actions. |

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
| `Server` | Optional MQTT broker host (default `localhost`, which uses the broker installed on the Buttonboard host) |
| `Port` | Broker port (default `1883`) |
| `Username` / `Password` | Broker credentials |
| `WillTopic` | Topic used for Last Will message (`offline`) |
| `OnlineTopic` | Topic used to announce `online` after connect |
| `Devices` | Scenario-specific list in `hardware.json` with optional startup/reset publish payload |

Device entry schema:
```json
{
  "Name": "Beacon 1",
  "Topic": "cmnd/bremus/entertainment/beaconcontroller1/POWER1",
  "Reset": "OFF"
}
```

Behavior:
- The bundled `appsettings.json` sets `Server` to `localhost`, so Buttonboard connects to the internal broker by default. Set it to a different host to use another broker. If the key is omitted, `localhost` remains the fallback.
- On MQTT connect, Buttonboard publishes retained `online` to `OnlineTopic`.
- Last Will is configured as retained `offline` on `WillTopic`.
- On scenario reset, each configured device with non-empty `Topic` and `Reset` receives its reset payload.

---

## Scenario-specific `hardware.json`

Place `hardware.json` next to the scene files in the directory selected by
`Application:ScenarioAssetsFolder`. The file is optional; omitted collections are treated as empty.
It is loaded after `appsettings.json`, so only the device inventories belong here. Connection data
such as the MQTT broker and Lyrion server remains in `appsettings.json`.

```json
{
  "Lyrion": {
    "Players": {
      "Halloween1": "b8:27:eb:75:e2:fa"
    }
  },
  "VLC": {
    "Devices": {
      "Videoplayer1": {
        "BaseUri": "http://videoplayer1:8080/",
        "Password": "videoplayer"
      }
    }
  },
  "Mqtt": {
    "Devices": [
      {
        "Name": "Beacon 1",
        "Topic": "cmnd/example/beacon/POWER1",
        "Reset": "OFF"
      }
    ]
  }
}
```

Only players and devices listed in this file are resolved by actions and contacted during a
scenario reset. The scenario loader reserves the filename `hardware.json` and does not interpret it
as a JSON scene. Restart Buttonboard after changing the file, because integration clients consume
their configuration when the host starts.
