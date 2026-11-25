using System.Collections.Generic;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Run;
using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class Act1RunDirectorTests
    {
        [Test]
        public void Route_Build_ThreeDistinctOrdinary_ThenSignal()
        {
            for (int seed = 0; seed < 64; seed++)
            {
                string[] route = Act1Route.Build(seed);
                Assert.AreEqual(Act1Route.ClubCount, route.Length);
                Assert.AreEqual(12, route.Length);
                Assert.AreEqual(Act1ClubIds.TheSignal, route[3]);
                Assert.AreEqual(CampaignClubIds.TheStaticThrone, route[7]);
                Assert.AreEqual(CampaignClubIds.FinalFestival, route[11]);
                Assert.AreNotEqual(route[0], route[1]);
                Assert.AreNotEqual(route[0], route[2]);
                Assert.AreNotEqual(route[1], route[2]);
                Assert.IsTrue(IsOrdinary(route[0]));
                Assert.IsTrue(IsOrdinary(route[1]));
                Assert.IsTrue(IsOrdinary(route[2]));
                Assert.IsFalse(IsOrdinary(route[3]));
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        Act1ClubIds.NeonBasement,
                        Act1ClubIds.WarehousePulse,
                        Act1ClubIds.BassFactory
                    },
                    new[] { route[0], route[1], route[2] });
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        CampaignClubIds.DreamLounge,
                        CampaignClubIds.RaveDistrict,
                        CampaignClubIds.ChromePier
                    },
                    new[] { route[4], route[5], route[6] });
                CollectionAssert.AreEquivalent(
                    new[]
                    {
                        CampaignClubIds.GlassCathedral,
                        CampaignClubIds.NightExchange,
                        CampaignClubIds.RoofRelay
                    },
                    new[] { route[8], route[9], route[10] });

                var seen = new HashSet<string>();
                for (int i = 0; i < route.Length; i++)
                    Assert.IsTrue(seen.Add(route[i]), route[i]);
            }
        }

        [Test]
        public void Route_SameSeed_SameOrder_AndPoolIsShuffled()
        {
            string[] a = Act1Route.Build(7);
            string[] b = Act1Route.Build(7);
            CollectionAssert.AreEqual(a, b);

            HashSet<string> firstSlots = new HashSet<string>();
            for (int seed = 0; seed < 48; seed++)
                firstSlots.Add(Act1Route.Build(seed)[0]);

            Assert.GreaterOrEqual(firstSlots.Count, 2);
            Assert.IsTrue(firstSlots.Contains(Act1ClubIds.NeonBasement)
                || firstSlots.Contains(Act1ClubIds.WarehousePulse)
                || firstSlots.Contains(Act1ClubIds.BassFactory));
        }

        [Test]
        public void Definitions_MatchEasedEarlyAct1Table()
        {
            Assert.AreEqual(50d, Act1ClubDefinition.NeonBasement.TargetHype, 1e-9d);
            Assert.AreEqual(42d, Act1ClubDefinition.NeonBasement.InitialHype, 1e-9d);
            Assert.AreEqual(64, Act1ClubDefinition.NeonBasement.DurationBars);
            Assert.AreEqual(Act1PassRule.None, Act1ClubDefinition.NeonBasement.PassRule);

            Assert.AreEqual(50d, Act1ClubDefinition.WarehousePulse.TargetHype, 1e-9d);
            Assert.AreEqual(40d, Act1ClubDefinition.WarehousePulse.InitialHype, 1e-9d);
            Assert.AreEqual(Act1PassRule.CleanPrepOrNoStale, Act1ClubDefinition.WarehousePulse.PassRule);

            Assert.AreEqual(50d, Act1ClubDefinition.BassFactory.TargetHype, 1e-9d);
            Assert.AreEqual(40d, Act1ClubDefinition.BassFactory.InitialHype, 1e-9d);
            Assert.AreEqual(Act1PassRule.BassCleanLimit, Act1ClubDefinition.BassFactory.PassRule);

            Assert.AreEqual(65d, Act1ClubDefinition.TheSignal.TargetHype, 1e-9d);
            Assert.AreEqual(45d, Act1ClubDefinition.TheSignal.InitialHype, 1e-9d);
            Assert.AreEqual(72, Act1ClubDefinition.TheSignal.DurationBars);
            Assert.AreEqual(48d, Act1RunDirector.SignalLockHype, 1e-9d);
            Assert.IsTrue(Act1ClubDefinition.TheSignal.IsBoss);

            Assert.AreEqual(40d, Act1ClubDefinition.DreamLounge.InitialHype, 1e-9d);
            Assert.AreEqual(58d, Act1ClubDefinition.DreamLounge.TargetHype, 1e-9d);
            Assert.AreEqual(72, Act1ClubDefinition.DreamLounge.DurationBars);
            Assert.AreEqual(Act1PassRule.SoftHold, Act1ClubDefinition.DreamLounge.PassRule);

            Assert.AreEqual(38d, Act1ClubDefinition.RaveDistrict.InitialHype, 1e-9d);
            Assert.AreEqual(60d, Act1ClubDefinition.RaveDistrict.TargetHype, 1e-9d);
            Assert.AreEqual(Act1PassRule.NeedDrop, Act1ClubDefinition.RaveDistrict.PassRule);

            Assert.AreEqual(38d, Act1ClubDefinition.ChromePier.InitialHype, 1e-9d);
            Assert.AreEqual(62d, Act1ClubDefinition.ChromePier.TargetHype, 1e-9d);
            Assert.AreEqual(Act1PassRule.SaltLimit, Act1ClubDefinition.ChromePier.PassRule);

            Assert.AreEqual(42d, Act1ClubDefinition.TheStaticThrone.InitialHype, 1e-9d);
            Assert.AreEqual(70d, Act1ClubDefinition.TheStaticThrone.TargetHype, 1e-9d);
            Assert.AreEqual(96, Act1ClubDefinition.TheStaticThrone.DurationBars);
            Assert.AreEqual(Act1PassRule.ThroneLock, Act1ClubDefinition.TheStaticThrone.PassRule);
            Assert.AreEqual(52d, Act1RunDirector.ThroneLockHype, 1e-9d);
            Assert.IsTrue(Act1ClubDefinition.TheStaticThrone.IsBoss);

            Assert.AreEqual(36d, Act1ClubDefinition.GlassCathedral.InitialHype, 1e-9d);
            Assert.AreEqual(64d, Act1ClubDefinition.GlassCathedral.TargetHype, 1e-9d);
            Assert.AreEqual(80, Act1ClubDefinition.GlassCathedral.DurationBars);
            Assert.AreEqual(Act1PassRule.EchoTwice, Act1ClubDefinition.GlassCathedral.PassRule);

            Assert.AreEqual(36d, Act1ClubDefinition.NightExchange.InitialHype, 1e-9d);
            Assert.AreEqual(66d, Act1ClubDefinition.NightExchange.TargetHype, 1e-9d);
            Assert.AreEqual(Act1PassRule.PaidDrop, Act1ClubDefinition.NightExchange.PassRule);

            Assert.AreEqual(36d, Act1ClubDefinition.RoofRelay.InitialHype, 1e-9d);
            Assert.AreEqual(68d, Act1ClubDefinition.RoofRelay.TargetHype, 1e-9d);
            Assert.AreEqual(Act1PassRule.ClearLine, Act1ClubDefinition.RoofRelay.PassRule);

            Assert.AreEqual(40d, Act1ClubDefinition.FinalFestival.InitialHype, 1e-9d);
            Assert.AreEqual(75d, Act1ClubDefinition.FinalFestival.TargetHype, 1e-9d);
            Assert.AreEqual(96, Act1ClubDefinition.FinalFestival.DurationBars);
            Assert.AreEqual(Act1PassRule.Finale, Act1ClubDefinition.FinalFestival.PassRule);
            Assert.AreEqual(58d, Act1RunDirector.FestivalLockHype, 1e-9d);
            Assert.IsTrue(Act1ClubDefinition.FinalFestival.IsBoss);
        }

        [Test]
        public void OrdinaryWin_OpensReward_ThenNextClub_ResetsHype_KeepsReputation()
        {
            int seed = SeedWithFirst(Act1ClubIds.NeonBasement);
            Act1RunDirector director = new Act1RunDirector(seed);
            Assert.AreEqual(Act1ClubIds.NeonBasement, director.Readout.ClubId);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(0, director.Readout.ClubIndex);
            Assert.AreEqual(50d, director.Readout.TargetHype, 1e-9d);
            Assert.AreEqual(128d, director.Readout.RemainingSeconds, 1e-6d);

            PlayHardCuts(director, 128d);
            Act1Readout win = director.Readout;
            Assert.AreEqual(SetOutcome.Win, win.Outcome);
            Assert.AreEqual(RunPhase.Result, win.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonTargetMet, win.ReasonId);
            Assert.GreaterOrEqual(win.Hype, 50d);
            double reputation = director.Reputation;
            Assert.Greater(reputation, 0d);

            director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.IsTrue(director.AdvanceFromReward());

            Act1Readout next = director.Readout;
            Assert.AreEqual(RunPhase.PreClub, next.Phase);
            Assert.AreEqual(1, next.ClubIndex);
            Assert.AreEqual(director.Route[1], next.ClubId);
            Assert.AreEqual(Act1ClubDefinition.Require(next.ClubId).InitialHype, next.Hype, 1e-9d);
            Assert.AreEqual(SetOutcome.InProgress, next.Outcome);
            Assert.AreEqual("", next.ReasonId);
            Assert.AreEqual(reputation, director.Reputation, 1e-9d);
            Assert.AreEqual(1, director.Combo);
            Assert.IsFalse(next.IsBoss);
        }

        [Test]
        public void TimeoutBelowTarget_LosesFinal_ContinueIgnored()
        {
            int seed = SeedWithFirst(Act1ClubIds.NeonBasement);
            Act1RunDirector director = new Act1RunDirector(seed);
            PlaySameGroove(director, 128d);

            Act1Readout readout = director.Readout;
            Assert.AreEqual(SetOutcome.Lose, readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonTimeout, readout.ReasonId);
            Assert.Less(readout.Hype, 50d);

            director.Continue();
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.IsFalse(director.AdvanceFromReward());
        }

        [Test]
        public void Warehouse_StaleWithoutPrep_FailsSpecial()
        {
            int seed = SeedWithFirst(Act1ClubIds.WarehousePulse);
            Act1RunDirector director = new Act1RunDirector(seed);
            Assert.AreEqual(Act1ClubIds.WarehousePulse, director.Readout.ClubId);

            PlayStaleDrops(director, 128d);
            Assert.GreaterOrEqual(director.Readout.Hype, 54d);
            Assert.AreEqual(SetOutcome.Lose, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonWarehouseRule, director.Readout.ReasonId);
        }

        [Test]
        public void Warehouse_StaleWithCleanPrep_Passes()
        {
            int seed = SeedWithFirst(Act1ClubIds.WarehousePulse);
            Act1RunDirector director = new Act1RunDirector(seed);
            EnsurePerforming(director);
            director.NotifyCleanPrepTransition();
            PlayStaleDrops(director, 128d);

            Assert.GreaterOrEqual(director.Readout.Hype, 54d);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonTargetMet, director.Readout.ReasonId);
        }

        [Test]
        public void BassFactory_HardCuts_PassWithCleans()
        {
            int seed = SeedWithFirst(Act1ClubIds.BassFactory);
            Act1RunDirector director = new Act1RunDirector(seed);
            Assert.AreEqual(50d, director.Readout.TargetHype, 1e-9d);

            PlayHardCuts(director, 128d);
            Assert.GreaterOrEqual(director.Readout.Hype, 56d);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
        }

        [Test]
        public void BassFactory_NoCleanTransition_FailsSpecial()
        {
            int seed = SeedWithFirst(Act1ClubIds.BassFactory);
            Act1RunDirector director = new Act1RunDirector(seed);
            PlaySameGroove(director, 128d);

            // Same groove stays under target → timeout beats special; force special path with drops only.
            director.Reset(seed);
            EnsurePerforming(director);
            Tick(director, FullA(0d));
            Tick(director, QuietBass(1d));
            Tick(director, QuietBass(3d));
            Tick(director, FullA(4d));
            for (double time = 16d; time <= 112d; time += 16d)
                Tick(director, FullA(time));
            Tick(director, FullA(128d));

            if (director.Readout.Hype + 1e-9d >= 56d)
            {
                Assert.AreEqual(SetOutcome.Lose, director.Readout.Outcome);
                Assert.AreEqual(Act1RunDirector.ReasonBassRule, director.Readout.ReasonId);
            }
            else
            {
                Assert.AreEqual(Act1RunDirector.ReasonTimeout, director.Readout.ReasonId);
            }
        }

        [Test]
        public void Boss_Clear_RequiresLockAndSurge()
        {
            int seed = 0;
            Act1RunDirector director = new Act1RunDirector(seed);
            ClearOrdinary(director);
            Assert.AreEqual(Act1ClubIds.TheSignal, director.Readout.ClubId);
            Assert.IsTrue(director.Readout.IsBoss);
            Assert.AreEqual(3, director.Readout.ClubIndex);
            Assert.AreEqual(65d, director.Readout.TargetHype, 1e-9d);
            Assert.AreEqual(45d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(144d, director.Readout.RemainingSeconds, 1e-6d);

            EnsurePerforming(director);
            director.NotifyCueReadyHonor();
            PlayDropAndHardCuts(director, 144d);

            Act1Readout clear = director.Readout;
            Assert.AreEqual(SetOutcome.Win, clear.Outcome, clear.ReasonId + " hype=" + clear.Hype);
            Assert.AreEqual(RunPhase.Result, clear.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonSignalClear, clear.ReasonId);
            Assert.GreaterOrEqual(clear.Hype, 65d);
            Assert.IsTrue(clear.MetaHasCarrier);
            Assert.IsTrue(director.Crate.Contains(TrackRewardRoll.BossCarrierId));
            Assert.AreEqual("", clear.OfferedTrack0);
            Assert.IsFalse(director.AdvanceFromReward());

            director.Continue();
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(4, director.Readout.ClubIndex);
            Assert.AreEqual(director.Route[4], director.Readout.ClubId);
            Assert.IsFalse(director.Readout.IsBoss);
            Assert.AreEqual("", director.Readout.OfferedTrack0);
        }

        [Test]
        public void OrdinaryReward_ChooseTrack_AddsToCrate()
        {
            int seed = SeedWithFirst(Act1ClubIds.NeonBasement);
            Act1RunDirector director = new Act1RunDirector(seed);
            Assert.AreEqual(4, director.Readout.CrateCount);

            PlayHardCuts(director, 128d);
            director.Continue();
            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            Assert.IsFalse(string.IsNullOrEmpty(director.Readout.OfferedTrack0));
            Assert.IsFalse(string.IsNullOrEmpty(director.Readout.OfferedTrack1));

            string pick = director.Readout.OfferedTrack0;
            Assert.IsTrue(director.ChooseTrack(pick));
            Assert.AreEqual(pick, director.Readout.ChosenTrackId);
            Assert.AreEqual(5, director.Crate.Count);
            Assert.IsTrue(director.Crate.Contains(pick));
            Assert.IsTrue(director.AdvanceFromReward());
        }

        [Test]
        public void OrdinaryReward_FirstClub_OffersSpecialInThirdSlot()
        {
            int seed = SeedWithFirst(Act1ClubIds.NeonBasement);
            Act1RunDirector director = new Act1RunDirector(seed);
            PlayHardCuts(director, 128d);
            director.Continue();

            Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
            var specials = new[]
            {
                TrackAura.AmberBloomId,
                TrackAura.VoidStaticId,
                TrackAura.GlassRainId,
                TrackAura.RedlineSirenId
            };
            CollectionAssert.AreEquivalent(
                new[] { "track_voltage_alley", "track_mutabor_pulse" },
                new[] { director.Readout.OfferedTrack0, director.Readout.OfferedTrack1 });
            CollectionAssert.Contains(specials, director.Readout.OfferedTrack2);
        }

        [Test]
        public void Boss_MissingSurgeHonor_Loses()
        {
            int seed = 0;
            Act1RunDirector director = new Act1RunDirector(seed);
            ClearOrdinary(director);
            PlayDropAndHardCuts(director, 144d);

            Assert.GreaterOrEqual(director.Readout.Hype, 65d);
            Assert.AreEqual(SetOutcome.Lose, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonSignalSurge, director.Readout.ReasonId);
        }

        [Test]
        public void Boss_MidTimerLockFail_Loses()
        {
            int seed = 0;
            Act1RunDirector director = new Act1RunDirector(seed);
            ClearOrdinary(director);
            EnsurePerforming(director);
            director.NotifyCueReadyHonor();

            // Stay quiet through mid-timer (72s) so lock fails; later recover is irrelevant.
            Tick(director, FullA(0d));
            for (double time = 16d; time <= 144d; time += 16d)
                Tick(director, FullA(time));

            Assert.AreEqual(SetOutcome.Lose, director.Readout.Outcome);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.IsTrue(
                director.Readout.ReasonId == Act1RunDirector.ReasonSignalLock
                || director.Readout.ReasonId == Act1RunDirector.ReasonTimeout);
        }

        [Test]
        public void Reset_ReturnsToRouteStart()
        {
            int seed = 11;
            Act1RunDirector director = new Act1RunDirector(seed);
            string first = director.Route[0];
            PlaySameGroove(director, 128d);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);

            director.Reset(seed);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual(0, director.Readout.ClubIndex);
            Assert.AreEqual(first, director.Readout.ClubId);
            Assert.AreEqual(Act1RunDirector.OpeningHype(director.CurrentClub, 0), director.Readout.Hype, 1e-9d);
            Assert.AreEqual(0d, director.Reputation, 1e-9d);
            Assert.AreEqual(SetOutcome.InProgress, director.Readout.Outcome);
            CollectionAssert.AreEqual(Act1Route.Build(seed), director.Route);
        }

        [Test]
        public void Pause_DoesNotAdvanceTimerOrHype()
        {
            int seed = SeedWithFirst(Act1ClubIds.NeonBasement);
            Act1RunDirector director = new Act1RunDirector(seed);
            director.BeginClub();
            Tick(director, FullA(0d));
            Tick(director, FullA(10d));
            Assert.AreEqual(Act1RunDirector.OpeningHype(Act1ClubDefinition.NeonBasement, 0), director.Readout.Hype, 1e-9d);
            Assert.AreEqual(118d, director.Readout.RemainingSeconds, 1e-6d);

            Tick(director, Paused(40d));
            Assert.AreEqual(Act1RunDirector.OpeningHype(Act1ClubDefinition.NeonBasement, 0), director.Readout.Hype, 1e-9d);
            Assert.AreEqual(118d, director.Readout.RemainingSeconds, 1e-6d);
        }

        [Test]
        public void ApplyDeckReactions_CueReadyHonor_AddsHypeOnce_AndCountsForBoss()
        {
            int seed = 0;
            Act1RunDirector director = new Act1RunDirector(seed);
            ClearOrdinary(director);
            Assert.AreEqual(Act1ClubIds.TheSignal, director.Readout.ClubId);
            EnsurePerforming(director);

            double hypeBefore = director.Readout.Hype;
            double reputationBefore = director.Reputation;
            MixSnapshot mix = FullA(0d);
            DeckReactionCues honor = new DeckReactionCues(
                0,
                11,
                isCueReadyB: true,
                requestActive: true,
                requestSatisfied: true,
                deadAir: false,
                hotSwapFumble: false,
                cleanPrepEligible: false,
                transitionEventId: "");

            Assert.AreEqual(1, director.ApplyDeckReactions(mix, honor));
            Assert.AreEqual(hypeBefore + 3d, director.Readout.Hype, 1e-9d);
            Assert.AreEqual(reputationBefore + 40d, director.Reputation, 1e-9d);

            Assert.AreEqual(0, director.ApplyDeckReactions(mix, honor));
            Assert.AreEqual(hypeBefore + 3d, director.Readout.Hype, 1e-9d);

            PlayDropAndHardCuts(director, 144d);
            Act1Readout clear = director.Readout;
            Assert.AreEqual(SetOutcome.Win, clear.Outcome, clear.ReasonId + " hype=" + clear.Hype);
            Assert.AreEqual(Act1RunDirector.ReasonSignalClear, clear.ReasonId);
            Assert.AreEqual(RunPhase.Result, clear.Phase);
        }

        [Test]
        public void ApplyDeckReactions_CleanPrep_CountsForWarehouse()
        {
            int seed = SeedWithFirst(Act1ClubIds.WarehousePulse);
            Act1RunDirector director = new Act1RunDirector(seed);
            EnsurePerforming(director);

            MixSnapshot mix = FullA(0d);
            DeckReactionCues prep = new DeckReactionCues(
                0,
                0,
                isCueReadyB: true,
                requestActive: false,
                requestSatisfied: false,
                deadAir: false,
                hotSwapFumble: false,
                cleanPrepEligible: true,
                transitionEventId: "transition-0-0-1");

            double hypeBefore = director.Readout.Hype;
            Assert.AreEqual(1, director.ApplyDeckReactions(mix, prep));
            Assert.AreEqual(hypeBefore + 2d, director.Readout.Hype, 1e-9d);

            PlayStaleDrops(director, 128d);
            Assert.GreaterOrEqual(director.Readout.Hype, 54d);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome);
            Assert.AreEqual(Act1RunDirector.ReasonTargetMet, director.Readout.ReasonId);
        }

        [Test]
        public void ClubRequestDirector_OpensPrepB_EverySecondPhrase()
        {
            var requests = new ClubRequestDirector(seed: 0);

            requests.Tick(performing: true, phrase: 0);
            Assert.IsTrue(requests.IsActive);
            Assert.AreEqual(ClubRequestDirector.RequestPrepB, requests.RequestKind);
            DeckReactionCues open = requests.BuildCues(0, isCueReadyB: true);
            Assert.IsTrue(open.RequestActive);
            Assert.IsTrue(open.RequestSatisfied);
            Assert.AreEqual(requests.RequestId, open.RequestId);

            requests.Tick(performing: true, phrase: 1);
            Assert.IsFalse(requests.IsActive);
            Assert.IsFalse(requests.BuildCues(1, isCueReadyB: true).RequestActive);

            requests.Tick(performing: true, phrase: 2);
            Assert.IsTrue(requests.IsActive);
            Assert.AreEqual(2, requests.ActivePhrase);

            var odd = new ClubRequestDirector(seed: 1);
            odd.Tick(performing: true, phrase: 0);
            Assert.IsFalse(odd.IsActive);
            odd.Tick(performing: true, phrase: 1);
            Assert.IsTrue(odd.IsActive);
        }

        [Test]
        public void SecondClub_AboveTarget_NeedsTwoCleansOrWarehouseDialect()
        {
            int neonSeed = SeedWithClubAt(1, Act1ClubIds.NeonBasement);
            Act1RunDirector unanswered = ReachClub(neonSeed, 1);
            Assert.AreEqual(Act1ClubIds.NeonBasement, unanswered.Readout.ClubId);
            Assert.AreEqual(Act1ClubDefinition.NeonBasement.TargetHype, unanswered.Readout.TargetHype, 1e-9d);
            PumpHonors(unanswered, 16);
            PlaySameGroove(unanswered, 128d);
            Act1Readout silent = unanswered.Readout;
            Assert.GreaterOrEqual(silent.Hype, Act1ClubDefinition.NeonBasement.TargetHype);
            Assert.AreEqual(SetOutcome.Lose, silent.Outcome, silent.ReasonId + " hype=" + silent.Hype);
            Assert.AreEqual(RunPhase.FinalResult, silent.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonRoomUnanswered, silent.ReasonId);

            int warehouseSeed = SeedWithClubAt(1, Act1ClubIds.WarehousePulse);
            Act1RunDirector warehouse = ReachClub(warehouseSeed, 1);
            Assert.AreEqual(Act1ClubDefinition.WarehousePulse.TargetHype, warehouse.Readout.TargetHype, 1e-9d);
            EnsurePerforming(warehouse);
            warehouse.NotifyCleanPrepTransition();
            PumpHonors(warehouse, 16);
            PlayOneCleanOnto(warehouse, "track_mutabor_pulse", 128d);
            Act1Readout dialect = warehouse.Readout;
            Assert.GreaterOrEqual(dialect.Hype, Act1ClubDefinition.WarehousePulse.TargetHype);
            Assert.AreEqual(SetOutcome.Win, dialect.Outcome, dialect.ReasonId + " hype=" + dialect.Hype);
            Assert.AreEqual(RunPhase.Result, dialect.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonTargetMet, dialect.ReasonId);

            int bassSeed = SeedWithClubAt(1, Act1ClubIds.BassFactory);
            Act1RunDirector bass = ReachClub(bassSeed, 1);
            Assert.AreEqual(Act1ClubDefinition.BassFactory.TargetHype, bass.Readout.TargetHype, 1e-9d);
            PumpHonors(bass, 16);
            PlayOneCleanOnto(bass, "track_mutabor_pulse", 128d);
            Act1Readout missed = bass.Readout;
            Assert.GreaterOrEqual(missed.Hype, Act1ClubDefinition.BassFactory.TargetHype);
            Assert.AreEqual(SetOutcome.Lose, missed.Outcome, missed.ReasonId + " hype=" + missed.Hype);
            Assert.AreEqual(RunPhase.FinalResult, missed.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonRoomUnanswered, missed.ReasonId);
        }

        [Test]
        public void ThreeActNight_SumsTo1824Seconds_SignalContinues_FestivalClears()
        {
            string[] defs =
            {
                Act1ClubIds.NeonBasement,
                Act1ClubIds.WarehousePulse,
                Act1ClubIds.BassFactory,
                Act1ClubIds.TheSignal,
                CampaignClubIds.DreamLounge,
                CampaignClubIds.RaveDistrict,
                CampaignClubIds.ChromePier,
                CampaignClubIds.TheStaticThrone,
                CampaignClubIds.GlassCathedral,
                CampaignClubIds.NightExchange,
                CampaignClubIds.RoofRelay,
                CampaignClubIds.FinalFestival
            };
            int bars = 0;
            for (int i = 0; i < defs.Length; i++)
                bars += Act1ClubDefinition.Require(defs[i]).DurationBars;
            Assert.AreEqual(1824, bars * 2);

            string[] route = Act1Route.Build(0);
            Assert.AreEqual(12, route.Length);
            Assert.AreEqual(Act1ClubIds.TheSignal, route[3]);
            Assert.AreEqual(CampaignClubIds.TheStaticThrone, route[7]);
            Assert.AreEqual(CampaignClubIds.FinalFestival, route[11]);
            Assert.IsTrue(IsOrdinary(route[0]));

            var director = new Act1RunDirector(0);
            for (int i = 0; i < 3; i++)
                WinOrdinary(director);

            Assert.AreEqual(Act1ClubIds.TheSignal, director.Readout.ClubId);
            WinCurrent(director);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome, director.Readout.ReasonId + " hype=" + director.Readout.Hype);
            Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonSignalClear, director.Readout.ReasonId);
            Assert.IsTrue(director.Crate.Contains(TrackRewardRoll.BossCarrierId));
            Assert.AreEqual("", director.Readout.OfferedTrack0);
            director.Continue();
            Assert.AreEqual(4, director.Readout.ClubIndex);
            Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
            Assert.AreEqual("", director.Readout.OfferedTrack0);

            while (director.Readout.ClubIndex < 11)
            {
                int index = director.Readout.ClubIndex;
                string id = director.Readout.ClubId;
                WinCurrent(director);
                Assert.AreEqual(
                    SetOutcome.Win,
                    director.Readout.Outcome,
                    id + " " + director.Readout.ReasonId + " hype=" + director.Readout.Hype);
                if (id == CampaignClubIds.TheStaticThrone)
                {
                    Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
                    Assert.AreEqual(Act1RunDirector.ReasonThroneClear, director.Readout.ReasonId);
                    Assert.IsTrue(director.Crate.Contains(TrackAura.StaticCrownId));
                    Assert.IsTrue(director.Meta.Contains(TrackAura.StaticCrownId));
                    director.Continue();
                    Assert.AreEqual(index + 1, director.Readout.ClubIndex);
                    Assert.AreEqual(RunPhase.PreClub, director.Readout.Phase);
                    Assert.AreEqual("", director.Readout.OfferedTrack0);
                }
                else
                {
                    Assert.AreEqual(RunPhase.Result, director.Readout.Phase);
                    Assert.AreEqual(Act1RunDirector.ReasonTargetMet, director.Readout.ReasonId);
                    director.Continue();
                    Assert.AreEqual(RunPhase.Reward, director.Readout.Phase);
                    if (index == 4)
                    {
                        CollectionAssert.AreEquivalent(
                            new[] { "track_lounge_key", "track_district_hat" },
                            new[] { director.Readout.OfferedTrack0, director.Readout.OfferedTrack1 });
                        CollectionAssert.Contains(
                            new[] { TrackAura.VelvetHushId, TrackAura.HardlineYellowId, TrackAura.SaltChromeId },
                            director.Readout.OfferedTrack2);
                    }

                    if (index == 8)
                    {
                        CollectionAssert.AreEquivalent(
                            new[] { "track_nave_pad", "track_market_click" },
                            new[] { director.Readout.OfferedTrack0, director.Readout.OfferedTrack1 });
                        CollectionAssert.Contains(
                            new[] { TrackAura.PrismChoirId, TrackAura.NightLedgerId, TrackAura.SkywireId },
                            director.Readout.OfferedTrack2);
                    }

                    Assert.IsTrue(director.AdvanceFromReward());
                    Assert.AreEqual(index + 1, director.Readout.ClubIndex);
                }
            }

            Assert.AreEqual(CampaignClubIds.FinalFestival, director.Readout.ClubId);
            WinCurrent(director);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome, director.Readout.ReasonId + " hype=" + director.Readout.Hype);
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.AreEqual(Act1RunDirector.ReasonCampaignClear, director.Readout.ReasonId);
            Assert.IsTrue(director.Crate.Contains(TrackAura.FestivalAnthemId));
            Assert.IsTrue(director.Meta.Contains(TrackAura.FestivalAnthemId));
            director.Continue();
            Assert.AreEqual(RunPhase.FinalResult, director.Readout.Phase);
            Assert.IsFalse(director.AdvanceFromReward());
        }

        private static void WinOrdinary(Act1RunDirector director)
        {
            WinCurrent(director);
            Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome, director.Readout.ClubId + " " + director.Readout.ReasonId);
            director.Continue();
            Assert.IsTrue(director.AdvanceFromReward());
        }

        private static void WinCurrent(Act1RunDirector director)
        {
            Act1ClubDefinition club = director.CurrentClub;
            EnsurePerforming(director);
            if (club.PassRule == Act1PassRule.SignalPhases
                || club.PassRule == Act1PassRule.ThroneLock
                || club.PassRule == Act1PassRule.ClearLine)
                director.NotifyCueReadyHonor();
            PlayDropAndHardCuts(director, club.DurationBars * 2d);
        }

        private static void ClearOrdinary(Act1RunDirector director)
        {
            for (int i = 0; i < Act1Route.OrdinaryPick; i++)
            {
                Assert.IsFalse(director.Readout.IsBoss);
                PlayHardCuts(director, 128d);
                Assert.AreEqual(SetOutcome.Win, director.Readout.Outcome, director.Readout.ClubId + " " + director.Readout.ReasonId);
                director.Continue();
                Assert.IsTrue(director.AdvanceFromReward());
            }
        }

        private static Act1RunDirector ReachClub(int seed, int index)
        {
            var director = new Act1RunDirector(seed);
            for (int i = 0; i < index; i++)
            {
                PlayHardCuts(director, 128d);
                Assert.AreEqual(
                    SetOutcome.Win,
                    director.Readout.Outcome,
                    director.Readout.ClubId + " " + director.Readout.ReasonId);
                director.Continue();
                Assert.IsTrue(director.AdvanceFromReward());
            }

            return director;
        }

        private static int SeedWithClubAt(int index, string clubId)
        {
            for (int seed = 0; seed < 256; seed++)
            {
                if (Act1Route.Build(seed)[index] == clubId)
                    return seed;
            }

            Assert.Fail("No seed has " + clubId + " at " + index);
            return 0;
        }

        private static int SeedWithFirst(string clubId)
        {
            for (int seed = 0; seed < 256; seed++)
            {
                if (Act1Route.Build(seed)[0] == clubId)
                    return seed;
            }

            Assert.Fail("No seed starts with " + clubId);
            return 0;
        }

        private static bool IsOrdinary(string clubId)
        {
            return clubId == Act1ClubIds.NeonBasement
                || clubId == Act1ClubIds.WarehousePulse
                || clubId == Act1ClubIds.BassFactory;
        }

        private static void PlayHardCuts(Act1RunDirector director, double endTime)
        {
            EnsurePerforming(director);
            if (director.Readout.ClubId == Act1ClubIds.WarehousePulse)
                director.NotifyCleanPrepTransition();

            Tick(director, FullA(0d));
            Tick(director, FullA(0.5d));
            bool onA = true;
            for (double boundary = 16d; boundary < endTime - 1e-9d; boundary += 16d)
            {
                onA = !onA;
                Tick(director, onA ? FullA(boundary) : FullB(boundary));
                Tick(director, onA ? FullA(boundary + 0.5d) : FullB(boundary + 0.5d));
            }

            Tick(director, onA ? FullA(endTime) : FullB(endTime));
        }

        private static void PlayDropAndHardCuts(Act1RunDirector director, double endTime)
        {
            EnsurePerforming(director);
            if (director.Readout.ClubId == Act1ClubIds.WarehousePulse)
                director.NotifyCleanPrepTransition();

            Tick(director, FullA(0d));
            Tick(director, QuietBass(1d));
            Tick(director, QuietBass(3d));
            Tick(director, FullA(4d));
            bool onA = true;
            for (double boundary = 16d; boundary < endTime - 1e-9d; boundary += 16d)
            {
                onA = !onA;
                Tick(director, onA ? FullA(boundary) : FullB(boundary));
                Tick(director, onA ? FullA(boundary + 0.5d) : FullB(boundary + 0.5d));
            }

            Tick(director, onA ? FullA(endTime) : FullB(endTime));
        }

        private static void PlayStaleDrops(Act1RunDirector director, double endTime)
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
            for (double time = 64d; time <= endTime; time += 16d)
                Tick(director, FullA(time));
        }

        private static void PumpHonors(Act1RunDirector director, int count)
        {
            EnsurePerforming(director);
            for (int i = 0; i < count; i++)
            {
                DeckReactionCues honor = new DeckReactionCues(
                    0,
                    i,
                    isCueReadyB: true,
                    requestActive: true,
                    requestSatisfied: true,
                    deadAir: false,
                    hotSwapFumble: false,
                    cleanPrepEligible: false,
                    transitionEventId: "");
                Assert.AreEqual(1, director.ApplyDeckReactions(FullA(0d), honor));
            }
        }

        private static void PlayOneCleanOnto(Act1RunDirector director, string incomingTrack, double endTime)
        {
            EnsurePerforming(director);
            const string outgoing = "track_neon_pulse";
            Tick(director, Lead(0d, true, outgoing, incomingTrack));
            Tick(director, Lead(0.5d, true, outgoing, incomingTrack));
            Tick(director, Lead(16d, false, outgoing, incomingTrack));
            Tick(director, Lead(16.5d, false, outgoing, incomingTrack));
            for (double time = 32d; time <= endTime; time += 16d)
                Tick(director, Lead(time, false, outgoing, incomingTrack));
        }

        private static void PlaySameGroove(Act1RunDirector director, double endTime)
        {
            EnsurePerforming(director);
            Tick(director, FullA(0d));
            for (double time = 16d; time <= endTime; time += 16d)
                Tick(director, FullA(time));
        }

        private static void EnsurePerforming(Act1RunDirector director)
        {
            if (director.Readout.Phase == RunPhase.PreClub)
                director.BeginClub();
        }

        private static void Tick(Act1RunDirector director, MixSnapshot snapshot)
        {
            director.Tick(snapshot);
        }

        private static MixSnapshot FullA(double time)
        {
            return At(time, false, true, false, 1d, 0d, 1d, 0d);
        }

        private static MixSnapshot FullB(double time)
        {
            return At(time, false, false, true, 0d, 1d, 0d, 1d);
        }

        private static MixSnapshot QuietBass(double time)
        {
            return At(time, false, true, false, 1d, 0d, 0d, 0d);
        }

        private static MixSnapshot Paused(double time)
        {
            return At(time, true, true, true, 0.8d, 0.8d, 0.8d, 0.8d);
        }

        private static MixSnapshot Lead(double time, bool deckA, string trackA, string trackB)
        {
            if (deckA)
                return AtTracks(time, false, true, false, 1d, 0d, 1d, 0d, trackA, trackB);
            return AtTracks(time, false, false, true, 0d, 1d, 0d, 1d, trackA, trackB);
        }

        private static MixSnapshot At(
            double time,
            bool paused,
            bool deckA,
            bool deckB,
            double gainA,
            double gainB,
            double lowA,
            double lowB)
        {
            return AtTracks(
                time,
                paused,
                deckA,
                deckB,
                gainA,
                gainB,
                lowA,
                lowB,
                "track_neon_pulse",
                "track_afterhours");
        }

        private static MixSnapshot AtTracks(
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
            return new MixSnapshot(
                time,
                paused,
                deckA,
                deckB,
                gainA,
                gainB,
                lowA,
                lowB,
                trackA,
                trackB,
                true);
        }
    }
}
