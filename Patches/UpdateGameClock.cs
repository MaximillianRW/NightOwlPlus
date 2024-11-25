using Microsoft.Xna.Framework;
using StardewValley;
using StardewModdingAPI;

namespace NightOwlPlus.Patches
{
    class UpdateGameClock
    {

        public static void Postfix()
        {
            int getStartingToGetLitTime = 2800;
            int getTrulyLitTime = 3000;
            int getMidNight = (int)(Game1.getTrulyDarkTime(Game1.currentLocation) + getStartingToGetLitTime) / 2;

            int AlteredTimeofDay = (Game1.timeOfDay < 600 ? Game1.timeOfDay + 2400 : Game1.timeOfDay);

            try
            {
                if (AlteredTimeofDay >= getStartingToGetLitTime)
                {
                    int adjustedTime = (int)((float)(AlteredTimeofDay - AlteredTimeofDay % 100) + (float)(AlteredTimeofDay % 100 / 10) * 16.66f);
                    float transparency = Math.Min(0.93f, 0.3f + ((float)(getTrulyLitTime - adjustedTime) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) * 0.00225f);
                    Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency;
                }
                else if (AlteredTimeofDay >= getMidNight)
                {
                    int adjustedTime = (int)((float)(AlteredTimeofDay - AlteredTimeofDay % 100) + (float)(AlteredTimeofDay % 100 / 10) * 16.66f);
                    float transparency = Math.Min(0.93f, 0.75f + ((float)(getStartingToGetLitTime - adjustedTime) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) * 0.000625f);
                    Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency;
                }
                else if (AlteredTimeofDay >= Game1.getTrulyDarkTime(Game1.currentLocation))
                {
                    int adjustedTime = (int)((float)(AlteredTimeofDay - AlteredTimeofDay % 100) + (float)(AlteredTimeofDay % 100 / 10) * 16.66f);
                    float transparency = Math.Min(0.93f, 0.75f + ((float)(adjustedTime - Game1.getTrulyDarkTime(Game1.currentLocation)) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) * 0.000625f);
                    Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency;
                }
                else if (AlteredTimeofDay >= Game1.getStartingToGetDarkTime(Game1.currentLocation))
                {
                    int adjustedTime = (int)((float)(AlteredTimeofDay - AlteredTimeofDay % 100) + (float)(AlteredTimeofDay % 100 / 10) * 16.66f);
                    float transparency = Math.Min(0.93f, 0.3f + ((float)(adjustedTime - Game1.getStartingToGetDarkTime(Game1.currentLocation)) + (float)Game1.gameTimeInterval / (float)Game1.realMilliSecondsPerGameTenMinutes * 16.6f) * 0.00225f);
                    Game1.outdoorLight = (Game1.IsRainingHere() ? Game1.ambientLight : Game1.eveningColor) * transparency;
                }
                else if (Game1.IsRainingHere())
                {
                    Game1.outdoorLight = Game1.ambientLight * 0.3f;
                }
                else
                {
                    Game1.outdoorLight = Game1.ambientLight;
                }
            }
            catch (Exception ex)
            {
                ModEntry.M.Log($"Failed in {nameof(UpdateGameClock)}:\n{ex}", LogLevel.Error);
            }
        }
    }

    class isTimeToTurnOffLighting
    {
        public static void Postfix(GameLocation location, ref bool __result)
        {
            __result = ((Game1.timeOfDay >= Game1.getTrulyDarkTime(location) - 100) || (Game1.timeOfDay <= 300));
        }
    }
    
    class updateAmbientLighting
    {
        public static void Postfix()
        {
            Color indoorLightingColor = new(100,120,30);
            Color indoorLightingNightColor = new(150,150,30);
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
    }
}
