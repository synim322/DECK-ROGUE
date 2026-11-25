using System.Collections.Generic;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Run;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class TrackRewardTests
    {
        [Test]
        public void FromCommon_ReturnsBothCommonPoolIds()
        {
            IReadOnlyList<string> offer = TrackRewardRoll.FromCommon(7);

            Assert.AreEqual(2, offer.Count);
            CollectionAssert.AreEquivalent(
                new[] { "track_voltage_alley", "track_mutabor_pulse" },
                offer);
        }

        [Test]
        public void FromCommon_IsDeterministicForTheSameSeed()
        {
            IReadOnlyList<string> a = TrackRewardRoll.FromCommon(42);
            IReadOnlyList<string> b = TrackRewardRoll.FromCommon(42);

            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void FromMid_WithOneCommonOwned_FillsToThree()
        {
            IReadOnlyList<string> offer = TrackRewardRoll.FromMid(
                3,
                new[] { "track_voltage_alley" });

            Assert.AreEqual(3, offer.Count);
            CollectionAssert.IsSubsetOf(offer, new[] {
                "track_foundry_drop", "track_afterglow_key", "track_mirror_shunt", "track_mutabor_pulse"
            });
            CollectionAssert.DoesNotContain(offer, "track_voltage_alley");
        }

        [Test]
        public void FromMid_WhenBothCommonOwned_OffersUpToMidOnly()
        {
            IReadOnlyList<string> offer = TrackRewardRoll.FromMid(
                11,
                new[] { "track_voltage_alley", "track_mutabor_pulse" });

            Assert.AreEqual(3, offer.Count);
            CollectionAssert.AreEquivalent(
                new[] { "track_foundry_drop", "track_afterglow_key", "track_mirror_shunt" },
                offer);
        }

        [Test]
        public void FromMid_SkipsAlreadyOwnedMid()
        {
            IReadOnlyList<string> offer = TrackRewardRoll.FromMid(
                5,
                new[] { "track_foundry_drop", "track_voltage_alley" });

            Assert.LessOrEqual(offer.Count, 3);
            CollectionAssert.DoesNotContain(offer, "track_foundry_drop");
            CollectionAssert.DoesNotContain(offer, "track_voltage_alley");
            CollectionAssert.Contains(offer, "track_afterglow_key");
            CollectionAssert.Contains(offer, "track_mutabor_pulse");
        }

        [Test]
        public void BossCarrier_IsSignalCarrier()
        {
            Assert.AreEqual("track_signal_carrier", TrackRewardRoll.BossCarrier());
            Assert.AreEqual("track_signal_carrier", TrackRewardRoll.BossCarrierId);
        }

        [Test]
        public void FromSpecial_SkipsOwnedAndIsDeterministic()
        {
            var owned = new[] { TrackAura.AmberBloomId };
            string a = TrackRewardRoll.FromSpecial(17, owned);
            string b = TrackRewardRoll.FromSpecial(17, owned);

            Assert.AreEqual(a, b);
            Assert.AreNotEqual(TrackAura.AmberBloomId, a);
            CollectionAssert.Contains(
                new[]
                {
                    TrackAura.VoidStaticId,
                    TrackAura.GlassRainId,
                    TrackAura.RedlineSirenId
                },
                a);
        }

        [Test]
        public void FromSpecial_EmptyWhenAllFourOwned()
        {
            string id = TrackRewardRoll.FromSpecial(
                9,
                new[]
                {
                    TrackAura.AmberBloomId,
                    TrackAura.VoidStaticId,
                    TrackAura.GlassRainId,
                    TrackAura.RedlineSirenId
                });

            Assert.AreEqual("", id);
        }

        [Test]
        public void FromSpecial_NullOwnedStillReturnsOneOfTheFour()
        {
            string id = TrackRewardRoll.FromSpecial(4, null);

            CollectionAssert.Contains(
                new[]
                {
                    TrackAura.AmberBloomId,
                    TrackAura.VoidStaticId,
                    TrackAura.GlassRainId,
                    TrackAura.RedlineSirenId
                },
                id);
        }

        [Test]
        public void MetaUnlocks_UnlockAndContains()
        {
            var meta = new MetaUnlocks();

            Assert.IsFalse(meta.Contains(TrackRewardRoll.BossCarrierId));

            meta.Unlock(TrackRewardRoll.BossCarrier());

            Assert.IsTrue(meta.Contains("track_signal_carrier"));
            CollectionAssert.Contains(meta.UnlockedTrackIds, "track_signal_carrier");
        }

        [Test]
        public void MetaUnlocks_UnlockIsIdempotent()
        {
            var meta = new MetaUnlocks();
            meta.Unlock("track_signal_carrier");
            meta.Unlock("track_signal_carrier");

            Assert.AreEqual(1, meta.UnlockedTrackIds.Count);
        }

        [Test]
        public void MetaUnlocks_Hydrate_ReplacesSet()
        {
            var meta = new MetaUnlocks();
            meta.Unlock("track_voltage_alley");
            meta.Hydrate(new[] { "track_signal_carrier", "track_signal_carrier" });

            Assert.IsFalse(meta.Contains("track_voltage_alley"));
            Assert.IsTrue(meta.Contains("track_signal_carrier"));
            Assert.AreEqual(1, meta.UnlockedTrackIds.Count);

            meta.Hydrate(null);
            Assert.AreEqual(0, meta.UnlockedTrackIds.Count);
        }

        [Test]
        public void Act1Catalog_RewardAndBossFlagsMatchPools()
        {
            TrackLibrary catalog = TrackLibrary.CreateAct1Catalog();

            Assert.AreEqual(TrackFlags.Reward, catalog.Get("track_voltage_alley").Flags);
            Assert.AreEqual(TrackFlags.Reward, catalog.Get("track_mutabor_pulse").Flags);
            Assert.AreEqual(TrackFlags.Reward, catalog.Get("track_foundry_drop").Flags);
            Assert.AreEqual(TrackFlags.Reward, catalog.Get("track_afterglow_key").Flags);
            Assert.AreEqual(
                TrackFlags.Reward | TrackFlags.BossDrop | TrackFlags.MetaUnlock,
                catalog.Get("track_signal_carrier").Flags);
            Assert.AreEqual(TrackFlags.BossExclusive, catalog.Get("track_signal_breach").Flags);
        }

        [Test]
        public void FromCommon_Act1AndAct2_ReturnThatActsPair()
        {
            IReadOnlyList<string> act1 = TrackRewardRoll.FromCommon(7, 1);
            IReadOnlyList<string> act1Again = TrackRewardRoll.FromCommon(7, 1);
            Assert.AreEqual(2, act1.Count);
            CollectionAssert.AreEqual(act1, act1Again);
            CollectionAssert.AreEquivalent(
                new[] { "track_lounge_key", "track_district_hat" },
                act1);

            IReadOnlyList<string> act2 = TrackRewardRoll.FromCommon(7, 2);
            CollectionAssert.AreEquivalent(
                new[] { "track_nave_pad", "track_market_click" },
                act2);
            CollectionAssert.AreEquivalent(
                new[] { "track_voltage_alley", "track_mutabor_pulse" },
                TrackRewardRoll.FromCommon(7, 0));
        }

        [Test]
        public void FromMid_Act1_SkipsOwnedCommonAndStaysInAct()
        {
            IReadOnlyList<string> offer = TrackRewardRoll.FromMid(
                3,
                new[] { "track_lounge_key" },
                1);

            Assert.AreEqual(3, offer.Count);
            CollectionAssert.IsSubsetOf(
                offer,
                new[]
                {
                    "track_siren_sub",
                    "track_pier_roll",
                    "track_chrome_wake",
                    "track_district_hat"
                });
            CollectionAssert.DoesNotContain(offer, "track_lounge_key");
            CollectionAssert.DoesNotContain(offer, "track_voltage_alley");
            CollectionAssert.DoesNotContain(offer, "track_foundry_drop");
        }

        [Test]
        public void FromMid_Act2_WhenBothCommonOwned_OffersMidOnly()
        {
            IReadOnlyList<string> offer = TrackRewardRoll.FromMid(
                11,
                new[] { "track_nave_pad", "track_market_click" },
                2);

            Assert.AreEqual(3, offer.Count);
            CollectionAssert.AreEquivalent(
                new[] { "track_roof_hum", "track_exchange_acid", "track_relay_dub" },
                offer);
        }

        [Test]
        public void FromSpecial_Act1_SkipsOwned_EmptyWhenAllOwned()
        {
            string id = TrackRewardRoll.FromSpecial(17, new[] { TrackAura.VelvetHushId }, 1);
            string again = TrackRewardRoll.FromSpecial(17, new[] { TrackAura.VelvetHushId }, 1);
            Assert.AreEqual(id, again);
            CollectionAssert.Contains(
                new[] { TrackAura.HardlineYellowId, TrackAura.SaltChromeId },
                id);

            Assert.AreEqual(
                "",
                TrackRewardRoll.FromSpecial(
                    9,
                    new[] { TrackAura.VelvetHushId, TrackAura.HardlineYellowId, TrackAura.SaltChromeId },
                    1));
        }

        [Test]
        public void FromSpecial_Act2_ReturnsOneOfThatActsThree()
        {
            string id = TrackRewardRoll.FromSpecial(4, null, 2);
            CollectionAssert.Contains(
                new[] { TrackAura.PrismChoirId, TrackAura.NightLedgerId, TrackAura.SkywireId },
                id);
            CollectionAssert.DoesNotContain(
                new[] { TrackAura.AmberBloomId, TrackAura.VelvetHushId, TrackAura.StaticCrownId },
                id);
        }

        [Test]
        public void BossDropId_MapsSignalThroneAndFestival()
        {
            Assert.AreEqual(TrackRewardRoll.BossCarrierId, TrackRewardRoll.BossDropId(Act1ClubIds.TheSignal));
            Assert.AreEqual(TrackAura.StaticCrownId, TrackRewardRoll.BossDropId(CampaignClubIds.TheStaticThrone));
            Assert.AreEqual(TrackAura.FestivalAnthemId, TrackRewardRoll.BossDropId(CampaignClubIds.FinalFestival));
            Assert.AreEqual("", TrackRewardRoll.BossDropId(Act1ClubIds.NeonBasement));
            Assert.AreEqual("track_signal_carrier", TrackRewardRoll.BossCarrier());
        }
    }
}
