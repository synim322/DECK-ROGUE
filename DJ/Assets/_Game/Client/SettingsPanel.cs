using DeckRogue.Core.Save;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>uGUI Settings: audio, UI, display (fullscreen + resolution), flashing, language.</summary>
    public sealed class SettingsPanel
    {
        private readonly GameObject _root;
        private readonly Slider _music;
        private readonly Slider _sfx;
        private readonly Slider _uiScale;
        private readonly Text _flashLabel;
        private readonly Text _langLabel;
        private readonly Text _fullscreenLabel;
        private readonly Text _resolutionLabel;
        private readonly SettingsData _draft;
        private readonly System.Action _onClosed;
        private readonly Resolution[] _resolutions;
        private int _resolutionIndex;

        public SettingsPanel(Transform parent, SettingsData source, System.Action onClosed)
        {
            _onClosed = onClosed;
            _draft = (source ?? SettingsData.CreateDefault()).Clone();
            _draft.Normalize();
            _resolutions = GameSettings.ListUniqueResolutions();
            _resolutionIndex = FindResolutionIndex(_draft.ScreenWidth, _draft.ScreenHeight);

            _root = new GameObject("SettingsPanel");
            _root.transform.SetParent(parent, false);
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            ShellUi.FullBleed(_root.transform, "Dim", new Color(0.02f, 0.03f, 0.05f, 0.92f));

            Color ink = new Color(0.86f, 0.95f, 1f, 1f);
            Color face = new Color(0.16f, 0.2f, 0.3f, 1f);
            Color accent = new Color(0.22f, 0.55f, 0.5f, 1f);

            ShellUi.Label(
                _root.transform,
                "Title",
                "Settings",
                48,
                TextAnchor.MiddleCenter,
                ink,
                new Vector2(0.1f, 0.90f),
                new Vector2(0.9f, 0.98f));

            ShellUi.Label(
                _root.transform,
                "MusicLabel",
                "Music",
                24,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.80f),
                new Vector2(0.30f, 0.87f));
            _music = ShellUi.VolumeSlider(
                _root.transform,
                "MusicVolume",
                _draft.MusicVolume,
                new Vector2(0.32f, 0.81f),
                new Vector2(0.90f, 0.86f));
            _music.onValueChanged.AddListener(v => _draft.MusicVolume = v);

            ShellUi.Label(
                _root.transform,
                "SfxLabel",
                "SFX",
                24,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.72f),
                new Vector2(0.30f, 0.79f));
            _sfx = ShellUi.VolumeSlider(
                _root.transform,
                "SfxVolume",
                _draft.SfxVolume,
                new Vector2(0.32f, 0.73f),
                new Vector2(0.90f, 0.78f));
            _sfx.onValueChanged.AddListener(v => _draft.SfxVolume = v);

            ShellUi.Label(
                _root.transform,
                "ScaleLabel",
                "UI Scale",
                24,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.64f),
                new Vector2(0.30f, 0.71f));
            float scaleNorm = (_draft.UiScale - 0.75f) / 0.75f;
            _uiScale = ShellUi.VolumeSlider(
                _root.transform,
                "UiScale",
                Mathf.Clamp01(scaleNorm),
                new Vector2(0.32f, 0.65f),
                new Vector2(0.90f, 0.70f));
            _uiScale.onValueChanged.AddListener(v => _draft.UiScale = 0.75f + v * 0.75f);

            _fullscreenLabel = ShellUi.Label(
                _root.transform,
                "FullscreenLabel",
                FullscreenCaption(_draft.Fullscreen),
                24,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.54f),
                new Vector2(0.52f, 0.62f));
            ShellUi.Button(
                _root.transform,
                "FullscreenToggle",
                "Toggle",
                24,
                new Vector2(0.56f, 0.55f),
                new Vector2(0.90f, 0.61f),
                face,
                ink,
                () =>
                {
                    _draft.Fullscreen = !_draft.Fullscreen;
                    _fullscreenLabel.text = FullscreenCaption(_draft.Fullscreen);
                });

            _resolutionLabel = ShellUi.Label(
                _root.transform,
                "ResolutionLabel",
                ResolutionCaption(),
                24,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.44f),
                new Vector2(0.52f, 0.52f));
            ShellUi.Button(
                _root.transform,
                "ResolutionCycle",
                "Change",
                24,
                new Vector2(0.56f, 0.45f),
                new Vector2(0.90f, 0.51f),
                face,
                ink,
                CycleResolution);

            _flashLabel = ShellUi.Label(
                _root.transform,
                "FlashLabel",
                FlashCaption(_draft.ReducedFlashing),
                22,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.34f),
                new Vector2(0.52f, 0.42f));
            ShellUi.Button(
                _root.transform,
                "FlashToggle",
                "Toggle",
                24,
                new Vector2(0.56f, 0.35f),
                new Vector2(0.90f, 0.41f),
                face,
                ink,
                () =>
                {
                    _draft.ReducedFlashing = !_draft.ReducedFlashing;
                    _flashLabel.text = FlashCaption(_draft.ReducedFlashing);
                });

            _langLabel = ShellUi.Label(
                _root.transform,
                "LangLabel",
                LangCaption(_draft.Language),
                22,
                TextAnchor.MiddleLeft,
                ink,
                new Vector2(0.10f, 0.24f),
                new Vector2(0.52f, 0.32f));
            ShellUi.Button(
                _root.transform,
                "LangToggle",
                "en / ru",
                24,
                new Vector2(0.56f, 0.25f),
                new Vector2(0.90f, 0.31f),
                face,
                ink,
                () =>
                {
                    _draft.Language = string.Equals(_draft.Language, "en", System.StringComparison.OrdinalIgnoreCase)
                        ? "ru"
                        : "en";
                    _langLabel.text = LangCaption(_draft.Language);
                });

            ShellUi.Button(
                _root.transform,
                "Apply",
                "Apply",
                30,
                new Vector2(0.18f, 0.08f),
                new Vector2(0.48f, 0.18f),
                accent,
                ink,
                ApplyAndClose);

            ShellUi.Button(
                _root.transform,
                "Back",
                "Back",
                28,
                new Vector2(0.52f, 0.08f),
                new Vector2(0.82f, 0.18f),
                face,
                ink,
                DestroyOnly);
        }

        public void Destroy()
        {
            if (_root != null)
                Object.Destroy(_root);
        }

        private void CycleResolution()
        {
            if (_resolutions == null || _resolutions.Length == 0)
                return;

            _resolutionIndex = (_resolutionIndex + 1) % _resolutions.Length;
            Resolution pick = _resolutions[_resolutionIndex];
            _draft.ScreenWidth = pick.width;
            _draft.ScreenHeight = pick.height;
            _resolutionLabel.text = ResolutionCaption();
        }

        private void ApplyAndClose()
        {
            _draft.Normalize();
            GameSaveService save = GameSaveService.Ensure();
            save.ApplySettings(_draft);
            if (_onClosed != null)
                _onClosed();
            Destroy();
        }

        private void DestroyOnly()
        {
            if (_onClosed != null)
                _onClosed();
            Destroy();
        }

        private int FindResolutionIndex(int width, int height)
        {
            if (_resolutions == null || _resolutions.Length == 0)
                return 0;

            if (width <= 0 || height <= 0)
            {
                int w = 0;
                int h = 0;
                GameSettings.ResolveSize(ref w, ref h);
                width = w;
                height = h;
            }

            for (int i = 0; i < _resolutions.Length; i++)
            {
                if (_resolutions[i].width == width && _resolutions[i].height == height)
                    return i;
            }

            // Nearest by area.
            long bestDelta = long.MaxValue;
            int best = _resolutions.Length - 1;
            long target = (long)width * height;
            for (int i = 0; i < _resolutions.Length; i++)
            {
                long area = (long)_resolutions[i].width * _resolutions[i].height;
                long delta = area > target ? area - target : target - area;
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    best = i;
                }
            }

            _draft.ScreenWidth = _resolutions[best].width;
            _draft.ScreenHeight = _resolutions[best].height;
            return best;
        }

        private string ResolutionCaption()
        {
            if (_resolutions == null || _resolutions.Length == 0)
                return "Resolution: native";

            if (_resolutionIndex < 0 || _resolutionIndex >= _resolutions.Length)
                _resolutionIndex = 0;

            Resolution r = _resolutions[_resolutionIndex];
            return "Resolution: " + r.width + "×" + r.height;
        }

        private static string FullscreenCaption(bool on)
        {
            return on ? "Display: Fullscreen" : "Display: Windowed";
        }

        private static string FlashCaption(bool on)
        {
            return on ? "Reduced flashing: ON" : "Reduced flashing: OFF";
        }

        private static string LangCaption(string lang)
        {
            return "Language: " + (string.IsNullOrEmpty(lang) ? "en" : lang);
        }
    }
}
