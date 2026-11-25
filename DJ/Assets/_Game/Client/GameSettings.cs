using DeckRogue.Core.Save;
using UnityEngine;

namespace DeckRogue.Client
{
    /// <summary>
    /// Runtime mirror of <see cref="SettingsData"/> for booth / hall consumers.
    /// <see cref="ReducedFlashing"/> is the flag PixelClubView / BoothHud can read without
    /// holding a save service reference.
    /// </summary>
    public static class GameSettings
    {
        public static float MusicVolume { get; private set; } = SettingsData.DefaultMusicVolume;
        public static float SfxVolume { get; private set; } = SettingsData.DefaultSfxVolume;
        public static float UiScale { get; private set; } = SettingsData.DefaultUiScale;
        public static bool ReducedFlashing { get; private set; }
        public static string Language { get; private set; } = SettingsData.DefaultLanguage;
        public static bool TutorialCompleted { get; private set; }
        public static bool Fullscreen { get; private set; } = true;
        public static int ScreenWidth { get; private set; }
        public static int ScreenHeight { get; private set; }

        /// <summary>Apply saved display mode as early as possible so Boot opens fullscreen.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedDisplayEarly()
        {
            GameSaveService save = GameSaveService.Ensure();
            if (save.Document != null && save.Document.Settings != null)
                ApplyFrom(save.Document.Settings);
            else
                ApplyDisplay(true, 0, 0);
        }

        public static void ApplyFrom(SettingsData settings)
        {
            if (settings == null)
                settings = SettingsData.CreateDefault();
            settings.Normalize();

            MusicVolume = settings.MusicVolume;
            SfxVolume = settings.SfxVolume;
            UiScale = settings.UiScale;
            ReducedFlashing = settings.ReducedFlashing;
            Language = string.IsNullOrEmpty(settings.Language)
                ? SettingsData.DefaultLanguage
                : settings.Language;
            TutorialCompleted = settings.TutorialCompleted;
            Fullscreen = settings.Fullscreen;
            ScreenWidth = settings.ScreenWidth;
            ScreenHeight = settings.ScreenHeight;

            // Act1 has no separate SFX bus yet — both map to master listener volume (music wins).
            AudioListener.volume = MusicVolume;
            ApplyDisplay(Fullscreen, ScreenWidth, ScreenHeight);
        }

        /// <summary>
        /// Push resolution / fullscreen to the OS window.
        /// Width/height 0 → native desktop size.
        /// </summary>
        public static void ApplyDisplay(bool fullscreen, int width, int height)
        {
            if (Application.isBatchMode)
                return;

            ResolveSize(ref width, ref height);
            Fullscreen = fullscreen;
            ScreenWidth = width;
            ScreenHeight = height;

            FullScreenMode mode = fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;

            if (Screen.width == width
                && Screen.height == height
                && Screen.fullScreenMode == mode)
                return;

            Screen.SetResolution(width, height, mode);
        }

        public static void ResolveSize(ref int width, ref int height)
        {
            if (width > 0 && height > 0)
                return;

            Resolution native = Screen.currentResolution;
            if (native.width > 0 && native.height > 0)
            {
                width = native.width;
                height = native.height;
                return;
            }

            width = Mathf.Max(1280, Display.main.systemWidth);
            height = Mathf.Max(720, Display.main.systemHeight);
        }

        /// <summary>Unique desktop modes (width×height), highest refresh preferred per size.</summary>
        public static Resolution[] ListUniqueResolutions()
        {
            Resolution[] all = Screen.resolutions;
            if (all == null || all.Length == 0)
            {
                int w = 0;
                int h = 0;
                ResolveSize(ref w, ref h);
                return new[] { new Resolution { width = w, height = h, refreshRateRatio = Screen.currentResolution.refreshRateRatio } };
            }

            var unique = new System.Collections.Generic.List<Resolution>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                Resolution candidate = all[i];
                if (candidate.width < 640 || candidate.height < 480)
                    continue;

                int existing = -1;
                for (int j = 0; j < unique.Count; j++)
                {
                    if (unique[j].width == candidate.width && unique[j].height == candidate.height)
                    {
                        existing = j;
                        break;
                    }
                }

                if (existing < 0)
                {
                    unique.Add(candidate);
                    continue;
                }

                if (RefreshHz(candidate) > RefreshHz(unique[existing]))
                    unique[existing] = candidate;
            }

            if (unique.Count == 0)
            {
                int w = 0;
                int h = 0;
                ResolveSize(ref w, ref h);
                unique.Add(new Resolution { width = w, height = h, refreshRateRatio = Screen.currentResolution.refreshRateRatio });
            }

            unique.Sort((a, b) =>
            {
                int area = (a.width * a.height).CompareTo(b.width * b.height);
                return area != 0 ? area : a.width.CompareTo(b.width);
            });
            return unique.ToArray();
        }

        private static double RefreshHz(Resolution resolution)
        {
            try
            {
                return resolution.refreshRateRatio.value;
            }
            catch
            {
                return 0d;
            }
        }
    }
}
