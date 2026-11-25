using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// A drop is a prepared bass return: LOW under 0.20 for at least one bar,
    /// then back above 0.75 on a bar downbeat. Cooldown is four bars.
    /// An instant bass-kill toggle never arms the detector.
    /// </summary>
    public sealed class DropDetector
    {
        public const double CutThreshold = 0.20d;
        public const double ReturnThreshold = 0.75d;
        public const int CooldownBars = 4;

        private double _cutSince = -1d;
        private bool _armed;
        private int _cooldownUntilBar = -1;

        public void ResetPrepare()
        {
            _cutSince = -1d;
            _armed = false;
        }

        public void ResetAll()
        {
            ResetPrepare();
            _cooldownUntilBar = -1;
        }

        /// <summary>LOW has stayed under the cut for at least a bar. The return has not landed yet.</summary>
        public bool HoldingRise => _armed;

        public bool TryRead(MixSnapshot snapshot, MusicPosition position, double barDuration, out ScoreEvent scoreEvent)
        {
            scoreEvent = default;
            bool audible = (snapshot.DeckAPlaying && snapshot.GainA >= 0.35d)
                || (snapshot.DeckBPlaying && snapshot.GainB >= 0.35d);
            if (!audible)
            {
                ResetPrepare();
                return false;
            }

            double low = snapshot.LowA + snapshot.LowB;
            if (low < CutThreshold)
            {
                if (_cutSince < 0d)
                    _cutSince = snapshot.AudioTime;
                if (!_armed && snapshot.AudioTime - _cutSince >= barDuration)
                    _armed = true;
                return false;
            }

            if (!_armed)
            {
                _cutSince = -1d;
                return false;
            }

            if (low <= ReturnThreshold)
                return false;

            bool onBar = position.BeatInBar == 0;
            bool cooled = _cooldownUntilBar < 0 || position.Bar >= _cooldownUntilBar;
            bool prepared = snapshot.AudioTime - _cutSince >= barDuration;
            if (onBar && cooled && prepared)
            {
                int bar = position.Bar;
                _cooldownUntilBar = bar + CooldownBars;
                ResetPrepare();
                scoreEvent = ScoreEvent.Drop(bar);
                return true;
            }

            ResetPrepare();
            return false;
        }
    }
}
