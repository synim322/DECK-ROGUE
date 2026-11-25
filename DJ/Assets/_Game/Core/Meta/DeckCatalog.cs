namespace DeckRogue.Core.Meta
{
    /// <summary>
    /// Hub shop. Decks do not stack: one equipped id enters a run.
    /// Prices are flat coins, never a Hype percent.
    /// </summary>
    public static class DeckCatalog
    {
        public const string StarterId = "deck_starter";
        public const string FilterId = "deck_filter";
        public const string StemsId = "deck_stems";
        public const string SpareLeashId = "gear_spare_leash";

        public const int StarterPrice = 0;
        public const int FilterPrice = 60;
        public const int StemsPrice = 120;
        public const int SpareLeashPrice = 40;

        public static bool IsDeck(string id)
        {
            return id == StarterId || id == FilterId || id == StemsId;
        }

        public static bool IsItem(string id)
        {
            return id == SpareLeashId;
        }

        public static bool TryGetPrice(string id, out int price)
        {
            if (id == StarterId)
            {
                price = StarterPrice;
                return true;
            }

            if (id == FilterId)
            {
                price = FilterPrice;
                return true;
            }

            if (id == StemsId)
            {
                price = StemsPrice;
                return true;
            }

            if (id == SpareLeashId)
            {
                price = SpareLeashPrice;
                return true;
            }

            price = 0;
            return false;
        }
    }
}
