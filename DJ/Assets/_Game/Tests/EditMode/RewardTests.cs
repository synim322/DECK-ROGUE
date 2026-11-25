using DeckRogue.Core.Run;
using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class RewardTests
    {
        [Test]
        public void Seed_OffersTheSamePair_AndThePairChanges()
        {
            RewardOffer first = new ClubDirector(0).Offer;
            AssertOffer(new ClubDirector(0), first);
            Assert.AreNotEqual(first.Left, first.Right);

            RewardOffer negative = new ClubDirector(-3).Offer;
            AssertOffer(new ClubDirector(-3), negative);

            bool changed = false;
            for (int seed = 1; seed < 48; seed++)
            {
                ClubDirector director = new ClubDirector(seed);
                RewardOffer offer = director.Offer;
                Assert.AreNotEqual(offer.Left, offer.Right);
                Assert.IsTrue(UpgradeId.IsKnown(offer.Left));
                Assert.IsTrue(UpgradeId.IsKnown(offer.Right));
                AssertOffer(new ClubDirector(seed), offer);
                if (offer.Left != first.Left || offer.Right != first.Right)
                    changed = true;
            }

            Assert.IsTrue(changed);
        }

        [Test]
        public void ForeignId_IsIgnored_AndSet2DoesNotStartWithoutAChoice()
        {
            ClubDirector director = WonSet1(SeedWith(UpgradeId.AutoBass));
            Assert.IsFalse(director.Choose(director.Offer.Left));
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
            Assert.AreEqual("", director.ChosenUpgradeId);

            director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.AreEqual(1, director.Readout.SetIndex);
            string outsider = Outsider(director.Offer);
            Assert.IsFalse(director.Choose(outsider));
            Assert.IsFalse(director.Choose("upgrade_not_in_the_pool"));
            Assert.IsFalse(director.Choose(null));
            Assert.IsFalse(director.Choose(""));
            Assert.AreEqual("", director.ChosenUpgradeId);
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);

            double hype = director.Readout.Hype;
            director.BeginSet();
            Tick(director, FullA(200d));
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.AreEqual(1, director.Readout.SetIndex);
            Assert.AreEqual(hype, director.Readout.Hype, 1e-9d);
            Assert.IsFalse(director.EffectAudible);

            Assert.IsTrue(director.Choose(director.Offer.Left));
            Assert.AreEqual(director.Offer.Left, director.ChosenUpgradeId);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(2, director.Readout.SetIndex);
            Assert.IsFalse(director.Choose(director.Offer.Right));
            Assert.AreEqual(director.Offer.Left, director.ChosenUpgradeId);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);

            Tick(director, FullA(0d));
            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
        }

        [Test]
        public void Set1Loss_DoesNotOpenAReward()
        {
            ClubDirector director = new ClubDirector(1);
            director.BeginSet();
            Tick(director, FullA(0d));
            for (double time = 16d; time <= 128d; time += 16d)
                Tick(director, FullA(time));

            Assert.AreEqual(SetOutcome.Lose, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            director.Continue();
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.IsFalse(director.Choose(director.Offer.Left));
            Assert.AreEqual("", director.ChosenUpgradeId);
            Assert.AreEqual(1, director.Readout.SetIndex);
        }

        [Test]
        public void AutoBass_CapsTheSecondaryLow_AndDoesNotGrantHype()
        {
            MixSnapshot lead = Lead(0d, 0.9d, 0.8d);
            MixSnapshot heard = AutoBassDucking.Apply(lead, out bool ducked);
            Assert.IsTrue(ducked);
            Assert.AreEqual(0.9d, heard.LowA, 1e-9d);
            Assert.AreEqual(UpgradeId.AutoBassLowMax, heard.LowB, 1e-9d);
            Assert.AreEqual(1d, heard.GainA, 1e-9d);
            Assert.AreEqual(0.2d, heard.GainB, 1e-9d);

            MixSnapshot otherLead = new MixSnapshot(
                0d, false, true, true, 0.2d, 1d, 0.9d, 1d, "track_neon_pulse", "track_afterhours", true);
            MixSnapshot otherHeard = AutoBassDucking.Apply(otherLead, out bool otherDucked);
            Assert.IsTrue(otherDucked);
            Assert.AreEqual(UpgradeId.AutoBassLowMax, otherHeard.LowA, 1e-9d);
            Assert.AreEqual(1d, otherHeard.LowB, 1e-9d);

            MixSnapshot already = Lead(0d, 0.9d, 0.25d);
            MixSnapshot same = AutoBassDucking.Apply(already, out bool alreadyDucked);
            Assert.IsFalse(alreadyDucked);
            Assert.AreEqual(0.25d, same.LowB, 1e-9d);

            MixSnapshot blend = new MixSnapshot(
                0d, false, true, true, 0.8d, 0.8d, 0.8d, 0.8d, "track_neon_pulse", "track_afterhours", true);
            MixSnapshot blendHeard = AutoBassDucking.Apply(blend, out bool blendDucked);
            Assert.IsFalse(blendDucked);
            Assert.AreEqual(0.8d, blendHeard.LowA, 1e-9d);
            Assert.AreEqual(0.8d, blendHeard.LowB, 1e-9d);

            ClubDirector plain = new ClubDirector(1);
            plain.BeginSet();
            Tick(plain, Lead(0d, 0.9d, 0.8d));
            Tick(plain, Lead(1d, 0.9d, 0.8d));
            Assert.AreEqual(29d, plain.Readout.Hype, 1e-9d);
            Assert.IsFalse(plain.EffectAudible);

            ClubDirector duckedRun = Equip(UpgradeId.AutoBass);
            double carried = duckedRun.Reputation;
            duckedRun.BeginSet();
            Tick(duckedRun, Lead(0d, 0.9d, 0.8d));
            Assert.IsTrue(duckedRun.EffectAudible);
            Assert.IsTrue(duckedRun.Readout.EffectAudible);
            Tick(duckedRun, Lead(1d, 0.9d, 0.8d));
            Assert.AreEqual(35d, duckedRun.Readout.Hype, 1e-9d);
            Assert.AreEqual(carried, duckedRun.Reputation, 1e-9d);
            Assert.IsTrue(duckedRun.EffectAudible);
            Assert.LessOrEqual(duckedRun.Readout.Hype, 100d);
            Assert.GreaterOrEqual(duckedRun.Reputation, 0d);

            ClubDirector blendRun = Equip(UpgradeId.AutoBass);
            double blendCarried = blendRun.Reputation;
            blendRun.BeginSet();
            Tick(blendRun, blend);
            Tick(blendRun, new MixSnapshot(
                1d, false, true, true, 0.8d, 0.8d, 0.8d, 0.8d, "track_neon_pulse", "track_afterhours", true));
            Assert.AreEqual(29d, blendRun.Readout.Hype, 1e-9d);
            Assert.AreEqual(blendCarried, blendRun.Reputation, 1e-9d);
            Assert.IsFalse(blendRun.EffectAudible);
        }

        [Test]
        public void Riser_PaysPlus4OnceOnTheFirstDropOfAPhrase()
        {
            // DECK_ROGUE_MASTER §04: Riser Machine is +4 Hype on the first credited drop of each phrase.
            ClubDirector hold = Equip(UpgradeId.Riser);
            double heldRep = hold.Reputation;
            hold.BeginSet();
            Tick(hold, FullA(0d));
            Tick(hold, Quiet(1d));
            Assert.AreEqual(35d, hold.Readout.Hype, 1e-9d);
            Assert.IsFalse(hold.EffectAudible);
            Tick(hold, Quiet(3d));
            Assert.AreEqual(35d, hold.Readout.Hype, 1e-9d);
            Assert.AreEqual(heldRep, hold.Reputation, 1e-9d);
            Assert.IsTrue(hold.EffectAudible);

            ClubDirector dropped = Equip(UpgradeId.Riser);
            double dropRep = dropped.Reputation;
            dropped.BeginSet();
            Tick(dropped, FullA(0d));
            Tick(dropped, Quiet(1d));
            Tick(dropped, Quiet(3d));
            Assert.AreEqual(35d, dropped.Readout.Hype, 1e-9d);
            Tick(dropped, FullA(4d));
            Assert.AreEqual(51d, dropped.Readout.Hype, 1e-9d);
            Assert.AreEqual(dropRep + 140d, dropped.Reputation, 1e-9d);
            Assert.IsFalse(dropped.EffectAudible);
            Tick(dropped, FullA(4d));
            Assert.AreEqual(51d, dropped.Readout.Hype, 1e-9d);
            Assert.AreEqual(dropRep + 140d, dropped.Reputation, 1e-9d);

            Tick(dropped, Quiet(5d));
            Tick(dropped, Quiet(7d));
            Tick(dropped, FullA(8d));
            Assert.AreEqual(51d, dropped.Readout.Hype, 1e-9d);
            Tick(dropped, Quiet(9d));
            Tick(dropped, Quiet(11d));
            Tick(dropped, FullA(12d));
            Assert.AreEqual(63d, dropped.Readout.Hype, 1e-9d);
            Assert.AreEqual(dropRep + 280d, dropped.Reputation, 1e-9d);

            Tick(dropped, Quiet(13d));
            Tick(dropped, Quiet(15d));
            Tick(dropped, Quiet(16d));
            Assert.AreEqual(61d, dropped.Readout.Hype, 1e-9d);
            Assert.IsTrue(dropped.EffectAudible);
            Tick(dropped, Quiet(18d));
            Tick(dropped, FullA(20d));
            Assert.AreEqual(77d, dropped.Readout.Hype, 1e-9d);
            Assert.AreEqual(dropRep + 420d, dropped.Reputation, 1e-9d);
            Assert.IsFalse(dropped.EffectAudible);
            Assert.GreaterOrEqual(dropped.Reputation, 0d);
            Assert.LessOrEqual(dropped.Readout.Hype, 100d);

            ClubDirector cutOnly = Equip(UpgradeId.Riser);
            cutOnly.BeginSet();
            PlayPhraseHardCut(cutOnly);
            Assert.AreEqual(41d, cutOnly.Readout.Hype, 1e-9d);
            Assert.IsFalse(cutOnly.EffectAudible);
        }

        [Test]
        public void HardCut_Adds4OncePerEvent_SmoothStaysPlain()
        {
            // DECK_ROGUE_MASTER §04 writes +4 Hype for an ideal hard cut.
            // The same line also promises a Reputation bonus but gives no figure.
            // Playtest hypothesis: leave Reputation on the clean-transition base (100 × combo)
            // until a number is written down. Smooth takes neither bonus.
            ClubDirector plain = new ClubDirector(1);
            plain.BeginSet();
            PlayPhraseHardCut(plain);
            Assert.AreEqual(41d, plain.Readout.Hype, 1e-9d);
            Assert.AreEqual(100d, plain.Reputation, 1e-9d);

            ClubDirector cut = Equip(UpgradeId.HardCut);
            double smoothRep = cut.Reputation;
            cut.BeginSet();
            PlaySmooth(cut);
            Assert.AreEqual(43d, cut.Readout.Hype, 1e-9d);
            Assert.AreEqual(smoothRep + 100d, cut.Reputation, 1e-9d);
            Assert.IsFalse(cut.EffectAudible);

            ClubDirector hard = Equip(UpgradeId.HardCut);
            double hardRep = hard.Reputation;
            hard.BeginSet();
            PlayPhraseHardCut(hard);
            Assert.AreEqual(45d, hard.Readout.Hype, 1e-9d);
            Assert.AreEqual(hardRep + 100d, hard.Reputation, 1e-9d);
            Assert.IsTrue(hard.EffectAudible);
            Assert.IsTrue(hard.Readout.EffectAudible);
            Tick(hard, FullB(16.5d));
            Assert.AreEqual(45d, hard.Readout.Hype, 1e-9d);
            Assert.AreEqual(hardRep + 100d, hard.Reputation, 1e-9d);
            Assert.IsFalse(hard.EffectAudible);

            Tick(hard, FullA(32d));
            Tick(hard, FullA(32.5d));
            Assert.AreEqual(55d, hard.Readout.Hype, 1e-9d);
            Assert.IsTrue(hard.EffectAudible);
            Tick(hard, FullA(32.5d));
            Assert.AreEqual(55d, hard.Readout.Hype, 1e-9d);
            Assert.IsFalse(hard.EffectAudible);
            Assert.LessOrEqual(hard.Readout.Hype, 100d);
            Assert.GreaterOrEqual(hard.Reputation, 0d);

            ClubDirector dropOnly = Equip(UpgradeId.HardCut);
            double dropRep = dropOnly.Reputation;
            dropOnly.BeginSet();
            Tick(dropOnly, FullA(0d));
            Tick(dropOnly, Quiet(1d));
            Tick(dropOnly, Quiet(3d));
            Tick(dropOnly, FullA(4d));
            Assert.AreEqual(47d, dropOnly.Readout.Hype, 1e-9d);
            Assert.AreEqual(dropRep + 140d, dropOnly.Reputation, 1e-9d);
            Assert.IsFalse(dropOnly.EffectAudible);
        }

        [Test]
        public void Reset_ClearsTheCard()
        {
            int seed = SeedWith(UpgradeId.Riser);
            ClubDirector director = Equip(UpgradeId.Riser);
            director.BeginSet();
            Tick(director, FullA(0d));
            Tick(director, Quiet(1d));
            Tick(director, Quiet(3d));
            Assert.IsTrue(director.EffectAudible);
            Assert.AreEqual(UpgradeId.Riser, director.ChosenUpgradeId);

            director.Reset(seed);
            Assert.AreEqual("", director.ChosenUpgradeId);
            Assert.AreEqual("", director.Readout.ChosenUpgradeId);
            Assert.IsFalse(director.EffectAudible);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(1, director.Readout.SetIndex);
            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(0d, director.Reputation, 1e-9d);

            director.BeginSet();
            Tick(director, FullA(0d));
            Tick(director, Quiet(1d));
            Tick(director, Quiet(3d));
            Tick(director, FullA(4d));
            Assert.AreEqual(47d, director.Readout.Hype, 1e-9d);
            Assert.IsFalse(director.EffectAudible);
        }

        private static void AssertOffer(ClubDirector director, RewardOffer offer)
        {
            Assert.AreEqual(offer.Left, director.Offer.Left);
            Assert.AreEqual(offer.Right, director.Offer.Right);
            Assert.AreEqual(offer.Left, director.Readout.OfferedLeft);
            Assert.AreEqual(offer.Right, director.Readout.OfferedRight);
            Assert.IsTrue(UpgradeId.IsKnown(offer.Left));
            Assert.IsTrue(UpgradeId.IsKnown(offer.Right));
        }

        private static string Outsider(RewardOffer offer)
        {
            if (!offer.Contains(UpgradeId.AutoBass))
                return UpgradeId.AutoBass;
            if (!offer.Contains(UpgradeId.Riser))
                return UpgradeId.Riser;
            return UpgradeId.HardCut;
        }

        private static int SeedWith(string id)
        {
            for (int seed = 0; seed < 64; seed++)
            {
                if (new ClubDirector(seed).Offer.Contains(id))
                    return seed;
            }

            Assert.Fail("No seed offered " + id);
            return 0;
        }

        private static ClubDirector WonSet1(int seed)
        {
            ClubDirector director = new ClubDirector(seed);
            director.BeginSet();
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
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
            return director;
        }

        private static ClubDirector Equip(string id)
        {
            ClubDirector director = WonSet1(SeedWith(id));
            director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.IsTrue(director.Choose(id));
            Assert.AreEqual(id, director.ChosenUpgradeId);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(35d, director.Readout.Hype, 1e-9d);
            return director;
        }

        private static void PlayPhraseHardCut(ClubDirector director)
        {
            Tick(director, FullA(0d));
            Tick(director, FullA(15d));
            Tick(director, FullB(16d));
            Tick(director, FullB(16.5d));
        }

        private static void PlaySmooth(ClubDirector director)
        {
            Tick(director, FullA(0d));
            Tick(director, FullA(0.5d));
            Tick(director, Middle(2d));
            Tick(director, FullB(4d));
            Tick(director, FullB(4.5d));
        }

        private static void Tick(ClubDirector director, MixSnapshot snapshot)
        {
            director.Tick(snapshot);
        }

        private static MixSnapshot Lead(double time, double lowA, double lowB)
        {
            return new MixSnapshot(time, false, true, true, 1d, 0.2d, lowA, lowB, "track_neon_pulse", "track_afterhours", true);
        }

        private static MixSnapshot FullA(double time)
        {
            return At(time, true, false, 1d, 0d, 1d, 0d);
        }

        private static MixSnapshot FullB(double time)
        {
            return At(time, false, true, 0d, 1d, 0d, 1d);
        }

        private static MixSnapshot Quiet(double time)
        {
            return At(time, true, false, 1d, 0d, 0d, 0d);
        }

        private static MixSnapshot Middle(double time)
        {
            return At(time, true, true, 0.6d, 0.6d, 0d, 0d);
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
            return new MixSnapshot(time, false, deckA, deckB, gainA, gainB, lowA, lowB, "track_neon_pulse", "track_afterhours", true);
        }
    }
}
