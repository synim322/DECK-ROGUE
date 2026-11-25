using System.Collections.Generic;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Run;
using DeckRogue.Core.Save;
using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class SaveGameTests
    {
        [Test]
        public void CreateDefault_HasSchemaV1_EmptyUnlocks_DefaultSettings()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();

            Assert.AreEqual(SaveSchema.CurrentVersion, doc.SchemaVersion);
            Assert.AreEqual(5, SaveSchema.CurrentVersion);
            Assert.IsFalse(doc.HasResumableCheckpoint);
            Assert.AreEqual(0, doc.BestReputation);
            Assert.AreEqual(0, doc.UnlockedTrackIds.Count);
            Assert.AreEqual(0, doc.UnlockedGearIds.Count);
            Assert.AreEqual(0, doc.UnlockedClubIds.Count);
            Assert.AreEqual("en", doc.Settings.Language);
            Assert.AreEqual(0.8f, doc.Settings.MusicVolume, 1e-6f);
            Assert.AreEqual(0.7f, doc.Settings.SfxVolume, 1e-6f);
            Assert.AreEqual(1.0f, doc.Settings.UiScale, 1e-6f);
            Assert.IsFalse(doc.Settings.ReducedFlashing);
            Assert.IsFalse(doc.Settings.TutorialCompleted);
            Assert.IsFalse(doc.Settings.BoothCoachCompleted);
            Assert.IsTrue(doc.Settings.Fullscreen);
            Assert.AreEqual(0, doc.Settings.ScreenWidth);
            Assert.AreEqual(0, doc.Settings.ScreenHeight);
        }

        [Test]
        public void Migrate_Null_YieldsDefault()
        {
            SaveGameDocument doc = SaveGameDocument.Migrate(null);
            Assert.AreEqual(5, doc.SchemaVersion);
            Assert.AreEqual(0, doc.UnlockedTrackIds.Count);
            Assert.IsFalse(doc.HasResumableCheckpoint);
        }

        [Test]
        public void Migrate_Schema0_CarriesTracksAndClampsVolumes()
        {
            var loaded = new SaveGameDocument
            {
                SchemaVersion = 0,
                Settings = new SettingsData
                {
                    Language = "",
                    MusicVolume = 2.5f,
                    SfxVolume = -0.2f,
                    UiScale = 9f,
                    ReducedFlashing = true,
                    TutorialCompleted = true,
                    Fullscreen = false,
                    ScreenWidth = 800,
                    ScreenHeight = 600
                },
                UnlockedTrackIds = new List<string> { "track_signal_carrier", "track_signal_carrier", "" },
                UnlockedGearIds = new List<string> { "upgrade_auto_bass" },
                UnlockedClubIds = new List<string> { "club_neon_basement" },
                BestReputation = 420
            };

            SaveGameDocument doc = SaveGameDocument.Migrate(loaded);

            Assert.AreEqual(5, doc.SchemaVersion);
            Assert.AreEqual("en", doc.Settings.Language);
            Assert.AreEqual(1f, doc.Settings.MusicVolume, 1e-6f);
            Assert.AreEqual(0f, doc.Settings.SfxVolume, 1e-6f);
            Assert.AreEqual(1.5f, doc.Settings.UiScale, 1e-6f);
            Assert.IsTrue(doc.Settings.ReducedFlashing);
            // Pre-v3 TutorialCompleted was a stub — migrator re-opens the real tutorial.
            Assert.IsFalse(doc.Settings.TutorialCompleted);
            // Pre-v4 display fields reset to fullscreen + native.
            Assert.IsTrue(doc.Settings.Fullscreen);
            Assert.AreEqual(0, doc.Settings.ScreenWidth);
            Assert.AreEqual(0, doc.Settings.ScreenHeight);
            Assert.AreEqual(420, doc.BestReputation);
            CollectionAssert.AreEqual(new[] { "track_signal_carrier" }, doc.UnlockedTrackIds);
            Assert.AreEqual(0, doc.UnlockedGearIds.Count);
            Assert.AreEqual(0, doc.UnlockedClubIds.Count);
        }

        [Test]
        public void ApplyToMeta_HydratesAndSurviveDirectorReset()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.UnlockedTrackIds.Add(TrackRewardRoll.BossCarrierId);

            var meta = new MetaUnlocks();
            doc.ApplyToMeta(meta);
            Assert.IsTrue(meta.Contains(TrackRewardRoll.BossCarrierId));

            // Director Meta survives Reset; run crate does not keep carrier without a clear.
            var director = new Act1RunDirector(7);
            director.Meta.Hydrate(doc.UnlockedTrackIds);
            Assert.IsTrue(director.Meta.Contains(TrackRewardRoll.BossCarrierId));
            director.Reset(7);
            Assert.IsTrue(director.Meta.Contains(TrackRewardRoll.BossCarrierId));
            Assert.AreEqual(4, director.Crate.Count);
            Assert.IsFalse(director.Crate.Contains(TrackRewardRoll.BossCarrierId));
        }

        [Test]
        public void CaptureMeta_RoundTripsUnlockIds()
        {
            var meta = new MetaUnlocks();
            meta.Unlock(TrackRewardRoll.BossCarrierId);

            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.CaptureMeta(meta);

            CollectionAssert.AreEqual(new[] { TrackRewardRoll.BossCarrierId }, doc.UnlockedTrackIds);

            var again = new MetaUnlocks();
            doc.ApplyToMeta(again);
            Assert.IsTrue(again.Contains(TrackRewardRoll.BossCarrierId));
        }

        [Test]
        public void TryUpdateBestReputation_OnlyRaises()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            Assert.IsFalse(doc.TryUpdateBestReputation(0));
            Assert.IsTrue(doc.TryUpdateBestReputation(100));
            Assert.AreEqual(100, doc.BestReputation);
            Assert.IsFalse(doc.TryUpdateBestReputation(50));
            Assert.AreEqual(100, doc.BestReputation);
            Assert.IsTrue(doc.TryUpdateBestReputation(101));
            Assert.AreEqual(101, doc.BestReputation);
            Assert.IsFalse(doc.TryUpdateBestReputation(-5));
            Assert.AreEqual(101, doc.BestReputation);
        }

        [Test]
        public void FlushTriggers_AreDocumentedForShell()
        {
            Assert.AreEqual(0, (int)SaveFlushTrigger.SettingsChanged);
            Assert.AreEqual(1, (int)SaveFlushTrigger.MetaUnlockChanged);
            Assert.AreEqual(2, (int)SaveFlushTrigger.RunEndedBestReputation);
            Assert.AreEqual(3, (int)SaveFlushTrigger.RunCheckpointChanged);
            Assert.AreEqual(4, (int)SaveFlushTrigger.MetaWalletChanged);
        }

        [Test]
        public void CaptureCheckpoint_RoundTripsThroughDirectorRestore()
        {
            int seed = 7;
            var director = new Act1RunDirector(seed);
            director.BeginClub();
            director.Tick(new MixSnapshot(
                0d, false, true, false, 1d, 0d, 1d, 0d,
                "track_neon_pulse", "track_afterhours", true));
            director.Tick(new MixSnapshot(
                12d, false, true, false, 1d, 0d, 1d, 0d,
                "track_neon_pulse", "track_afterhours", true));

            RunCheckpoint checkpoint = director.CaptureCheckpoint();
            Assert.IsTrue(checkpoint.CanResume);
            Assert.AreEqual(seed, checkpoint.Seed);
            Assert.AreEqual(RunPhase.Performing, checkpoint.Phase);
            Assert.AreEqual(12d, checkpoint.ElapsedSeconds, 1e-9d);

            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.SetCheckpoint(checkpoint);
            Assert.IsTrue(doc.HasResumableCheckpoint);

            var again = new Act1RunDirector(seed);
            Assert.IsTrue(again.TryRestore(doc.Checkpoint));
            Assert.AreEqual(RunPhase.Performing, again.Readout.Phase);
            Assert.AreEqual(director.Readout.ClubId, again.Readout.ClubId);
            Assert.AreEqual(
                director.Readout.RemainingSeconds,
                again.Readout.RemainingSeconds,
                1e-6d);
            Assert.AreEqual(116d, again.Readout.RemainingSeconds, 1e-6d);
        }

        [Test]
        public void PlaceLine_NamesClubAndLevel()
        {
            int seed = 7;
            string[] route = Act1Route.Build(seed);
            RunCheckpoint empty = RunCheckpoint.CreateEmpty();
            Assert.AreEqual("", empty.PlaceLine());

            var checkpoint = RunCheckpoint.CreateEmpty();
            checkpoint.Active = true;
            checkpoint.Seed = seed;
            checkpoint.ClubIndex = 2;
            checkpoint.Phase = RunPhase.Performing;
            string expected = "club 3/"
                + Act1Route.ClubCount
                + " · "
                + CampaignCatalog.DisplayName(route[2]);
            Assert.AreEqual(expected, checkpoint.PlaceLine());

            checkpoint.Phase = RunPhase.FinalResult;
            Assert.AreEqual("", checkpoint.PlaceLine());
        }

        [Test]
        public void CaptureCheckpoint_KeepsWornDeckAndLevel()
        {
            var director = new Act1RunDirector(7);
            director.BeginClub();
            RunCheckpoint checkpoint = director.CaptureCheckpoint(DeckCatalog.FilterId);

            Assert.AreEqual(DeckCatalog.FilterId, checkpoint.DeckId);
            Assert.AreEqual(0, checkpoint.ClubIndex);
            Assert.IsTrue(checkpoint.PlaceLine().StartsWith("club 1/"));
            StringAssert.Contains(CampaignCatalog.DisplayName(director.Readout.ClubId), checkpoint.PlaceLine());

            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.OwnedDeckIds.Add(DeckCatalog.FilterId);
            doc.EquippedDeckId = DeckCatalog.FilterId;
            doc.SetCheckpoint(checkpoint);

            SaveGameDocument kept = SaveGameDocument.Migrate(doc);
            Assert.AreEqual(DeckCatalog.FilterId, kept.Checkpoint.DeckId);
            Assert.AreEqual(0, kept.Checkpoint.ClubIndex);
            Assert.AreEqual(checkpoint.PlaceLine(), kept.Checkpoint.PlaceLine());
            CollectionAssert.Contains(kept.OwnedDeckIds, DeckCatalog.FilterId);
            Assert.AreEqual(DeckCatalog.FilterId, kept.EquippedDeckId);
        }

        [Test]
        public void HydrateFrom_ThenReset_KeepsCarrierUnlock()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.UnlockedTrackIds.Add(TrackRewardRoll.BossCarrierId);

            var director = new Act1RunDirector(7);
            director.HydrateFrom(doc);
            Assert.IsTrue(director.Meta.Contains(TrackRewardRoll.BossCarrierId));
            Assert.IsTrue(director.Readout.MetaHasCarrier);

            director.Reset(7);
            Assert.IsTrue(director.Meta.Contains(TrackRewardRoll.BossCarrierId));
            Assert.IsTrue(director.Readout.MetaHasCarrier);
            Assert.AreEqual(4, director.Crate.Count);
            Assert.IsFalse(director.Crate.Contains(TrackRewardRoll.BossCarrierId));
        }

        [Test]
        public void Act1SaveSync_UnlockViaMeta_CaptureMeta_FiresMetaUnlockChanged()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var director = new Act1RunDirector(3);
            Assert.IsFalse(director.Meta.Contains(TrackRewardRoll.BossCarrierId));

            director.Meta.Unlock(TrackRewardRoll.BossCarrierId);

            SaveFlushTrigger[] triggers = Act1SaveSync.Apply(doc, director);
            CollectionAssert.AreEqual(new[] { SaveFlushTrigger.MetaUnlockChanged }, triggers);
            CollectionAssert.AreEqual(new[] { TrackRewardRoll.BossCarrierId }, doc.UnlockedTrackIds);

            // Idempotent: second poll with same meta yields no flush.
            triggers = Act1SaveSync.Apply(doc, director);
            Assert.AreEqual(0, triggers.Length);
        }

        [Test]
        public void Act1SaveSync_BossClear_CaptureMeta_RoundTrip()
        {
            // seed 0: three ordinary, then Signal; same path as Act1RunDirectorTests.Boss_Clear.
            var director = new Act1RunDirector(0);
            ClearOrdinaryForBoss(director);
            Assert.AreEqual(Act1ClubIds.TheSignal, director.Readout.ClubId);

            if (director.Readout.Phase == RunPhase.PreClub)
                director.BeginClub();
            director.NotifyCueReadyHonor();
            PlayDropAndHardCutsForBoss(director, 144d);

            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonSignalClear, director.Readout.ReasonId);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome);
            Assert.IsTrue(director.Meta.Contains(TrackRewardRoll.BossCarrierId));

            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            SaveFlushTrigger[] triggers = Act1SaveSync.Apply(doc, director);

            CollectionAssert.Contains(triggers, SaveFlushTrigger.MetaUnlockChanged);
            CollectionAssert.AreEqual(new[] { TrackRewardRoll.BossCarrierId }, doc.UnlockedTrackIds);

            var again = new MetaUnlocks();
            doc.ApplyToMeta(again);
            Assert.IsTrue(again.Contains(TrackRewardRoll.BossCarrierId));
        }

        [Test]
        public void Act1SaveSync_FinalResult_RaisesBestReputation()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            doc.BestReputation = 10;

            var meta = new MetaUnlocks();
            SaveFlushTrigger[] triggers = Act1SaveSync.Apply(
                doc, meta, RunPhase.FinalResult, 42.9d);

            CollectionAssert.AreEqual(new[] { SaveFlushTrigger.RunEndedBestReputation }, triggers);
            Assert.AreEqual(42, doc.BestReputation);

            triggers = Act1SaveSync.Apply(doc, meta, RunPhase.FinalResult, 40d);
            Assert.AreEqual(0, triggers.Length);
            Assert.AreEqual(42, doc.BestReputation);

            // Non-final phases never touch best rep.
            triggers = Act1SaveSync.Apply(doc, meta, RunPhase.Performing, 999d);
            Assert.AreEqual(0, triggers.Length);
            Assert.AreEqual(42, doc.BestReputation);
        }

        [Test]
        public void Act1SaveSync_FinalResult_WithNewMeta_FiresBothTriggers()
        {
            SaveGameDocument doc = SaveGameDocument.CreateDefault();
            var meta = new MetaUnlocks();
            meta.Unlock(TrackRewardRoll.BossCarrierId);

            SaveFlushTrigger[] triggers = Act1SaveSync.Apply(
                doc, meta, RunPhase.FinalResult, 100d);

            CollectionAssert.AreEqual(
                new[] { SaveFlushTrigger.MetaUnlockChanged, SaveFlushTrigger.RunEndedBestReputation },
                triggers);
            Assert.AreEqual(100, doc.BestReputation);
            CollectionAssert.AreEqual(new[] { TrackRewardRoll.BossCarrierId }, doc.UnlockedTrackIds);
        }

        private static void ClearOrdinaryForBoss(Act1RunDirector director)
        {
            for (int i = 0; i < Act1Route.OrdinaryPick; i++)
            {
                Assert.IsFalse(director.Readout.IsBoss);
                PlayHardCutsForBoss(director, 128d);
                Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome, director.Readout.ClubId);
                director.Continue();
                Assert.IsTrue(director.AdvanceFromReward());
            }
        }

        private static void PlayHardCutsForBoss(Act1RunDirector director, double endTime)
        {
            if (director.Readout.Phase == RunPhase.PreClub)
                director.BeginClub();
            if (director.Readout.ClubId == Act1ClubIds.WarehousePulse)
                director.NotifyCleanPrepTransition();

            director.Tick(FullA(0d));
            director.Tick(FullA(0.5d));
            bool onA = true;
            for (double boundary = 16d; boundary < endTime - 1e-9d; boundary += 16d)
            {
                onA = !onA;
                director.Tick(onA ? FullA(boundary) : FullB(boundary));
                director.Tick(onA ? FullA(boundary + 0.5d) : FullB(boundary + 0.5d));
            }

            director.Tick(onA ? FullA(endTime) : FullB(endTime));
        }

        private static void PlayDropAndHardCutsForBoss(Act1RunDirector director, double endTime)
        {
            if (director.Readout.Phase == RunPhase.PreClub)
                director.BeginClub();
            if (director.Readout.ClubId == Act1ClubIds.WarehousePulse)
                director.NotifyCleanPrepTransition();

            director.Tick(FullA(0d));
            director.Tick(QuietBass(1d));
            director.Tick(QuietBass(3d));
            director.Tick(FullA(4d));
            bool onA = true;
            for (double boundary = 16d; boundary < endTime - 1e-9d; boundary += 16d)
            {
                onA = !onA;
                director.Tick(onA ? FullA(boundary) : FullB(boundary));
                director.Tick(onA ? FullA(boundary + 0.5d) : FullB(boundary + 0.5d));
            }

            director.Tick(onA ? FullA(endTime) : FullB(endTime));
        }

        private static MixSnapshot FullA(double time)
        {
            return new MixSnapshot(
                time, false, true, false, 1d, 0d, 1d, 0d,
                "track_neon_pulse", "track_afterhours", true);
        }

        private static MixSnapshot FullB(double time)
        {
            return new MixSnapshot(
                time, false, false, true, 0d, 1d, 0d, 1d,
                "track_neon_pulse", "track_afterhours", true);
        }

        private static MixSnapshot QuietBass(double time)
        {
            return new MixSnapshot(
                time, false, true, false, 1d, 0d, 0d, 0d,
                "track_neon_pulse", "track_afterhours", true);
        }
    }
}
