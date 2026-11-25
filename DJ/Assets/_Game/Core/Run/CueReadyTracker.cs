using DeckRogue.Core.Audio;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// CueReady when a non-playing deck stays Loaded+Queued for at least one MusicClock bar.
    /// </summary>
    public sealed class CueReadyTracker
    {
        private double _armedSinceA = -1d;
        private double _armedSinceB = -1d;
        private bool _readyA;
        private bool _readyB;

        public void Reset()
        {
            _armedSinceA = -1d;
            _armedSinceB = -1d;
            _readyA = false;
            _readyB = false;
        }

        public void Tick(MusicClock clock, DeckId deck, bool loaded, bool queued, bool playing)
        {
            if (clock == null)
                return;

            if (playing || !loaded || !queued)
            {
                SetArmedSince(deck, -1d);
                SetReady(deck, false);
                return;
            }

            double since = ArmedSince(deck);
            if (since < 0d)
            {
                since = clock.Elapsed;
                SetArmedSince(deck, since);
            }

            SetReady(deck, clock.Elapsed - since >= clock.BarDuration);
        }

        public bool IsCueReady(DeckId deck)
        {
            return deck == DeckId.A ? _readyA : _readyB;
        }

        private double ArmedSince(DeckId deck)
        {
            return deck == DeckId.A ? _armedSinceA : _armedSinceB;
        }

        private void SetArmedSince(DeckId deck, double value)
        {
            if (deck == DeckId.A)
                _armedSinceA = value;
            else
                _armedSinceB = value;
        }

        private void SetReady(DeckId deck, bool ready)
        {
            if (deck == DeckId.A)
                _readyA = ready;
            else
                _readyB = ready;
        }
    }
}
