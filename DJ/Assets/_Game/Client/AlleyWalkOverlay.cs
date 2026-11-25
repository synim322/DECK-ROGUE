using System.Globalization;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Save;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>
    /// Thirty-second alley on top of the day hub. No booth, no Hype, no fader.
    /// Hold «Вести» to walk the dog. Coins land only at 0:00.
    /// </summary>
    public static class AlleyWalkOverlay
    {
        private const float WalkSeconds = 30f;

        public static void Open(Transform parent)
        {
            if (parent == null || parent.Find("AlleyWalk") != null)
                return;

            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;

            var profile = new MetaProfile(save.Document);
            if (!profile.WalkAvailable)
                return;

            bool thickLeash = profile.Owns(DeckCatalog.SpareLeashId);

            var alleyObject = new GameObject("AlleyWalk");
            alleyObject.transform.SetParent(parent, false);
            Stretch(alleyObject.AddComponent<RectTransform>());
            alleyObject.transform.SetAsLastSibling();

            AlleyTicker ticker = alleyObject.AddComponent<AlleyTicker>();

            Image edge = ShellUi.FullBleed(alleyObject.transform, "Edge", DayHubPalette.Soot);
            Image day = ShellUi.Panel(edge.transform, "Day", DayHubPalette.Day, Vector2.zero, Vector2.one, true);
            day.rectTransform.offsetMin = new Vector2(2f, 2f);
            day.rectTransform.offsetMax = new Vector2(-2f, -2f);
            Transform face = day.transform;

            ShellUi.Panel(face, "Wall", DayHubPalette.Concrete, new Vector2(0f, 0.40f), new Vector2(1f, 0.76f));
            for (int i = 0; i < 5; i++)
            {
                float x = 0.16f + i * 0.15f;
                ShellUi.Panel(
                    face,
                    "Bar" + i.ToString(CultureInfo.InvariantCulture),
                    DayHubPalette.Soot,
                    new Vector2(x, 0.42f),
                    new Vector2(x + 0.018f, 0.74f));
            }

            ShellUi.Panel(face, "Curb", DayHubPalette.Soot, new Vector2(0f, 0.36f), new Vector2(1f, 0.42f));
            ShellUi.Panel(face, "Ground", DayHubPalette.Day, new Vector2(0f, 0f), new Vector2(1f, 0.36f));
            ShellUi.Panel(face, "Puddle", DayHubPalette.Plate, new Vector2(0.62f, 0.10f), new Vector2(0.84f, 0.20f));

            float half = thickLeash ? 0.028f : 0.008f;
            ShellUi.Panel(
                face,
                "Leash",
                DayHubPalette.Copper,
                new Vector2(0.04f, 0.30f - half),
                new Vector2(0.24f, 0.30f + half));

            RectTransform body = HubDog.Build(face, "Dog", new Vector2(0.18f, 0.16f), new Vector2(0.42f, 0.44f));

            Text clock = ShellUi.Label(
                face,
                "Clock",
                "0:30",
                72,
                TextAnchor.MiddleCenter,
                DayHubPalette.Soot,
                new Vector2(0.22f, 0.86f),
                new Vector2(0.78f, 0.99f));
            clock.fontStyle = FontStyle.Bold;

            Image track = ShellUi.Panel(
                face,
                "Track",
                DayHubPalette.Plate,
                new Vector2(0.18f, 0.78f),
                new Vector2(0.82f, 0.855f),
                false);
            Image fill = ShellUi.Panel(track.transform, "Fill", DayHubPalette.Copper, Vector2.zero, Vector2.one, false);

            Text pose = ShellUi.Label(
                face,
                "Pose",
                "стоит",
                36,
                TextAnchor.MiddleCenter,
                DayHubPalette.Soot,
                new Vector2(0.18f, 0.44f),
                new Vector2(0.46f, 0.52f));

            Image lead = ShellUi.Panel(
                face,
                "Lead",
                DayHubPalette.Copper,
                new Vector2(0.22f, 0.03f),
                new Vector2(0.66f, 0.16f),
                true);
            Text leadLabel = ShellUi.Label(
                lead.transform,
                "Caption",
                "Вести",
                32,
                TextAnchor.MiddleCenter,
                DayHubPalette.Soot,
                Vector2.zero,
                Vector2.one);
            leadLabel.fontStyle = FontStyle.Bold;
            WalkLeadHold hold = lead.gameObject.AddComponent<WalkLeadHold>();

            Button leave = ShellUi.Button(
                face,
                "Leave",
                "Уйти",
                22,
                new Vector2(0.02f, 0.03f),
                new Vector2(0.16f, 0.11f),
                DayHubPalette.Plate,
                DayHubPalette.Cream,
                () => ticker.LeaveEarly());

            var closed = new GameObject("Closed");
            closed.transform.SetParent(face, false);
            Stretch(closed.AddComponent<RectTransform>());
            ShellUi.Label(
                closed.transform,
                "BackWord",
                "Назад",
                26,
                TextAnchor.MiddleCenter,
                DayHubPalette.Concrete,
                new Vector2(0.70f, 0.09f),
                new Vector2(0.96f, 0.17f));
            ShellUi.Label(
                closed.transform,
                "Shut",
                "закрыто",
                22,
                TextAnchor.MiddleCenter,
                DayHubPalette.Concrete,
                new Vector2(0.70f, 0.02f),
                new Vector2(0.96f, 0.09f));

            Button back = ShellUi.Button(
                face,
                "Back",
                "Назад",
                26,
                new Vector2(0.70f, 0.03f),
                new Vector2(0.96f, 0.15f),
                DayHubPalette.Copper,
                DayHubPalette.Soot,
                () => ticker.Back());
            back.gameObject.SetActive(false);

            ticker.Bind(clock, fill.rectTransform, pose, hold, body, leave, closed, back, face);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class AlleyTicker : MonoBehaviour
        {
            private float _left = WalkSeconds;
            private bool _bound;
            private bool _done;
            private bool _looseShown;
            private Text _clock;
            private RectTransform _fill;
            private Text _pose;
            private WalkLeadHold _hold;
            private RectTransform _body;
            private Vector2 _bodyMin;
            private Vector2 _bodyMax;
            private Button _leave;
            private GameObject _closed;
            private Button _back;
            private Transform _face;

            public void Bind(
                Text clock,
                RectTransform fill,
                Text pose,
                WalkLeadHold hold,
                RectTransform body,
                Button leave,
                GameObject closed,
                Button back,
                Transform face)
            {
                _clock = clock;
                _fill = fill;
                _pose = pose;
                _hold = hold;
                _body = body;
                _leave = leave;
                _closed = closed;
                _back = back;
                _face = face;
                if (_body != null)
                {
                    _bodyMin = _body.anchorMin;
                    _bodyMax = _body.anchorMax;
                }

                _left = WalkSeconds;
                _bound = true;
                Paint();
            }

            public void LeaveEarly()
            {
                if (_done)
                    return;

                GameSaveService save = GameSaveService.Ensure();
                if (save.Document != null)
                    new MetaProfile(save.Document).AbandonWalk();
                DayHubOverlay.ReturnFromAlley();
            }

            public void Back()
            {
                if (!_done)
                    return;
                DayHubOverlay.ReturnFromAlley();
            }

            private void Update()
            {
                if (!_bound || _done)
                {
                    if (_bound)
                        Paint();
                    return;
                }

                _left -= Time.unscaledDeltaTime;
                if (_left <= 0f)
                {
                    _left = 0f;
                    Finish();
                }

                Paint();
            }

            private void Finish()
            {
                if (_done)
                    return;
                _done = true;
                _left = 0f;

                GameSaveService save = GameSaveService.Ensure();
                if (save.Document != null
                    && new MetaProfile(save.Document).TryCompleteWalk(out _))
                    save.Flush(SaveFlushTrigger.MetaWalletChanged);

                if (_leave != null)
                    _leave.gameObject.SetActive(false);
                if (_closed != null)
                    _closed.SetActive(false);
                if (_back != null)
                    _back.gameObject.SetActive(true);
                ShowLooseChange();
            }

            private void ShowLooseChange()
            {
                if (_looseShown || _face == null)
                    return;
                _looseShown = true;

                float x = 0.50f;
                for (int i = 0; i < 3; i++)
                {
                    ShellUi.Panel(
                        _face,
                        "Coin" + i.ToString(CultureInfo.InvariantCulture),
                        DayHubPalette.Copper,
                        new Vector2(x, 0.20f),
                        new Vector2(x + 0.032f, 0.256f));
                    x += 0.05f;
                }

                ShellUi.Label(
                    _face,
                    "Loose",
                    "мелочь",
                    36,
                    TextAnchor.MiddleLeft,
                    DayHubPalette.Copper,
                    new Vector2(0.66f, 0.18f),
                    new Vector2(0.90f, 0.28f));
            }

            private void Paint()
            {
                if (_clock != null)
                    _clock.text = FormatClock();

                if (_fill != null)
                {
                    float remain = _done ? 0f : Mathf.Clamp01(_left / WalkSeconds);
                    _fill.anchorMin = Vector2.zero;
                    _fill.anchorMax = new Vector2(remain, 1f);
                    _fill.offsetMin = Vector2.zero;
                    _fill.offsetMax = Vector2.zero;
                }

                bool pulling = !_done && _hold != null && _hold.Held;
                if (_pose != null)
                    _pose.text = _done ? "замер" : (pulling ? "тянет" : "стоит");

                if (_body == null)
                    return;

                float extra = pulling ? 0.12f : 0f;
                float minX = Mathf.Clamp01(_bodyMin.x - extra);
                float maxX = Mathf.Clamp01(_bodyMax.x + extra);
                if (maxX < minX)
                    maxX = minX;
                _body.anchorMin = new Vector2(minX, _bodyMin.y);
                _body.anchorMax = new Vector2(maxX, _bodyMax.y);
                _body.offsetMin = Vector2.zero;
                _body.offsetMax = Vector2.zero;
            }

            private string FormatClock()
            {
                int seconds = _done ? 0 : Mathf.CeilToInt(_left);
                if (seconds < 0)
                    seconds = 0;
                if (seconds > 30)
                    seconds = 30;
                int mins = seconds / 60;
                int secs = seconds % 60;
                return mins.ToString(CultureInfo.InvariantCulture)
                    + ":"
                    + secs.ToString("00", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Hold «Вести». Finger down pulls the dog; finger up lets it stand.</summary>
        private sealed class WalkLeadHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
        {
            private int _pressed;

            public bool Held
            {
                get { return _pressed > 0; }
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                _pressed++;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (_pressed > 0)
                    _pressed--;
            }

            private void OnDisable()
            {
                _pressed = 0;
            }
        }
    }
}
