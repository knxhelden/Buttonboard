using Microsoft.Extensions.Logging;

namespace BSolutions.Buttonboard.Services.Logging
{
    public static class LogEvents
    {
        // App/Service lifecycle
        public static readonly EventId LoaderStarted = new(1000, nameof(LoaderStarted));
        public static readonly EventId LoaderStopped = new(1001, nameof(LoaderStopped));

        // Assets
        public static readonly EventId AssetLoaded = new(2000, nameof(AssetLoaded));
        public static readonly EventId AssetRemoved = new(2001, nameof(AssetRemoved));
        public static readonly EventId AssetJsonInvalid = new(2002, nameof(AssetJsonInvalid));

        // Asset load errors
        public static readonly EventId AssetLoadError = new(3000, nameof(AssetLoadError));

        // Runtime
        public static readonly EventId RuntimeStartIgnored = new(4000, nameof(RuntimeStartIgnored));
        public static readonly EventId RuntimeSceneMissing = new(4001, nameof(RuntimeSceneMissing));
        public static readonly EventId RuntimeStarted = new(4010, nameof(RuntimeStarted));
        public static readonly EventId RuntimeFinished = new(4011, nameof(RuntimeFinished));
        public static readonly EventId RuntimeCanceled = new(4012, nameof(RuntimeCanceled));
        public static readonly EventId StepExecuting = new(4020, nameof(StepExecuting));
        public static readonly EventId StepExecuted = new(4021, nameof(StepExecuted));
        public static readonly EventId StepFailed = new(4022, nameof(StepFailed));

        // Executor
        public static readonly EventId ExecUnknownAction = new(5000, nameof(ExecUnknownAction));
        public static readonly EventId ExecArgMissing = new(5001, nameof(ExecArgMissing));
        public static readonly EventId ExecResourceMissing = new(5002, nameof(ExecResourceMissing));

        public static readonly EventId ExecAudioPlay = new(5100, nameof(ExecAudioPlay));
        public static readonly EventId ExecVideoNext = new(5200, nameof(ExecVideoNext));
        public static readonly EventId ExecVideoPause = new(5201, nameof(ExecVideoPause));

        public static readonly EventId ExecGpioOn = new(5300, nameof(ExecGpioOn));
        public static readonly EventId ExecGpioOff = new(5301, nameof(ExecGpioOff));
        public static readonly EventId ExecGpioBlink = new(5302, nameof(ExecGpioBlink));

        public static readonly EventId ExecMqttPublish = new(5400, nameof(ExecMqttPublish));

        // --- GPIO ---
        public static readonly EventId GpioInitialized = new(6000, nameof(GpioInitialized));
        public static readonly EventId GpioReset = new(6001, nameof(GpioReset));
        public static readonly EventId GpioLedOn = new(6010, nameof(GpioLedOn));
        public static readonly EventId GpioLedOff = new(6011, nameof(GpioLedOff));
        public static readonly EventId GpioBlinkStart = new(6020, nameof(GpioBlinkStart));
        public static readonly EventId GpioBlinkEnd = new(6021, nameof(GpioBlinkEnd));
        public static readonly EventId GpioButtonRead = new(6030, nameof(GpioButtonRead));
        public static readonly EventId GpioOperationErr = new(6099, nameof(GpioOperationErr));
    }
}
