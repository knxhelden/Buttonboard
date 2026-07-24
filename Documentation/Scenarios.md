# Scenarios 🎬

Scenarios define **what happens when** inside Buttonboard.  
They are loaded from the assets directory configured via `Application:ScenarioAssetsFolder` (default: `assets`, typically `/opt/buttonboard/assets/` in production deployments).

Two file formats are supported:
- **DSL scene files** (`*.scene`) – recommended for authoring
- **JSON scene files** (`*.json`) – still supported and loaded by the same runtime

There are also two runtime kinds of assets:
- **Setup asset** → file key equals `Scenario:Setup:Key` (default `setup`)
- **Scene assets** → all other files

---

## Scene DSL Format

Buttonboard supports a lightweight DSL for `.scene` files.  
Each non-empty line is either:
- scene metadata (`name: ...`)
- group definition (`group ... = ...`)
- timeline step (`<time> <action> [target] [key=value ...]`)

### Example: `scene3.scene`

```text
name: Szene 3 – Feuer im Haus

group V = Videoplayer1, Videoplayer2, Videoplayer3, Videoplayer4, Videoplayer5
group Fog = cmnd/bremus/entertainment/fogmachine2/POWER, cmnd/bremus/entertainment/fogmachine3/POWER, cmnd/bremus/entertainment/fogmachine4/POWER
group RGB_Red = cmnd/bremus/entertainment/rgblight1/POWER, cmnd/bremus/entertainment/rgblight2/POWER, cmnd/bremus/entertainment/rgblight3/POWER
group RGB_Blue = cmnd/bremus/entertainment/rgblight4/POWER, cmnd/bremus/entertainment/rgblight5/POWER
group Beacons = cmnd/bremus/entertainment/beaconcontroller1/POWER1, cmnd/bremus/entertainment/beaconcontroller1/POWER2, cmnd/bremus/entertainment/beaconcontroller1/POWER3, cmnd/bremus/entertainment/beaconcontroller1/POWER4

00:00 audio.play player=Halloween1 url="file:///mnt/usb/Halloween/03 - Szene 3.MP3"
00:00 video.playItem V[1-4] position=1
00:00 video.playItem Videoplayer5 position=3
00:30 mqtt.pub Fog payload=ON
00:56 mqtt.pub Beacons payload=ON
01:22 mqtt.pub Fog payload=OFF
01:50 video.pause V
01:55 gpio.off led=ButtonBottomCenter
01:55 gpio.on led=ButtonBottomRight
```


## DSL Rules

### General
- Empty lines are ignored.
- Comment lines are ignored only when the line starts with `//` or `#`.
- Times can be written as `ss` or `mm:ss` (`90` = `01:30`).
- Steps are sorted by `AtMs` during load.
- Steps with the same timestamp are scheduled together by the runtime.

### 1) Scene Metadata
```text
name: <display name>
```
- Optional.
- If missing, the file key (filename without extension) is used.

### 2) Group Definitions
```text
group <Name> = <item1>, <item2>, ...
```
- `<Name>` supports letters, numbers, `_` and `-`.
- Groups can be referenced as:
  - `V` (whole group)
  - `V[1-3]` (1-based inclusive slice)

Multiline groups are supported when lines are comma-continued:
```text
group Fog =
  cmnd/.../fogmachine2/POWER,
  cmnd/.../fogmachine3/POWER,
  cmnd/.../fogmachine4/POWER
```

### 3) Timeline Steps
```text
<time> <action> [target] [key=value ...]
```
- The 3rd token is treated as `target` only if it is **not** `key=value`.
- Values in quotes are kept as one token.
- Argument values are auto-typed by parser when possible:
  - integer → number
  - `true`/`false` → bool
  - `null` → JSON null
  - otherwise → string


## Target Expansion Behavior

If `target` is present and resolves to one or more items (single value, group, or slice), the parser expands one step per target item.

During expansion, target is mapped by action domain:
- `mqtt.*` → `args.topic = <targetItem>`
- `video.*` → `args.player = <targetItem>`
- `gpio.*` → `args.led = <targetItem>` (only if `led` not already set)
- other domains → `args.target = <targetItem>` (fallback)

This allows compact authoring like:
```text
00:00 mqtt.pub Fog payload=ON
00:00 video.playItem V[1-4] position=1
```


## JSON Scenario Format (supported)

`*.json` files are still supported. The model fields are:
- `name` *(string)*
- `version` *(int, optional, default `1`)*
- `steps` *(array)*

Each step supports:
- `name` *(string, optional)*
- `atMs` *(int, required)*
- `action` *(string, required)*
- `args` *(object, optional)*
- `onError` *(string, optional, default `continue`; supports `continue` / `abort`)*


## Practical Notes

- The DSL parser may insert missing argument keys with `null` for some domains (e.g. `mqtt` payload/topic, `video.playItem` player/position, `lcd.*` required keys). Runtime action handlers can still reject missing/invalid values.
- Unknown or invalid DSL files are skipped and logged; they are not added to the active asset cache.
- Asset kind (`Setup` vs `Scene`) is derived from filename key compared to `Scenario:Setup:Key`.