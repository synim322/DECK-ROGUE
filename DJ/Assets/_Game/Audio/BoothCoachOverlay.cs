using System;
using DeckRogue.Client;
using DeckRogue.Core.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Audio
{
    /// <summary>
    /// Spotlight over one booth control plus a card that says what that control does.
    /// The dim does not eat clicks — the set clock keeps running and the highlighted control stays usable.
    /// </summary>
    public sealed class BoothCoachOverlay
    {
        private static readonly Color Dim = new Color(0.02f, 0.03f, 0.05f, 0.72f);
        private static readonly Color Card = new Color(0.07f, 0.09f, 0.13f, 0.96f);
        private static readonly Color Accent = Hex(0x7CF0FF);
        private static readonly Color Ink = Hex(0xF7F4EA);
        private static readonly Color Muted = new Color(0.62f, 0.68f, 0.74f, 1f);
        private static readonly Color Cta = Hex(0x1A5C5A);
        private static readonly Color Quiet = new Color(0.16f, 0.20f, 0.28f, 1f);

        private static readonly Rect[] Slots =
        {
            new Rect(0.22f, 0.62f, 0.56f, 0.28f),
            new Rect(0.22f, 0.08f, 0.56f, 0.28f),
            new Rect(0.02f, 0.28f, 0.32f, 0.34f),
            new Rect(0.66f, 0.28f, 0.32f, 0.34f)
        };

        private readonly GameObject _root;
        private readonly RectTransform _rootRect;
        private readonly Image _dimTop;
        private readonly Image _dimBottom;
        private readonly Image _dimLeft;
        private readonly Image _dimRight;
        private readonly Image _glowTop;
        private readonly Image _glowBottom;
        private readonly Image _glowLeft;
        private readonly Image _glowRight;
        private readonly Image _frameTop;
        private readonly Image _frameBottom;
        private readonly Image _frameLeft;
        private readonly Image _frameRight;
        private readonly Text _here;
        private readonly RectTransform _hereRect;
        private readonly Text _eyebrow;
        private readonly Text _title;
        private readonly Text _body;
        private readonly Button _advance;
        private readonly Text _advanceLabel;
        private readonly RectTransform _cardRect;
        private float _pulse;

        public BoothCoachOverlay(Transform parent, Action onAdvance, Action onSkip)
        {
            if (parent == null)
                return;

            Sprite solid = Solid();
            _root = new GameObject("BoothCoach");
            _root.transform.SetParent(parent, false);
            _rootRect = _root.AddComponent<RectTransform>();
            Stretch(_rootRect);

            _dimTop = Band(solid, "DimTop");
            _dimBottom = Band(solid, "DimBottom");
            _dimLeft = Band(solid, "DimLeft");
            _dimRight = Band(solid, "DimRight");
            _glowTop = Band(solid, "GlowTop");
            _glowBottom = Band(solid, "GlowBottom");
            _glowLeft = Band(solid, "GlowLeft");
            _glowRight = Band(solid, "GlowRight");
            _frameTop = Band(solid, "FrameTop");
            _frameBottom = Band(solid, "FrameBottom");
            _frameLeft = Band(solid, "FrameLeft");
            _frameRight = Band(solid, "FrameRight");

            _here = ShellUi.Label(_root.transform, "Here", "HERE", 18, TextAnchor.MiddleCenter, Accent, Vector2.zero, Vector2.one);
            _here.fontStyle = FontStyle.Bold;
            _here.raycastTarget = false;
            _hereRect = _here.rectTransform;
            _hereRect.offsetMin = Vector2.zero;
            _hereRect.offsetMax = Vector2.zero;

            var card = new GameObject("Card");
            card.transform.SetParent(_root.transform, false);
            _cardRect = card.AddComponent<RectTransform>();
            Image cardFace = card.AddComponent<Image>();
            cardFace.sprite = solid;
            cardFace.color = Card;
            cardFace.raycastTarget = true;

            Image accent = Band(solid, "CardAccent");
            accent.transform.SetParent(card.transform, false);
            accent.color = Accent;
            accent.raycastTarget = false;
            Anchor(accent.rectTransform, 0f, 0.94f, 1f, 1f);

            _eyebrow = ShellUi.Label(card.transform, "Eyebrow", "FIRST NIGHT", 16, TextAnchor.MiddleLeft, Accent, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.94f));
            _title = ShellUi.Label(card.transform, "Title", "", 26, TextAnchor.MiddleLeft, Ink, new Vector2(0.06f, 0.60f), new Vector2(0.94f, 0.80f));
            _title.fontStyle = FontStyle.Bold;
            _body = ShellUi.Label(card.transform, "Body", "", 18, TextAnchor.UpperLeft, Muted, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.62f));
            _body.verticalOverflow = VerticalWrapMode.Truncate;

            _advance = ShellUi.Button(
                card.transform,
                "Advance",
                "NEXT",
                18,
                new Vector2(0.52f, 0.06f),
                new Vector2(0.94f, 0.24f),
                Cta,
                Ink,
                () =>
                {
                    if (onAdvance != null)
                        onAdvance();
                });
            _advanceLabel = _advance.GetComponentInChildren<Text>();

            ShellUi.Button(
                card.transform,
                "SkipLesson",
                "Skip lesson",
                16,
                new Vector2(0.06f, 0.06f),
                new Vector2(0.48f, 0.24f),
                Quiet,
                Muted,
                () =>
                {
                    if (onSkip != null)
                        onSkip();
                });

            _root.SetActive(false);
        }

        public void Hide()
        {
            if (_root != null && _root.activeSelf)
                _root.SetActive(false);
        }

        public void Dismiss()
        {
            if (_root != null)
                UnityEngine.Object.Destroy(_root);
        }

        public void Tick(float deltaTime)
        {
            if (_root == null || !_root.activeSelf)
                return;
            if (deltaTime < 0f || float.IsNaN(deltaTime))
                deltaTime = 0f;
            _pulse += deltaTime * 3.4f;
        }

        public void Present(CoachCard card, RectTransform target)
        {
            if (_root == null)
                return;

            _root.SetActive(true);
            _root.transform.SetAsLastSibling();

            if (_eyebrow.text != Eyebrow(card))
                _eyebrow.text = Eyebrow(card);
            if (_title.text != card.Title)
                _title.text = card.Title;
            if (_body.text != card.Body)
                _body.text = card.Body;

            string action = card.RequiresAction ? "Skip step" : "NEXT";
            if (_advanceLabel != null && _advanceLabel.text != action)
                _advanceLabel.text = action;
            PaintAdvance(card.RequiresAction);

            Rect hole;
            bool hasHole = TryHole(target, out hole);
            SetSpotlight(hasHole);
            if (hasHole)
            {
                LayoutHole(hole);
                Rect slot = PickSlot(hole);
                Anchor(_cardRect, slot.xMin, slot.yMin, slot.xMax, slot.yMax);
                PlaceHere(hole, slot);
            }
            else
            {
                Anchor(_cardRect, Slots[0].xMin, Slots[0].yMin, Slots[0].xMax, Slots[0].yMax);
            }

            float wave = GameSettings.ReducedFlashing ? 1f : 0.45f + 0.55f * Mathf.Sin(_pulse);
            Color frame = Accent;
            frame.a = 0.55f + 0.45f * wave;
            Paint(_frameTop, frame);
            Paint(_frameBottom, frame);
            Paint(_frameLeft, frame);
            Paint(_frameRight, frame);
            Color glow = Accent;
            glow.a = 0.18f + 0.22f * wave;
            Paint(_glowTop, glow);
            Paint(_glowBottom, glow);
            Paint(_glowLeft, glow);
            Paint(_glowRight, glow);
            if (_here != null)
            {
                Color here = Accent;
                here.a = 0.65f + 0.35f * wave;
                _here.color = here;
            }
        }

        private void PaintAdvance(bool skipStep)
        {
            if (_advance == null)
                return;
            Color face = skipStep ? Quiet : Cta;
            ColorBlock colors = _advance.colors;
            colors.normalColor = face;
            colors.highlightedColor = Color.Lerp(face, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(face, Color.black, 0.22f);
            colors.selectedColor = colors.highlightedColor;
            _advance.colors = colors;
            if (_advanceLabel != null)
                _advanceLabel.color = skipStep ? Muted : Ink;
        }

        private void SetSpotlight(bool on)
        {
            SetOn(_dimTop, on);
            SetOn(_dimBottom, on);
            SetOn(_dimLeft, on);
            SetOn(_dimRight, on);
            SetOn(_glowTop, on);
            SetOn(_glowBottom, on);
            SetOn(_glowLeft, on);
            SetOn(_glowRight, on);
            SetOn(_frameTop, on);
            SetOn(_frameBottom, on);
            SetOn(_frameLeft, on);
            SetOn(_frameRight, on);
            if (_here != null)
                _here.gameObject.SetActive(on);
        }

        private void LayoutHole(Rect hole)
        {
            float width = _rootRect.rect.width;
            float height = _rootRect.rect.height;
            if (width < 2f)
                width = 1920f;
            if (height < 2f)
                height = 1080f;
            float gx = 14f / width;
            float gy = 14f / height;
            float fx = 4f / width;
            float fy = 4f / height;

            Anchor(_dimBottom.rectTransform, 0f, 0f, 1f, hole.yMin);
            Anchor(_dimTop.rectTransform, 0f, hole.yMax, 1f, 1f);
            Anchor(_dimLeft.rectTransform, 0f, hole.yMin, hole.xMin, hole.yMax);
            Anchor(_dimRight.rectTransform, hole.xMax, hole.yMin, 1f, hole.yMax);

            Anchor(_frameBottom.rectTransform, hole.xMin, hole.yMin - fy, hole.xMax, hole.yMin);
            Anchor(_frameTop.rectTransform, hole.xMin, hole.yMax, hole.xMax, hole.yMax + fy);
            Anchor(_frameLeft.rectTransform, hole.xMin - fx, hole.yMin - fy, hole.xMin, hole.yMax + fy);
            Anchor(_frameRight.rectTransform, hole.xMax, hole.yMin - fy, hole.xMax + fx, hole.yMax + fy);

            Anchor(_glowBottom.rectTransform, hole.xMin - gx, hole.yMin - gy, hole.xMax + gx, hole.yMin - fy);
            Anchor(_glowTop.rectTransform, hole.xMin - gx, hole.yMax + fy, hole.xMax + gx, hole.yMax + gy);
            Anchor(_glowLeft.rectTransform, hole.xMin - gx, hole.yMin, hole.xMin - fx, hole.yMax);
            Anchor(_glowRight.rectTransform, hole.xMax + fx, hole.yMin, hole.xMax + gx, hole.yMax);
        }

        private void PlaceHere(Rect hole, Rect slot)
        {
            if (_hereRect == null)
                return;

            float bob = GameSettings.ReducedFlashing ? 0f : 0.006f * Mathf.Sin(_pulse * 2.2f);
            bool cardAbove = slot.center.y >= hole.center.y;
            float y = cardAbove ? hole.yMax + 0.004f + bob : hole.yMin - 0.044f - bob;
            float x = Mathf.Clamp(hole.center.x - 0.05f, 0.02f, 0.88f);
            Anchor(_hereRect, x, y, x + 0.10f, y + 0.04f);
            _hereRect.SetAsLastSibling();
        }

        private static Rect PickSlot(Rect hole)
        {
            Rect best = Slots[0];
            float bestOverlap = float.MaxValue;
            Rect grown = new Rect(hole.xMin - 0.02f, hole.yMin - 0.02f, hole.width + 0.04f, hole.height + 0.04f);
            for (int i = 0; i < Slots.Length; i++)
            {
                float overlap = Overlap(Slots[i], grown);
                if (overlap < bestOverlap)
                {
                    bestOverlap = overlap;
                    best = Slots[i];
                }
            }

            return best;
        }

        private bool TryHole(RectTransform target, out Rect hole)
        {
            hole = default(Rect);
            if (target == null || _rootRect == null || !target.gameObject.activeInHierarchy)
                return false;

            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 bl;
            Vector2 tr;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect,
                    RectTransformUtility.WorldToScreenPoint(null, corners[0]),
                    null,
                    out bl))
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rootRect,
                    RectTransformUtility.WorldToScreenPoint(null, corners[2]),
                    null,
                    out tr))
                return false;

            Rect area = _rootRect.rect;
            if (area.width < 2f || area.height < 2f)
                return false;

            float padX = 16f / area.width;
            float padY = 16f / area.height;
            float x0 = Mathf.Clamp01((bl.x - area.xMin) / area.width - padX);
            float y0 = Mathf.Clamp01((bl.y - area.yMin) / area.height - padY);
            float x1 = Mathf.Clamp01((tr.x - area.xMin) / area.width + padX);
            float y1 = Mathf.Clamp01((tr.y - area.yMin) / area.height + padY);
            if (x1 - x0 < 0.012f || y1 - y0 < 0.012f)
                return false;

            hole = Rect.MinMaxRect(x0, y0, x1, y1);
            return true;
        }

        private Image Band(Sprite sprite, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.AddComponent<RectTransform>();
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = name.StartsWith("Dim", StringComparison.Ordinal) ? Dim : Accent;
            image.raycastTarget = false;
            return image;
        }

        private static void Paint(Image image, Color color)
        {
            if (image != null)
                image.color = color;
        }

        private static void SetOn(Image image, bool on)
        {
            if (image != null && image.gameObject.activeSelf != on)
                image.gameObject.SetActive(on);
        }

        private static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
        {
            if (rect == null)
                return;
            x0 = Mathf.Clamp01(x0);
            y0 = Mathf.Clamp01(y0);
            x1 = Mathf.Clamp01(x1);
            y1 = Mathf.Clamp01(y1);
            if (x1 < x0)
                x1 = x0;
            if (y1 < y0)
                y1 = y0;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static float Overlap(Rect a, Rect b)
        {
            float x = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
            float y = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
            return x * y;
        }

        private static string Eyebrow(CoachCard card)
        {
            return "FIRST NIGHT  ·  " + card.Number.ToString() + " / " + card.Count.ToString();
        }

        private static Sprite Solid()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply(false, false);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static Color Hex(int rgb)
        {
            float r = ((rgb >> 16) & 0xFF) / 255f;
            float g = ((rgb >> 8) & 0xFF) / 255f;
            float b = (rgb & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }
    }
}
