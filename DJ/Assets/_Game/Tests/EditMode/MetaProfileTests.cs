using System.Collections.Generic;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Run;
using DeckRogue.Core.Save;
using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class MetaProfileTests
    {
        [Test]
        public void Payout_PassedHype70_Pays29()
        {
            Assert.AreEqual(29, GigPayout.Coins(SetOutcome.Win, 70));
            Assert.AreEqual(15, GigPayout.Coins(SetOutcome.Win, 0));
            Assert.AreEqual(15, GigPayout.Coins(SetOutcome.Win, -8));
            Assert.AreEqual(35, GigPayout.Coins(SetOutcome.Win, 100));
            Assert.AreEqual(35, GigPayout.Coins(SetOutcome.Win, 250));
        }

        [Test]
        public void Payout_FailedHype40_Pays6()
        {
            Assert.AreEqual(6, GigPayout.Coins(SetOutcome.Lose, 40));
            Assert.AreEqual(12, GigPayout.Coins(SetOutcome.Lose, 100));
            Assert.AreEqual(12, GigPayout.Coins(SetOutcome.Lose, 400));
        }

        [Test]
        public void Payout_FailedHype0_Pays2()
        {
            Assert.AreEqual(2, GigPayout.Coins(SetOutcome.Lose, 0));
            Assert.AreEqual(2, GigPayout.Coins(SetOutcome.Lose, -3));
        }

        [Test]
        public void Payout_InProgress_PaysZero()
        {
            Assert.AreEqual(0, GigPayout.Coins(SetOutcome.InProgress, 100));
            Assert.AreEqual(0, GigPayout.Coins((SetOutcome)99, 80));

            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            int coins;
            Assert.IsFalse(profile.TryClaimGig("night", 0, SetOutcome.InProgress, 80, out coins));
            Assert.AreEqual(0, coins);
            Assert.AreEqual(0, doc.MetaCoins);
            Assert.AreEqual(0, doc.ClaimedGigKeys.Count);

            Assert.IsTrue(profile.TryClaimGig("night", 0, SetOutcome.Win, 70, out coins));
            Assert.AreEqual(29, coins);
            Assert.AreEqual(29, doc.MetaCoins);
        }

        [Test]
        public void Payout_SameClaimKey_PaysOnce()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);

            int coins;
            Assert.IsTrue(profile.TryClaimGig("night", 1, SetOutcome.Win, 70, out coins));
            Assert.AreEqual(29, coins);
            Assert.AreEqual(29, doc.MetaCoins);
            CollectionAssert.AreEqual(new[] { "gig:night:1" }, doc.ClaimedGigKeys);

            Assert.IsFalse(profile.TryClaimGig("night", 1, SetOutcome.Win, 100, out coins));
            Assert.AreEqual(0, coins);
            Assert.AreEqual(29, doc.MetaCoins);
            Assert.AreEqual(1, doc.ClaimedGigKeys.Count);

            Assert.IsFalse(profile.TryClaimGig("", 1, SetOutcome.Win, 70, out coins));
            Assert.IsFalse(profile.TryClaimGig(null, 1, SetOutcome.Lose, 40, out coins));
            Assert.AreEqual(0, coins);
            Assert.AreEqual(29, doc.MetaCoins);
            Assert.AreEqual(1, doc.ClaimedGigKeys.Count);

            Assert.IsTrue(profile.TryClaimGig("night", 0, SetOutcome.Lose, 0, out coins));
            Assert.AreEqual(2, coins);
            Assert.AreEqual(31, doc.MetaCoins);
            CollectionAssert.AreEqual(new[] { "gig:night:1", "gig:night:0" }, doc.ClaimedGigKeys);
        }

        [Test]
        public void Payout_DoesNotWriteHype()
        {
            Assert.AreEqual(35d, HypeSystem.StartingHype);
            Assert.AreEqual(60d, ClubDirector.FirstTargetHype);
            Assert.AreEqual(65d, ClubDirector.SecondTargetHype);

            int hype = 70;
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.BestReputation = 15;
            doc.Checkpoint.Hype = 70d;
            doc.Checkpoint.Reputation = 15d;
            var profile = new MetaProfile(doc);

            Assert.AreEqual(29, GigPayout.Coins(SetOutcome.Win, hype));
            int coins;
            Assert.IsTrue(profile.TryClaimGig("night", 0, SetOutcome.Win, hype, out coins));
            Assert.AreEqual(29, coins);
            Assert.AreEqual(70, hype);
            Assert.AreEqual(29, doc.MetaCoins);
            Assert.AreEqual(15, doc.BestReputation);
            Assert.AreEqual(70d, doc.Checkpoint.Hype, 1e-9d);
            Assert.AreEqual(15d, doc.Checkpoint.Reputation, 1e-9d);
        }

        [Test]
        public void DefaultSave_ContainsStarterDeck()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();

            Assert.AreEqual(5, doc.SchemaVersion);
            Assert.AreEqual(0, doc.MetaCoins);
            CollectionAssert.AreEqual(new[] { DeckCatalog.StarterId }, doc.OwnedDeckIds);
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);
            Assert.AreEqual(0, doc.OwnedItemIds.Count);
            Assert.AreEqual(0, doc.UnlockedGearIds.Count);
            Assert.AreEqual(0, doc.HubDay);
            Assert.IsFalse(doc.WalkUsedThisDay);
            Assert.AreEqual("", doc.SettledRunId);
            Assert.AreEqual(0, doc.ClaimedGigKeys.Count);

            int price;
            Assert.IsTrue(DeckCatalog.TryGetPrice(DeckCatalog.StarterId, out price));
            Assert.AreEqual(0, price);
            Assert.IsTrue(DeckCatalog.IsDeck(DeckCatalog.StarterId));
            Assert.IsFalse(DeckCatalog.IsItem(DeckCatalog.StarterId));

            var profile = new MetaProfile(doc);
            Assert.IsTrue(profile.Owns(DeckCatalog.StarterId));
            Assert.IsFalse(profile.EquippedKit.Filter);
            Assert.IsFalse(profile.EquippedKit.Stems);
            Assert.IsTrue(profile.WalkAvailable);
        }

        [Test]
        public void Purchase_FilterAndStems_DeductOnceAndPersist()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            doc.MetaCoins = 200;

            DeckKit filterKit = DeckKit.For(DeckCatalog.FilterId);
            DeckKit stemsKit = DeckKit.For(DeckCatalog.StemsId);
            Assert.IsTrue(filterKit.Filter);
            Assert.IsFalse(filterKit.Stems);
            Assert.IsFalse(stemsKit.Filter);
            Assert.IsTrue(stemsKit.Stems);

            Assert.IsTrue(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.AreEqual(140, doc.MetaCoins);
            Assert.IsFalse(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.AreEqual(140, doc.MetaCoins);
            Assert.AreEqual(1, CountId(doc.OwnedDeckIds, DeckCatalog.FilterId));

            Assert.IsTrue(profile.TryPurchase(DeckCatalog.StemsId));
            Assert.AreEqual(20, doc.MetaCoins);
            Assert.AreEqual(1, CountId(doc.OwnedDeckIds, DeckCatalog.StemsId));
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);
            Assert.IsFalse(profile.EquippedKit.Filter);
            Assert.IsFalse(profile.EquippedKit.Stems);

            var again = new MetaProfile(doc);
            Assert.AreEqual(20, doc.MetaCoins);
            Assert.IsTrue(again.Owns(DeckCatalog.FilterId));
            Assert.IsTrue(again.Owns(DeckCatalog.StemsId));
            Assert.IsTrue(again.Owns(DeckCatalog.StarterId));
            Assert.AreEqual(3, doc.OwnedDeckIds.Count);
        }

        [Test]
        public void Purchase_DuplicateOrBroke_ChangesNothing()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            doc.MetaCoins = 39;

            Assert.IsFalse(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.IsFalse(profile.TryPurchase(DeckCatalog.StemsId));
            Assert.IsFalse(profile.TryPurchase(DeckCatalog.SpareLeashId));
            Assert.IsFalse(profile.TryPurchase("deck_nope"));
            Assert.IsFalse(profile.TryPurchase(null));
            Assert.IsFalse(profile.TryPurchase(""));
            Assert.IsFalse(profile.TryPurchase(DeckCatalog.StarterId));

            Assert.AreEqual(39, doc.MetaCoins);
            Assert.IsFalse(profile.Owns(DeckCatalog.FilterId));
            Assert.AreEqual(1, doc.OwnedDeckIds.Count);
            Assert.AreEqual(0, doc.OwnedItemIds.Count);
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);

            int missing;
            Assert.IsFalse(DeckCatalog.TryGetPrice("deck_nope", out missing));
            Assert.AreEqual(0, missing);

            doc.MetaCoins = 60;
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.AreEqual(0, doc.MetaCoins);
            Assert.IsFalse(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.AreEqual(0, doc.MetaCoins);
            Assert.AreEqual(1, CountId(doc.OwnedDeckIds, DeckCatalog.FilterId));
        }

        [Test]
        public void Purchase_SpareLeash_HasNoScoreEffect()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            doc.MetaCoins = DeckCatalog.SpareLeashPrice;
            doc.WalkUsedThisDay = true;
            doc.BestReputation = 11;
            doc.Checkpoint.Hype = 48d;

            var bare = new HypeSystem();
            ScoreChange bareChange;
            Assert.IsTrue(bare.TryApply(
                new ScoreEvent("e1", ScoreKind.CleanTransition, 4d, 10d, true, false, false),
                out bareChange));

            Assert.IsTrue(DeckCatalog.IsItem(DeckCatalog.SpareLeashId));
            Assert.IsFalse(DeckCatalog.IsDeck(DeckCatalog.SpareLeashId));
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.SpareLeashId));
            Assert.AreEqual(0, doc.MetaCoins);
            Assert.IsTrue(profile.Owns(DeckCatalog.SpareLeashId));
            Assert.IsFalse(profile.TryEquip(DeckCatalog.SpareLeashId));
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);

            DeckKit kit = DeckKit.For(DeckCatalog.SpareLeashId);
            Assert.IsFalse(kit.Filter);
            Assert.IsFalse(kit.Stems);
            Assert.IsFalse(profile.EquippedKit.Filter);
            Assert.IsFalse(profile.EquippedKit.Stems);
            Assert.IsFalse(profile.WalkAvailable);
            Assert.AreEqual(11, doc.BestReputation);
            Assert.AreEqual(48d, doc.Checkpoint.Hype, 1e-9d);
            Assert.AreEqual(35d, HypeSystem.StartingHype);

            var withLeash = new HypeSystem();
            ScoreChange leashChange;
            Assert.IsTrue(withLeash.TryApply(
                new ScoreEvent("e1", ScoreKind.CleanTransition, 4d, 10d, true, false, false),
                out leashChange));
            Assert.AreEqual(bare.Hype, withLeash.Hype);
            Assert.AreEqual(bare.Reputation, withLeash.Reputation);
            Assert.AreEqual(bareChange.HypeDelta, leashChange.HypeDelta);
            Assert.AreEqual(bareChange.ReputationDelta, leashChange.ReputationDelta);

            Assert.IsFalse(profile.TryPurchase(DeckCatalog.SpareLeashId));
            Assert.AreEqual(0, doc.MetaCoins);
            Assert.AreEqual(1, doc.OwnedItemIds.Count);
        }

        [Test]
        public void Equip_RejectsUnownedAndItems()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            doc.MetaCoins = DeckCatalog.SpareLeashPrice;
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.SpareLeashId));

            Assert.IsFalse(profile.TryEquip(DeckCatalog.FilterId));
            Assert.IsFalse(profile.TryEquip(DeckCatalog.StemsId));
            Assert.IsFalse(profile.TryEquip(DeckCatalog.SpareLeashId));
            Assert.IsFalse(profile.TryEquip("deck_nope"));
            Assert.IsFalse(profile.TryEquip(null));
            Assert.IsFalse(profile.TryEquip(""));
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);

            Assert.IsTrue(profile.TryEquip(DeckCatalog.StarterId));
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);

            doc.MetaCoins = DeckCatalog.FilterPrice;
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.IsTrue(profile.TryEquip(DeckCatalog.FilterId));
            Assert.AreEqual(DeckCatalog.FilterId, doc.EquippedDeckId);
            Assert.IsTrue(profile.EquippedKit.Filter);
            Assert.IsFalse(profile.EquippedKit.Stems);
        }

        [Test]
        public void RunDeckLock_IgnoresLaterEquip()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            var locked = new RunDeckLock(doc);
            Assert.AreEqual(DeckCatalog.StarterId, locked.DeckId);
            Assert.IsFalse(locked.Kit.Filter);
            Assert.IsFalse(locked.Kit.Stems);

            doc.MetaCoins = DeckCatalog.FilterPrice + DeckCatalog.StemsPrice;
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.StemsId));
            Assert.IsTrue(profile.TryEquip(DeckCatalog.FilterId));
            Assert.IsTrue(profile.EquippedKit.Filter);
            Assert.IsFalse(profile.EquippedKit.Stems);
            Assert.IsFalse(locked.Kit.Filter);
            Assert.IsFalse(locked.Kit.Stems);
            Assert.AreEqual(DeckCatalog.StarterId, locked.DeckId);

            Assert.IsTrue(profile.TryEquip(DeckCatalog.StemsId));
            Assert.IsFalse(profile.EquippedKit.Filter);
            Assert.IsTrue(profile.EquippedKit.Stems);
            Assert.IsFalse(locked.Kit.Filter);
            Assert.IsFalse(locked.Kit.Stems);

            doc.EquippedDeckId = "deck_mystery";
            var unknown = new RunDeckLock(doc);
            Assert.AreEqual(DeckCatalog.StarterId, unknown.DeckId);
            Assert.IsFalse(unknown.Kit.Filter);
            Assert.IsFalse(unknown.Kit.Stems);
            Assert.AreEqual("deck_mystery", doc.EquippedDeckId);

            DeckKit mystery = DeckKit.For("deck_mystery");
            Assert.IsFalse(mystery.Filter);
            Assert.IsFalse(mystery.Stems);
            DeckKit missing = DeckKit.For(null);
            Assert.IsFalse(missing.Filter);
            Assert.IsFalse(missing.Stems);
        }

        [Test]
        public void Lose_KeepsCoinsAndGear()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            doc.MetaCoins = DeckCatalog.FilterPrice + DeckCatalog.StemsPrice + DeckCatalog.SpareLeashPrice;
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.FilterId));
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.StemsId));
            Assert.IsTrue(profile.TryPurchase(DeckCatalog.SpareLeashId));
            Assert.IsTrue(profile.TryEquip(DeckCatalog.StemsId));
            Assert.AreEqual(0, doc.MetaCoins);

            int coins;
            Assert.IsTrue(profile.TryClaimGig("night", 1, SetOutcome.Lose, 40, out coins));
            Assert.AreEqual(6, coins);
            Assert.AreEqual(6, doc.MetaCoins);
            Assert.IsTrue(profile.Owns(DeckCatalog.FilterId));
            Assert.IsTrue(profile.Owns(DeckCatalog.StemsId));
            Assert.IsTrue(profile.Owns(DeckCatalog.SpareLeashId));
            Assert.AreEqual(DeckCatalog.StemsId, doc.EquippedDeckId);

            Assert.IsFalse(profile.TryClaimGig("night", 1, SetOutcome.Lose, 0, out coins));
            Assert.AreEqual(0, coins);
            Assert.AreEqual(6, doc.MetaCoins);
            Assert.IsTrue(profile.Owns(DeckCatalog.FilterId));
        }

        [Test]
        public void DogWalk_FirstOfDay_Pays4()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.Checkpoint.Hype = 55d;
            doc.BestReputation = 9;
            var profile = new MetaProfile(doc);

            Assert.IsTrue(profile.WalkAvailable);
            Assert.IsFalse(profile.Owns(DeckCatalog.SpareLeashId));
            int coins;
            Assert.IsTrue(profile.TryCompleteWalk(out coins));
            Assert.AreEqual(4, coins);
            Assert.AreEqual(MetaProfile.WalkCoins, coins);
            Assert.AreEqual(4, doc.MetaCoins);
            Assert.IsTrue(doc.WalkUsedThisDay);
            Assert.IsFalse(profile.WalkAvailable);
            Assert.AreEqual(0, doc.HubDay);
            Assert.AreEqual(55d, doc.Checkpoint.Hype, 1e-9d);
            Assert.AreEqual(9, doc.BestReputation);
        }

        [Test]
        public void DogWalk_SameDayAgain_PaysZero()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);

            int coins;
            Assert.IsTrue(profile.TryCompleteWalk(out coins));
            Assert.AreEqual(4, coins);

            Assert.IsFalse(profile.TryCompleteWalk(out coins));
            Assert.AreEqual(0, coins);
            Assert.AreEqual(4, doc.MetaCoins);
            Assert.IsTrue(doc.WalkUsedThisDay);
            Assert.AreEqual(0, doc.HubDay);
        }

        [Test]
        public void DogWalk_Abandon_DoesNotSpendDay()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);
            int day = doc.HubDay;
            int coinsBefore = doc.MetaCoins;

            profile.AbandonWalk();

            Assert.AreEqual(coinsBefore, doc.MetaCoins);
            Assert.AreEqual(day, doc.HubDay);
            Assert.IsFalse(doc.WalkUsedThisDay);
            Assert.AreEqual("", doc.SettledRunId);
            Assert.IsTrue(profile.WalkAvailable);

            int coins;
            Assert.IsTrue(profile.TryCompleteWalk(out coins));
            Assert.AreEqual(4, coins);
            Assert.AreEqual(4, doc.MetaCoins);
        }

        [Test]
        public void SettleNight_SameRun_DoesNotAdvanceDay()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);

            Assert.IsTrue(profile.SettleNight("night-1"));
            Assert.AreEqual(1, doc.HubDay);
            Assert.IsFalse(doc.WalkUsedThisDay);
            Assert.AreEqual("night-1", doc.SettledRunId);

            int coins;
            Assert.IsTrue(profile.TryCompleteWalk(out coins));
            Assert.AreEqual(4, doc.MetaCoins);
            Assert.IsTrue(doc.WalkUsedThisDay);

            Assert.IsFalse(profile.SettleNight("night-1"));
            Assert.AreEqual(1, doc.HubDay);
            Assert.IsTrue(doc.WalkUsedThisDay);
            Assert.AreEqual(4, doc.MetaCoins);
            Assert.AreEqual("night-1", doc.SettledRunId);

            Assert.IsFalse(profile.SettleNight(""));
            Assert.IsFalse(profile.SettleNight(null));
            Assert.AreEqual(1, doc.HubDay);
            Assert.IsTrue(doc.WalkUsedThisDay);

            Assert.IsTrue(profile.SettleNight("night-2"));
            Assert.AreEqual(2, doc.HubDay);
            Assert.IsFalse(doc.WalkUsedThisDay);
            Assert.AreEqual("night-2", doc.SettledRunId);
            Assert.AreEqual(4, doc.MetaCoins);
            Assert.IsTrue(profile.WalkAvailable);
        }

        [Test]
        public void Migrate_V4_ReopensCoachAndKeepsTracks()
        {
            var loaded = new SaveGameDocument
            {
                SchemaVersion = 4,
                Settings = SettingsData.CreateDefault(),
                UnlockedTrackIds = new List<string> { "track_signal_carrier", "track_signal_carrier" },
                UnlockedGearIds = new List<string> { "upgrade_auto_bass" },
                BestReputation = 80,
                MetaCoins = 999,
                OwnedDeckIds = new List<string> { DeckCatalog.FilterId },
                EquippedDeckId = DeckCatalog.FilterId,
                OwnedItemIds = new List<string> { DeckCatalog.SpareLeashId },
                HubDay = 4,
                WalkUsedThisDay = true,
                SettledRunId = "old-run",
                ClaimedGigKeys = new List<string> { "gig:old-run:0" }
            };
            loaded.Settings.BoothCoachCompleted = true;
            loaded.Settings.TutorialCompleted = true;
            loaded.Settings.Fullscreen = false;
            loaded.Settings.ScreenWidth = 1280;
            loaded.Settings.ScreenHeight = 720;

            RunCheckpoint checkpoint = RunCheckpoint.CreateEmpty();
            checkpoint.Active = true;
            checkpoint.Phase = RunPhase.Performing;
            checkpoint.ClubIndex = 2;
            checkpoint.Seed = 7;
            checkpoint.Hype = 44d;
            checkpoint.Reputation = 12d;
            loaded.Checkpoint = checkpoint;

            SaveGameDocument doc = SaveGameDocument.Migrate(loaded);

            Assert.AreEqual(5, doc.SchemaVersion);
            Assert.IsFalse(doc.Settings.BoothCoachCompleted);
            Assert.IsTrue(loaded.Settings.BoothCoachCompleted);
            Assert.IsTrue(doc.Settings.TutorialCompleted);
            Assert.IsFalse(doc.Settings.Fullscreen);
            Assert.AreEqual(1280, doc.Settings.ScreenWidth);
            Assert.AreEqual(720, doc.Settings.ScreenHeight);
            CollectionAssert.AreEqual(new[] { "track_signal_carrier" }, doc.UnlockedTrackIds);
            Assert.AreEqual(80, doc.BestReputation);
            Assert.IsTrue(doc.HasResumableCheckpoint);
            Assert.AreEqual(7, doc.Checkpoint.Seed);
            Assert.AreEqual(2, doc.Checkpoint.ClubIndex);
            Assert.AreEqual(RunPhase.Performing, doc.Checkpoint.Phase);
            Assert.AreEqual(44d, doc.Checkpoint.Hype, 1e-9d);
            Assert.AreEqual(12d, doc.Checkpoint.Reputation, 1e-9d);
            Assert.AreEqual(0, doc.UnlockedGearIds.Count);
            Assert.AreEqual(0, doc.MetaCoins);
            CollectionAssert.AreEqual(new[] { DeckCatalog.StarterId }, doc.OwnedDeckIds);
            Assert.AreEqual(DeckCatalog.StarterId, doc.EquippedDeckId);
            Assert.AreEqual(0, doc.OwnedItemIds.Count);
            Assert.AreEqual(0, doc.HubDay);
            Assert.IsFalse(doc.WalkUsedThisDay);
            Assert.AreEqual("", doc.SettledRunId);
            Assert.AreEqual(0, doc.ClaimedGigKeys.Count);
            Assert.IsFalse(doc.HubUnlocked);

            SaveGameDocument current = SaveGameDocument.CreateDefault();
            current.MetaCoins = 17;
            current.OwnedDeckIds.Add(DeckCatalog.FilterId);
            current.EquippedDeckId = DeckCatalog.FilterId;
            current.OwnedItemIds.Add(DeckCatalog.SpareLeashId);
            current.HubDay = 3;
            current.WalkUsedThisDay = true;
            current.SettledRunId = "run-9";
            current.ClaimedGigKeys.Add("gig:run-9:1");
            current.UnlockedTrackIds.Add("track_amber_bloom");
            current.BestReputation = 12;
            current.Settings.BoothCoachCompleted = true;

            SaveGameDocument kept = SaveGameDocument.Migrate(current);
            Assert.AreEqual(5, kept.SchemaVersion);
            Assert.IsTrue(kept.Settings.BoothCoachCompleted);
            Assert.AreEqual(17, kept.MetaCoins);
            CollectionAssert.AreEqual(
                new[] { DeckCatalog.StarterId, DeckCatalog.FilterId },
                kept.OwnedDeckIds);
            Assert.AreEqual(DeckCatalog.FilterId, kept.EquippedDeckId);
            CollectionAssert.AreEqual(new[] { DeckCatalog.SpareLeashId }, kept.OwnedItemIds);
            Assert.AreEqual(3, kept.HubDay);
            Assert.IsTrue(kept.WalkUsedThisDay);
            Assert.AreEqual("run-9", kept.SettledRunId);
            CollectionAssert.AreEqual(new[] { "gig:run-9:1" }, kept.ClaimedGigKeys);
            CollectionAssert.AreEqual(new[] { "track_amber_bloom" }, kept.UnlockedTrackIds);
            Assert.AreEqual(12, kept.BestReputation);
            Assert.IsTrue(kept.HubUnlocked);
        }

        [Test]
        public void StarterDeck_ReachesOpeningClubs_WithoutKnobs()
        {
            DeckKit starter = DeckKit.For(DeckCatalog.StarterId);
            Assert.IsFalse(starter.Filter);
            Assert.IsFalse(starter.Stems);

            AssertOpeningClub(Act1ClubDefinition.NeonBasement);
            AssertOpeningClub(Act1ClubDefinition.WarehousePulse);
            AssertOpeningClub(Act1ClubDefinition.BassFactory);
        }

        [Test]
        public void DefaultSave_HubLocked()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            Assert.IsFalse(doc.HubUnlocked);
            Assert.AreEqual(0, doc.MetaCoins);
            Assert.AreEqual(0, doc.HubDay);
            Assert.AreEqual(0, doc.ClaimedGigKeys.Count);

            SaveGameDocument fresh = SaveGameDocument.Migrate(doc);
            Assert.IsFalse(fresh.HubUnlocked);
            Assert.AreEqual(0, fresh.MetaCoins);
            Assert.AreEqual(0, fresh.HubDay);
            Assert.AreEqual(0, fresh.ClaimedGigKeys.Count);
        }

        [Test]
        public void UnlockHub_Once()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var profile = new MetaProfile(doc);

            Assert.IsTrue(profile.TryUnlockHub());
            Assert.IsTrue(doc.HubUnlocked);
            Assert.IsFalse(profile.TryUnlockHub());
            Assert.IsTrue(doc.HubUnlocked);

            SaveGameDocument copied = SaveGameDocument.Migrate(doc);
            Assert.IsTrue(copied.HubUnlocked);
            Assert.AreEqual(0, copied.MetaCoins);
            Assert.AreEqual(0, copied.HubDay);
            Assert.AreEqual(0, copied.ClaimedGigKeys.Count);
        }

        [Test]
        public void Migrate_PriorEarnings_UnlocksHub()
        {
            SaveGameDocument coins = SaveGameDocument.CreateDefault();
            coins.MetaCoins = 4;
            Assert.IsFalse(coins.HubUnlocked);
            SaveGameDocument fromCoins = SaveGameDocument.Migrate(coins);
            Assert.IsTrue(fromCoins.HubUnlocked);
            Assert.AreEqual(4, fromCoins.MetaCoins);
            Assert.AreEqual(0, fromCoins.HubDay);

            SaveGameDocument key = SaveGameDocument.CreateDefault();
            key.ClaimedGigKeys.Add("gig:night:0");
            SaveGameDocument fromKey = SaveGameDocument.Migrate(key);
            Assert.IsTrue(fromKey.HubUnlocked);
            Assert.AreEqual(0, fromKey.MetaCoins);
            Assert.AreEqual(0, fromKey.HubDay);
            CollectionAssert.AreEqual(new[] { "gig:night:0" }, fromKey.ClaimedGigKeys);

            SaveGameDocument day = SaveGameDocument.CreateDefault();
            day.HubDay = 2;
            SaveGameDocument fromDay = SaveGameDocument.Migrate(day);
            Assert.IsTrue(fromDay.HubUnlocked);
            Assert.AreEqual(0, fromDay.MetaCoins);
            Assert.AreEqual(2, fromDay.HubDay);
            Assert.AreEqual(0, fromDay.ClaimedGigKeys.Count);

            SaveGameDocument spent = SaveGameDocument.CreateDefault();
            spent.OwnedDeckIds.Add(DeckCatalog.FilterId);
            SaveGameDocument stillLocked = SaveGameDocument.Migrate(spent);
            Assert.IsFalse(stillLocked.HubUnlocked);
            Assert.AreEqual(0, stillLocked.MetaCoins);
            Assert.AreEqual(0, stillLocked.HubDay);
        }

        private static void AssertOpeningClub(Act1ClubDefinition club)
        {
            var hype = new HypeSystem();
            hype.BeginNextSet(club.InitialHype);
            Assert.AreEqual(club.InitialHype, hype.Hype, 1e-9d, club.Id);

            int decays = club.DurationBars / 8;
            for (int phrase = 0; phrase < decays; phrase++)
                Assert.IsTrue(hype.TryApply(ScoreEvent.Decay(phrase), out _));
            Assert.IsTrue(hype.TryApply(ScoreEvent.Clean(0, 0, 1, false), out _));
            Assert.IsTrue(hype.TryApply(ScoreEvent.Clean(1, 1, 0, false), out _));
            Assert.IsTrue(hype.TryApply(ScoreEvent.Drop(8), out _));

            Assert.GreaterOrEqual(hype.Hype, club.TargetHype, club.Id);
        }

        private static int CountId(List<string> ids, string id)
        {
            int count = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    count++;
            }

            return count;
        }
    }
}
