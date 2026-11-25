using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>Shared uGUI builders for Boot / MainMenu / Settings (runtime overlays).</summary>
    public static class ShellUi
    {
        public static Font ResolveFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
                return font;
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        public static Canvas CreateCanvas(string name, int sortingOrder)
        {
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            float scale = GameSettings.UiScale;
            if (scale < 0.75f)
                scale = 0.75f;
            if (scale > 1.5f)
                scale = 1.5f;
            scaler.scaleFactor = scale;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            EventSystem current = EventSystem.current;
            if (current == null)
            {
                var eventObject = new GameObject("EventSystem");
                current = eventObject.AddComponent<EventSystem>();
            }

            InputSystemUIInputModule module = current.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                StandaloneInputModule legacy = current.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                    Object.Destroy(legacy);
                module = current.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            module.pointerBehavior = UIPointerBehavior.SingleMouseOrPenButMultiTouchAndTrack;
        }

        public static Image FullBleed(Transform parent, string name, Color color)
        {
            // Blocks raycasts (Settings dim / Boot backdrop) — keep default true.
            return Panel(parent, name, color, Vector2.zero, Vector2.one, raycastTarget: true);
        }

        /// <summary>Stretched panel (solid color) in anchor space. Décor defaults to non-blocking.</summary>
        public static Image Panel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            bool raycastTarget = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>
        /// Vertical gradient via a tiny procedural Texture2D (2–4 px) stretched full-bleed.
        /// y=0 = bottom color, y=max = top color.
        /// </summary>
        public static Image VerticalGradient(
            Transform parent,
            string name,
            Color top,
            Color bottom,
            int heightPx = 4)
        {
            if (heightPx < 2)
                heightPx = 2;
            if (heightPx > 8)
                heightPx = 8;

            var tex = new Texture2D(1, heightPx, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };
            for (int y = 0; y < heightPx; y++)
            {
                float t = (float)y / (heightPx - 1);
                tex.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }

            tex.Apply(false, false);
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, 1f, heightPx),
                new Vector2(0.5f, 0.5f),
                1f);
            sprite.hideFlags = HideFlags.DontSave;

            Image image = Panel(parent, name, Color.white, Vector2.zero, Vector2.one, raycastTarget: false);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return image;
        }

        /// <summary>Soft radial vignette (dark edges). Procedural, no sprite assets.</summary>
        public static Image Vignette(
            Transform parent,
            string name,
            Color edge,
            float strength = 0.72f,
            int size = 48)
        {
            if (size < 16)
                size = 16;
            if (size > 128)
                size = 128;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float maxDist = center.magnitude;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float a = Mathf.Clamp01((dist - 0.35f) / 0.65f);
                    a = a * a * strength;
                    tex.SetPixel(x, y, new Color(edge.r, edge.g, edge.b, a));
                }
            }

            tex.Apply(false, false);
            Sprite sprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.hideFlags = HideFlags.DontSave;

            Image image = Panel(parent, name, Color.white, Vector2.zero, Vector2.one, raycastTarget: false);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            return image;
        }

        public static Text Label(
            Transform parent,
            string name,
            string text,
            int fontSize,
            TextAnchor align,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<Text>();
            label.font = ResolveFont();
            label.fontSize = fontSize;
            label.alignment = align;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.text = text;
            return label;
        }

        public static Button Button(
            Transform parent,
            string name,
            string caption,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color face,
            Color ink,
            UnityEngine.Events.UnityAction onClick)
        {
            Color hover = Color.Lerp(face, Color.white, 0.18f);
            Color press = Color.Lerp(face, Color.black, 0.22f);
            return Button(parent, name, caption, fontSize, anchorMin, anchorMax, face, hover, press, ink, onClick);
        }

        public static Button Button(
            Transform parent,
            string name,
            string caption,
            int fontSize,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color face,
            Color hover,
            Color press,
            Color ink,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.AddComponent<Image>();
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = face;
            colors.highlightedColor = hover;
            colors.pressedColor = press;
            colors.selectedColor = hover;
            colors.disabledColor = new Color(face.r, face.g, face.b, 0.4f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            Label(
                go.transform,
                "Label",
                caption,
                fontSize,
                TextAnchor.MiddleCenter,
                ink,
                Vector2.zero,
                Vector2.one);

            return button;
        }

        public static Slider VolumeSlider(
            Transform parent,
            string name,
            float value,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.2f, 1f);

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0.02f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(0.98f, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.35f, 0.78f, 0.72f, 1f);

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28f, 28f);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.9f, 0.95f, 1f, 1f);

            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = value;
            return slider;
        }
    }
}
