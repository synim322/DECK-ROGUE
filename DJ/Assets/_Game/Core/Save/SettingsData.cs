namespace DeckRogue.Core.Save
{
    /// <summary>
    /// Nested settings blob inside <see cref="SaveGameDocument"/>. No schemaVersion here —
    /// version lives on the parent document.
    /// </summary>
    public sealed class SettingsData
    {
        public const string DefaultLanguage = "en";
        public const float DefaultMusicVolume = 0.8f;
        public const float DefaultSfxVolume = 0.7f;
        public const float DefaultUiScale = 1.0f;

        /// <summary>0 = use native / current desktop resolution.</summary>
        public const int NativeResolutionSentinel = 0;

        public string Language { get; set; }
        public float MusicVolume { get; set; }
        public float SfxVolume { get; set; }
        public float UiScale { get; set; }
        public bool ReducedFlashing { get; set; }
        public bool TutorialCompleted { get; set; }

        /// <summary>
        /// First-club spotlight lesson finished or skipped. Separate from <see cref="TutorialCompleted"/>
        /// so players who already dismissed the old text pages still see the booth coach once.
        /// </summary>
        public bool BoothCoachCompleted { get; set; }

        /// <summary>Start and stay in fullscreen unless the player opts out.</summary>
        public bool Fullscreen { get; set; }

        /// <summary>Preferred width; 0 means native desktop resolution.</summary>
        public int ScreenWidth { get; set; }

        /// <summary>Preferred height; 0 means native desktop resolution.</summary>
        public int ScreenHeight { get; set; }

        public static SettingsData CreateDefault()
        {
            return new SettingsData
            {
                Language = DefaultLanguage,
                MusicVolume = DefaultMusicVolume,
                SfxVolume = DefaultSfxVolume,
                UiScale = DefaultUiScale,
                ReducedFlashing = false,
                TutorialCompleted = false,
                BoothCoachCompleted = false,
                Fullscreen = true,
                ScreenWidth = NativeResolutionSentinel,
                ScreenHeight = NativeResolutionSentinel
            };
        }

        /// <summary>Clamp volumes to 0..1 and uiScale to 0.75..1.5; empty language → default.</summary>
        public void Normalize()
        {
            if (string.IsNullOrEmpty(Language))
                Language = DefaultLanguage;

            MusicVolume = Clamp01(MusicVolume);
            SfxVolume = Clamp01(SfxVolume);
            UiScale = Clamp(UiScale, 0.75f, 1.5f);

            if (ScreenWidth < 0)
                ScreenWidth = NativeResolutionSentinel;
            if (ScreenHeight < 0)
                ScreenHeight = NativeResolutionSentinel;
            if (ScreenWidth > 0 && ScreenWidth < 640)
                ScreenWidth = 640;
            if (ScreenHeight > 0 && ScreenHeight < 480)
                ScreenHeight = 480;
            // One axis set without the other → treat as native.
            if ((ScreenWidth == 0) != (ScreenHeight == 0))
            {
                ScreenWidth = NativeResolutionSentinel;
                ScreenHeight = NativeResolutionSentinel;
            }
        }

        public SettingsData Clone()
        {
            return new SettingsData
            {
                Language = Language ?? DefaultLanguage,
                MusicVolume = MusicVolume,
                SfxVolume = SfxVolume,
                UiScale = UiScale,
                ReducedFlashing = ReducedFlashing,
                TutorialCompleted = TutorialCompleted,
                BoothCoachCompleted = BoothCoachCompleted,
                Fullscreen = Fullscreen,
                ScreenWidth = ScreenWidth,
                ScreenHeight = ScreenHeight
            };
        }

        private static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        private static float Clamp(float value, float min, float max)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return min;
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
