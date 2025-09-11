//using BSolutions.Buttonboard.Services.Enumerations;
//using BSolutions.Buttonboard.Services.MqttClients;
//using BSolutions.Buttonboard.Services.RestApiClients;
//using BSolutions.Buttonboard.Services.Settings;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Device.Gpio;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace BSolutions.Buttonboard.Scenario.Murder
//{
//    public class MurderScenario : ScenarioBase
//    {
//        private const string ITEM_AUDIO_STREAM = "OU_Garage_Audio_StreamURL";
//        private const string ITEM_AUDIO_VOLUME = "OU_Garage_Audio_Volume";
//        private const string ITEM_AUDIO_CONTROL = "OU_Garage_Audio_Control";
//        private const string ITEM_RGBSTRAHLER_1 = "Show_Licht_RGBStrahler1_Schalten";
//        private const string ITEM_RGBSTRAHLER_2 = "Show_Licht_RGBStrahler2_Schalten";
//        private const string ITEM_RGBSTRAHLER_3 = "Show_Licht_RGBStrahler3_Schalten";
//        private const string ITEM_RGBSTRAHLER_5 = "Show_Licht_RGBStrahler5_Schalten";
//        private const string ITEM_RGBSTRAHLER_6 = "Show_Licht_RGBStrahler6_Schalten";
//        private const string ITEM_MAINSTRAHLER = "OU_Garage_Verbraucher_Werkstatt_Werkbank";
//        private const string ITEM_STROBO_1 = "Show_Licht_Strobo1_Schalten";
//        private const string ITEM_STROBO_2 = "Show_Licht_Strobo2_Schalten";
//        private const string ITEM_RGBSTROBO_1 = "Show_Licht_RGBStrobo1_Schalten";
//        private const string ITEM_RGBSTROBO_2 = "Show_Licht_RGBStrobo2_Schalten";
//        private const string ITEM_FOGMACHINE_1 = "Show_Nebel_Nebelmaschine1_Schalten";
//        private const string ITEM_FOGMACHINE_2 = "Show_Nebel_Nebelmaschine2_Schalten";
//        private const string ITEM_BEAMER_1_STREAM = "Show_Media_Beamer1_URIabspielen";
//        private const string ITEM_BEAMER_1_TASTE = "Show_Media_Beamer1_Tastendruck";

//        private bool _scene2WasPlayed = false;
//        private bool _scene3WasPlayed = false;
//        private bool _scene4WasPlayed = false;

//        #region --- Constructor ---

//        public MurderScenario(ILogger<MurderScenario> logger,
//            ISettingsProvider settingsProvider,
//            GpioController gpioController,
//            IOpenHabClient openhab,
//            IButtonboardMqttClient mqtt)
//            : base (logger, settingsProvider, gpioController, openhab, mqtt)
//        {

//        }

//        #endregion

//        protected override async Task RunScene1()
//        {
//            await Task.Run(async () =>
//            {
//                if ((this._scene2WasPlayed && this._scene3WasPlayed && this._scene4WasPlayed)
//                    || this._settings.Application.TestOperation)
//                {
//                    this._logger.LogInformation("Szene 1 wurde gestartet ...");

//                    // Licht ohne Mörder
//                    await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1402/download/Halloween%20Theme.mp3");
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.ON);
//                    Thread.Sleep(11000);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_5, OpenHabCommand.ON);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_6, OpenHabCommand.ON);
//                    Thread.Sleep(7000);
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.OFF);
                    
//                    Thread.Sleep(8000);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_5, OpenHabCommand.OFF);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_6, OpenHabCommand.OFF);
//                    Thread.Sleep(10000);
//                    await this._openhab.SendCommandAsync(ITEM_MAINSTRAHLER, OpenHabCommand.ON);
//                    Thread.Sleep(5000);
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_1, OpenHabCommand.ON);
//                    Thread.Sleep(30000);
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_1, OpenHabCommand.OFF);
//                    await this._openhab.SendCommandAsync(ITEM_MAINSTRAHLER, OpenHabCommand.OFF);
//                    Thread.Sleep(7000);

//                    // Dankeschön
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.ON);
//                    await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1403/download/Michael%20Jackson%20-%20Thriller.mp3");
//                    Thread.Sleep(19000);

//                    // Michael Jackson - Thriller
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.OFF);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTROBO_1, OpenHabCommand.ON);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTROBO_2, OpenHabCommand.ON);
//                    Thread.Sleep(321000);

//                    // ENDE
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTROBO_1, OpenHabCommand.OFF);
//                    await this._openhab.SendCommandAsync(ITEM_RGBSTROBO_2, OpenHabCommand.OFF);

//                    Thread.Sleep(3000);
//                    this._logger.LogInformation("Szene 1 wurde beendet.");
//                    await this.ResetAsync();
//                }
//                else
//                {
//                    await this.LedBlinkingAsync(10, 100);

//                    if(this._scene2WasPlayed)
//                    {
//                        this.Controller.Write(this.LedRed1Gpio, PinValue.High);
//                        this.Controller.Write(this.LedRed2Gpio, PinValue.High);
//                        this.Controller.Write(this.LedRed3Gpio, PinValue.High);
//                    }

//                    if (this._scene3WasPlayed)
//                    {
//                        this.Controller.Write(this.LedYellow1Gpio, PinValue.High);
//                        this.Controller.Write(this.LedYellow2Gpio, PinValue.High);
//                        this.Controller.Write(this.LedYellow3Gpio, PinValue.High);
//                    }

//                    if (this._scene4WasPlayed)
//                    {
//                        this.Controller.Write(this.LedGreen1Gpio, PinValue.High);
//                        this.Controller.Write(this.LedGreen2Gpio, PinValue.High);
//                        this.Controller.Write(this.LedGreen3Gpio, PinValue.High);
//                    }
//                }
//            });
//        }

//        protected override Task RunScene2()
//        {
//            return Task.Run(async () =>
//            {
//                this._logger.LogInformation("Szene 2 wurde gestartet ...");

//                // Nebelmaschine
//                if (!this._scene2WasPlayed)
//                {
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.ON);
//                    Thread.Sleep(18000);
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.OFF);
//                }

//                // Clown sagt "Willkommen
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.ON);
//                Thread.Sleep(3000);
//                await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1393/download/Szene%201%20-%20Clown.mp3");
//                Thread.Sleep(4000);
                
//                Thread.Sleep(54000);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.OFF);

//                // Mörder läuft durchs Büro
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_TASTE, "Back");
//                Thread.Sleep(500);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/videos/NS_GruesomeGang_Shad_Win_V.mp4");
//                Thread.Sleep(25000);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/pictures/Blackscreen.png");

//                // Clown lacht und warnt
//                await this._openhab.SendCommandAsync(ITEM_STROBO_1, OpenHabCommand.ON);
//                Thread.Sleep(6000);
//                await this._openhab.SendCommandAsync(ITEM_STROBO_1, OpenHabCommand.OFF);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.ON);
//                Thread.Sleep(16000);

//                // Ende der Szene 2
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.OFF);
//                await this._openhab.SendCommandAsync(ITEM_STROBO_1, OpenHabCommand.OFF);

//                // LEDs einschalten
//                Thread.Sleep(3000);
//                this.Controller.Write(this.LedRed1Gpio, PinValue.High);
//                this.Controller.Write(this.LedRed2Gpio, PinValue.High);
//                this.Controller.Write(this.LedRed3Gpio, PinValue.High);

//                this._scene2WasPlayed = true;
//                this._logger.LogInformation("Szene 2 wurde beendet.");
//            });
//        }

//        protected override Task RunScene3()
//        {
//            return Task.Run(async () =>
//            {
//                this._logger.LogInformation("Szene 3 wurde gestartet ...");

//                // Strahler einschalten
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_2, OpenHabCommand.ON);
//                Thread.Sleep(3000);

//                // Audio abspielen
//                await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1397/download/Szene%202%20-%20Frau%201.mp3");
//                Thread.Sleep(13000);

//                // Videosequenz abspielen
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_TASTE, "Back");
//                Thread.Sleep(100);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/videos/NS_MeatHook_Shad_Win_V.mp4");
//                Thread.Sleep(20000);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/pictures/Blackscreen.png");

//                // Audio abspielen
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_2, OpenHabCommand.OFF);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.ON);
//                Thread.Sleep(1000);
//                await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1398/download/Szene%202%20-%20Frau%202.mp3");

//                // Strobo einschalten
//                Thread.Sleep(5000);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.OFF);
//                await this._openhab.SendCommandAsync(ITEM_STROBO_2, OpenHabCommand.ON);
//                Thread.Sleep(11000);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.ON);
//                Thread.Sleep(10000);

//                await this._openhab.SendCommandAsync(ITEM_STROBO_2, OpenHabCommand.OFF);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.OFF);

//                // LEDs einschalten
//                Thread.Sleep(3000);
//                this.Controller.Write(this.LedYellow1Gpio, PinValue.High);
//                this.Controller.Write(this.LedYellow2Gpio, PinValue.High);
//                this.Controller.Write(this.LedYellow3Gpio, PinValue.High);

//                this._scene3WasPlayed = true;
//                this._logger.LogInformation("Szene 3 wurde beendet.");
//            });
//        }

//        protected override Task RunScene4()
//        {
//            return Task.Run(async () =>
//            {
//                this._logger.LogInformation("Szene 4 wurde gestartet ...");

//                // Nebelmaschine
//                if (!this._scene4WasPlayed) {
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.ON);
//                    Thread.Sleep(18000);
//                    await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.OFF);
//                }

//                // Frau in Tiaras Zimmer
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.ON);
//                await this._openhab.SendCommandAsync(ITEM_STROBO_2, OpenHabCommand.ON);
//                Thread.Sleep(1000);
//                await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1399/download/Szene%203%20-%20Frau%201.mp3");
//                Thread.Sleep(5000);
//                await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.OFF);
//                await this._openhab.SendCommandAsync(ITEM_STROBO_2, OpenHabCommand.OFF);
//                Thread.Sleep(4000);

//                // Frau im Hausflur
//                await this._openhab.SendCommandAsync("EG_Garderobe_Licht_Spots", "100");
//                Thread.Sleep(2000);
//                await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1400/download/Szene%203%20-%20Frau%202.mp3");
//                Thread.Sleep(5000);
//                await this._openhab.SendCommandAsync("EG_Garderobe_Licht_Spots", "0");
//                Thread.Sleep(3000);

//                // Frau wird im Büro verfolgt
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_TASTE, "Back");
//                Thread.Sleep(100);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/videos/Verfolgung.mp4");
//                Thread.Sleep(47000);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/pictures/Blackscreen.png");
//                Thread.Sleep(3000);

//                // Frau im Hausflur
//                await this._openhab.SendCommandAsync("EG_Garderobe_Licht_Spots", "100");
//                Thread.Sleep(2000);
//                await this._openhab.SendCommandAsync(ITEM_AUDIO_STREAM, "http://192.168.21.9:9000/music/1401/download/Szene%203%20-%20Scream.mp3");
//                Thread.Sleep(5000);
//                await this._openhab.SendCommandAsync("EG_Garderobe_Licht_Spots", "0");
//                Thread.Sleep(3000);

//                // Videosequenz abspielen (Ermorderung)
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_TASTE, "Back");
//                Thread.Sleep(100);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/videos/Ermorderung.mp4");
//                Thread.Sleep(19000);
//                await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/pictures/Blackscreen.png");

//                // LEDs einschalten
//                Thread.Sleep(3000);
//                this.Controller.Write(this.LedGreen1Gpio, PinValue.High);
//                this.Controller.Write(this.LedGreen2Gpio, PinValue.High);
//                this.Controller.Write(this.LedGreen3Gpio, PinValue.High);

//                this._scene4WasPlayed = true;
//                this._logger.LogInformation("Szene 4 wurde beendet.");
//            });
//        }

//        public override async Task ResetAsync()
//        {
//            await base.ResetAsync();

//            // Audio
//            await this._openhab.SendCommandAsync(ITEM_AUDIO_VOLUME, this._settings.Audio.Volume.ToString());

//            // Beamer
//            await this._openhab.SendCommandAsync(ITEM_BEAMER_1_STREAM, "/storage/pictures/Blackscreen.png");

//            // Strahler
//            await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_1, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_2, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_3, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_5, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_RGBSTRAHLER_6, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_MAINSTRAHLER, OpenHabCommand.OFF);

//            // Strobo
//            await this._openhab.SendCommandAsync(ITEM_STROBO_1, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_STROBO_2, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_RGBSTROBO_1, OpenHabCommand.OFF);
//            await this._openhab.SendCommandAsync(ITEM_RGBSTROBO_2, OpenHabCommand.OFF);

//            // Nebelmaschine
//            await this._openhab.SendCommandAsync(ITEM_FOGMACHINE_2, OpenHabCommand.OFF);

//            this._scene2WasPlayed = false;
//            this._scene3WasPlayed = false;
//            this._scene4WasPlayed = false;
//        }
//    }
//}
