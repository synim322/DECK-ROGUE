using System;
using DeckRogue.Core.Music;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;

namespace DeckRogue.Core.Run
{
    public enum RunPhase
    {
        PreClub = 0,
        Performing = 1,
        Result = 2,
        FinalResult = 3,
        Reward = 4
    }

    public enum SetOutcome
    {
        InProgress = 0,
        Win = 1,
        Lose = 2
    }

    public enum ClubRuleId
    {
        None = 0,
        MaintainEnergy = 1,
        CleanDrop = 2
    }

    public readonly struct ClubReadout
    {
        public ClubReadout(
            RunPhase phase,
            int setIndex,
            int setCount,
            string clubId,
            ClubRuleId rule,
            double hype,
            double targetHype,
            double remainingSeconds,
            SetOutcome outcome,
            string reasonId,
            string offeredLeft,
            string offeredRight,
            string chosenUpgradeId,
            bool effectAudible)
        {
            Phase = phase;
            SetIndex = setIndex;
            SetCount = setCount;
            ClubId = clubId ?? "";
            Rule = rule;
            Hype = hype;
            TargetHype = targetHype;
            RemainingSeconds = remainingSeconds;
            Outcome = outcome;
            ReasonId = reasonId ?? "";
            OfferedLeft = offeredLeft ?? "";
            OfferedRight = offeredRight ?? "";
            ChosenUpgradeId = chosenUpgradeId ?? "";
            EffectAudible = effectAudible;
        }

        public RunPhase Phase { get; }
        public int SetIndex { get; }
        public int SetCount { get; }
        public string ClubId { get; }
        public ClubRuleId Rule { get; }
        public double Hype { get; }
        public double TargetHype { get; }
        public double RemainingSeconds { get; }
        public SetOutcome Outcome { get; }
        public string ReasonId { get; }
        public string OfferedLeft { get; }
        public string OfferedRight { get; }
        public string ChosenUpgradeId { get; }
        public bool EffectAudible { get; }
    }

    /// <summary>
    /// Two sets at club_neon_basement. The clock is the mix snapshot's audio time.
    /// Set 2's extra rule is chosen once from the run seed.
    /// A set-1 win stops on Reward until Choose equips one offered upgrade for set 2.
    /// </summary>
    public sealed class ClubDirector
    {
        public const string NeonBasementId = "club_neon_basement";
        public const int SetCount = 2;
        public const int SetLengthBars = 64;
        public const double FirstTargetHype = 60d;
        public const double SecondTargetHype = 65d;

        public const string ReasonTargetMet = "target_met";
        public const string ReasonTimeout = "timeout";
        public const string ReasonHypeFloor = "hype_floor";
        public const string ReasonCleanDropMissing = "clean_drop_missing";
        public const string ReasonEnergyLost = "energy_lost";

        private readonly MusicClock _clock;
        private readonly ScoreChange[] _applied = new ScoreChange[8];
        private readonly double _setDuration;

        private SetJudge _judge;
        private ClubRuleId _rule;
        private RunPhase _phase;
        private SetOutcome _outcome;
        private int _setIndex;
        private string _reason;
        private double _elapsed;
        private double _lastAudioTime;
        private bool _anchored;
        private bool _hadDrop;
        private bool _energyLost;
        private RewardOffer _offer;
        private string _chosen = "";
        private bool _effectAudible;

        public ClubDirector(int seed)
        {
            _clock = new MusicClock(new ManualTimeProvider(), MusicClock.ReferenceBpm, 0d, 0d);
            _setDuration = _clock.BarDuration * SetLengthBars;
            Reset(seed);
        }

        public double Reputation => _judge.Reputation;

        public int Combo => _judge.Combo;

        public RewardOffer Offer => _offer;

        public string ChosenUpgradeId => _chosen;

        public bool EffectAudible => _effectAudible;

        public ClubReadout Readout
        {
            get
            {
                double remaining = _phase == RunPhase.PreClub
                    ? _setDuration
                    : _setDuration - _elapsed;
                if (remaining < 0d)
                    remaining = 0d;

                return new ClubReadout(
                    _phase,
                    _setIndex,
                    SetCount,
                    NeonBasementId,
                    _setIndex >= 2 ? _rule : ClubRuleId.None,
                    _judge.Hype,
                    _setIndex >= 2 ? SecondTargetHype : FirstTargetHype,
                    remaining,
                    _outcome,
                    _reason,
                    _offer.Left,
                    _offer.Right,
                    _chosen,
                    _effectAudible);
            }
        }

        public void BeginSet()
        {
            if (_phase != RunPhase.PreClub)
                return;

            _phase = RunPhase.Performing;
            _outcome = SetOutcome.InProgress;
            _reason = "";
            _elapsed = 0d;
            _anchored = false;
            _hadDrop = false;
            _energyLost = false;
        }

        public void Tick(MixSnapshot snapshot)
        {
            if (_phase != RunPhase.Performing)
                return;
            if (double.IsNaN(snapshot.AudioTime) || double.IsInfinity(snapshot.AudioTime))
                return;

            if (!_anchored)
            {
                _anchored = true;
                _lastAudioTime = snapshot.AudioTime;
                if (snapshot.Paused)
                {
                    _judge.Observe(snapshot, _applied);
                    _effectAudible = false;
                }
                else
                    Score(snapshot);
                CloseIfNeeded();
                return;
            }

            if (snapshot.AudioTime < _lastAudioTime)
                return;

            if (snapshot.Paused)
            {
                _judge.Observe(snapshot, _applied);
                _lastAudioTime = snapshot.AudioTime;
                _effectAudible = false;
                return;
            }

            _elapsed += snapshot.AudioTime - _lastAudioTime;
            _lastAudioTime = snapshot.AudioTime;
            Score(snapshot);
            CloseIfNeeded();
        }

        /// <summary>
        /// Leaves the set-1 result and opens the reward. Set 2 stays closed until Choose.
        /// </summary>
        public void Continue()
        {
            if (_phase != RunPhase.Result || _outcome != SetOutcome.Win || _setIndex != 1)
                return;

            _phase = RunPhase.Reward;
            _effectAudible = false;
        }

        /// <summary>
        /// Equips one offered upgrade and opens set 2. Any other id is ignored.
        /// </summary>
        public bool Choose(string upgradeId)
        {
            if (_phase != RunPhase.Reward || !_offer.Contains(upgradeId))
                return false;

            _chosen = upgradeId;
            _setIndex = 2;
            _phase = RunPhase.PreClub;
            _outcome = SetOutcome.InProgress;
            _reason = "";
            _elapsed = 0d;
            _anchored = false;
            _hadDrop = false;
            _energyLost = false;
            _effectAudible = false;
            _judge.BeginNextSet();
            return true;
        }

        public void Reset(int seed)
        {
            _rule = RuleFor(seed);
            _offer = RewardRoll.From(seed);
            _chosen = "";
            _judge = new SetJudge(_clock);
            _setIndex = 1;
            _phase = RunPhase.PreClub;
            _outcome = SetOutcome.InProgress;
            _reason = "";
            _elapsed = 0d;
            _anchored = false;
            _hadDrop = false;
            _energyLost = false;
            _effectAudible = false;
        }

        private void Score(MixSnapshot snapshot)
        {
            bool ducked = false;
            if (_chosen == UpgradeId.AutoBass)
                snapshot = AutoBassDucking.Apply(snapshot, out ducked);

            double dropBonus = _chosen == UpgradeId.Riser ? UpgradeId.RiserDropHype : 0d;
            double cutBonus = _chosen == UpgradeId.HardCut ? UpgradeId.HardCutHype : 0d;
            int count = _judge.Observe(snapshot, _applied, dropBonus, cutBonus);
            _effectAudible = Heard(ducked, count);
            for (int i = 0; i < count; i++)
            {
                if (_applied[i].Kind == ScoreKind.Drop)
                    _hadDrop = true;
                else if (_applied[i].Kind == ScoreKind.StaleGroove)
                    _energyLost = true;
            }
        }

        private bool Heard(bool ducked, int count)
        {
            if (_chosen == UpgradeId.AutoBass)
                return ducked;
            if (_chosen == UpgradeId.Riser)
                return _judge.HoldingRise;
            if (_chosen != UpgradeId.HardCut)
                return false;

            for (int i = 0; i < count; i++)
            {
                if (_applied[i].Kind == ScoreKind.CleanTransition && _applied[i].HardCut)
                    return true;
            }

            return false;
        }

        private void CloseIfNeeded()
        {
            if (_phase != RunPhase.Performing)
                return;
            if (_judge.IsLost)
            {
                Finish(SetOutcome.Lose, ReasonHypeFloor, RunPhase.FinalResult);
                return;
            }

            if (_elapsed + 1e-9 < _setDuration)
                return;

            double target = _setIndex >= 2 ? SecondTargetHype : FirstTargetHype;
            if (_judge.Hype < target)
            {
                Finish(SetOutcome.Lose, ReasonTimeout, RunPhase.FinalResult);
                return;
            }

            if (_setIndex >= 2 && _rule == ClubRuleId.CleanDrop && !_hadDrop)
            {
                Finish(SetOutcome.Lose, ReasonCleanDropMissing, RunPhase.FinalResult);
                return;
            }

            // PhraseDecay is the passive -2 on every phrase. It is not this rule.
            // StaleGroove is the accepted "energy lost" event (stale-{phrase}, -4).
            if (_setIndex >= 2 && _rule == ClubRuleId.MaintainEnergy && _energyLost)
            {
                Finish(SetOutcome.Lose, ReasonEnergyLost, RunPhase.FinalResult);
                return;
            }

            if (_setIndex <= 1)
                Finish(SetOutcome.Win, ReasonTargetMet, RunPhase.Result);
            else
                Finish(SetOutcome.Win, ReasonTargetMet, RunPhase.FinalResult);
        }

        private void Finish(SetOutcome outcome, string reason, RunPhase phase)
        {
            _outcome = outcome;
            _reason = reason;
            _phase = phase;
        }

        private static ClubRuleId RuleFor(int seed)
        {
            unchecked
            {
                uint x = (uint)seed;
                x ^= x >> 16;
                x *= 0x7feb352du;
                x ^= x >> 15;
                x *= 0x846ca68bu;
                x ^= x >> 16;
                return (x & 1u) == 0u ? ClubRuleId.MaintainEnergy : ClubRuleId.CleanDrop;
            }
        }
    }
}
