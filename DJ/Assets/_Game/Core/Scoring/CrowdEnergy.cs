namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// CrowdMotion tier for PixelClubView (runtime cheerIntensity 0..3).
    /// Bumps on quality mix gestures, holds a couple of bars, then decays one step per bar.
    /// </summary>
    public sealed class CrowdEnergy
    {
        public const int MaxIntensity = 3;
        public const int HoldBars = 2;

        private int _holdRemaining;

        public int CheerIntensity { get; private set; }

        public void Note(ScoreKind kind)
        {
            if (!Bumps(kind))
                return;

            if (CheerIntensity < MaxIntensity)
                CheerIntensity++;
            _holdRemaining = HoldBars;
        }

        /// <summary>
        /// Call once when the music bar index advances.
        /// </summary>
        public void AdvanceBar()
        {
            if (_holdRemaining > 0)
            {
                _holdRemaining--;
                return;
            }

            if (CheerIntensity > 0)
                CheerIntensity--;
        }

        public void Reset()
        {
            CheerIntensity = 0;
            _holdRemaining = 0;
        }

        public static bool Bumps(ScoreKind kind)
        {
            return kind == ScoreKind.CleanTransition
                || kind == ScoreKind.Drop
                || kind == ScoreKind.CueReadyHonor
                || kind == ScoreKind.CleanPrepTransition
                || kind == ScoreKind.FilterPhrase
                || kind == ScoreKind.StemPunch
                || kind == ScoreKind.LoopSnap
                || kind == ScoreKind.PhraseStart
                || kind == ScoreKind.BassHandoff
                || kind == ScoreKind.LineLift;
        }
    }
}
