using BSolutions.Buttonboard.Services.Enumerations;
using BSolutions.Buttonboard.Services.Gpio;
using BSolutions.Buttonboard.Services.MqttClients;
using BSolutions.Buttonboard.Services.RestApiClients;
using BSolutions.Buttonboard.Services.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSolutions.Buttonboard.Scenario.MichaelMyers
{
    public class MichaelMyersScenario : ScenarioBase
    {
        private readonly AudioPlayer _audioPlayer;

        #region --- Constructor ---

        public MichaelMyersScenario(ILogger<MichaelMyersScenario> logger,
            ISettingsProvider settingsProvider,
            IButtonboardGpioController gpioController,
            IOpenHabClient openhab,
            IVlcPlayerClient vlc,
            IButtonboardMqttClient mqtt)
            : base(logger, settingsProvider, gpioController, openhab, vlc, mqtt)
        {
            this._audioPlayer = this._settings.OpenHAB.Audio.Players.Single(p => p.Name == "Player1");
        }

        #endregion

        /// <summary>
        /// Intro und Spielregeln
        /// </summary>
        protected override Task RunScene1()
        {
            return Task.Run(async () =>
            {
                if (this.IsScene1Played == true)
                {
                    await this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 1 wurde gestartet ...");

                    /******************************************************/
                    /* Clown begrüßt die Zuschauer und erklärt die Regeln */
                    /******************************************************/

                    // Bodenstrahler
                    this._logger.LogInformation("LICHT (Bodenstrahler): Aus");
                    await this._openhab.SendCommandAsync("OU_Garage_Licht_Bodenstrahler", OpenHabCommand.OFF); // Aus

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Ein");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFB04F\",\"DataLSB\":\"0xFF0DF2\",\"Repeat\":0}"); // Farbe: Feuer violett
                    Thread.Sleep(2000);

                    // Audio Player 1
                    this._logger.LogInformation("AUDIO: 'Intro und Anleitung'");
                    await this._openhab.SendCommandAsync(this._audioPlayer.StreamItem, "http://192.168.20.28:9000/music/3209/download/Szene%201%20-%20Intro.mp3");
                    Thread.Sleep(55500);

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Aus");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus

                    // Strobo 1
                    this._logger.LogInformation("LICHT (Strobo 1): Ein");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo1/POWER", "ON"); // An
                    Thread.Sleep(5000);
                    this._logger.LogInformation("LICHT (Strobo 1): Aus");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo1/POWER", "OFF"); // Aus

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Ein");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    Thread.Sleep(12000);
                    this._logger.LogInformation("LICHT (RGB Light 1): Aus");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus

                    // History
                    this.IsScene1Played = true;
                    await this._gpioController.LedOffAsync(Led.ButtonTopCenter);
                    await this._gpioController.LedOnAsync(Led.ButtonBottomLeft);
                    this._logger.LogInformation("Szene 1 wurde beendet");
                }
            });
        }

        protected override Task RunScene2()
        {
            return Task.Run(async () =>
            {
                if ((this.IsScene2Played == true || this.IsScene1Played == false) && this._settings.Application.TestOperation == false)
                {
                    await this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 2 wurde gestartet ...");

                    /**************************************************/
                    /* Clown erzählt die Geschichte von Michael Myers */
                    /**************************************************/

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Ein");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFF9867\",\"DataLSB\":\"0xFF19E6\",\"Repeat\":0}"); // Farbe: Feuer türkis
                    Thread.Sleep(2000);

                    // Audio Player 1
                    this._logger.LogInformation("AUDIO: 'Geschichte und Überfall von Michael Myers'");
                    await this._openhab.SendCommandAsync(this._audioPlayer.StreamItem, "http://192.168.20.28:9000/music/3210/download/Szene%202%20-%20%C3%83%C2%9Cberfall.mp3");
                    Thread.Sleep(35000);

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Aus");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus
                    Thread.Sleep(1000);

                    // Videoplayer 1: Silhouette von Michael auf der Hauswand
                    this._logger.LogInformation("VIDEO (Videoplayer 1): Starte 'Silhouette von Michael Myers'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    Thread.Sleep(17000);
                    this._logger.LogInformation("VIDEO (Videoplayer 1): Stoppe 'Silhouette von Michael Myers'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    Thread.Sleep(3000);

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Ein");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    Thread.Sleep(10000);

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Aus");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus
                    Thread.Sleep(23000);

                    // Videoplayer 2 - 4: Kampf im Haus
                    this._logger.LogInformation("VIDEO (Videoplayer 2-4): Starte 'Kampf im Haus'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));
                    Thread.Sleep(50000);
                    this._logger.LogInformation("VIDEO (Videoplayer 2-4): Stoppe 'Kampf im Haus'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));

                    // History
                    this.IsScene2Played = true;
                    await this._gpioController.LedOffAsync(Led.ButtonBottomLeft);
                    await this._gpioController.LedOnAsync(Led.ButtonBottomCenter);
                    this._logger.LogInformation("Szene 2 wurde beendet");
                }
            });
        }

        protected override Task RunScene3()
        {
            return Task.Run(async () =>
            {
                if ((this.IsScene3Played == true || this.IsScene2Played == false) && this._settings.Application.TestOperation == false)
                {
                    await this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 3 wurde gestartet ...");

                    /*************************************************/
                    /* Michael geht in die Falle und das Haus brennt */
                    /*************************************************/

                    // Buttonboard: Michael geht in die Falle
                    this._logger.LogInformation("VIDEO (Buttonboard): Starte 'Michael geht in die Falle'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Buttonboard"));
                    Thread.Sleep(68000);

                    // Videoplayer 1 - 4: Feuer
                    this._logger.LogInformation("VIDEO (Videoplayer 1-4): Starte 'Feuersimulation'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));

                    // Nebelmaschine 1, 3, 4
                    this._logger.LogInformation("NEBEL (Maschine 1, 3, 4): Einschalten");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine1/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "ON"); // An

                    // Audio Player 1
                    this._logger.LogInformation("AUDIO: 'Feuer und Aufforderung zum Löschen'");
                    await this._openhab.SendCommandAsync(this._audioPlayer.StreamItem, "http://192.168.20.28:9000/music/3211/download/Szene%203%20-%20Feuer.mp3");
                    Thread.Sleep(11000);

                    // Buttonboard
                    this._logger.LogInformation("VIDEO (Buttonboard): Stoppe 'Michael geht in die Falle'");
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Buttonboard"));

                    // RGB Light 2 - 5
                    this._logger.LogInformation("LICHT (RGB Light 2-5): Einschalten");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight2/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight3/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight4/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight5/POWER", "ON"); // An
                    Thread.Sleep(21000);

                    // Nebelmaschine 2
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "ON"); // An
                    Thread.Sleep(20000);

                    // RGB Light 1
                    this._logger.LogInformation("LICHT (RGB Light 1): Einschalten");
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFF6897\",\"DataLSB\":\"0xFF16E9\",\"Repeat\":0}"); // Farbe: Feuer rot
                    Thread.Sleep(19000);

                    // Videplayer 1 - 4
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.NEXT, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));
                    Thread.Sleep(2000);
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer1"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer2"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer3"));
                    await this._vlc.SendCommandAsync(VlcPlayerCommand.PAUSE, this._settings.VLC.Players.First(p => p.Name == "Mediaplayer4"));

                    // Nebelmaschine 1 - 4
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine1/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "OFF"); // Aus
                    Thread.Sleep(20000);

                    // RGB Light 1
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus

                    // History
                    this.IsScene3Played = true;
                    await this._gpioController.LedOffAsync(Led.ButtonBottomCenter);
                    await this._gpioController.LedOnAsync(Led.ButtonBottomRight);
                    this._logger.LogInformation("Szene 3 wurde beendet");
                }
            });
        }

        protected override Task RunScene4()
        {
            return Task.Run(async () =>
            {
                if ((this.IsScene4Played == true || this.IsScene3Played == false) && this._settings.Application.TestOperation == false)
                {
                    await this._gpioController.LedsBlinkingAsync(5, 100);
                }
                else
                {
                    this._logger.LogInformation("Szene 4 wurde gestartet ...");

                    /**************************************/
                    /* Feuer wird gelöscht, Michael lebt! */
                    /**************************************/

                    // Audio Player 1
                    await this._openhab.SendCommandAsync(this._audioPlayer.StreamItem, "http://192.168.20.28:9000/music/3212/download/Szene%204%20-%20L%C3%83%C2%B6schung.mp3");
                    Thread.Sleep(11000);

                    // RGB Light 2 - 6
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight2/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight3/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight4/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight5/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight6/POWER", "ON"); // An
                    Thread.Sleep(11000);

                    // RGB Light 1
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    Thread.Sleep(6000);
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus

                    // Strobo 1
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo1/POWER", "ON"); // An
                    Thread.Sleep(5000);
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo1/POWER", "OFF"); // Aus

                    // Nebelmaschine
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine1/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "ON"); // An
                    Thread.Sleep(20000);

                    /***********************/
                    /* Michael taucht auf! */
                    /***********************/

                    // Strobo 2 - 4
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo2/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "ON"); // An
                    Thread.Sleep(10000);

                    // Nebelmaschine
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine1/POWER", "OFF"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "OFF"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "OFF"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "OFF"); // An
                    Thread.Sleep(15000);

                    // RGB Light 6
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight6/POWER", "OFF"); // Aus

                    /***********************************/
                    /* Danke und bis zum nächsten Jahr */
                    /***********************************/

                    // Audio Player 1
                    await this._openhab.SendCommandAsync(this._audioPlayer.StreamItem, "http://192.168.20.28:9000/music/3213/download/Szene%204%20-%20Vielen%20Dank.mp3");

                    // RGB Light 1
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFE21D\",\"DataLSB\":\"0xFF47B8\",\"Repeat\":0}"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFB04F\",\"DataLSB\":\"0xFF0DF2\",\"Repeat\":0}"); // Farbe: Feuer violett
                    Thread.Sleep(20000);
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus

                    // Bodenstrahler
                    await this._openhab.SendCommandAsync("OU_Garage_Licht_Bodenstrahler", OpenHabCommand.ON); // An

                    // RGB Strobo 1 - 2
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo1/POWER", "ON"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo2/POWER", "ON"); // An
                    Thread.Sleep(19000);

                    // Strobo 2 - 4
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo2/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "OFF"); // Aus
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "OFF"); // Aus
                    Thread.Sleep(300000);

                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo1/POWER", "OFF"); // An
                    await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo2/POWER", "OFF"); // An

                    // Reset Buttonboard
                    await this.ResetAsync();
                    this._logger.LogInformation("Szene 4 wurde beendet");
                }
            });
        }

        public override async Task SetupAsync()
        {
            // Basic Setup
            await base.SetupAsync();

            // Audio
            await this._openhab.SendCommandAsync(this._audioPlayer.VolumeItem, this._audioPlayer.Volume.ToString());
        }

        public override async Task ResetAsync()
        {
            // Basic Reset
            await base.ResetAsync();

            // Nebelmaschine
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine1/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine2/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine3/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/fogmachine4/POWER", "OFF"); // Aus

            // Strobo
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo1/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo2/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo3/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/strobo4/POWER", "OFF"); // Aus

            // RGB Light
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight1/POWER", "ON"); // An
            Thread.Sleep(1000);
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/irremote1/IRSend", "{\"Protocol\":\"NEC\",\"Bits\":32,\"Data\":\"0xFFC23D\",\"DataLSB\":\"0xFF43BC\",\"Repeat\":0}"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight2/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight3/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight4/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight5/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgblight6/POWER", "OFF"); // Aus

            // RGB Strobo
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo1/POWER", "OFF"); // Aus
            await this._mqtt.PublishAsync("cmnd/bremus/entertainment/rgbstrobo2/POWER", "OFF"); // Aus

            // Bodenstrahler
            await this._openhab.SendCommandAsync("OU_Garage_Licht_Bodenstrahler", OpenHabCommand.ON); // An
        }
    }
}
