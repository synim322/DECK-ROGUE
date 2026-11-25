using System.Collections.Generic;

namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// Which record sits on each deck. A playing or queued deck keeps its record
    /// until the stop has finished.
    /// </summary>
    public sealed class DeckRack
    {
        private readonly Dictionary<DeckId, TrackDefinition> _loaded = new Dictionary<DeckId, TrackDefinition>();

        public TrackDefinition Loaded(DeckId deck)
        {
            TrackDefinition track;
            _loaded.TryGetValue(deck, out track);
            return track;
        }

        public bool TryLoad(DeckId deck, TrackDefinition track, bool deckIsActive, out string reason)
        {
            if (track == null)
            {
                reason = "track-missing";
                return false;
            }

            if (deckIsActive)
            {
                reason = "deck-is-active";
                return false;
            }

            _loaded[deck] = track;
            reason = "loaded";
            return true;
        }

        public void Replace(DeckId deck, TrackDefinition track)
        {
            _loaded[deck] = track;
        }
    }
}
