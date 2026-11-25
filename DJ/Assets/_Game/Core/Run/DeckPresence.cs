using DeckRogue.Core.Music;
using DeckRogue.Core.Scoring;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Dual-deck constraints: DeadAir sustain and HotSwapFumble helper.
    /// </summary>
    public sealed class DeckPresence
    {
        /// <summary>Below this gain a deck is not audible (same floor as QuietGain).</summary>
        public const double AudibleThreshold = TransitionEvaluator.QuietGain;

        public const int DeadAirBeatCount = 2;

        private double _quietSince = -1d;
        private bool _deadAir;

        public bool IsDeadAir => _deadAir;

        public void Reset()
        {
            _quietSince = -1d;
            _deadAir = false;
        }

        /// <summary>
        /// DeadAir when both audible gains stay below <see cref="AudibleThreshold"/> for more than two beats.
        /// Gains are already zero when a deck is not playing.
        /// </summary>
        public bool Tick(MixSnapshot snapshot, double beatDuration)
        {
            if (snapshot.Paused || beatDuration <= 0d)
            {
                _quietSince = -1d;
                _deadAir = false;
                return false;
            }

            bool bothQuiet = snapshot.GainA < AudibleThreshold && snapshot.GainB < AudibleThreshold;
            if (!bothQuiet)
            {
                _quietSince = -1d;
                _deadAir = false;
                return false;
            }

            if (_quietSince < 0d)
                _quietSince = snapshot.AudioTime;

            _deadAir = snapshot.AudioTime - _quietSince > beatDuration * DeadAirBeatCount;
            return _deadAir;
        }

        /// <summary>
        /// Stop of the dominant deck mid-phrase with no audible handoff on the other deck.
        /// </summary>
        public static bool IsHotSwapFumble(
            MusicPosition position,
            bool stoppedDominant,
            bool otherDeckAudible)
        {
            if (!stoppedDominant || otherDeckAudible)
                return false;

            bool atPhraseStart = position.BarInPhrase == 0 && position.BeatInBar == 0;
            return !atPhraseStart;
        }
    }
}
