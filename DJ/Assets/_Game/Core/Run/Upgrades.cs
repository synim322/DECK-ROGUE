using DeckRogue.Core.Scoring;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// MVP pool. One card per run, no stacking. Ids follow UpgradeDefinition.
    /// </summary>
    public static class UpgradeId
    {
        public const string AutoBass = "upgrade_auto_bass";
        public const string Riser = "upgrade_riser";
        public const string HardCut = "upgrade_hard_cut";

        /// <summary>UpgradeDefinition params.secondaryLowMaxGain.</summary>
        public const double AutoBassLowMax = 0.25d;

        /// <summary>Riser Machine: extra Hype on the first credited drop of a phrase.</summary>
        public const double RiserDropHype = 4d;

        /// <summary>Hard Cut Specialist: extra Hype on a clean hard cut. Reputation figure is not in the doc.</summary>
        public const double HardCutHype = 4d;

        public static bool IsKnown(string id)
        {
            return id == AutoBass || id == Riser || id == HardCut;
        }
    }

    public readonly struct RewardOffer
    {
        public RewardOffer(string left, string right)
        {
            Left = left ?? "";
            Right = right ?? "";
        }

        public string Left { get; }
        public string Right { get; }

        public bool Contains(string id)
        {
            return id != null && id.Length > 0 && (id == Left || id == Right);
        }
    }

    /// <summary>
    /// Two distinct upgrades out of the three. Salted apart from the club-rule bit
    /// so the reward roll is not the same coin flip as MaintainEnergy / CleanDrop.
    /// </summary>
    public static class RewardRoll
    {
        private static readonly string[] Pool =
        {
            UpgradeId.AutoBass,
            UpgradeId.Riser,
            UpgradeId.HardCut
        };

        public static RewardOffer From(int seed)
        {
            unchecked
            {
                uint firstMix = Mix((uint)seed ^ 0xA24BAED4u);
                int first = (int)(firstMix % 3u);
                uint secondMix = Mix(firstMix ^ 0x6C8E9CF5u);
                int second = (secondMix & 1u) == 0u ? (first + 1) % 3 : (first + 2) % 3;
                return new RewardOffer(Pool[first], Pool[second]);
            }
        }

        private static uint Mix(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return x;
        }
    }

    /// <summary>
    /// Caps the non-dominant deck's LOW in the snapshot scoring reads.
    /// A deck is dominant the same way TransitionEvaluator decides it.
    /// With no leading deck both lows stay put, so a full blend can still clash.
    /// </summary>
    public static class AutoBassDucking
    {
        public static MixSnapshot Apply(MixSnapshot snapshot, out bool ducked)
        {
            ducked = false;
            int dominant = Dominant(snapshot);
            if (dominant < 0)
                return snapshot;

            double lowA = snapshot.LowA;
            double lowB = snapshot.LowB;
            if (dominant == 0)
                lowB = Cap(lowB, ref ducked);
            else
                lowA = Cap(lowA, ref ducked);

            if (!ducked)
                return snapshot;

            return new MixSnapshot(
                snapshot.AudioTime,
                snapshot.Paused,
                snapshot.DeckAPlaying,
                snapshot.DeckBPlaying,
                snapshot.GainA,
                snapshot.GainB,
                lowA,
                lowB,
                snapshot.TrackA,
                snapshot.TrackB,
                snapshot.ClubMatch,
                snapshot.FilterA,
                snapshot.FilterB,
                snapshot.PercA,
                snapshot.PercB,
                snapshot.MusicA,
                snapshot.MusicB,
                snapshot.LoopBarsA,
                snapshot.LoopBarsB,
                snapshot.QuantizedStartA,
                snapshot.QuantizedStartB,
                snapshot.ChannelA,
                snapshot.ChannelB);
        }

        private static int Dominant(MixSnapshot snapshot)
        {
            if (snapshot.DeckAPlaying
                && snapshot.GainA >= TransitionEvaluator.DominantGain
                && snapshot.GainB <= TransitionEvaluator.QuietGain)
                return 0;
            if (snapshot.DeckBPlaying
                && snapshot.GainB >= TransitionEvaluator.DominantGain
                && snapshot.GainA <= TransitionEvaluator.QuietGain)
                return 1;
            return -1;
        }

        private static double Cap(double low, ref bool ducked)
        {
            if (double.IsNaN(low) || double.IsInfinity(low))
                return low;
            if (low > UpgradeId.AutoBassLowMax)
            {
                ducked = true;
                return UpgradeId.AutoBassLowMax;
            }

            return low;
        }
    }
}
