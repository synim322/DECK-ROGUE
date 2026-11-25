using DeckRogue.Core.Audio;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Applies the dominant deck's aura to one score event. Hype only.
    /// </summary>
    public static class TrackAuraModifier
    {
        /// <summary>
        /// Dominant audible deck: if both playing, higher gain wins; tie goes to B.
        /// If only one playing, that one. If neither, empty.
        /// </summary>
        public static string DominantTrackId(MixSnapshot snapshot)
        {
            if (snapshot.DeckAPlaying && snapshot.DeckBPlaying)
            {
                if (snapshot.GainA > snapshot.GainB)
                    return snapshot.TrackA;
                return snapshot.TrackB;
            }

            if (snapshot.DeckAPlaying)
                return snapshot.TrackA;
            if (snapshot.DeckBPlaying)
                return snapshot.TrackB;
            return "";
        }

        /// <summary>
        /// Returns scoreEvent unchanged when the dominant track is not special.
        /// Adjusts HypeDelta only via <see cref="ScoreEvent.WithBonus"/>.
        /// </summary>
        public static ScoreEvent Apply(ScoreEvent scoreEvent, MixSnapshot snapshot)
        {
            double hype = HypeAdjustment(TrackAura.MoodOf(DominantTrackId(snapshot)), scoreEvent);
            if (hype == 0d)
                return scoreEvent;
            return scoreEvent.WithBonus(hype, 0d);
        }

        private static double HypeAdjustment(TrackMood mood, ScoreEvent scoreEvent)
        {
            ScoreKind kind = scoreEvent.Kind;
            if (mood == TrackMood.AmberBloom)
            {
                if (kind == ScoreKind.CleanTransition
                    || kind == ScoreKind.CleanPrepTransition
                    || kind == ScoreKind.Drop)
                    return 3d;
                if (kind == ScoreKind.BassConflict)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.VoidStatic)
            {
                if (kind == ScoreKind.MissedTransition
                    || kind == ScoreKind.LateLoad
                    || kind == ScoreKind.DeadAir)
                {
                    if (scoreEvent.HypeDelta < 0d)
                        return -scoreEvent.HypeDelta;
                    return 0d;
                }

                if (kind == ScoreKind.CleanTransition
                    || kind == ScoreKind.CleanPrepTransition
                    || kind == ScoreKind.Drop)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.GlassRain)
            {
                if (kind == ScoreKind.FilterPhrase
                    || kind == ScoreKind.StemPunch
                    || kind == ScoreKind.LoopSnap)
                    return 3d;
                if (kind == ScoreKind.Fuss || kind == ScoreKind.HotSwapFumble)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.RedlineSiren)
            {
                if (kind == ScoreKind.Drop)
                    return 5d;
                if (kind == ScoreKind.StaleGroove || kind == ScoreKind.PhraseDecay)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.VelvetHush)
            {
                if (kind == ScoreKind.PhraseDecay || kind == ScoreKind.StaleGroove)
                {
                    if (scoreEvent.HypeDelta < 0d)
                        return -scoreEvent.HypeDelta;
                    return 0d;
                }

                if (kind == ScoreKind.Drop)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.Hardline)
            {
                if (kind == ScoreKind.Drop)
                    return 4d;
                if (kind == ScoreKind.StemPunch)
                    return 3d;
                if (kind == ScoreKind.BassConflict)
                    return -4d;
                return 0d;
            }

            if (mood == TrackMood.SaltChrome)
            {
                if (kind == ScoreKind.CleanTransition || kind == ScoreKind.BassHandoff)
                    return 3d;
                if (kind == ScoreKind.MissedTransition)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.PrismChoir)
            {
                if (kind == ScoreKind.FilterPhrase)
                    return 4d;
                if (kind == ScoreKind.LoopSnap)
                    return 3d;
                if (kind == ScoreKind.Fuss)
                    return -4d;
                return 0d;
            }

            if (mood == TrackMood.Ledger)
            {
                if (kind == ScoreKind.CleanPrepTransition)
                    return 4d;
                if (kind == ScoreKind.HotSwapFumble)
                    return -4d;
                return 0d;
            }

            if (mood == TrackMood.Skywire)
            {
                if (kind == ScoreKind.LineLift)
                    return 4d;
                if (kind == ScoreKind.PhraseStart)
                    return 2d;
                if (kind == ScoreKind.PhraseDecay)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.StaticCrown)
            {
                if (kind == ScoreKind.CueReadyHonor)
                    return 4d;
                if (kind == ScoreKind.StaleGroove)
                    return -4d;
                if (kind == ScoreKind.DeadAir && scoreEvent.HypeDelta < 0d)
                    return -3d;
                return 0d;
            }

            if (mood == TrackMood.FestivalAnthem)
            {
                if (kind == ScoreKind.Drop)
                    return 4d;
                if (kind == ScoreKind.CleanTransition)
                    return 2d;
                if (kind == ScoreKind.BassConflict)
                    return -5d;
                return 0d;
            }

            return 0d;
        }
    }
}
