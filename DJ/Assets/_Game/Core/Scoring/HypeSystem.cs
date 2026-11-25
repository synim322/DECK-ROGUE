using System;
using System.Collections.Generic;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Hype, Combo and Reputation. One event id scores once.
    /// Pause drops the event instead of banking it for later.
    /// </summary>
    public sealed class HypeSystem
    {
        public const double StartingHype = 35d;

        private readonly HashSet<string> _seen = new HashSet<string>();
        private readonly CrowdEnergy _crowd = new CrowdEnergy();
        private ScoreKind _lastQuality;
        private bool _hasQuality;
        private int _variety;
        private double _zeroSince = -1d;

        public HypeSystem()
        {
            Hype = StartingHype;
            Combo = 1;
        }

        public double Hype { get; private set; }
        public int Combo { get; private set; }
        public double Reputation { get; private set; }
        public bool Paused { get; set; }
        public bool IsLost { get; private set; }
        public int CheerIntensity => _crowd.CheerIntensity;

        public int CrowdBand
        {
            get
            {
                if (Hype >= 70d)
                    return 2;
                if (Hype >= 40d)
                    return 1;
                return 0;
            }
        }

        public bool TryApply(ScoreEvent scoreEvent, out ScoreChange change)
        {
            change = default;
            if (Paused || scoreEvent.Id == null || scoreEvent.Id.Length == 0)
                return false;
            if (!_seen.Add(scoreEvent.Id))
                return false;

            if (scoreEvent.ResetsCombo)
                ResetCombo();
            else if (scoreEvent.IsQuality)
                NoteQuality(scoreEvent.Kind);

            double reputationDelta = scoreEvent.ReputationBase * Combo;
            Hype = Clamp(Hype + scoreEvent.HypeDelta, 0d, 100d);
            Reputation = Math.Max(0d, Reputation + reputationDelta);
            _crowd.Note(scoreEvent.Kind);
            change = new ScoreChange(
                scoreEvent.Id,
                scoreEvent.Kind,
                scoreEvent.HypeDelta,
                reputationDelta,
                Hype,
                Combo,
                Reputation,
                scoreEvent.HardCut);
            return true;
        }

        /// <summary>
        /// Advances hall cheer decay when the music bar index moves forward.
        /// </summary>
        public void AdvanceCrowdBar()
        {
            _crowd.AdvanceBar();
        }

        public void AdvanceDefeat(double audioTime, double phraseDuration)
        {
            if (Paused)
                return;
            if (Hype > 0d)
            {
                _zeroSince = -1d;
                return;
            }

            if (_zeroSince < 0d)
                _zeroSince = audioTime;
            else if (audioTime - _zeroSince >= phraseDuration)
                IsLost = true;
        }

        /// <summary>
        /// Silence does not count toward the empty-room timer.
        /// </summary>
        public void HoldDefeatClock(double audioTime)
        {
            if (_zeroSince >= 0d)
                _zeroSince = audioTime;
        }

        /// <summary>
        /// Next set keeps Reputation. Hype, Combo, the defeat clock and seen event ids start over.
        /// </summary>
        public void BeginNextSet()
        {
            BeginNextSet(StartingHype);
        }

        /// <summary>
        /// Same as <see cref="BeginNextSet()"/> but opens at a club-specific initial Hype (Act1 Signal = 40).
        /// </summary>
        public void BeginNextSet(double initialHype)
        {
            Hype = Clamp(initialHype, 0d, 100d);
            Combo = 1;
            IsLost = false;
            Paused = false;
            _seen.Clear();
            _lastQuality = default(ScoreKind);
            _hasQuality = false;
            _variety = 0;
            _zeroSince = -1d;
            _crowd.Reset();
        }

        /// <summary>
        /// Mid-run restore: keep Reputation across the night, re-open current set numbers.
        /// Seen-event ids clear so scoring continues from a clean pause boundary.
        /// </summary>
        public void Restore(double hype, int combo, double reputation)
        {
            Hype = Clamp(hype, 0d, 100d);
            Combo = combo < 1 ? 1 : (combo > 3 ? 3 : combo);
            Reputation = reputation < 0d ? 0d : reputation;
            IsLost = Hype <= 0d;
            Paused = false;
            _seen.Clear();
            _lastQuality = default(ScoreKind);
            _hasQuality = false;
            _variety = Combo >= 3 ? 4 : (Combo >= 2 ? 2 : 0);
            _zeroSince = -1d;
            _crowd.Reset();
        }

        private void NoteQuality(ScoreKind kind)
        {
            if (!_hasQuality || kind != _lastQuality)
            {
                _variety++;
                _hasQuality = true;
                _lastQuality = kind;
            }

            if (_variety >= 4)
                Combo = 3;
            else if (_variety >= 2)
                Combo = 2;
            else
                Combo = 1;
        }

        private void ResetCombo()
        {
            Combo = 1;
            _variety = 0;
            _hasQuality = false;
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
