using DeckRogue.Core.Music;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class ScoringTests
    {
        private readonly ScoreChange[] _changes = new ScoreChange[8];

        [Test]
        public void Hype_ClampsAndIgnoresARepeatedEvent()
        {
            var hype = new HypeSystem();
            Assert.IsTrue(hype.TryApply(ScoreEvent.Clean(0, 0, 1, false), out ScoreChange first));
            Assert.AreEqual(43d, first.Hype, 1e-9d);
            Assert.AreEqual(100d, first.Reputation, 1e-9d);
            Assert.IsFalse(hype.TryApply(ScoreEvent.Clean(0, 0, 1, false), out _));

            for (int i = 0; i < 20; i++)
                hype.TryApply(ScoreEvent.Clean(i + 1, 0, 1, false), out _);
            Assert.AreEqual(100d, hype.Hype, 1e-9d);

            for (int i = 0; i < 30; i++)
                hype.TryApply(ScoreEvent.Bass(i), out _);
            Assert.AreEqual(0d, hype.Hype, 1e-9d);
            Assert.GreaterOrEqual(hype.Reputation, 0d);
        }

        [Test]
        public void Hype_PauseDoesNotScore_AndZeroForAPhraseLoses()
        {
            var hype = new HypeSystem();
            hype.Paused = true;
            Assert.IsFalse(hype.TryApply(ScoreEvent.Drop(1), out _));
            Assert.AreEqual(35d, hype.Hype, 1e-9d);

            hype.Paused = false;
            hype.TryApply(ScoreEvent.Bass(0), out _);
            hype.TryApply(ScoreEvent.Bass(1), out _);
            hype.TryApply(ScoreEvent.Bass(2), out _);
            hype.TryApply(ScoreEvent.Bass(3), out _);
            hype.TryApply(ScoreEvent.Bass(4), out _);
            hype.TryApply(ScoreEvent.Bass(5), out _);
            Assert.AreEqual(0d, hype.Hype, 1e-9d);

            hype.AdvanceDefeat(0d, 16d);
            hype.AdvanceDefeat(15d, 16d);
            Assert.IsFalse(hype.IsLost);
            hype.AdvanceDefeat(16d, 16d);
            Assert.IsTrue(hype.IsLost);
        }

        [Test]
        public void Combo_SecondDifferentEventUsesTimesTwo_ConflictResets()
        {
            var hype = new HypeSystem();
            hype.TryApply(ScoreEvent.Clean(0, 0, 1, false), out ScoreChange clean);
            hype.TryApply(ScoreEvent.Drop(4), out ScoreChange drop);
            Assert.AreEqual(1, clean.Combo);
            Assert.AreEqual(100d, clean.ReputationDelta, 1e-9d);
            Assert.AreEqual(2, drop.Combo);
            Assert.AreEqual(280d, drop.ReputationDelta, 1e-9d);

            hype.TryApply(ScoreEvent.Bass(0), out ScoreChange conflict);
            Assert.AreEqual(1, conflict.Combo);
            hype.TryApply(ScoreEvent.Clean(1, 1, 0, true), out ScoreChange again);
            Assert.AreEqual(1, again.Combo);
            Assert.AreEqual(100d, again.ReputationDelta, 1e-9d);
        }

        [Test]
        public void CleanTransition_PaysOnce_BouncePaysNothing()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, FullA(0.5d));
            Feed(judge, Middle(2d, 0d, 0d));
            Feed(judge, FullB(4d));
            int count = Feed(judge, FullB(4.5d));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.CleanTransition, _changes[0].Kind);
            Assert.IsFalse(_changes[0].HardCut);
            Assert.AreEqual(43d, judge.Hype, 1e-9d);
            Assert.AreEqual(100d, judge.Reputation, 1e-9d);
            Assert.AreEqual(1, judge.CrowdBand);
        }

        [Test]
        public void CrossfaderBounce_DoesNotScoreACleanTransition()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, At(1d, true, true, 0.85d, 0.52d, 0.85d, 0.52d));
            Feed(judge, At(1.4d, true, true, 0.52d, 0.85d, 0.52d, 0.85d));
            Feed(judge, At(1.8d, true, true, 0.85d, 0.52d, 0.85d, 0.52d));
            int count = Feed(judge, FullA(2.2d));

            Assert.AreEqual(0, count);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
        }

        [Test]
        public void SecondTransitionInTheSamePhrase_IsFussNotAnotherClean()
        {
            SetJudge judge = Judge();
            BlendToB(judge);
            Feed(judge, FullB(6d));
            Feed(judge, Middle(6.2d, 0d, 0d));
            Feed(judge, FullA(8d));
            int count = Feed(judge, FullA(8.5d));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.Fuss, _changes[0].Kind);
            Assert.AreEqual(39d, judge.Hype, 1e-9d);
            Assert.AreEqual(100d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void DirtyBass_Misses_AndALaterCleanStillPays()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, Middle(2d, 0.8d, 0.8d));
            int conflict = Feed(judge, Middle(3d, 0.8d, 0.8d));
            Assert.AreEqual(1, conflict);
            Assert.AreEqual(ScoreKind.BassConflict, _changes[0].Kind);
            Feed(judge, Middle(4d, 0.8d, 0.8d));
            Feed(judge, FullB(6d));
            int missed = Feed(judge, FullB(6.5d));
            Assert.AreEqual(1, missed);
            Assert.AreEqual(ScoreKind.MissedTransition, _changes[0].Kind);
            Assert.AreEqual(29d, judge.Hype, 1e-9d);

            Feed(judge, FullB(8d));
            Feed(judge, Middle(8d, 0d, 0d));
            Feed(judge, FullA(10d));
            Feed(judge, FullA(10.5d));
            Assert.AreEqual(37d, judge.Hype, 1e-9d);
            Assert.AreEqual(100d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void HardCut_OnThePhraseDownbeat_IsClean()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, FullA(15d));
            int count = Feed(judge, FullB(16d));
            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.PhraseDecay, _changes[0].Kind);

            count = Feed(judge, FullB(16.5d));
            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.CleanTransition, _changes[0].Kind);
            Assert.IsTrue(_changes[0].HardCut);
            Assert.AreEqual(41d, judge.Hype, 1e-9d);
        }

        [Test]
        public void BassConflict_NeedsTwoBeats_AndFiresOncePerPhrase()
        {
            SetJudge judge = Judge();
            MixSnapshot hot = At(0d, true, true, 0.8d, 0.8d, 0.8d, 0.8d);
            Feed(judge, hot);
            Assert.AreEqual(0, Feed(judge, At(0.99d, true, true, 0.8d, 0.8d, 0.8d, 0.8d)));
            int count = Feed(judge, At(1d, true, true, 0.8d, 0.8d, 0.8d, 0.8d));
            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.BassConflict, _changes[0].Kind);
            Assert.AreEqual(29d, judge.Hype, 1e-9d);
            Assert.AreEqual(1, judge.Combo);

            Assert.AreEqual(0, Feed(judge, At(1.5d, true, true, 0.8d, 0.8d, 0.8d, 0.8d)));
            count = Feed(judge, At(16d, true, true, 0.8d, 0.8d, 0.8d, 0.8d));
            Assert.AreEqual(2, count);
            Assert.AreEqual(ScoreKind.PhraseDecay, _changes[0].Kind);
            Assert.AreEqual(ScoreKind.BassConflict, _changes[1].Kind);
            Assert.AreEqual(21d, judge.Hype, 1e-9d);
        }

        [Test]
        public void OneFrameOfDoubleBass_DoesNotPenalise()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, At(0.016d, true, true, 0.8d, 0.8d, 0.8d, 0.8d));
            Feed(judge, FullA(0.032d));
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
        }

        [Test]
        public void Drop_RequiresABarOfCut_LandsOnTheDownbeat_ThenCoolsDown()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, QuietBass(1d));
            Feed(judge, QuietBass(3d));
            int count = Feed(judge, FullA(4d));
            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.Drop, _changes[0].Kind);
            Assert.AreEqual(47d, judge.Hype, 1e-9d);
            Assert.AreEqual(140d, judge.Reputation, 1e-9d);

            Feed(judge, QuietBass(4.5d));
            Feed(judge, QuietBass(6.5d));
            Assert.AreEqual(0, Feed(judge, FullA(8d)));
            Assert.AreEqual(47d, judge.Hype, 1e-9d);

            Feed(judge, QuietBass(10d));
            Feed(judge, QuietBass(12d));
            Feed(judge, QuietBass(14d));
            count = Feed(judge, FullA(16d));
            Assert.AreEqual(2, count);
            Assert.AreEqual(ScoreKind.PhraseDecay, _changes[0].Kind);
            Assert.AreEqual(ScoreKind.Drop, _changes[1].Kind);
            Assert.AreEqual(57d, judge.Hype, 1e-9d);
        }

        [Test]
        public void BassKillToggledTenTimesInABeat_DoesNotFarm()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            for (int i = 0; i < 10; i++)
            {
                double time = 1d + i * 0.04d;
                Feed(judge, i % 2 == 0 ? QuietBass(time) : FullA(time));
            }

            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Assert.AreEqual(0d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void OffGridReturn_DoesNotDrop()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            Feed(judge, QuietBass(0.5d));
            Feed(judge, QuietBass(2.5d));
            Feed(judge, FullA(3.5d));
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
        }

        [Test]
        public void SevenPhrases_DecayFourteen_SameGrooveAlsoGoesStale()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            for (int phrase = 1; phrase <= 7; phrase++)
                Feed(judge, FullA(phrase * 16d, "track_neon_pulse"));

            Assert.AreEqual(13d, judge.Hype, 1e-9d);

            SetJudge rotating = Judge();
            Feed(rotating, FullA(0d, "track_neon_pulse"));
            for (int phrase = 1; phrase <= 7; phrase++)
            {
                string track = phrase % 2 == 0 ? "track_neon_pulse" : "track_afterhours";
                Feed(rotating, FullA(phrase * 16d, track));
            }

            Assert.AreEqual(21d, rotating.Hype, 1e-9d);
        }

        [Test]
        public void PauseAcrossAPhrase_DoesNotDecayOrScore()
        {
            SetJudge judge = Judge();
            Feed(judge, FullA(0d));
            MixSnapshot hidden = FullA(16d);
            hidden = new MixSnapshot(
                16d, true, true, false, 1d, 0d, 1d, 0d, "track_neon_pulse", "track_afterhours", true);
            Assert.AreEqual(0, Feed(judge, hidden));
            Feed(judge, FullA(16.1d));
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
        }

        private void BlendToB(SetJudge judge)
        {
            Feed(judge, FullA(0d));
            Feed(judge, Middle(2d, 0d, 0d));
            Feed(judge, FullB(4d));
            Feed(judge, FullB(4.5d));
            Assert.AreEqual(43d, judge.Hype, 1e-9d);
        }

        private int Feed(SetJudge judge, MixSnapshot snapshot)
        {
            return judge.Observe(snapshot, _changes);
        }

        private static SetJudge Judge()
        {
            var time = new ManualTimeProvider();
            var clock = new MusicClock(time, 120d, 0d, 0d);
            return new SetJudge(clock);
        }

        private static MixSnapshot FullA(double time)
        {
            return FullA(time, "track_neon_pulse");
        }

        private static MixSnapshot FullA(double time, string track)
        {
            return At(time, true, false, 1d, 0d, 1d, 0d, track, "track_afterhours");
        }

        private static MixSnapshot FullB(double time)
        {
            return At(time, false, true, 0d, 1d, 0d, 1d);
        }

        private static MixSnapshot QuietBass(double time)
        {
            return At(time, true, false, 1d, 0d, 0d, 0d);
        }

        private static MixSnapshot Middle(double time, double lowA, double lowB)
        {
            return At(time, true, true, 0.6d, 0.6d, lowA, lowB);
        }

        private static MixSnapshot At(
            double time,
            bool deckA,
            bool deckB,
            double gainA,
            double gainB,
            double lowA,
            double lowB)
        {
            return At(time, deckA, deckB, gainA, gainB, lowA, lowB, "track_neon_pulse", "track_afterhours");
        }

        private static MixSnapshot At(
            double time,
            bool deckA,
            bool deckB,
            double gainA,
            double gainB,
            double lowA,
            double lowB,
            string trackA,
            string trackB)
        {
            return new MixSnapshot(time, false, deckA, deckB, gainA, gainB, lowA, lowB, trackA, trackB, true);
        }
    }
}
