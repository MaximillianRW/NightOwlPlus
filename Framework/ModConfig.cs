namespace NightOwlPlus.Framework
{
    /// <summary>The mod configuration.</summary>
    internal class ModConfig


    {
        /// <summary>Whether to use the internal fish asset editor.</summary>
        public bool EditFishTimes { get; set; } = true;

        /// <summary>Wheter should take care of late night light.</summary>
        public bool TakeCareOfLateNightLight { get; set; } = true;

        /// <summary>Wheter should take care of late night music.</summary>
        public bool TakeCareOfLateNightMusic { get; set; } = true;

        /// <summary>Whether disable the time shaking warn at late time.</summary>
        public bool NoClockShake { get; set; } = true;

        /// <summary>Whether to restore the player's position on new day.</summary>
        public bool RestorePosition { get; set; } = true;

        /// <summary>Whether to raise exhaustion on turning day without sleep</summary>
        public bool GetExhausted { get; set; } = true;

        /// <summary>Whether to restore stamina as base game on new day.</summary>
        public bool CustomStaminaRestoration { get; set; } = true;

        /// <summary>Whether to restore health as base game on new day.</summary>
        public bool CustomHealthRestoration { get; set; } = true;

        /// <summary>How many hours of rest are necessary to full replenish energy and health.</summary>
        public int HoursToFullRest { get; set; } = 6;
    }
}
