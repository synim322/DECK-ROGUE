using System;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Mixer depth gestures from MixSnapshot deltas: FilterPhrase on phrase/bar boundaries
    /// and StemPunch into an armed drop. Anti-farm: once per phrase / armed window;
    /// filter travel must stay audible for ≥1 bar (no idle knob spam).
    /// </summary>
    public sealed class MixGestureDetector
    {
        public const double AudibleGain = 0.35d;
        public const double FilterDeltaMin = 0.2d;
        public const double StemDeltaMin = 0.2d;
        public const int MinAudibleChangeBars = 1;
        public const double PhraseWindowSeconds = 0.5d;
        public const int BarsPerPhrase = 8;

        private bool _haveSample;
        private double _filterA;
        private double _filterB;
        private double _percA;
        private double _percB;
        private double _musicA;
        private double _musicB;

        private bool _sweepActive;
        private int _sweepDeck = -1;
        private double _sweepStart;
        private double _sweepOrigin;
        private double _sweepLastMove;

        private int _filterPhrase = -1;
        private int _stemBar = -1;
        private bool _stemFiredThisArm;

        public void Reset()
        {
            _haveSample = false;
            _filterA = 0d;
            _filterB = 0d;
            _percA = 1d;
            _percB = 1d;
            _musicA = 1d;
            _musicB = 1d;
            ClearSweep();
            _filterPhrase = -1;
            _stemBar = -1;
            _stemFiredThisArm = false;
        }

        /// <summary>
        /// Writes zero or more ScoreEvents into <paramref name="results"/> starting at
        /// <paramref name="offset"/>. Returns how many were written.
        /// </summary>
        public int Collect(
            MixSnapshot snapshot,
            MusicPosition position,
            double phraseDuration,
            bool dropArmed,
            ScoreEvent[] results,
            int offset)
        {
            if (results == null || offset < 0 || offset >= results.Length)
                return 0;
            if (snapshot.Paused)
            {
                ClearSweep();
                return 0;
            }

            int written = 0;
            written += TryFilterPhrase(snapshot, position, phraseDuration, results, offset + written);
            written += TryStemPunch(snapshot, position, dropArmed, results, offset + written);
            Remember(snapshot);
            if (!dropArmed)
                _stemFiredThisArm = false;
            return written;
        }

        private int TryFilterPhrase(
            MixSnapshot snapshot,
            MusicPosition position,
            double phraseDuration,
            ScoreEvent[] results,
            int index)
        {
            if (index >= results.Length)
                return 0;

            TrackSweep(snapshot, 0, snapshot.FilterA, snapshot.DeckAPlaying, snapshot.GainA);
            TrackSweep(snapshot, 1, snapshot.FilterB, snapshot.DeckBPlaying, snapshot.GainB);

            if (!_sweepActive || _sweepDeck < 0)
                return 0;
            if (_filterPhrase == position.Phrase)
                return 0;

            double current = _sweepDeck == 0 ? snapshot.FilterA : snapshot.FilterB;
            double travel = Math.Abs(current - _sweepOrigin);
            if (travel < FilterDeltaMin)
                return 0;
            double minSeconds = BarSeconds(phraseDuration) * MinAudibleChangeBars;
            if (snapshot.AudioTime - _sweepStart + 1e-9d < minSeconds)
                return 0;
            if (!NearPhraseBoundary(position, phraseDuration))
                return 0;

            bool audible = _sweepDeck == 0
                ? snapshot.DeckAPlaying && snapshot.GainA >= AudibleGain
                : snapshot.DeckBPlaying && snapshot.GainB >= AudibleGain;
            if (!audible)
                return 0;

            _filterPhrase = position.Phrase;
            ClearSweep();
            results[index] = ScoreEvent.FilterPhrase(position.Phrase);
            return 1;
        }

        private void TrackSweep(
            MixSnapshot snapshot,
            int deck,
            double filter,
            bool playing,
            double gain)
        {
            if (!_haveSample)
                return;

            double previous = deck == 0 ? _filterA : _filterB;
            double step = Math.Abs(filter - previous);
            bool audible = playing && gain >= AudibleGain;

            if (!_sweepActive)
            {
                if (step >= FilterDeltaMin * 0.25d && audible)
                {
                    _sweepActive = true;
                    _sweepDeck = deck;
                    _sweepStart = snapshot.AudioTime;
                    _sweepOrigin = previous;
                    _sweepLastMove = snapshot.AudioTime;
                }

                return;
            }

            if (_sweepDeck != deck)
                return;

            if (step >= 1e-4d)
                _sweepLastMove = snapshot.AudioTime;

            // Idle after a move: keep the sweep until the phrase boundary check (1 bar grace).
            double idleLimit = 2d;
            if (!audible && snapshot.AudioTime - _sweepLastMove > idleLimit)
                ClearSweep();
        }

        private int TryStemPunch(
            MixSnapshot snapshot,
            MusicPosition position,
            bool dropArmed,
            ScoreEvent[] results,
            int index)
        {
            if (index >= results.Length)
                return 0;
            if (!dropArmed || !_haveSample || _stemFiredThisArm)
                return 0;
            if (_stemBar == position.Bar)
                return 0;

            bool raised = false;
            if (snapshot.DeckAPlaying && snapshot.GainA >= AudibleGain)
            {
                if (snapshot.PercA - _percA >= StemDeltaMin || snapshot.MusicA - _musicA >= StemDeltaMin)
                    raised = true;
            }

            if (!raised && snapshot.DeckBPlaying && snapshot.GainB >= AudibleGain)
            {
                if (snapshot.PercB - _percB >= StemDeltaMin || snapshot.MusicB - _musicB >= StemDeltaMin)
                    raised = true;
            }

            if (!raised)
                return 0;

            _stemBar = position.Bar;
            _stemFiredThisArm = true;
            results[index] = ScoreEvent.StemPunch(position.Bar);
            return 1;
        }

        private static double BarSeconds(double phraseDuration)
        {
            if (phraseDuration <= 0d)
                return 2d;
            return phraseDuration / BarsPerPhrase;
        }

        private void Remember(MixSnapshot snapshot)
        {
            _haveSample = true;
            _filterA = snapshot.FilterA;
            _filterB = snapshot.FilterB;
            _percA = snapshot.PercA;
            _percB = snapshot.PercB;
            _musicA = snapshot.MusicA;
            _musicB = snapshot.MusicB;
        }

        private void ClearSweep()
        {
            _sweepActive = false;
            _sweepDeck = -1;
            _sweepStart = 0d;
            _sweepOrigin = 0d;
            _sweepLastMove = 0d;
        }

        private static bool NearPhraseBoundary(MusicPosition position, double phraseDuration)
        {
            if (phraseDuration <= 0d)
                return false;

            // Distance into the current phrase in seconds (barInPhrase * bar + beat fraction ≈ via audio).
            double phraseStart = position.Phrase * phraseDuration;
            double into = position.AudioTime - phraseStart;
            if (into < 0d)
                into = 0d;
            double toEnd = phraseDuration - into;
            if (toEnd < 0d)
                toEnd = 0d;
            return into <= PhraseWindowSeconds || toEnd <= PhraseWindowSeconds;
        }
    }
}
