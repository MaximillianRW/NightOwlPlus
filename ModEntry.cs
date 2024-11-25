using System.Security.AccessControl;
using HarmonyLib;
using Microsoft.Xna.Framework;
using NightOwlPlus.Framework;
using NightOwlPlus.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Menus;
using StardewValley.Extensions;
using StardewValley.GameData;
using System.Reflection;

// TODO:
namespace NightOwlPlus
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {

        /*********
        ** Static Fields
        *********/
        /// <summary>
        /// Events that are handled after the player has warped after they have stayed up late.
        /// </summary>
        public static Dictionary<string, Func<bool>> PostWarpCharacter = new Dictionary<string, Func<bool>>();
        /// <summary>
        /// Events that are handled when the player has stayed up late and are going to collapse.
        /// </summary>
        public static Dictionary<string, Func<bool>> OnPlayerStayingUpLate = new Dictionary<string, Func<bool>>();

        public static string ModDataKeyID = "RestoreData";

        /*********
        ** Fields
        *********/
        /// <summary>The mod configuration.</summary>
        private ModConfig Config;

        public static IMonitor M;

        /****
        ** Context
        ****/

        /// <summary>Whether a new day has started.</summary>
        private bool isNewDay;

        public int fullrestHour;

        public int restTime;

        /****
        ** Pre-collapse state
        ****/

        public NOPlusData? Data;



        /*********
        ** Public methods
        *********/

        public sealed class NOPlusData
        {
            public int? Health = null;
            public float? Stamina = null;
            public bool isExhausted = false;

            public bool RestorePos = false;
            public string? Map = null;
            public Point Tile = new(0);
            public int? Facing = null;
            public bool IsBathing = false;
            public bool IsInSwimSuit = false;
            public ISittable? Sittable = null;
            public Horse? Horse = null;
        }

        public override void Entry(IModHelper helper)
        {
            M = Monitor;
            Config = helper.ReadConfig<ModConfig>();
            Data = null;
            fullrestHour = (Config.HoursToFullRest > 24 ? 24 : (Config.HoursToFullRest < 0 ? 0 : Config.HoursToFullRest));
            restTime = (int)(300 - fullrestHour * 10) * 10;

            if (fullrestHour != Config.HoursToFullRest)
            {
                Config.HoursToFullRest = fullrestHour;
                helper.WriteConfig(Config);
            }

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted;
            helper.Events.GameLoop.TimeChanged += this.OnTimeChanged;
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
            helper.Events.GameLoop.Saving += this.OnSaving;

            if (Config.EditFishTimes)
                helper.Events.Content.AssetRequested += this.OnAssetRequested;

            if (Config.TakeCareOfLateNightMusic)
                helper.Events.GameLoop.UpdateTicking += this.OnUpdateTicking;

            if (Config.TakeCareOfLateNightLight)
            {
                var harmony = new Harmony(this.ModManifest.UniqueID);

                harmony.Patch(
                    original: AccessTools.Method(typeof(Game1), nameof(Game1.UpdateGameClock)),
                    postfix: new HarmonyMethod(typeof(UpdateGameClock), nameof(UpdateGameClock.Postfix))
                );

                harmony.Patch(
                    original: AccessTools.Method(typeof(Game1), nameof(Game1.isTimeToTurnOffLighting)),
                    postfix: new HarmonyMethod(typeof(isTimeToTurnOffLighting), nameof(isTimeToTurnOffLighting.Postfix))
                );

                MethodInfo? ual = typeof(GameLocation).GetMethod("_updateAmbientLighting",
        BindingFlags.NonPublic | BindingFlags.Instance);
                if (ual != null)
                {
                    harmony.Patch(
                        original: ual,
                        postfix: new HarmonyMethod(typeof(updateAmbientLighting), nameof(updateAmbientLighting.Postfix))
                    );
                }

            }

        }

        /*********
        ** Private methods
        *********/


        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Fish"))
            {
                e.Edit(asset =>
                { 
                    var data = asset.AsDictionary<string, string>().Data;
                    foreach (var (Key,Value) in data)
                    {
                        string[] ValueSplit = Value.Split('/');
                        string[] FishTimes = ValueSplit[5].Split(" ");
                        if (FishTimes.Last() == "2600")
                        {
                            if (FishTimes.First() == "600")
                                FishTimes[0] = "150";
                            else
                            {
                                int oldLength = FishTimes.Length;
                                string[] oldFishTimes = new string[oldLength];
                                FishTimes.CopyTo(oldFishTimes, 0);
                                Array.Resize(ref FishTimes, oldLength + 2);
                                FishTimes[0] = "150";
                                FishTimes[1] = "600";
                                oldFishTimes.CopyTo(FishTimes, 2);
                            }
                            ValueSplit[5] = string.Join(" ", FishTimes);
                            string NewValue = string.Join("/", ValueSplit);

                            data[Key] = NewValue;
                        }

                    }
                },
                AssetEditPriority.Late);
            }
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.fishtimes.label"),
                tooltip: () => Helper.Translation.Get("config.fishtimes.tooltip"),
                getValue: () => Config.EditFishTimes,
                setValue: value => Config.EditFishTimes = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.latenightlights.label"),
                tooltip: () => Helper.Translation.Get("config.latenightlights.tooltip"),
                getValue: () => Config.TakeCareOfLateNightLight,
                setValue: value => Config.TakeCareOfLateNightLight = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.latenightmusic.label"),
                tooltip: () => Helper.Translation.Get("config.latenightmusic.tooltip"),
                getValue: () => Config.TakeCareOfLateNightMusic,
                setValue: value => Config.TakeCareOfLateNightMusic = value
            );


            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.notimeshake.label"),
                tooltip: () => Helper.Translation.Get("config.notimeshake.tooltip"),
                getValue: () => Config.NoClockShake,
                setValue: value => Config.NoClockShake = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.restoreposition.label"),
                tooltip: () => Helper.Translation.Get("config.restoreposition.tooltip"),
                getValue: () => Config.RestorePosition,
                setValue: value => Config.RestorePosition = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.getexhausted.label"),
                tooltip: () => Helper.Translation.Get("config.getexhausted.tooltip"),
                getValue: () => Config.GetExhausted,
                setValue: value => Config.GetExhausted = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.customstamina.label"),
                tooltip: () => Helper.Translation.Get("config.customstamina.tooltip"),
                getValue: () => Config.CustomStaminaRestoration,
                setValue: value => Config.CustomStaminaRestoration = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.customhealth.label"),
                tooltip: () => Helper.Translation.Get("config.customhealth.tooltip"),
                getValue: () => Config.CustomHealthRestoration,
                setValue: value => Config.CustomHealthRestoration = value
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => Helper.Translation.Get("config.fullresthours.label"),
                tooltip: () => Helper.Translation.Get("config.fullresthours.tooltip"),
                max: 24,
                min: 0,
                getValue: () => Config.HoursToFullRest,
                setValue: value => Config.HoursToFullRest = value
            );
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("OnSaveLoaded Events Running");
            Data = this.Helper.Data.ReadSaveData<NOPlusData>($"{ModDataKeyID}");
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            Monitor.Log("OnDayStarted Events Running", LogLevel.Debug);
            try
            {
                if (Data != null)
                {
                    Monitor.Log("OnDayStarted: Restoring Farmer Data from last night");

                    // transition to the next day

                    if (Config.CustomStaminaRestoration && Data.Stamina != null)
                    {
                        Game1.player.stamina = (float)Data.Stamina;
                    }

                    if (Config.CustomHealthRestoration && Data.Health != null)
                    {
                        Game1.player.health = (int)Data.Health;
                    }

                    Game1.player.exhausted.Set(Data.isExhausted);

                    if (Data.RestorePos && !Game1.weddingToday)
                    {
                        Game1.fadeToBlackAlpha = 1f;
                        Game1.warpFarmer(Data.Map, Data.Tile.X, Data.Tile.Y, false);
                        Game1.player.faceDirection((int)Data.Facing);

                        foreach (var v in PostWarpCharacter)
                        {
                            v.Value.Invoke();
                        }

                        if (Data.IsInSwimSuit)
                        {
                            Game1.player.changeIntoSwimsuit();
                        }

                        if (Data.IsBathing)
                        {
                            Game1.player.swimming.Value = true;
                        }

                        if (Data.Sittable != null)
                        {
                            Data.Sittable.AddSittingFarmer(Game1.player);
                            Game1.player.sittingFurniture = Data.Sittable;
                            Game1.player.isSitting = new(true);
                        }

                        if (Data.Horse != null)
                        {
                            Game1.warpCharacter(Data.Horse, Game1.player.currentLocation.ToString(), Game1.player.position.Get());
                        }

                        Game1.fadeToBlackAlpha = 1.2f;
                    }
                    Data = null;
                }
            }
            catch (Exception ex)
            {
                Monitor.Log(ex.ToString(), LogLevel.Error);
                WriteErrorLog();
            }
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e)
        {

            if (Game1.dayTimeMoneyBox.timeShakeTimer != 0 && Config.NoClockShake)
            {
                Monitor.Log("Time shake supressed.");
                Game1.dayTimeMoneyBox.timeShakeTimer = 0;
            }

            // transition to next morning
            if (e.NewTime == 2550)
            {
                Game1.timeOfDay = 150;
                isNewDay = false;
                Monitor.Log("OnTimeChanged: isNewDay set to false", LogLevel.Debug);

            }

            // save & reset at 6am
            if (e.NewTime == 600 && !isNewDay)
            {
                Monitor.Log("OnTimeChanged: Saving and Reseting at 6am", LogLevel.Debug);

                try
                {
                    Data = new();

                    if (Game1.player.isRidingHorse())
                    {
                        foreach (var character in Game1.player.currentLocation.characters)
                        {
                            try
                            {
                                if (character is Horse)
                                {
                                    (character as Horse).dismount();
                                    if (Config.RestorePosition)
                                    {
                                        Data.Horse = character as Horse;
                                    }
                                }
                            }
                            catch { }
                        }
                    }

                    Data.Stamina = Game1.player.stamina;
                    Data.Health = Game1.player.health;
                    if (Config.GetExhausted)
                        Data.isExhausted = true;
                    else
                        Data.isExhausted = Game1.player.exhausted.Get();

                    if (Config.RestorePosition)
                    {
                        Data.RestorePos = true;
                        Data.Map = Game1.player.currentLocation.Name;
                        Data.Tile = Game1.player.TilePoint;
                        Data.Facing = Game1.player.FacingDirection;
                        Data.IsInSwimSuit = Game1.player.bathingClothes.Value;
                        Data.IsBathing = Game1.player.swimming.Value;
                        if (Game1.player.isSitting.Value)
                            Data.Sittable = Game1.player.sittingFurniture;
                    }

                    foreach (var v in OnPlayerStayingUpLate)
                    {
                        v.Value.Invoke();
                    }

                    NewDay();
                }
                catch (Exception ex)
                {
                    Monitor.Log(ex.ToString(), LogLevel.Error);
                    WriteErrorLog();
                }
            }
        }

        private void OnUpdateTicking(object sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (Game1.currentLocation is null)
                return;

            if (Game1.timeOfDay < 600 && Game1.currentSong.Name.Contains(Game1.currentSeason) && !Game1.currentSong.Name.Contains("ambient"))
            {
                if (Game1.getMusicTrackName(MusicContext.Default).StartsWith(Game1.currentSeason) && !Game1.getMusicTrackName(MusicContext.Default).Contains("ambient") && (!Game1.eventUp && Game1.timeOfDay < 600))
                    Game1.changeMusicTrack("none", true, MusicContext.Default);
                if (Game1.currentLocation != null && Game1.currentLocation.IsOutdoors && !Game1.isRaining && (!Game1.eventUp &&
                    Game1.getMusicTrackName(MusicContext.Default) != null && Game1.getMusicTrackName(MusicContext.Default).Contains("day")) && Game1.timeOfDay < 600)
                    Game1.changeMusicTrack("none", true, MusicContext.Default);
                Game1.currentLocation?.checkForMusic(Game1.currentGameTime);
            }
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            isNewDay = true;
            Monitor.Log("OnDayEnding Events Running", LogLevel.Debug);
            if (Data == null && Config.HoursToFullRest > 0 && (Config.CustomStaminaRestoration || Config.CustomHealthRestoration))
            {
                Monitor.Log("OnDayEnding: Calculating and Saving Farmer Stats", LogLevel.Debug);
                float sleepTime = (Game1.timeOfDay >= 600 ? Game1.timeOfDay : Game1.timeOfDay + 2400);
                float sleepHoursRatio = (3000 - sleepTime) / (fullrestHour * 100);

                if (sleepHoursRatio < 1)
                {
                    float CalcStamina = Game1.player.stamina + Game1.player.MaxStamina * sleepHoursRatio;
                    float CalcHealth = Game1.player.health + Game1.player.maxHealth * sleepHoursRatio;

                    if (Game1.player.exhausted.Get())
                    {
                        CalcStamina -= Game1.player.MaxStamina / 2;

                        if (CalcStamina < 1)
                            CalcStamina = 1;
                    }

                    if (CalcStamina < Game1.player.MaxStamina || CalcHealth < Game1.player.maxHealth)
                    {
                        Data = new()
                        {
                            Stamina = ((Config.CustomStaminaRestoration &&  CalcStamina < Game1.player.MaxStamina) ? (int)CalcStamina : null),
                            Health = ((Config.CustomHealthRestoration && CalcHealth < Game1.player.maxHealth) ? (int)CalcHealth : null)
                        };
                    }
                }
            }
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            Monitor.Log("OnSaving Events Running", LogLevel.Debug);
            this.Helper.Data.WriteSaveData($"{ModDataKeyID}", Data);
        }

        private static void NewDay()
        {
            M.Log("Start NewDay.", LogLevel.Debug);

            ReadyCheckDialog.behavior doNewDay = delegate
            {
                Game1.player.lastSleepLocation.Value = Game1.player.currentLocation.NameOrUniqueName;
                Game1.player.lastSleepPoint.Value = Game1.player.TilePoint;
                Game1.dialogueUp = false; // Close "Go to bed" dialogue to prevent stuck.
                Game1.currentMinigame = null;
                Game1.activeClickableMenu?.emergencyShutDown();
                Game1.activeClickableMenu = null;
                Game1.newDay = true;

                foreach (var v in OnPlayerStayingUpLate)
                {
                    v.Value.Invoke();
                }

                Game1.newDaySync = new NewDaySynchronizer();
                Game1.fadeScreenToBlack();
                Game1.fadeToBlackAlpha = 0f;
            };
            if (Game1.IsMultiplayer)
            {
                Game1.activeClickableMenu?.emergencyShutDown();
                Game1.activeClickableMenu = new ReadyCheckDialog("sleep", false, doNewDay);
            }
            else
            {
                doNewDay(null);
            }
        }

        private void WriteErrorLog()
        {
            var state = new
            {
                Config,
                isNewDay,
                Data.Map,
                Data.Tile,
                Data.Stamina,
                Data.Health,
            };
            Helper.Data.WriteJsonFile("Error_Logs/Mod_State.json", state);
        }

        /**
public static void updateLighting()
{
    Color indoorLightingColor = new(100, 120, 30);
    Color indoorLightingNightColor = new(150, 150, 30);

    if (!Game1.currentLocation.isOutdoors.Value || (bool)Game1.currentLocation.ignoreOutdoorLighting.Value)
    {
        if (Game1.timeOfDay < 600)
        {
            int time = Game1.timeOfDay + Game1.gameTimeInterval / (Game1.realMilliSecondsPerGameMinute);
            float lerp = 1f - Utility.Clamp((float)Utility.CalculateMinutesBetweenTimes(400, time) / 120f, 0f, 1f);
            Game1.ambientLight = new Color((byte)Utility.Lerp((int)indoorLightingColor.R, (int)indoorLightingNightColor.R, lerp), (byte)Utility.Lerp((int)indoorLightingColor.G, (int)indoorLightingNightColor.G, lerp), (byte)Utility.Lerp((int)indoorLightingColor.B, (int)indoorLightingNightColor.B, lerp));
        }
        else
        {
            Game1.ambientLight = indoorLightingColor;
        }
    }
    else
    {
        Game1.ambientLight = (Game1.currentLocation.IsRainingHere() ? new Color(255, 200, 80) : Color.White);
    }
}

private void onUpdateTicked(object? sender, UpdateTickedEventArgs e)
{
    if (!Context.IsWorldReady)
        return;

    if (Game1.currentLocation is null)
        return;

    updateLighting();
}

public override object GetApi()
{
    return new NightOwlPlusAPI();
}*/
    }
}
