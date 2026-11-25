using DeckRogue.Core.Run;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class CampaignStoryTests
    {
        [Test]
        public void Catalog_ThreeActs_EachThreeOrdinaryPlusBoss()
        {
            Assert.AreEqual(3, CampaignCatalog.ActCount);
            for (int i = 0; i < 3; i++)
            {
                CampaignActDefinition act = CampaignCatalog.GetAct(i);
                Assert.AreEqual(3, act.OrdinaryClubIds.Length);
                Assert.IsFalse(string.IsNullOrEmpty(act.BossId));
                Assert.AreEqual(4, act.ClubCount);
                Assert.IsFalse(string.IsNullOrEmpty(act.Title));
            }

            Assert.AreEqual(Act1ClubIds.TheSignal, CampaignCatalog.Act1.BossId);
            Assert.AreEqual(CampaignClubIds.TheStaticThrone, CampaignCatalog.Act2.BossId);
            Assert.AreEqual(CampaignClubIds.FinalFestival, CampaignCatalog.Act3.BossId);
        }

        [Test]
        public void Cast_HasPromoterRivalAndBossVoices()
        {
            CastMember[] crew = CampaignCast.All;
            Assert.GreaterOrEqual(crew.Length, 8);
            Assert.AreEqual("Mira Vance", CampaignCast.Mira.DisplayName);
            Assert.AreEqual("Kid Voltage", CampaignCast.KidVoltage.DisplayName);
            Assert.AreEqual("The Antenna", CampaignCast.Antenna.DisplayName);
            Assert.AreEqual("Static Crown", CampaignCast.StaticCrown.DisplayName);
            Assert.AreEqual("Festival Choir", CampaignCast.FestivalChoir.DisplayName);
        }

        [Test]
        public void Intro_HasMultipleReadablePages()
        {
            Assert.GreaterOrEqual(CampaignStory.IntroPages.Length, 3);
            for (int i = 0; i < CampaignStory.IntroPages.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(CampaignStory.IntroPages[i].Body));
                Assert.IsFalse(string.IsNullOrEmpty(CampaignStory.IntroPages[i].SpeakerBadge));
            }
        }

        [Test]
        public void Act1Clubs_HaveStoryBeats()
        {
            string[] ids =
            {
                Act1ClubIds.NeonBasement,
                Act1ClubIds.WarehousePulse,
                Act1ClubIds.BassFactory,
                Act1ClubIds.TheSignal
            };

            for (int i = 0; i < ids.Length; i++)
            {
                bool boss = ids[i] == Act1ClubIds.TheSignal;
                StoryBeat beat = CampaignStory.ForClubEntry(ids[i], i, boss);
                Assert.IsFalse(beat.IsEmpty, ids[i]);
                Assert.IsTrue(beat.Body.Length > 20, ids[i]);
                StringAssert.Contains(CampaignCatalog.DisplayName(ids[i]), beat.Title);
            }
        }

        [Test]
        public void Act1Route_MatchesCatalogOrdinaryPool()
        {
            Assert.AreEqual(12, Act1Route.ClubCount);
            Assert.AreEqual(3, Act1Route.OrdinaryPick);
            CollectionAssert.AreEquivalent(
                CampaignCatalog.Act1.OrdinaryClubIds,
                new[]
                {
                    Act1ClubIds.NeonBasement,
                    Act1ClubIds.WarehousePulse,
                    Act1ClubIds.BassFactory
                });
        }
    }
}
