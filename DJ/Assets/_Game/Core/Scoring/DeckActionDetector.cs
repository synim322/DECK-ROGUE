using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Deck quality gestures: PhraseStart, BassHandoff, LineLift.
    /// Playtest hypothesis. One id pays once through HypeSystem; this detector also
    /// refuses a second emit of the same gesture inside one phrase.
    /// </summary>
    public sealed class DeckActionDetector
    {
        public const double AudibleGain = 0.35d;
        public const double LowPresent = 0.45d;
        public const double LowQuiet = 0.20d;
        public const double LineDown = 0.25d;
        public const double LineUp = 0.75d;

        private readonly int[] _phraseStartPaid = new int[] { -1, -1 };

        private double _bassSince = -1d;
        private int _bassBar = -1;
        private int _bassPhrase = -1;

        private readonly bool[] _lineArmed = new bool[2];
        private readonly double[] _lineSince = new double[] { -1d, -1d };
        private readonly int[] _lineBar = new int[] { -1, -1 };
        private readonly int[] _linePhrase = new int[] { -1, -1 };

        public void Reset()
        {
            _phraseStartPaid[0] = -1;
            _phraseStartPaid[1] = -1;
            _bassSince = -1d;
            _bassBar = -1;
            _bassPhrase = -1;
            for (int deck = 0; deck < 2; deck++)
            {
                _lineArmed[deck] = false;
                _lineSince[deck] = -1d;
                _lineBar[deck] = -1;
                _linePhrase[deck] = -1;
            }
        }

        /// <summary>
        /// Writes zero or more ScoreEvents into <paramref name="results"/> starting at
        /// <paramref name="offset"/>. Returns how many were written. Stops when the buffer is full.
        /// </summary>
        public int Collect(
            MixSnapshot snapshot,
            MusicPosition position,
            double barDuration,
            ScoreEvent[] results,
            int offset)
        {
            if (results == null || offset < 0)
                return 0;

            int written = 0;
            written += TryPhraseStart(
                0,
                snapshot.QuantizedStartA,
                snapshot.DeckAPlaying,
                snapshot.GainA,
                snapshot.DeckBPlaying,
                snapshot.GainB,
                position,
                results,
                offset + written);
            written += TryPhraseStart(
                1,
                snapshot.QuantizedStartB,
                snapshot.DeckBPlaying,
                snapshot.GainB,
                snapshot.DeckAPlaying,
                snapshot.GainA,
                position,
                results,
                offset + written);
            written += TryBassHandoff(snapshot, position, barDuration, results, offset + written);
            written += TryLineLift(
                0,
                snapshot.ChannelA,
                snapshot.DeckAPlaying,
                snapshot.GainA,
                snapshot,
                position,
                barDuration,
                results,
                offset + written);
            written += TryLineLift(
                1,
                snapshot.ChannelB,
                snapshot.DeckBPlaying,
                snapshot.GainB,
                snapshot,
                position,
                barDuration,
                results,
                offset + written);
            return written;
        }

        private int TryPhraseStart(
            int deck,
            bool quantized,
            bool playing,
            double gain,
            bool otherPlaying,
            double otherGain,
            MusicPosition position,
            ScoreEvent[] results,
            int index)
        {
            if (!quantized)
                return 0;
            if (position.BarInPhrase != 0 || position.BeatInBar != 0)
                return 0;
            if (!Audible(playing, gain) || !Audible(otherPlaying, otherGain))
                return 0;
            if (_phraseStartPaid[deck] == position.Phrase)
                return 0;
            if (index < 0 || index >= results.Length)
                return 0;

            _phraseStartPaid[deck] = position.Phrase;
            results[index] = ScoreEvent.PhraseStart(position.Phrase, deck);
            return 1;
        }

        private int TryBassHandoff(
            MixSnapshot snapshot,
            MusicPosition position,
            double barDuration,
            ScoreEvent[] results,
            int index)
        {
            bool both = Audible(snapshot.DeckAPlaying, snapshot.GainA)
                && Audible(snapshot.DeckBPlaying, snapshot.GainB);
            bool lowOnA = snapshot.LowA >= LowPresent && snapshot.LowB <= LowQuiet;
            bool lowOnB = snapshot.LowB >= LowPresent && snapshot.LowA <= LowQuiet;
            bool held = both && lowOnA != lowOnB;
            if (!held)
            {
                _bassSince = -1d;
                _bassBar = -1;
                return 0;
            }

            if (_bassSince < 0d)
            {
                _bassSince = snapshot.AudioTime;
                _bassBar = position.Bar;
            }

            if (_bassPhrase == position.Phrase)
                return 0;
            if (position.BeatInBar != 0)
                return 0;
            if (position.Bar <= _bassBar)
                return 0;
            if (snapshot.AudioTime - _bassSince + 1e-9d < barDuration * 0.95d)
                return 0;
            if (index < 0 || index >= results.Length)
                return 0;

            _bassPhrase = position.Phrase;
            results[index] = ScoreEvent.BassHandoff(position.Phrase);
            return 1;
        }

        private int TryLineLift(
            int deck,
            double line,
            bool playing,
            double gain,
            MixSnapshot snapshot,
            MusicPosition position,
            double barDuration,
            ScoreEvent[] results,
            int index)
        {
            if (line <= LineDown)
            {
                if (!_lineArmed[deck])
                {
                    _lineArmed[deck] = true;
                    _lineSince[deck] = snapshot.AudioTime;
                    _lineBar[deck] = position.Bar;
                }

                return 0;
            }

            if (!_lineArmed[deck] || line < LineUp)
                return 0;

            // A blip that tops out in the same bar is not a one-bar lift.
            if (position.Bar == _lineBar[deck])
            {
                _lineArmed[deck] = false;
                return 0;
            }

            bool timed = snapshot.AudioTime - _lineSince[deck] + 1e-9d >= barDuration;
            bool onGrid = position.BeatInBar == 0 && position.Bar > _lineBar[deck];
            if (!timed || !onGrid)
                return 0;
            if (!Audible(playing, gain))
                return 0;

            if (_linePhrase[deck] == position.Phrase)
            {
                _lineArmed[deck] = false;
                return 0;
            }

            if (index < 0 || index >= results.Length)
                return 0;

            _lineArmed[deck] = false;

            _linePhrase[deck] = position.Phrase;
            results[index] = ScoreEvent.LineLift(position.Phrase, deck);
            return 1;
        }

        private static bool Audible(bool playing, double gain)
        {
            return playing && gain >= AudibleGain;
        }
    }
}
