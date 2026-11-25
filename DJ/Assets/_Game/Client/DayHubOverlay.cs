using System.Globalization;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DeckRogue.Client
{
    /// <summary>
    /// Day basement over the night menu. Shop, spare leash, booth door, alley door.
    /// Same <see cref="SaveGameDocument"/> as the booth. No second scene.
    /// </summary>
    public static class DayHubOverlay
    {
        public const string CanvasName = "DayHubCanvas";

        /// <summary>Result or final-night button sets this; the next menu mount opens the basement and clears it.</summary>
        public static bool EnterOnNextMenu;

        /// <summary>
        /// One-shot line under the cash register. The next basement sheet shows it once, then clears it.
        /// </summary>
        public static string ArrivalLine;

        public static void Mount()
        {
            if (GameObject.Find(CanvasName) != null)
                return;

            ShellUi.EnsureEventSystem();
            Canvas canvas = ShellUi.CreateCanvas(CanvasName, 60);
            RebuildSheet(canvas.transform);
        }

        public static void Close()
        {
            GameObject canvas = GameObject.Find(CanvasName);
            if (canvas != null)
                Object.Destroy(canvas);
        }

        public static void ReturnFromAlley()
        {
            GameObject canvas = GameObject.Find(CanvasName);
            if (canvas == null)
            {
                Mount();
                return;
            }

            Transform alley = canvas.transform.Find("AlleyWalk");
            if (alley != null)
            {
                alley.gameObject.SetActive(false);
                Object.Destroy(alley.gameObject);
            }

            RebuildSheet(canvas.transform);
        }

        private static void Rebuild()
        {
            GameObject canvas = GameObject.Find(CanvasName);
            if (canvas == null)
            {
                Mount();
                return;
            }

            RebuildSheet(canvas.transform);
        }

        private static void RebuildSheet(Transform canvas)
        {
            Transform old = canvas.Find("Sheet");
            if (old != null)
            {
                old.gameObject.SetActive(false);
                Object.Destroy(old.gameObject);
            }

            var sheetObject = new GameObject("Sheet");
            sheetObject.transform.SetParent(canvas, false);
            Stretch(sheetObject.AddComponent<RectTransform>());

            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;

            BuildSheet(sheetObject.transform, save.Document);
        }

        private static void BuildSheet(Transform sheet, SaveGameDocument doc)
        {
            var profile = new MetaProfile(doc);

            Image edge = ShellUi.FullBleed(sheet, "Edge", DayHubPalette.Soot);
            Image day = ShellUi.Panel(edge.transform, "Day", DayHubPalette.Day, Vector2.zero, Vector2.one, true);
            day.rectTransform.offsetMin = new Vector2(2f, 2f);
            day.rectTransform.offsetMax = new Vector2(-2f, -2f);
            Transform root = day.transform;

            bool hasArrival = !string.IsNullOrEmpty(ArrivalLine);
            string arrival = hasArrival ? ArrivalLine : "";
            if (hasArrival)
                ArrivalLine = "";

            float headerBottom = hasArrival ? 0.945f : 0.905f;
            Text header = ShellUi.Label(
                root,
                "Header",
                "Касса за зал. Дека — навсегда.",
                30,
                TextAnchor.MiddleLeft,
                DayHubPalette.Soot,
                new Vector2(0.04f, headerBottom),
                new Vector2(0.68f, 0.98f));
            header.fontStyle = FontStyle.Bold;

            Text coins = ShellUi.Label(
                root,
                "Coins",
                doc.MetaCoins.ToString(CultureInfo.InvariantCulture),
                40,
                TextAnchor.MiddleRight,
                DayHubPalette.Copper,
                new Vector2(0.68f, hasArrival ? 0.945f : 0.90f),
                new Vector2(0.96f, 0.98f));
            coins.fontStyle = FontStyle.Bold;

            if (hasArrival)
            {
                Text arrivalLabel = ShellUi.Label(
                    root,
                    "Arrival",
                    arrival,
                    22,
                    TextAnchor.MiddleLeft,
                    DayHubPalette.Soot,
                    new Vector2(0.04f, 0.895f),
                    new Vector2(0.96f, 0.94f));
                arrivalLabel.fontStyle = FontStyle.Bold;
            }

            Text stall = ShellUi.Label(
                root,
                "Stall",
                "Железо",
                24,
                TextAnchor.MiddleLeft,
                DayHubPalette.Soot,
                new Vector2(0.04f, 0.825f),
                new Vector2(0.34f, 0.895f));
            stall.fontStyle = FontStyle.Bold;

            BuildDeckCard(
                root, "CardStarter", "Черновая", "", false, 0,
                new Vector2(0.04f, 0.48f), new Vector2(0.34f, 0.81f),
                profile, doc, DeckCatalog.StarterId);
            BuildDeckCard(
                root, "CardFilter", "Срез", "фильтр", true, DeckCatalog.FilterPrice,
                new Vector2(0.36f, 0.48f), new Vector2(0.66f, 0.81f),
                profile, doc, DeckCatalog.FilterId);
            BuildDeckCard(
                root, "CardStems", "Полосы", "PERC / MUSIC / LINE", true, DeckCatalog.StemsPrice,
                new Vector2(0.68f, 0.48f), new Vector2(0.96f, 0.81f),
                profile, doc, DeckCatalog.StemsId);

            BuildLeashRow(root, profile, doc);

            string night = doc.HasResumableCheckpoint && doc.Checkpoint != null
                ? doc.Checkpoint.PlaceLine()
                : "";
            string booth = string.IsNullOrEmpty(night) ? "На пульт" : "На пульт\n" + night;
            BuildDoor(
                root,
                "ToBooth",
                booth,
                new Vector2(0.04f, 0.05f),
                new Vector2(0.30f, 0.30f),
                GoToBooth,
                string.IsNullOrEmpty(night) ? 26 : 20);

            if (profile.WalkAvailable)
            {
                BuildDoor(
                    root,
                    "ToAlley",
                    "В переулок",
                    new Vector2(0.33f, 0.05f),
                    new Vector2(0.62f, 0.30f),
                    OpenAlley);
            }
            else
            {
                RectTransform shut = Frame(
                    root,
                    "ToAlley",
                    new Vector2(0.33f, 0.05f),
                    new Vector2(0.62f, 0.30f),
                    DayHubPalette.Plate,
                    false);
                ShellUi.Label(
                    shut,
                    "Shut",
                    "Уже гуляли",
                    26,
                    TextAnchor.MiddleCenter,
                    DayHubPalette.Day,
                    Vector2.zero,
                    Vector2.one);
            }

            HubDog.Build(root, "Dog", new Vector2(0.63f, 0.06f), new Vector2(0.76f, 0.29f));

            RectTransform menu = Frame(
                root,
                "ToMenu",
                new Vector2(0.78f, 0.06f),
                new Vector2(0.96f, 0.18f),
                DayHubPalette.Plate,
                false);
            ShellUi.Button(
                menu,
                "Hit",
                "В меню",
                24,
                Vector2.zero,
                Vector2.one,
                DayHubPalette.Plate,
                DayHubPalette.Cream,
                Close);
        }

        private static void BuildDeckCard(
            Transform parent,
            string name,
            string title,
            string blurb,
            bool showPrice,
            int price,
            Vector2 anchorMin,
            Vector2 anchorMax,
            MetaProfile profile,
            SaveGameDocument doc,
            string id)
        {
            RectTransform face = Frame(parent, name, anchorMin, anchorMax, DayHubPalette.Plate, false);
            bool bare = string.IsNullOrEmpty(blurb) && !showPrice;
            if (bare)
            {
                Text nameLabel = ShellUi.Label(
                    face,
                    "Name",
                    title,
                    32,
                    TextAnchor.MiddleCenter,
                    DayHubPalette.Cream,
                    new Vector2(0.06f, 0.40f),
                    new Vector2(0.94f, 0.82f));
                nameLabel.fontStyle = FontStyle.Bold;
            }
            else
            {
                Text nameLabel = ShellUi.Label(
                    face,
                    "Name",
                    title,
                    30,
                    TextAnchor.MiddleCenter,
                    DayHubPalette.Cream,
                    new Vector2(0.06f, 0.74f),
                    new Vector2(0.94f, 0.94f));
                nameLabel.fontStyle = FontStyle.Bold;
                if (!string.IsNullOrEmpty(blurb))
                {
                    ShellUi.Label(
                        face,
                        "Blurb",
                        blurb,
                        18,
                        TextAnchor.MiddleCenter,
                        DayHubPalette.Day,
                        new Vector2(0.06f, 0.52f),
                        new Vector2(0.94f, 0.72f));
                }

                if (showPrice)
                {
                    ShellUi.Label(
                        face,
                        "Price",
                        price.ToString(CultureInfo.InvariantCulture),
                        26,
                        TextAnchor.MiddleCenter,
                        DayHubPalette.Copper,
                        new Vector2(0.06f, 0.32f),
                        new Vector2(0.94f, 0.50f));
                }
            }

            BuildDeckAction(face, profile, doc, id);
        }

        private static void BuildDeckAction(Transform parent, MetaProfile profile, SaveGameDocument doc, string id)
        {
            var min = new Vector2(0.08f, 0.05f);
            var max = new Vector2(0.92f, 0.28f);
            bool owned = profile.Owns(id);
            bool equipped = string.Equals(doc.EquippedDeckId, id, System.StringComparison.Ordinal);
            if (owned && equipped)
            {
                ShellUi.Label(parent, "State", "На пульте", 22, TextAnchor.MiddleCenter, DayHubPalette.Cream, min, max);
                return;
            }

            if (owned)
            {
                string equipId = id;
                ShellUi.Button(
                    parent,
                    "Equip",
                    "На пульт",
                    22,
                    min,
                    max,
                    DayHubPalette.Copper,
                    DayHubPalette.Soot,
                    () => Equip(equipId));
                return;
            }

            int price;
            if (!DeckCatalog.TryGetPrice(id, out price) || doc.MetaCoins < price)
            {
                ShellUi.Label(parent, "State", "Мало", 22, TextAnchor.MiddleCenter, DayHubPalette.Day, min, max);
                return;
            }

            string buyId = id;
            ShellUi.Button(
                parent,
                "Buy",
                "Купить",
                22,
                min,
                max,
                DayHubPalette.Copper,
                DayHubPalette.Soot,
                () => Buy(buyId));
        }

        private static void BuildLeashRow(Transform parent, MetaProfile profile, SaveGameDocument doc)
        {
            RectTransform face = Frame(
                parent,
                "LeashRow",
                new Vector2(0.04f, 0.33f),
                new Vector2(0.96f, 0.46f),
                DayHubPalette.Plate,
                false);

            ShellUi.Label(
                face,
                "Name",
                "Запасной поводок",
                24,
                TextAnchor.MiddleLeft,
                DayHubPalette.Cream,
                new Vector2(0.03f, 0.12f),
                new Vector2(0.36f, 0.88f));

            bool owned = profile.Owns(DeckCatalog.SpareLeashId);
            float y0 = owned ? 0.22f : 0.40f;
            float y1 = owned ? 0.78f : 0.60f;
            ShellUi.Panel(
                face,
                "Strip",
                DayHubPalette.Copper,
                new Vector2(0.38f, y0),
                new Vector2(0.58f, y1));

            ShellUi.Label(
                face,
                "Price",
                DeckCatalog.SpareLeashPrice.ToString(CultureInfo.InvariantCulture),
                24,
                TextAnchor.MiddleCenter,
                DayHubPalette.Copper,
                new Vector2(0.60f, 0.12f),
                new Vector2(0.74f, 0.88f));

            var min = new Vector2(0.76f, 0.14f);
            var max = new Vector2(0.98f, 0.86f);
            if (owned)
            {
                ShellUi.Label(face, "State", "Надет", 22, TextAnchor.MiddleCenter, DayHubPalette.Cream, min, max);
                return;
            }

            if (doc.MetaCoins < DeckCatalog.SpareLeashPrice)
            {
                ShellUi.Label(face, "State", "Мало", 22, TextAnchor.MiddleCenter, DayHubPalette.Day, min, max);
                return;
            }

            ShellUi.Button(
                face,
                "Buy",
                "Купить",
                22,
                min,
                max,
                DayHubPalette.Copper,
                DayHubPalette.Soot,
                () => Buy(DeckCatalog.SpareLeashId));
        }

        private static void BuildDoor(
            Transform parent,
            string name,
            string caption,
            Vector2 anchorMin,
            Vector2 anchorMax,
            UnityEngine.Events.UnityAction onClick,
            int fontSize = 26)
        {
            RectTransform face = Frame(parent, name, anchorMin, anchorMax, DayHubPalette.Plate, false);
            ShellUi.Button(
                face,
                "Hit",
                caption,
                fontSize,
                Vector2.zero,
                Vector2.one,
                DayHubPalette.Copper,
                DayHubPalette.Soot,
                onClick);
        }

        private static RectTransform Frame(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color fill,
            bool block)
        {
            Image border = ShellUi.Panel(parent, name, DayHubPalette.Soot, anchorMin, anchorMax, block);
            Image face = ShellUi.Panel(border.transform, "Face", fill, Vector2.zero, Vector2.one, false);
            face.rectTransform.offsetMin = new Vector2(2f, 2f);
            face.rectTransform.offsetMax = new Vector2(-2f, -2f);
            return face.rectTransform;
        }

        private static void Buy(string id)
        {
            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;

            var profile = new MetaProfile(save.Document);
            if (!profile.TryPurchase(id))
                return;
            if (DeckCatalog.IsDeck(id))
                profile.TryEquip(id);
            save.Flush(SaveFlushTrigger.MetaWalletChanged);
            Rebuild();
        }

        private static void Equip(string id)
        {
            GameSaveService save = GameSaveService.Ensure();
            if (save.Document == null)
                return;
            if (!new MetaProfile(save.Document).TryEquip(id))
                return;
            save.Flush(SaveFlushTrigger.MetaWalletChanged);
            Rebuild();
        }

        private static void GoToBooth()
        {
            GameSaveService save = GameSaveService.Ensure();
            if (save.HasResumableCheckpoint)
            {
                save.PendingResume = true;
                SceneManager.LoadScene(ShellScenes.AudioSpike);
                return;
            }

            save.PendingResume = false;
            save.ClearRunCheckpoint();
            SceneManager.LoadScene(ShellScenes.AudioSpike);
        }

        private static void OpenAlley()
        {
            GameObject canvas = GameObject.Find(CanvasName);
            if (canvas == null)
                return;
            AlleyWalkOverlay.Open(canvas.transform);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    internal static class DayHubPalette
    {
        public static readonly Color Day = Hex("E4D5C0");
        public static readonly Color Concrete = Hex("7E7268");
        public static readonly Color Soot = Hex("2C2622");
        public static readonly Color Plate = Hex("3E3A4A");
        public static readonly Color Cream = Hex("F7F4EA");
        public static readonly Color Copper = Hex("C46A3A");

        private static Color Hex(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
                return color;
            return Color.magenta;
        }
    }

    /// <summary>Four soot rectangles. No eyes. Hub door and alley share this glyph.</summary>
    internal static class HubDog
    {
        public static RectTransform Build(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            Image root = ShellUi.Panel(parent, name, new Color(0f, 0f, 0f, 0f), anchorMin, anchorMax, false);
            ShellUi.Panel(root.transform, "Tail", DayHubPalette.Soot, new Vector2(0.00f, 0.34f), new Vector2(0.22f, 0.52f));
            Image body = ShellUi.Panel(root.transform, "Body", DayHubPalette.Soot, new Vector2(0.16f, 0.28f), new Vector2(0.70f, 0.62f));
            ShellUi.Panel(root.transform, "Head", DayHubPalette.Soot, new Vector2(0.62f, 0.46f), new Vector2(0.96f, 0.88f));
            ShellUi.Panel(root.transform, "Leg", DayHubPalette.Soot, new Vector2(0.34f, 0.02f), new Vector2(0.50f, 0.34f));
            return body.rectTransform;
        }
    }
}
