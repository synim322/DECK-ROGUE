using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>In-run pause overlay: continue, save, return to main menu.</summary>
    public sealed class PauseMenuPanel
    {
        private readonly GameObject _root;
        private readonly Text _status;
        private readonly System.Action _onContinue;
        private readonly System.Action _onSave;
        private readonly System.Action _onMainMenu;

        public PauseMenuPanel(
            Transform parent,
            System.Action onContinue,
            System.Action onSave,
            System.Action onMainMenu)
        {
            _onContinue = onContinue;
            _onSave = onSave;
            _onMainMenu = onMainMenu;

            _root = new GameObject("PauseMenuPanel");
            _root.transform.SetParent(parent, false);
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            ShellUi.FullBleed(_root.transform, "Dim", new Color(0.02f, 0.03f, 0.05f, 0.88f));

            Color ink = new Color(0.97f, 0.96f, 0.92f, 1f);
            Color muted = new Color(0.60f, 0.66f, 0.70f, 1f);
            Color cta = new Color(0.10f, 0.36f, 0.35f, 1f);
            Color face = new Color(0.12f, 0.16f, 0.22f, 1f);

            ShellUi.Label(
                _root.transform,
                "Title",
                "PAUSED",
                56,
                TextAnchor.MiddleCenter,
                ink,
                new Vector2(0.15f, 0.78f),
                new Vector2(0.85f, 0.92f));

            _status = ShellUi.Label(
                _root.transform,
                "Status",
                "",
                22,
                TextAnchor.MiddleCenter,
                muted,
                new Vector2(0.18f, 0.70f),
                new Vector2(0.82f, 0.78f));

            ShellUi.Button(
                _root.transform,
                "Continue",
                "CONTINUE",
                32,
                new Vector2(0.28f, 0.52f),
                new Vector2(0.72f, 0.64f),
                cta,
                ink,
                () =>
                {
                    if (_onContinue != null)
                        _onContinue();
                });

            ShellUi.Button(
                _root.transform,
                "Save",
                "SAVE",
                28,
                new Vector2(0.28f, 0.38f),
                new Vector2(0.72f, 0.49f),
                face,
                ink,
                () =>
                {
                    if (_onSave != null)
                        _onSave();
                });

            ShellUi.Button(
                _root.transform,
                "MainMenu",
                "MAIN MENU",
                26,
                new Vector2(0.28f, 0.22f),
                new Vector2(0.72f, 0.34f),
                face,
                muted,
                () =>
                {
                    if (_onMainMenu != null)
                        _onMainMenu();
                });
        }

        public void SetStatus(string text)
        {
            if (_status != null)
                _status.text = text ?? "";
        }

        public void Destroy()
        {
            if (_root != null)
                Object.Destroy(_root);
        }
    }
}
