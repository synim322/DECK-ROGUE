using DeckRogue.Core.Run;
using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class ClubDirectorTests
    {
        [Test]
        public void Set1_HypeAtLeast60_WinsIntoResult()
        {
            ClubDirector director = new ClubDirector(1);
            PlayHardCuts(director);

            ClubReadout readout = director.Readout;
            Assert.AreEqual(SetOutcome.Win, readout.Outcome);
            Assert.AreEqual(RunPhase.Result, readout.Phase);
            Assert.AreEqual("target_met", readout.ReasonId);
            Assert.AreEqual(1, readout.SetIndex);
            Assert.AreEqual(60d, readout.TargetHype, 1e-9d);
            Assert.AreEqual(75d, readout.Hype, 1e-9d);
            Assert.AreEqual(0d, readout.RemainingSeconds, 1e-6d);
            Assert.GreaterOrEqual(readout.Hype, 60d);

            double hype = readout.Hype;
            director.Tick(FullA(200d));
            Assert.AreEqual(hype, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
        }

        [Test]
        public void Set1_TimerEndsBelowTarget_LosesOnTimeout()
        {
            ClubDirector director = new ClubDirector(1);
            PlaySameGroove(director);

            ClubReadout readout = director.Readout;
            Assert.AreEqual(SetOutcome.Lose, readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, readout.Phase);
            Assert.AreEqual("timeout", readout.ReasonId);
            Assert.AreEqual(11d, readout.Hype, 1e-9d);
            Assert.Less(readout.Hype, 60d);
            Assert.AreEqual(0d, readout.RemainingSeconds, 1e-6d);

            director.Continue();
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.AreEqual(1, director.Readout.SetIndex);
        }

        [Test]
        public void HypeFloor_NeedsAFullPhrase_ShorterZeroDoesNotLose()
        {
            ClubDirector director = new ClubDirector(1);
            director.BeginSet();
            Tick(director, Hot(0d));
            Tick(director, Hot(1d));
            Tick(director, Hot(16d));
            Tick(director, Hot(32d));
            Tick(director, Hot(48d));
            Tick(director, Hot(64d));

            Assert.AreEqual(0d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(SetOutcome.InProgress, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.Performing, director.Readout.Phase);
            Assert.AreEqual("", director.Readout.ReasonId);

            Tick(director, Hot(79d));
            Assert.AreEqual(0d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(SetOutcome.InProgress, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.Performing, director.Readout.Phase);
            Assert.AreEqual(49d, director.Readout.RemainingSeconds, 1e-6d);

            Tick(director, Hot(80d));
            Assert.AreEqual(SetOutcome.Lose, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.AreEqual("hype_floor", director.Readout.ReasonId);
            Assert.Greater(director.Readout.RemainingSeconds, 0d);
        }

        [Test]
        public void Pause_DoesNotAdvanceTimerOrHype()
        {
            ClubDirector director = new ClubDirector(1);
            director.BeginSet();
            Tick(director, FullA(0d));
            Tick(director, FullA(10d));

            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(118d, director.Readout.RemainingSeconds, 1e-6d);

            Tick(director, Paused(40d, true, true, 0.8d, 0.8d, 0.8d, 0.8d));
            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(118d, director.Readout.RemainingSeconds, 1e-6d);
            Assert.AreEqual(RunPhase.Performing, director.Readout.Phase);

            Tick(director, FullA(40d));
            Tick(director, FullA(47d));
            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(111d, director.Readout.RemainingSeconds, 1e-6d);
            Assert.AreEqual(0d, director.Reputation, 1e-9d);
        }

        [Test]
        public void Continue_OpensSet2At35_RepeatedEventDoesNotScoreAgain()
        {
            int seed = SeedFor(ClubRuleId.CleanDrop);
            ClubDirector director = new ClubDirector(seed);
            director.BeginSet();
            director.Continue();
            Assert.AreEqual(RunPhase.Performing, director.Readout.Phase);
            Assert.AreEqual(1, director.Readout.SetIndex);

            PlayDropAndHardCuts(director);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
            Assert.AreEqual(87d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(1540d, director.Reputation, 1e-9d);
            Assert.AreEqual(2, director.Combo);

            director.Tick(QuietBass(4d));
            director.Tick(FullA(8d));
            Assert.AreEqual(87d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(1540d, director.Reputation, 1e-9d);

            director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.AreEqual(1, director.Readout.SetIndex);
            Assert.AreEqual(87d, director.Readout.Hype, 1e-9d);
            Assert.IsTrue(director.Choose(UpgradeId.AutoBass));
            ClubReadout opened = director.Readout;
            Assert.AreEqual(RunPhase.PreClub, opened.Phase);
            Assert.AreEqual(2, opened.SetIndex);
            Assert.AreEqual(2, opened.SetCount);
            Assert.AreEqual(35d, opened.Hype, 1e-9d);
            Assert.AreEqual(65d, opened.TargetHype, 1e-9d);
            Assert.AreEqual(128d, opened.RemainingSeconds, 1e-6d);
            Assert.AreEqual(SetOutcome.InProgress, opened.Outcome);
            Assert.AreEqual("", opened.ReasonId);
            Assert.AreEqual(ClubRuleId.CleanDrop, opened.Rule);
            Assert.AreEqual(1540d, director.Reputation, 1e-9d);
            Assert.AreEqual(1, director.Combo);

            director.Tick(FullB(16.5d));
            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);

            director.BeginSet();
            Tick(director, FullA(0d));
            Tick(director, FullA(0.5d));
            Tick(director, FullB(16d));
            Tick(director, FullB(16.5d));
            Assert.AreEqual(41d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(1640d, director.Reputation, 1e-9d);

            Tick(director, FullB(16.5d));
            Assert.AreEqual(41d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(1640d, director.Reputation, 1e-9d);
            Assert.AreEqual(RunPhase.Performing, director.Readout.Phase);
        }

        [Test]
        public void Set2_CleanDrop_RequiresAnAcceptedDrop()
        {
            ClubDirector missing = new ClubDirector(SeedFor(ClubRuleId.CleanDrop));
            PlayHardCuts(missing);
            TakeAutoBass(missing);
            PlayHardCuts(missing);

            Assert.GreaterOrEqual(missing.Readout.Hype, 65d);
            Assert.AreEqual(75d, missing.Readout.Hype, 1e-9d);
            Assert.AreEqual(SetOutcome.Lose, missing.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, missing.Readout.Phase);
            Assert.AreEqual("clean_drop_missing", missing.Readout.ReasonId);

            ClubDirector dropped = new ClubDirector(SeedFor(ClubRuleId.CleanDrop));
            PlayHardCuts(dropped);
            TakeAutoBass(dropped);
            PlayStaleDrops(dropped);

            Assert.GreaterOrEqual(dropped.Readout.Hype, 65d);
            Assert.AreEqual(SetOutcome.Win, dropped.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, dropped.Readout.Phase);
            Assert.AreEqual("target_met", dropped.Readout.ReasonId);
            dropped.Continue();
            Assert.AreEqual(RunPhase.FinalResult, dropped.Readout.Phase);
            Assert.IsFalse(dropped.Choose(UpgradeId.AutoBass));
        }

        [Test]
        public void Set2_MaintainEnergy_StaleGrooveLoses_PhraseDecayDoesNot()
        {
            ClubDirector stale = new ClubDirector(SeedFor(ClubRuleId.MaintainEnergy));
            PlayHardCuts(stale);
            TakeAutoBass(stale);
            PlayStaleDrops(stale);

            Assert.GreaterOrEqual(stale.Readout.Hype, 65d);
            Assert.AreEqual(71d, stale.Readout.Hype, 1e-9d);
            Assert.AreEqual(SetOutcome.Lose, stale.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, stale.Readout.Phase);
            Assert.AreEqual("energy_lost", stale.Readout.ReasonId);

            ClubDirector rotated = new ClubDirector(SeedFor(ClubRuleId.MaintainEnergy));
            PlayHardCuts(rotated);
            TakeAutoBass(rotated);
            PlayHardCuts(rotated);

            Assert.AreEqual(75d, rotated.Readout.Hype, 1e-9d);
            Assert.GreaterOrEqual(rotated.Readout.Hype, 65d);
            Assert.AreEqual(SetOutcome.Win, rotated.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, rotated.Readout.Phase);
            Assert.AreEqual("target_met", rotated.Readout.ReasonId);
        }

        [Test]
        public void Seed_PicksOneRule_ResetReturnsToFirstSet()
        {
            bool sawEnergy = false;
            bool sawDrop = false;
            for (int seed = 0; seed < 32; seed++)
            {
                ClubDirector probe = new ClubDirector(seed);
                PlayHardCuts(probe);
                TakeOffered(probe);
                if (probe.Readout.Rule == ClubRuleId.MaintainEnergy)
                    sawEnergy = true;
                if (probe.Readout.Rule == ClubRuleId.CleanDrop)
                    sawDrop = true;
            }

            Assert.IsTrue(sawEnergy);
            Assert.IsTrue(sawDrop);

            int pinned = 4;
            ClubDirector first = new ClubDirector(pinned);
            ClubDirector second = new ClubDirector(pinned);
            Assert.AreEqual(ClubRuleId.None, first.Readout.Rule);
            Assert.AreEqual("club_neon_basement", first.Readout.ClubId);
            Assert.AreEqual(2, first.Readout.SetCount);
            Assert.AreEqual(35d, first.Readout.Hype, 1e-9d);
            Assert.AreEqual(RunPhase.PreClub, first.Readout.Phase);

            PlayHardCuts(first);
            TakeOffered(first);
            PlayHardCuts(second);
            TakeOffered(second);
            Assert.AreEqual(first.Readout.Rule, second.Readout.Rule);
            Assert.AreNotEqual(ClubRuleId.None, first.Readout.Rule);

            first.BeginSet();
            Tick(first, FullA(0d));
            Tick(first, FullA(20d));
            Assert.AreEqual(RunPhase.Performing, first.Readout.Phase);
            Assert.AreEqual(2, first.Readout.SetIndex);

            first.Reset(pinned);
            ClubReadout reset = first.Readout;
            Assert.AreEqual(RunPhase.PreClub, reset.Phase);
            Assert.AreEqual(1, reset.SetIndex);
            Assert.AreEqual(35d, reset.Hype, 1e-9d);
            Assert.AreEqual(60d, reset.TargetHype, 1e-9d);
            Assert.AreEqual(128d, reset.RemainingSeconds, 1e-6d);
            Assert.AreEqual(SetOutcome.InProgress, reset.Outcome);
            Assert.AreEqual("", reset.ReasonId);
            Assert.AreEqual(ClubRuleId.None, reset.Rule);
            Assert.AreEqual(0d, first.Reputation, 1e-9d);
            Assert.AreEqual(1, first.Combo);

            PlayHardCuts(first);
            TakeOffered(first);
            Assert.AreEqual(second.Readout.Rule, first.Readout.Rule);
        }

        private static int SeedFor(ClubRuleId rule)
        {
            for (int seed = 0; seed < 128; seed++)
            {
                ClubDirector director = new ClubDirector(seed);
                if (!director.Offer.Contains(UpgradeId.AutoBass))
                    continue;
                PlayHardCuts(director);
                if (director.Readout.Outcome != SetOutcome.Win)
                    Assert.Fail("Set 1 did not open the reward. Hype " + director.Readout.Hype + " reason " + director.Readout.ReasonId);
                TakeAutoBass(director);
                if (director.Readout.Rule == rule)
                    return seed;
            }

            Assert.Fail("No seed produced " + rule + " with auto bass");
            return 0;
        }

        private static void TakeAutoBass(ClubDirector director)
        {
            if (director.Readout.Phase == RunPhase.Result)
                director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.IsTrue(director.Choose(UpgradeId.AutoBass));
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(2, director.Readout.SetIndex);
        }

        private static void TakeOffered(ClubDirector director)
        {
            director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.IsTrue(director.Choose(director.Offer.Left));
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(2, director.Readout.SetIndex);
        }

        private static void PlayHardCuts(ClubDirector director)
        {
            EnsurePerforming(director);
            Tick(director, FullA(0d));
            Tick(director, FullA(0.5d));
            bool onA = true;
            for (double boundary = 16d; boundary <= 112d; boundary += 16d)
            {
                onA = !onA;
                Tick(director, onA ? FullA(boundary) : FullB(boundary));
                Tick(director, onA ? FullA(boundary + 0.5d) : FullB(boundary + 0.5d));
            }

            Tick(director, onA ? FullA(128d) : FullB(128d));
        }

        private static void PlayDropAndHardCuts(ClubDirector director)
        {
            EnsurePerforming(director);
            Tick(director, FullA(0d));
            Tick(director, QuietBass(1d));
            Tick(director, QuietBass(3d));
            Tick(director, FullA(4d));
            bool onA = true;
            for (double boundary = 16d; boundary <= 112d; boundary += 16d)
            {
                onA = !onA;
                Tick(director, onA ? FullA(boundary) : FullB(boundary));
                Tick(director, onA ? FullA(boundary + 0.5d) : FullB(boundary + 0.5d));
            }

            Tick(director, onA ? FullA(128d) : FullB(128d));
        }

        private static void PlayStaleDrops(ClubDirector director)
        {
            EnsurePerforming(director);
            Tick(director, FullA(0d));
            Tick(director, QuietBass(1d));
            Tick(director, QuietBass(3d));
            Tick(director, FullA(4d));
            Tick(director, QuietBass(10d));
            Tick(director, QuietBass(12d));
            Tick(director, FullA(14d));
            Tick(director, FullA(16d));
            Tick(director, QuietBass(20d));
            Tick(director, QuietBass(22d));
            Tick(director, FullA(24d));
            Tick(director, QuietBass(30d));
            Tick(director, QuietBass(32d));
            Tick(director, FullA(34d));
            Tick(director, QuietBass(40d));
            Tick(director, QuietBass(42d));
            Tick(director, FullA(44d));
            Tick(director, FullA(48d));
            Tick(director, FullA(64d));
            Tick(director, FullA(80d));
            Tick(director, FullA(96d));
            Tick(director, FullA(112d));
            Tick(director, FullA(128d));
        }

        private static void PlaySameGroove(ClubDirector director)
        {
            EnsurePerforming(director);
            Tick(director, FullA(0d));
            for (double time = 16d; time <= 128d; time += 16d)
                Tick(director, FullA(time));
        }

        private static void EnsurePerforming(ClubDirector director)
        {
            if (director.Readout.Phase == RunPhase.PreClub)
                director.BeginSet();
        }

        private static void Tick(ClubDirector director, MixSnapshot snapshot)
        {
            director.Tick(snapshot);
        }

        private static MixSnapshot FullA(double time)
        {
            return At(time, false, true, false, 1d, 0d, 1d, 0d, "track_neon_pulse", "track_afterhours");
        }

        private static MixSnapshot FullB(double time)
        {
            return At(time, false, false, true, 0d, 1d, 0d, 1d, "track_neon_pulse", "track_afterhours");
        }

        private static MixSnapshot QuietBass(double time)
        {
            return At(time, false, true, false, 1d, 0d, 0d, 0d, "track_neon_pulse", "track_afterhours");
        }

        private static MixSnapshot Hot(double time)
        {
            return At(time, false, true, true, 0.8d, 0.8d, 0.8d, 0.8d, "track_neon_pulse", "track_afterhours");
        }

        private static MixSnapshot Paused(
            double time,
            bool deckA,
            bool deckB,
            double gainA,
            double gainB,
            double lowA,
            double lowB)
        {
            return At(time, true, deckA, deckB, gainA, gainB, lowA, lowB, "track_neon_pulse", "track_afterhours");
        }

        private static MixSnapshot At(
            double time,
            bool paused,
            bool deckA,
            bool deckB,
            double gainA,
            double gainB,
            double lowA,
            double lowB,
            string trackA,
            string trackB)
        {
            return new MixSnapshot(time, paused, deckA, deckB, gainA, gainB, lowA, lowB, trackA, trackB, true);
        }
    }
}
