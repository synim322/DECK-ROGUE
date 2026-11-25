namespace DeckRogue.Core.Meta
{
    /// <summary>
    /// Knobs on the booth for one equipped deck.
    /// Starter and any unknown id have neither filter nor stems.
    /// <c>deck_filter</c> is filter only. <c>deck_stems</c> is stems only.
    /// A leash is not a deck and adds no knobs.
    /// </summary>
    public readonly struct DeckKit
    {
        private DeckKit(bool filter, bool stems)
        {
            Filter = filter;
            Stems = stems;
        }

        public bool Filter { get; }

        public bool Stems { get; }

        public static DeckKit For(string deckId)
        {
            if (deckId == DeckCatalog.FilterId)
                return new DeckKit(true, false);
            if (deckId == DeckCatalog.StemsId)
                return new DeckKit(false, true);
            return new DeckKit(false, false);
        }
    }
}
