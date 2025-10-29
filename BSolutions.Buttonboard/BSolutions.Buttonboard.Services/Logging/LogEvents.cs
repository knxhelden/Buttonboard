using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.Logging
{
    /// <summary>
    /// Centralized list of <see cref="EventId"/> definitions used for structured logging.
    /// Grouped by functional domain and ID range.
    /// </summary>
    public static class LogEvents
    {
        // ─────────────────────────────── App / Service Lifecycle (1000–1999)
        public static readonly EventId LoaderStarted = new(1000, nameof(LoaderStarted));
        public static readonly EventId LoaderStopped = new(1001, nameof(LoaderStopped));

        // ─────────────────────────────── Assets (2000–2999)
        public static readonly EventId AssetLoaded = new(2000, nameof(AssetLoaded));
        public static readonly EventId AssetRemoved = new(2001, nameof(AssetRemoved));
        public static readonly EventId AssetJsonInvalid = new(2002, nameof(AssetJsonInvalid));

        // ─────────────────────────────── Asset Load Errors (3000–3999)
        public static readonly EventId AssetLoadError = new(3000, nameof(AssetLoadError));

        // ─────────────────────────────── Runtime (4000–4999)
        public static readonly EventId RuntimeStartIgnored = new(4000, nameof(RuntimeStartIgnored));
        public static readonly EventId RuntimeSceneMissing = new(4001, nameof(RuntimeSceneMissing));
        public static readonly EventId RuntimeStarted = new(4010, nameof(RuntimeStarted));
        public static readonly EventId RuntimeFinished = new(4011, nameof(RuntimeFinished));
        public static readonly EventId RuntimeCanceled = new(4012, nameof(RuntimeCanceled));
        public static readonly EventId StepExecuting = new(4020, nameof(StepExecuting));
        public static readonly EventId StepExecuted = new(4021, nameof(StepExecuted));
        public static readonly EventId StepFailed = new(4022, nameof(StepFailed));

        // ─────────────────────────────── Executor (5000–5999)
        public static readonly EventId ExecUnknownAction = new(5000, nameof(ExecUnknownAction));
        public static readonly EventId ExecArgMissing = new(5001, nameof(ExecArgMissing));
        public static readonly EventId ExecArgInvalid = new(5002, nameof(ExecArgInvalid));
        public static readonly EventId ExecResourceMissing = new(5003, nameof(ExecResourceMissing));
        public static readonly EventId ExecActionArgInvalid = new(5004, nameof(ExecActionArgInvalid));  // new
        public static readonly EventId ExecActionFailed = new(5005, nameof(ExecActionFailed));      // new

        // ─── Audio Actions (5100–5199)
        public static readonly EventId ExecAudioPlay = new(5100, nameof(ExecAudioPlay));
        public static readonly EventId ExecAudioPause = new(5101, nameof(ExecAudioPause));
        public static readonly EventId ExecAudioVolume = new(5102, nameof(ExecAudioVolume));

        // ─── Video Actions (5200–5299)
        public static readonly EventId ExecVideoNext = new(5200, nameof(ExecVideoNext));
        public static readonly EventId ExecVideoPause = new(5201, nameof(ExecVideoPause));
        public static readonly EventId ExecVideoPlayItem = new(5202, nameof(ExecVideoPlayItem));     // new

        // ─── GPIO Actions (5300–5399)
        public static readonly EventId ExecGpioOn = new(5300, nameof(ExecGpioOn));
        public static readonly EventId ExecGpioOff = new(5301, nameof(ExecGpioOff));
        public static readonly EventId ExecGpioBlink = new(5302, nameof(ExecGpioBlink));

        // ─── MQTT Actions (5400–5499)
        public static readonly EventId ExecMqttPublish = new(5400, nameof(ExecMqttPublish));

        // ─────────────────────────────── GPIO (6000–6999)
        public static readonly EventId GpioInitialized = new(6000, nameof(GpioInitialized));
        public static readonly EventId GpioReset = new(6001, nameof(GpioReset));
        public static readonly EventId GpioLedOn = new(6010, nameof(GpioLedOn));
        public static readonly EventId GpioLedOff = new(6011, nameof(GpioLedOff));
        public static readonly EventId GpioBlinkStart = new(6020, nameof(GpioBlinkStart));
        public static readonly EventId GpioBlinkEnd = new(6021, nameof(GpioBlinkEnd));
        public static readonly EventId GpioButtonRead = new(6030, nameof(GpioButtonRead));
        public static readonly EventId GpioOperationErr = new(6099, nameof(GpioOperationErr));

        // ─────────────────────────────── MQTT (7000–7999)
        public static readonly EventId MqttConnecting = new(7000, nameof(MqttConnecting));
        public static readonly EventId MqttConnected = new(7001, nameof(MqttConnected));
        public static readonly EventId MqttDisconnected = new(7002, nameof(MqttDisconnected));
        public static readonly EventId MqttConnectFailed = new(7003, nameof(MqttConnectFailed));
        public static readonly EventId MqttOnlineAnnounced = new(7010, nameof(MqttOnlineAnnounced));
        public static readonly EventId MqttPublishEnqueued = new(7020, nameof(MqttPublishEnqueued));
        public static readonly EventId MqttPublishDropped = new(7021, nameof(MqttPublishDropped));
        public static readonly EventId MqttInvalidTopic = new(7022, nameof(MqttInvalidTopic));
        public static readonly EventId MqttStopping = new(7030, nameof(MqttStopping));
        public static readonly EventId MqttStopped = new(7031, nameof(MqttStopped));
        public static readonly EventId MqttPendingDrained = new(7032, nameof(MqttPendingDrained));

        // ─── MQTT Reset (7100–7107)
        public static readonly EventId MqttResetStart = new(7100, nameof(MqttResetStart));
        public static readonly EventId MqttResetNoDevices = new(7101, nameof(MqttResetNoDevices));
        public static readonly EventId MqttResetSkippedEmptyTopic = new(7102, nameof(MqttResetSkippedEmptyTopic));
        public static readonly EventId MqttResetSkippedEmptyPayload = new(7103, nameof(MqttResetSkippedEmptyPayload));
        public static readonly EventId MqttResetEnqueued = new(7104, nameof(MqttResetEnqueued));
        public static readonly EventId MqttResetCompleted = new(7105, nameof(MqttResetCompleted));
        public static readonly EventId MqttResetCanceled = new(7106, nameof(MqttResetCanceled));
        public static readonly EventId MqttResetEnqueueFailed = new(7107, nameof(MqttResetEnqueueFailed));
        public static readonly EventId MqttError = new(7099, nameof(MqttError));

        // ─────────────────────────────── OpenHAB (8100–8199)
        public static readonly EventId OpenHabCommandSent = new(8100, nameof(OpenHabCommandSent));
        public static readonly EventId OpenHabStateRead = new(8101, nameof(OpenHabStateRead));
        public static readonly EventId OpenHabStateUpdated = new(8102, nameof(OpenHabStateUpdated));
        public static readonly EventId OpenHabNonSuccess = new(8103, nameof(OpenHabNonSuccess));
        public static readonly EventId OpenHabError = new(8104, nameof(OpenHabError));

        // ─────────────────────────────── VLC (8200–8299)
        public static readonly EventId VlcCommandSent = new(8200, nameof(VlcCommandSent));
        public static readonly EventId VlcNonSuccess = new(8201, nameof(VlcNonSuccess));
        public static readonly EventId VlcError = new(8202, nameof(VlcError));

        // ─────────────────────────────── Lyrion (8300–8399)
        public static readonly EventId LyrionResetStart = new(8300, nameof(LyrionResetStart));
        public static readonly EventId LyrionResetNoPlayers = new(8301, nameof(LyrionResetNoPlayers));
        public static readonly EventId LyrionResetSkippedEmptyId = new(8302, nameof(LyrionResetSkippedEmptyId));
        public static readonly EventId LyrionResetCompleted = new(8303, nameof(LyrionResetCompleted));
        public static readonly EventId LyrionCommandSent = new(8310, nameof(LyrionCommandSent));
        public static readonly EventId LyrionNoResponse = new(8311, nameof(LyrionNoResponse));
        public static readonly EventId LyrionError = new(8399, nameof(LyrionError));
    }
}
