# 📑 Logging Events

Buttonboard uses strongly typed `EventId` constants (`LogEvents`) for structured logging.  
Each event has a numeric ID and a stable name to support filtering and correlation.

Example:
```csharp
public static readonly EventId LoaderStarted = new(1000, nameof(LoaderStarted));
```

---

## Event ID Structure

- IDs are grouped by domain/range.
- Name always matches the constant name (`nameof(...)`).
- Logs can include both numeric ID and event name.

---

## Event Groups

| Domain / Category | ID Range | Description |
|---|---:|---|
| App / Service Lifecycle | 1000–1999 | Loader startup/shutdown events |
| Assets | 2000–2999 | Asset load/remove/parse-validation events |
| Asset Load Errors | 3000–3999 | Unexpected load failures |
| Runtime | 4000–4999 | Scenario runtime state and step execution |
| Executor Core | 5000–5099 | Generic dispatch/validation failures |
| LMS Actions | 5100–5199 | `lms.*` action events |
| Video Actions | 5200–5299 | `video.*` action events |
| GPIO Actions | 5300–5399 | `gpio.*` action events |
| MQTT Actions | 5400–5499 | `mqtt.*` action events |
| LCD Actions | 5500–5599 | `lcd.*` action events |
| GPIO Core | 6000–6999 | GPIO init/reset/read/blink/errors |
| MQTT Core | 7000–7999 | MQTT lifecycle/publish/reset/errors |
| OpenHAB | 8100–8199 | OpenHAB REST events |
| VLC | 8200–8299 | VLC command/HTTP events |
| Lyrion | 8300–8399 | Lyrion reset/command/response events |

> Hinweis: Im Code ist der Kommentar für den Executor-Block als `5000–5999` bezeichnet, die aktuell belegten IDs liegen aber bis `5505` (inkl. LCD-Actions-Block).

---

## App / Service Lifecycle (1000–1999)

| Event | ID | Description |
|---|---:|---|
| `LoaderStarted` | 1000 | Scenario asset loader started |
| `LoaderStopped` | 1001 | Scenario asset loader stopped |

## Assets (2000–2999)

| Event | ID | Description |
|---|---:|---|
| `AssetLoaded` | 2000 | Asset loaded into cache |
| `AssetRemoved` | 2001 | Asset removed from cache |
| `AssetJsonInvalid` | 2002 | JSON or DSL parse error |

## Asset Load Errors (3000–3999)

| Event | ID | Description |
|---|---:|---|
| `AssetLoadError` | 3000 | Unexpected error while loading asset |

## Runtime (4000–4999)

| Event | ID | Description |
|---|---:|---|
| `RuntimeStartIgnored` | 4000 | Start request ignored (e.g. busy) |
| `RuntimeSceneMissing` | 4001 | Requested scene key not found |
| `RuntimeStarted` | 4010 | Runtime started a scene |
| `RuntimeFinished` | 4011 | Runtime finished scene |
| `RuntimeCanceled` | 4012 | Runtime canceled scene |
| `StepExecuting` | 4020 | Step about to execute |
| `StepExecuted` | 4021 | Step executed successfully |
| `StepFailed` | 4022 | Step failed |

## Executor Core (5000–5099)

| Event | ID | Description |
|---|---:|---|
| `ExecUnknownAction` | 5000 | Unknown action key |
| `ExecArgMissing` | 5001 | Required argument missing |
| `ExecArgInvalid` | 5002 | Argument invalid |
| `ExecResourceMissing` | 5003 | Required resource missing |
| `ExecActionArgInvalid` | 5004 | Action handler arg validation failed |
| `ExecActionFailed` | 5005 | Action handler failed unexpectedly |

## LMS Actions (5100–5199)

| Event | ID | Description |
|---|---:|---|
| `ExecLmsPlay` | 5100 | LMS play action |
| `ExecLmsPause` | 5101 | LMS pause/resume action |
| `ExecLmsVolume` | 5102 | LMS volume action |

## Video Actions (5200–5299)

| Event | ID | Description |
|---|---:|---|
| `ExecVideoNext` | 5200 | Next playlist item action |
| `ExecVideoPause` | 5201 | Pause action |
| `ExecVideoPlayItem` | 5202 | Play item by position action |

## GPIO Actions (5300–5399)

| Event | ID | Description |
|---|---:|---|
| `ExecGpioOn` | 5300 | GPIO LED on action |
| `ExecGpioOff` | 5301 | GPIO LED off action |
| `ExecGpioBlink` | 5302 | GPIO blink action |

## MQTT Actions (5400–5499)

| Event | ID | Description |
|---|---:|---|
| `ExecMqttPublish` | 5400 | MQTT publish action |

## LCD Actions (5500–5599)

| Event | ID | Description |
|---|---:|---|
| `ExecLcdClear` | 5501 | Clear LCD |
| `ExecLcdWrite` | 5502 | Write text |
| `ExecLcdWriteLine` | 5503 | Write single line |
| `ExecLcdWriteLines` | 5504 | Write two lines |
| `ExecLcdBacklight` | 5505 | Toggle backlight |

## GPIO Core (6000–6999)

| Event | ID | Description |
|---|---:|---|
| `GpioInitialized` | 6000 | GPIO initialized |
| `GpioReset` | 6001 | GPIO reset |
| `GpioLedOn` | 6010 | Set LED on |
| `GpioLedOff` | 6011 | Set LED off |
| `GpioBlinkStart` | 6020 | Blink sequence start |
| `GpioBlinkEnd` | 6021 | Blink sequence end |
| `GpioButtonRead` | 6030 | Button state read |
| `GpioOperationErr` | 6099 | GPIO operation error |

## MQTT Core (7000–7999)

### Connection & Publish

| Event | ID | Description |
|---|---:|---|
| `MqttConnecting` | 7000 | Connecting to broker |
| `MqttConnected` | 7001 | Connected to broker |
| `MqttDisconnected` | 7002 | Disconnected from broker |
| `MqttConnectFailed` | 7003 | Connect attempt failed |
| `MqttOnlineAnnounced` | 7010 | `online` status announced |
| `MqttPublishEnqueued` | 7020 | Message enqueued |
| `MqttPublishDropped` | 7021 | Publish dropped/failure |
| `MqttInvalidTopic` | 7022 | Topic invalid |
| `MqttStopping` | 7030 | MQTT stop initiated |
| `MqttStopped` | 7031 | MQTT stopped |
| `MqttPendingDrained` | 7032 | Pending queue drained/timeout |
| `MqttError` | 7099 | Generic MQTT error |

### Reset Workflow

| Event | ID | Description |
|---|---:|---|
| `MqttResetStart` | 7100 | Reset started |
| `MqttResetNoDevices` | 7101 | No devices configured |
| `MqttResetSkippedEmptyTopic` | 7102 | Device skipped (topic missing) |
| `MqttResetSkippedEmptyPayload` | 7103 | Device skipped (reset payload missing) |
| `MqttResetEnqueued` | 7104 | Reset publish enqueued |
| `MqttResetCompleted` | 7105 | Reset completed |
| `MqttResetCanceled` | 7106 | Reset canceled |
| `MqttResetEnqueueFailed` | 7107 | Reset enqueue failed |

## OpenHAB (8100–8199)

| Event | ID | Description |
|---|---:|---|
| `OpenHabCommandSent` | 8100 | REST command sent |
| `OpenHabStateRead` | 8101 | Item state read |
| `OpenHabStateUpdated` | 8102 | Item state updated |
| `OpenHabNonSuccess` | 8103 | Non-success HTTP response |
| `OpenHabError` | 8104 | OpenHAB communication error |

## VLC (8200–8299)

| Event | ID | Description |
|---|---:|---|
| `VlcCommandSent` | 8200 | VLC command sent |
| `VlcNonSuccess` | 8201 | VLC returned non-success response |
| `VlcError` | 8202 | VLC communication error |

## Lyrion (8300–8399)

| Event | ID | Description |
|---|---:|---|
| `LyrionResetStart` | 8300 | Lyrion reset started |
| `LyrionResetNoPlayers` | 8301 | No players configured |
| `LyrionResetSkippedEmptyId` | 8302 | Player skipped (missing ID) |
| `LyrionResetCompleted` | 8303 | Lyrion reset completed |
| `LyrionCommandSent` | 8310 | Command sent to Lyrion |
| `LyrionNoResponse` | 8311 | No response from Lyrion |
| `LyrionError` | 8399 | Lyrion error |
