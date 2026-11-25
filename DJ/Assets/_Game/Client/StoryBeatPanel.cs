using DeckRogue.Core.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>
    /// Full-bleed story beat with typewriter body. Used for launch intro pages and in-run PreClub.
    /// </summary>
    public sealed class StoryBeatPanel
    {
        private readonly GameObject _root;
        private readonly Text _eyebrow;
        private readonly Text _title;
        private readonly Text _speaker;
        private readonly Text _body;
        private readonly Text _nextLabel;
        private readonly System.Action _onFinished;
        private readonly StoryBeat[] _pages;
        private readonly TypewriterDrive _drive;
        private int _page;
        private string _fullBody = "";

        public StoryBeatPanel(Transform parent, StoryBeat[] pages, System.Action onFinished, string eyebrow = "STORY")
        {
            _onFinished = onFinished;
            _pages = pages ?? System.Array.Empty<StoryBeat>();
            _page = 0;

            _root = new GameObject("StoryBeatPanel");
            _root.transform.SetParent(parent, false);
            RectTransform rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            ShellUi.FullBleed(_root.transform, "Dim", new Color(0.03f, 0.05f, 0.08f, 0.94f));

            Color ink = new Color(0.97f, 0.96f, 0.92f, 1f);
            Color muted = new Color(0.62f, 0.68f, 0.72f, 1f);
            Color cta = new Color(0.10f, 0.36f, 0.35f, 1f);
            Color face = new Color(0.12f, 0.16f, 0.22f, 1f);
            Color accent = new Color(0.49f, 0.94f, 1f, 1f);

            _eyebrow = ShellUi.Label(
                _root.transform,
                "Eyebrow",
                eyebrow ?? "STORY",
                22,
                TextAnchor.MiddleCenter,
                accent,
                new Vector2(0.2f, 0.88f),
                new Vector2(0.8f, 0.96f));

            _title = ShellUi.Label(
                _root.transform,
                "Title",
                "",
                40,
                TextAnchor.MiddleCenter,
                ink,
                new Vector2(0.08f, 0.76f),
                new Vector2(0.92f, 0.88f));
            _title.fontStyle = FontStyle.Bold;

            _speaker = ShellUi.Label(
                _root.transform,
                "Speaker",
                "",
                24,
                TextAnchor.MiddleCenter,
                accent,
                new Vector2(0.12f, 0.68f),
                new Vector2(0.88f, 0.76f));

            _body = ShellUi.Label(
                _root.transform,
                "Body",
                "",
                26,
                TextAnchor.UpperCenter,
                muted,
                new Vector2(0.12f, 0.30f),
                new Vector2(0.88f, 0.68f));

            var nextButton = ShellUi.Button(
                _root.transform,
                "Next",
                "NEXT",
                30,
                new Vector2(0.30f, 0.12f),
                new Vector2(0.70f, 0.23f),
                cta,
                ink,
                OnNext);
            _nextLabel = nextButton.GetComponentInChildren<Text>();

            ShellUi.Button(
                _root.transform,
                "Skip",
                "Skip",
                22,
                new Vector2(0.35f, 0.04f),
                new Vector2(0.65f, 0.11f),
                face,
                muted,
                Finish);

            var driveGo = new GameObject("Typewriter");
            driveGo.transform.SetParent(_root.transform, false);
            _drive = driveGo.AddComponent<TypewriterDrive>();
            _drive.Bind(this);

            ShowPage(0);
        }

        public bool IsOpen => _root != null;

        private void OnNext()
        {
            if (_drive != null && _drive.IsTyping)
            {
                _drive.Complete();
                return;
            }

            if (_page >= _pages.Length - 1)
            {
                Finish();
                return;
            }

            ShowPage(_page + 1);
        }

        private void ShowPage(int index)
        {
            if (_pages.Length == 0)
            {
                Finish();
                return;
            }

            if (index < 0)
                index = 0;
            if (index >= _pages.Length)
                index = _pages.Length - 1;

            _page = index;
            StoryBeat beat = _pages[_page];
            if (_title != null)
                _title.text = beat.Title;
            if (_speaker != null)
                _speaker.text = beat.SpeakerBadge;
            _fullBody = beat.Body ?? "";
            if (_body != null)
                _body.text = "";
            if (_nextLabel != null)
                _nextLabel.text = _page >= _pages.Length - 1 ? "CONTINUE" : "NEXT";
            if (_drive != null)
                _drive.StartTyping(_fullBody);
        }

        internal void SetVisibleBody(string text)
        {
            if (_body != null)
                _body.text = text ?? "";
        }

        internal string FullBody => _fullBody;

        private void Finish()
        {
            if (_drive != null)
                _drive.StopTyping();
            if (_root != null)
                Object.Destroy(_root);
            if (_onFinished != null)
                _onFinished();
        }

        /// <summary>Reveal body text over time; click Next while typing completes the line.</summary>
        private sealed class TypewriterDrive : MonoBehaviour
        {
            private StoryBeatPanel _panel;
            private string _target = "";
            private int _index;
            private float _accum;
            private bool _typing;
            private const float CharsPerSecond = 48f;

            public bool IsTyping => _typing;

            public void Bind(StoryBeatPanel panel)
            {
                _panel = panel;
            }

            public void StartTyping(string text)
            {
                _target = text ?? "";
                _index = 0;
                _accum = 0f;
                _typing = _target.Length > 0;
                if (_panel != null)
                    _panel.SetVisibleBody(_typing ? "" : _target);
            }

            public void Complete()
            {
                _typing = false;
                _index = _target.Length;
                if (_panel != null)
                    _panel.SetVisibleBody(_target);
            }

            public void StopTyping()
            {
                _typing = false;
            }

            private void Update()
            {
                if (!_typing || _panel == null)
                    return;

                _accum += Time.unscaledDeltaTime * CharsPerSecond;
                int advance = (int)_accum;
                if (advance <= 0)
                    return;

                _accum -= advance;
                _index += advance;
                if (_index >= _target.Length)
                {
                    Complete();
                    return;
                }

                _panel.SetVisibleBody(_target.Substring(0, _index));
            }
        }
    }
}
