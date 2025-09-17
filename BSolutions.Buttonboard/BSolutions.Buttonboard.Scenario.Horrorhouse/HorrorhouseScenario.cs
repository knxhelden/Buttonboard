using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BSolutions.Buttonboard.Scenario.Horrorhouse
{
    public class HorrorhouseScenario : ScenarioBase
    {
        private readonly AudioPlayer _audioPlayer1;
        private readonly AudioPlayer _audioPlayer2;

        #region --- Constructor ---

        public HorrorhouseScenario(ILogger<HorrorhouseScenario> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpioController,
            IOpenHabClient openhab,
            IVlcPlayerClient vlc,
            IButtonboardMqttClient mqtt)
            : base(logger, settingsProvider, gpioController, openhab, vlc, mqtt)
        {
            _audioPlayer1 = _settings.OpenHAB.Audio.Players.Single(p => p.Name == "Player1");
            _audioPlayer2 = _settings.OpenHAB.Audio.Players.Single(p => p.Name == "Player2");
        }

        #endregion

        /// <summary>
        /// Skelett heißt die Zuschauer willkommen
        /// </summary>
        protected override async Task RunScene1(CancellationToken ct = default)
        {
            var sw = Stopwatch.StartNew();

            if ((IsScene1Played == true) && _settings.Application.TestOperation == false)
            {
                await _gpioController.LedsBlinkingAsync(5, 100);
                return;
            }
            else
            {
                _logger.LogInformation("Scene 1 started…");

                // Audio Player 1
                _logger.LogDebug("Audio Player 1: START");
                await _openhab.SendCommandAsync(_audioPlayer1.StreamItem, "http://192.168.20.28:9000/music/35402/download/Szene1.MP3");

                // Videoplayer 5
                _logger.LogDebug("Videoplayer 5: NEXT");
                await _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));

                // Videoplayer 5
                await WaitUntilAsync(sw, 60000, ct);
                _logger.LogDebug("Videoplayer 5: PAUSE");
                await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));

                // History
                IsScene1Played = true;
                await _gpioController.LedOffAsync(Led.ButtonTopCenter);
                await _gpioController.LedOnAsync(Led.ButtonBottomLeft);
                _logger.LogInformation("Szene 1 wurde beendet");
            }
        }

        /// <summary>
        /// Geistermädchen erschreckt die Zuschauer
        /// </summary>
        protected override async Task RunScene2(CancellationToken ct = default)
        {
            if ((IsScene2Played == true || IsScene1Played == false) && _settings.Application.TestOperation == false)
            {
                await _gpioController.LedsBlinkingAsync(5, 100);
                return;
            }
            else
            {
                _logger.LogInformation("Scene 2 started…");

                //// Audio Player 1
                //_logger.LogDebug("Audio Player 1: START");
                //await _openhab.SendCommandAsync(_audioPlayer1.StreamItem, "http://192.168.20.28:9000/music/35403/download/Szene2.MP3");
                //Thread.Sleep(400);

                //// Videoplayer 5
                //_logger.LogDebug("Videoplayer 5: NEXT");
                //_vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));

                //Thread.Sleep(65000);
                //// Videoplayer 5
                //_logger.LogDebug("Videoplayer 5: PAUSE");
                //await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));

                //// Strobo 3-4
                //_logger.LogDebug("Strobo 3-4: AN");
                //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "ON"); // AN
                //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "ON"); // AN
                //Thread.Sleep(4000);
                //_logger.LogDebug("Strobo 3-4: AUS");
                //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "OFF"); // AUS
                //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "OFF"); // AUS

                //// History
                //IsScene2Played = true;
                //await _gpioController.LedOffAsync(Led.ButtonBottomLeft);
                //await _gpioController.LedOnAsync(Led.ButtonBottomCenter);
                //_logger.LogInformation("Szene 2 wurde beendet");
            }
        }

        /// <summary>
        /// Ein Feuer bricht im Haus aus
        /// </summary>
        protected override async Task RunScene3(CancellationToken ct = default)
        {
                if ((IsScene3Played == true || IsScene2Played == false) && _settings.Application.TestOperation == false)
                {
                    await _gpioController.LedsBlinkingAsync(5, 100);
                    return;
                }
                else
                {
                    _logger.LogInformation("Scene 3 started…");

                //    // Audio Player 1
                //    _logger.LogDebug("Audio Player 1: START");
                //    await _openhab.SendCommandAsync(_audioPlayer1.StreamItem, "http://192.168.20.28:9000/music/35405/download/Szene3.MP3");
                //    Thread.Sleep(400);

                //    // Videoplayer 1-5
                //    _logger.LogDebug("Videoplayer 1-5: NEXT");
                //    _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                //    _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                //    _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                //    _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));
                //    _vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));
                //    Thread.Sleep(30000);

                //    // Nebelmaschine 2-4
                //    _logger.LogDebug("Nebelmaschine 2-4: AN");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "ON"); // AN
                //    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "ON"); // AN
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "ON"); // AN
                //    Thread.Sleep(5000);

                //    // RGB Strahler 1 (rot)
                //    _logger.LogDebug("RGB Strahler 1: AN");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight2/POWER", "ON"); // AN
                //    Thread.Sleep(5000);

                //    // RGB Strahler 2-3 (rot)
                //    _logger.LogDebug("RGB Strahler 2-3: AN");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight1/POWER", "ON"); // AN
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight3/POWER", "ON"); // AN
                //    Thread.Sleep(16000);

                //    // Beacon Controller 1
                //    _logger.LogDebug("Beacon Controller 1: AN");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER1", "ON"); // AN
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER2", "ON"); // AN
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER3", "ON"); // AN
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER4", "ON"); // AN
                //    Thread.Sleep(26000);

                //    // Nebelmaschine 2-4
                //    _logger.LogDebug("Nebelmaschine 2-4: AUS");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "OFF"); // AUS
                //    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "OFF"); // AUS

                //    // RGB Strahler 4-5 (blau)
                //    _logger.LogDebug("RGB Strahler 4-5: AN");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight4/POWER", "ON"); // AN
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight5/POWER", "ON"); // AN
                //    Thread.Sleep(5000);

                //    // RGB Strahler 1-3 (rot)
                //    _logger.LogDebug("RGB Strahler 1-3: AUS");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight1/POWER", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight2/POWER", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight3/POWER", "OFF"); // AUS
                //    Thread.Sleep(3000);

                //    // Beacon Controller 1
                //    _logger.LogDebug("Beacon Controller 1: AUS");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER1", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER2", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER3", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER4", "OFF"); // AUS
                //    Thread.Sleep(10000);

                //    // RGB Strahler 4-5 (blau)
                //    _logger.LogDebug("RGB Strahler 4-5: AUS");
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight4/POWER", "OFF"); // AUS
                //    await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight5/POWER", "OFF"); // AUS
                //    Thread.Sleep(10000);

                //    // Videoplayer 5
                //    _logger.LogDebug("Videoplayer 1-5: PAUSE");
                //    await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                //    await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                //    await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                //    await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));
                //    await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));

                //    // History
                //    IsScene3Played = true;
                //    await _gpioController.LedOffAsync(Led.ButtonBottomCenter);
                //    await _gpioController.LedOnAsync(Led.ButtonBottomRight);
                //    _logger.LogInformation("Szene 3 wurde beendet");
                }
        }

        /// <summary>
        /// Eine Band spielt Michael Jackson - Thriller
        /// </summary>
        protected override async Task RunScene4(CancellationToken ct = default)
        {
                if ((IsScene4Played == true || IsScene3Played == false) && _settings.Application.TestOperation == false)
                {
                    await _gpioController.LedsBlinkingAsync(5, 100);
                return;
                }
                else
                {
                    _logger.LogInformation("Scene 4 started…");

                    //// Audio Player 1
                    //_logger.LogDebug("Audio Player 1: START");
                    //await _openhab.SendCommandAsync(_audioPlayer1.StreamItem, "http://192.168.20.28:9000/music/35404/download/Szene4.MP3");
                    //Thread.Sleep(400);

                    //// Videoplayer 1-5
                    //_logger.LogDebug("Videoplayer 1-5: NEXT");
                    //_vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    //_vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    //_vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    //_vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));
                    //_vlc.SendCommandAsync(VlcPlayerCommand.NEXT, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));
                    //Thread.Sleep(11000);

                    //// Strobo 3-4
                    //_logger.LogDebug("Strobo 3-4: AN");
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "ON"); // AN
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "ON"); // AN
                    //Thread.Sleep(2000);
                    //_logger.LogDebug("Strobo 3-4: AUS");
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "OFF"); // AUS
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "OFF"); // AUS
                    //Thread.Sleep(10000);

                    //// Nebelmaschine 2
                    //_logger.LogDebug("Nebelmaschine 2: AN");
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "ON"); // AN
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "ON"); // AN
                    //Thread.Sleep(14000);

                    //// RGB Strobo 1-2
                    //_logger.LogDebug("RGB Strobo 1-2: AN");
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo1/POWER", "ON"); // AN
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo2/POWER", "ON"); // AN
                    //Thread.Sleep(93000);

                    //// RGB Strobo 1-2
                    //_logger.LogDebug("RGB Strobo 1-2: AUS");
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo1/POWER", "OFF"); // AUS
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo2/POWER", "OFF"); // AUS

                    //// Nebelmaschine 2
                    //_logger.LogDebug("Nebelmaschine 2: AUS");
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "OFF"); // AUS
                    //await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "OFF"); // AUS
                    //Thread.Sleep(60000);

                    //// Videoplayer 5
                    //_logger.LogDebug("Videoplayer 1-5: PAUSE");
                    //await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    //await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    //await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    //await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));
                    //await _vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, _settings.VLC.Players.First(p => p.Name == "Mediaplayer5"));

                    //// Reset Buttonboard
                    //await ResetAsync();
                    //await _gpioController.LedOnAsync(Led.ButtonBottomRight);
                    //_logger.LogInformation("Szene 4 wurde beendet");
                }
        }

        public override async Task SetupAsync(CancellationToken ct = default)
        {
            // Basic Setup
            await base.SetupAsync(ct);

            // Audio
            await _openhab.SendCommandAsync(_audioPlayer1.VolumeItem, _audioPlayer1.Volume.ToString());
            //await _openhab.SendCommandAsync(_audioPlayer2.VolumeItem, _audioPlayer2.Volume.ToString());
        }

        public override async Task ResetAsync(CancellationToken ct = default)
        {
            // Basic Reset
            await base.ResetAsync(ct);

            // Nebelmaschine
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine1/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "OFF"); // Aus

            // Strobo
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo1/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo2/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "OFF"); // Aus

            // RGB Light
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight1/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight2/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight3/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight4/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight5/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight6/POWER", "OFF"); // Aus

            // Rundumleuchten
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER1", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER2", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER3", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/beaconcontroller1/POWER4", "OFF"); // Aus

            // RGB Strobo
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo1/POWER", "OFF"); // Aus
            await _mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo2/POWER", "OFF"); // Aus
        }

        private static async Task WaitUntilAsync(Stopwatch sw, int dueMs, CancellationToken ct = default)
        {
            var delay = dueMs - (int)sw.ElapsedMilliseconds;
            if (delay > 0)
                await Task.Delay(delay, ct);
        }

    }
}
