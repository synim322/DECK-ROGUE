using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>
    /// DJ-117 MainMenu: JRPG-premium club night plate, brand hero, Start Run → AudioSpike.
    /// Built at runtime. <see cref="Mount"/> is the single idempotent entry (router + first load).
    /// </summary>
    public static class MainMenuOverlay
    {
        private const string CanvasName = "MainMenuCanvas";

        /// <summary>
        /// Club-night menu palette (teal/cyan accent, no purple-AI default).
        /// Aligns with Doc/19 Neon Basement keys, shifted cooler for shell.
        /// </summary>
        private static class Palette
        {
            public static readonly Color VoidInk = Hex("0B1218");
            public static readonly Color NightMid = Hex("1A2836");
            public static readonly Color HallAmbient = Hex("243848");
            public static readonly Color ArchFar = Hex("1C2A36");
            public static readonly Color ArchNear = Hex("2A3E4E");
            public static readonly Color Floor = Hex("162028");
            public static readonly Color Crowd = Hex("0E141C");
            public static readonly Color StageWash = Hex("3A6A78");
            public static readonly Color Accent = Hex("7CF0FF");
            public static readonly Color AccentSoft = Hex("5ED3B1");
            public static readonly Color AccentDim = Hex("1E4A52");
            public static readonly Color CtaFace = Hex("1A5C5A");
            public static readonly Color CtaHover = Hex("248078");
            public static readonly Color CtaPress = Hex("123E40");
            public static readonly Color SecondaryFace = Hex("1E2A38");
            public static readonly Color SecondaryHover = Hex("2A3A4C");
            public static readonly Color SecondaryPress = Hex("141C28");
            public static readonly Color Ink = Hex("F7F4EA");
            public static readonly Color InkMuted = Hex("9AA8B4");
            public static readonly Color InkShadow = Hex("0E0C18");
            public static readonly Color VignetteEdge = Hex("05080C");

            private static Color Hex(string hex)
            {
                if (ColorUtility.TryParseHtmlString("#" + hex, out Color c))
                    return c;
                return Color.magenta;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterFirstScene()
        {
            Mount();
        }

        /// <summary>Idempotent MainMenu overlay (ShellSceneRouter + first-scene belt).</summary>
        public static void Mount()
        {
            MountForScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>
        /// Router passes the loaded scene name — do not rely only on GetActiveScene in players.
        /// </summary>
        public static void MountForScene(string sceneName)
        {
            if (!string.Equals(sceneName, ShellScenes.MainMenu, System.StringComparison.Ordinal))
                return;
            if (GameObject.Find(CanvasName) != null)
                return;

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Palette.VoidInk;
            }

            GameSaveService.Ensure();
            ShellUi.EnsureEventSystem();

            Canvas canvas = ShellUi.CreateCanvas(CanvasName, 50);
            Transform root = canvas.transform;

            ShellUi.VerticalGradient(root, "Backdrop", Palette.HallAmbient, Palette.VoidInk, 4);
            BuildClubScene(root);
            ShellUi.Vignette(root, "Vignette", Palette.VignetteEdge, 0.78f, 48);

            // Brand block — hero signal, upper half; float target for motion.
            var brandHost = new GameObject("BrandHost");
            brandHost.transform.SetParent(root, false);
            RectTransform brandRect = brandHost.AddComponent<RectTransform>();
            brandRect.anchorMin = new Vector2(0.08f, 0.52f);
            brandRect.anchorMax = new Vector2(0.92f, 0.86f);
            brandRect.offsetMin = Vector2.zero;
            brandRect.offsetMax = Vector2.zero;

            // Soft ink shadow via duplicate label (1px JRPG outline feel without SDF).
            Text brandShadow = ShellUi.Label(
                brandHost.transform,
                "BrandShadow",
                "DECK//ROGUE",
                86,
                TextAnchor.MiddleCenter,
                Palette.InkShadow,
                new Vector2(0f, 0.38f),
                new Vector2(1f, 1f));
            brandShadow.fontStyle = FontStyle.Bold;
            RectTransform shadowRect = brandShadow.rectTransform;
            shadowRect.offsetMin = new Vector2(3f, -3f);
            shadowRect.offsetMax = new Vector2(3f, -3f);

            Text brand = ShellUi.Label(
                brandHost.transform,
                "Brand",
                "DECK//ROGUE",
                86,
                TextAnchor.MiddleCenter,
                Palette.Ink,
                new Vector2(0f, 0.38f),
                new Vector2(1f, 1f));
            brand.fontStyle = FontStyle.Bold;

            ShellUi.Label(
                brandHost.transform,
                "Tag",
                "night club · mix the run",
                26,
                TextAnchor.UpperCenter,
                Palette.InkMuted,
                new Vector2(0.1f, 0.08f),
                new Vector2(0.9f, 0.42f));

            GameSaveService save = GameSaveService.Ensure();
            bool canLoad = save.HasResumableCheckpoint;

            // Primary CTA — one job; secondary quieter.
            ShellUi.Button(
                root,
                "StartRun",
                "START RUN",
                36,
                new Vector2(0.28f, canLoad ? 0.36f : 0.30f),
                new Vector2(0.72f, canLoad ? 0.48f : 0.42f),
                Palette.CtaFace,
                Palette.CtaHover,
                Palette.CtaPress,
                Palette.Ink,
                () => StartRun());

            Image accentBar = ShellUi.Panel(
                root,
                "CtaAccent",
                Palette.Accent,
                new Vector2(0.34f, canLoad ? 0.348f : 0.288f),
                new Vector2(0.66f, canLoad ? 0.358f : 0.298f));

            if (canLoad)
            {
                string place = save.Document != null && save.Document.Checkpoint != null
                    ? save.Document.Checkpoint.PlaceLine()
                    : "";
                string loadCaption = string.IsNullOrEmpty(place) ? "CONTINUE" : "CONTINUE\n" + place;
                ShellUi.Button(
                    root,
                    "LoadRun",
                    loadCaption,
                    string.IsNullOrEmpty(place) ? 30 : 22,
                    new Vector2(0.30f, 0.22f),
                    new Vector2(0.70f, 0.342f),
                    Palette.SecondaryFace,
                    Palette.SecondaryHover,
                    Palette.SecondaryPress,
                    Palette.Ink,
                    LoadRun);
            }

            ShellUi.Button(
                root,
                "Settings",
                "Settings",
                30,
                new Vector2(0.32f, canLoad ? 0.125f : 0.18f),
                new Vector2(0.68f, canLoad ? 0.205f : 0.27f),
                Palette.SecondaryFace,
                Palette.SecondaryHover,
                Palette.SecondaryPress,
                Palette.Ink,
                () => OpenSettings(root));

            ShellUi.Button(
                root,
                "Quit",
                "Quit",
                26,
                new Vector2(0.38f, 0.04f),
                new Vector2(0.62f, canLoad ? 0.11f : 0.15f),
                Palette.SecondaryFace,
                Palette.SecondaryHover,
                Palette.SecondaryPress,
                Palette.InkMuted,
                Quit);

            if (save.Document != null && save.Document.HubUnlocked)
            {
                ShellUi.Button(
                    root,
                    "Basement",
                    "Подвал",
                    26,
                    new Vector2(0.02f, 0.05f),
                    new Vector2(0.20f, 0.13f),
                    Palette.SecondaryFace,
                    Palette.SecondaryHover,
                    Palette.SecondaryPress,
                    Palette.Ink,
                    () => DayHubOverlay.Mount());
            }

            var tickerGo = new GameObject("MainMenuMotion");
            tickerGo.transform.SetParent(root, false);
            MainMenuMotion motion = tickerGo.AddComponent<MainMenuMotion>();
            motion.Bind(brandRect, accentBar);

            // Campaign intro reads on launch (once per session) before the player starts a night.
            StoryIntroPanel.TryShow(root, null);

            if (DayHubOverlay.EnterOnNextMenu)
            {
                DayHubOverlay.EnterOnNextMenu = false;
                DayHubOverlay.Mount();
            }
        }

        /// <summary>
        /// Upper-half storytelling plate: far arches, crowd silhouettes, stage wash.
        /// Procedural Rect strips only — no sprite assets.
        /// </summary>
        private static void BuildClubScene(Transform root)
        {
            var scene = new GameObject("ClubScene");
            scene.transform.SetParent(root, false);
            RectTransform sceneRect = scene.AddComponent<RectTransform>();
            sceneRect.anchorMin = new Vector2(0f, 0.48f);
            sceneRect.anchorMax = Vector2.one;
            sceneRect.offsetMin = Vector2.zero;
            sceneRect.offsetMax = Vector2.zero;

            ShellUi.Panel(scene.transform, "FarSky", Palette.NightMid, new Vector2(0f, 0.55f), Vector2.one);

            float[] archXs = { 0.06f, 0.18f, 0.32f, 0.48f, 0.62f, 0.76f, 0.90f };
            for (int i = 0; i < archXs.Length; i++)
            {
                float x = archXs[i];
                float w = 0.028f + (i % 3) * 0.006f;
                float h = 0.42f + (i % 2) * 0.12f;
                Color c = (i % 2 == 0) ? Palette.ArchFar : Palette.ArchNear;
                ShellUi.Panel(
                    scene.transform,
                    "Arch" + i,
                    c,
                    new Vector2(x - w * 0.5f, 0.55f),
                    new Vector2(x + w * 0.5f, 0.55f + h));
            }

            ShellUi.Panel(scene.transform, "Floor", Palette.Floor, new Vector2(0f, 0f), new Vector2(1f, 0.28f));
            float[] crowd = { 0.08f, 0.14f, 0.22f, 0.30f, 0.38f, 0.46f, 0.54f, 0.62f, 0.70f, 0.78f, 0.86f, 0.93f };
            for (int i = 0; i < crowd.Length; i++)
            {
                float x = crowd[i];
                float w = 0.018f + (i % 4) * 0.004f;
                float h = 0.18f + (i % 5) * 0.04f;
                ShellUi.Panel(
                    scene.transform,
                    "Crowd" + i,
                    Palette.Crowd,
                    new Vector2(x - w * 0.5f, 0.22f),
                    new Vector2(x + w * 0.5f, 0.22f + h));
            }

            ShellUi.Panel(scene.transform, "Stage", Palette.StageWash, new Vector2(0.28f, 0.10f), new Vector2(0.72f, 0.26f));
            ShellUi.Panel(
                scene.transform,
                "StageGlow",
                new Color(Palette.Accent.r, Palette.Accent.g, Palette.Accent.b, 0.22f),
                new Vector2(0.22f, 0.18f),
                new Vector2(0.78f, 0.34f));
            ShellUi.Panel(
                scene.transform,
                "AccentLamp",
                new Color(Palette.AccentSoft.r, Palette.AccentSoft.g, Palette.AccentSoft.b, 0.35f),
                new Vector2(0.46f, 0.30f),
                new Vector2(0.54f, 0.52f));

            ShellUi.Label(
                scene.transform,
                "Poster",
                "BASEMENT CIRCUIT · three floors + Signal",
                18,
                TextAnchor.MiddleCenter,
                new Color(Palette.InkMuted.r, Palette.InkMuted.g, Palette.InkMuted.b, 0.55f),
                new Vector2(0.62f, 0.72f),
                new Vector2(0.96f, 0.88f));
        }

        private static void StartRun()
        {
            GameSaveService save = GameSaveService.Ensure();
            save.PendingResume = false;
            save.ClearRunCheckpoint();
            // First visit is taught on the booth (spotlight + what each control does), not a text wall.
            EnterAudioSpike();
        }

        private static void EnterAudioSpike()
        {
            SceneManager.LoadScene(ShellScenes.AudioSpike);
        }

        private static void LoadRun()
        {
            GameSaveService save = GameSaveService.Ensure();
            if (!save.HasResumableCheckpoint)
                return;
            save.PendingResume = true;
            SceneManager.LoadScene(ShellScenes.AudioSpike);
        }

        private static void OpenSettings(Transform root)
        {
            if (root.Find("SettingsPanel") != null)
                return;
            GameSaveService save = GameSaveService.Ensure();
            new SettingsPanel(root, save.Document.Settings, null);
        }

        private static void Quit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Soft brand float + accent breathe. Reduced flashing: no pulse flashes —
        /// only ≤5% breathe on the accent bar.
        /// </summary>
        private sealed class MainMenuMotion : MonoBehaviour
        {
            private RectTransform _brand;
            private Image _accent;
            private Vector2 _brandBaseMin;
            private Vector2 _brandBaseMax;
            private Color _accentBase;
            private bool _bound;

            public void Bind(RectTransform brand, Image accent)
            {
                _brand = brand;
                _accent = accent;
                if (_brand != null)
                {
                    _brandBaseMin = _brand.offsetMin;
                    _brandBaseMax = _brand.offsetMax;
                }

                if (_accent != null)
                    _accentBase = _accent.color;
                _bound = true;
            }

            private void Update()
            {
                if (!_bound)
                    return;

                float t = Time.unscaledTime;
                bool reduced = GameSettings.ReducedFlashing;

                if (_brand != null)
                {
                    float amp = reduced ? 1.5f : 5f;
                    float y = Mathf.Sin(t * 0.65f) * amp;
                    _brand.offsetMin = _brandBaseMin + new Vector2(0f, y);
                    _brand.offsetMax = _brandBaseMax + new Vector2(0f, y);
                }

                if (_accent == null)
                    return;

                if (reduced)
                {
                    // ≤5% breathe — no impulsive flashes.
                    float breathe = 0.975f + 0.025f * (0.5f + 0.5f * Mathf.Sin(t * 0.45f));
                    Color c = _accentBase;
                    c.a = _accentBase.a * breathe;
                    _accent.color = c;
                }
                else
                {
                    float pulse = 0.72f + 0.28f * (0.5f + 0.5f * Mathf.Sin(t * 2.1f));
                    Color c = Color.Lerp(Palette.AccentDim, _accentBase, pulse);
                    c.a = Mathf.Lerp(0.45f, 1f, pulse);
                    _accent.color = c;
                }
            }
        }
    }
}
