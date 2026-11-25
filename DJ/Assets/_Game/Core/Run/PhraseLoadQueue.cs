using System;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Phrase-locked load: QueueLoad stays pending until MusicClock is at a phrase start.
    /// </summary>
    public sealed class PhraseLoadQueue
    {
        private string _pendingA;
        private string _pendingB;
        private int _appliedPhraseA = int.MinValue;
        private int _appliedPhraseB = int.MinValue;

        public void Reset()
        {
            _pendingA = null;
            _pendingB = null;
            _appliedPhraseA = int.MinValue;
            _appliedPhraseB = int.MinValue;
        }

        public bool HasPending(DeckId deck)
        {
            return Pending(deck) != null;
        }

        public bool TryGetPending(DeckId deck, out string trackId)
        {
            trackId = Pending(deck);
            return trackId != null;
        }

        /// <summary>
        /// Queues a load that applies on the next phrase boundary (or immediately if already there).
        /// Replaces any prior pending load for that deck.
        /// </summary>
        public void QueueLoad(DeckId deck, string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
                throw new ArgumentException("Track id is missing.", nameof(trackId));

            SetPending(deck, trackId);
        }

        /// <summary>
        /// Applies a pending load when the clock sits on a phrase start (bar-in-phrase 0, beat-in-bar 0).
        /// One apply per deck per phrase index.
        /// </summary>
        public bool TryApply(MusicClock clock, DeckId deck, out string trackId)
        {
            trackId = null;
            if (clock == null)
                return false;

            string pending = Pending(deck);
            if (pending == null)
                return false;

            MusicPosition position = clock.GetPosition();
            if (position.BarInPhrase != 0 || position.BeatInBar != 0)
                return false;

            int applied = AppliedPhrase(deck);
            if (applied == position.Phrase)
                return false;

            trackId = pending;
            SetPending(deck, null);
            SetAppliedPhrase(deck, position.Phrase);
            return true;
        }

        private string Pending(DeckId deck)
        {
            return deck == DeckId.A ? _pendingA : _pendingB;
        }

        private void SetPending(DeckId deck, string trackId)
        {
            if (deck == DeckId.A)
                _pendingA = trackId;
            else
                _pendingB = trackId;
        }

        private int AppliedPhrase(DeckId deck)
        {
            return deck == DeckId.A ? _appliedPhraseA : _appliedPhraseB;
        }

        private void SetAppliedPhrase(DeckId deck, int phrase)
        {
            if (deck == DeckId.A)
                _appliedPhraseA = phrase;
            else
                _appliedPhraseB = phrase;
        }
    }
}
