using DeckRogue.Core.Run;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    /// <summary>
    /// Hype, старт и длина ночи от crate не меняются. Со второго клуба зал хочет услышать ответ:
    /// один CleanTransition на жанре вкуса или два CleanTransition на любых. Crate без эфира не считается.
    /// </summary>
    public sealed class RoomDemandTests
    {
        [Test]
        public void Heat0_NoCleans_IsEnough()
        {
            RoomDemand opening = RoomDemand.ForClub(Act1ClubIds.NeonBasement, 0);

            Assert.AreEqual(0, opening.Heat);
            Assert.IsTrue(RoomDemand.HeardEnough(0, 0, 0));
            Assert.IsTrue(RoomDemand.HeardEnough(opening.Heat, 0, 0));
        }

        [Test]
        public void Heat2_OneAnyClean_IsNotEnough()
        {
            RoomDemand later = RoomDemand.ForClub(Act1ClubIds.WarehousePulse, 2);

            Assert.AreEqual(2, later.Heat);
            Assert.IsFalse(RoomDemand.HeardEnough(2, 0, 1));
            Assert.IsFalse(RoomDemand.HeardEnough(later.Heat, 0, 1));
        }

        [Test]
        public void Heat2_OneDialectClean_IsEnough()
        {
            Assert.IsTrue(RoomDemand.HeardEnough(2, 1, 0));
        }

        [Test]
        public void Heat2_TwoAnyCleans_AreEnough()
        {
            Assert.IsTrue(RoomDemand.HeardEnough(2, 0, 2));
        }

        [Test]
        public void Heat1_AlreadyAsksToBeHeard()
        {
            Assert.IsFalse(RoomDemand.HeardEnough(1, 0, 1));
            Assert.IsTrue(RoomDemand.HeardEnough(1, 1, 0));
            Assert.IsTrue(RoomDemand.HeardEnough(1, 0, 2));
        }

        [Test]
        public void NegativeHeat_CountsAsOpeningNight()
        {
            RoomDemand demand = RoomDemand.ForClub(Act1ClubIds.BassFactory, -3);

            Assert.AreEqual(0, demand.Heat);
            Assert.IsTrue(RoomDemand.HeardEnough(-3, 0, 0));
        }

        [Test]
        public void NegativeCleans_CountAsZero()
        {
            Assert.IsFalse(RoomDemand.HeardEnough(2, -4, 1));
            Assert.IsTrue(RoomDemand.HeardEnough(2, -1, 2));
            Assert.IsTrue(RoomDemand.HeardEnough(2, 1, -5));
        }

        [Test]
        public void GenreFits_WarehouseTechno_WarehouseAndSignalOnly()
        {
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Warehouse Techno"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Warehouse Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "Warehouse Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Warehouse Techno"));
        }

        [Test]
        public void GenreFits_BassTech_BassAndSignalOnly()
        {
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "Bass Tech"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Bass Tech"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Bass Tech"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Bass Tech"));
        }

        [Test]
        public void GenreFits_TechHouse_FitsNowhere()
        {
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Tech House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Tech House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "Tech House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Tech House"));
        }

        [Test]
        public void GenreFits_House_OnlyNeon()
        {
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "House"));
        }

        [Test]
        public void GenreFits_NeonDialects_OnlyNeon()
        {
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Deep House"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Electro House"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Acid House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Deep House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Electro House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Acid House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "Electro House"));
        }

        [Test]
        public void GenreFits_EmptyGenre_IsFalse()
        {
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, ""));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, ""));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, ""));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, ""));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, null));
        }

        [Test]
        public void GenreFits_NamedMisses_AreNotAliases()
        {
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Electro"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Broken Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Glass Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Electro"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Broken Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Glass Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Tech House"));
        }

        [Test]
        public void GenreFits_SignalDialects_StayOnTheirFloors()
        {
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Dub Techno"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Dub Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "Dub Techno"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Industrial"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.WarehousePulse, "Industrial"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.BassFactory, "Industrial"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "Industrial"));
        }

        [Test]
        public void GenreFits_LaterActs_UseTheirOwnLists()
        {
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.DreamLounge, "Deep House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.DreamLounge, "House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.DreamLounge, "Electro"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.RaveDistrict, "Broken Techno"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.RaveDistrict, "Bass Tech"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.RaveDistrict, "Electro"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.ChromePier, "Electro"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.ChromePier, "Electro House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.ChromePier, "Deep House"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.TheStaticThrone, "Industrial"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.TheStaticThrone, "Electro"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.TheStaticThrone, "Bass Tech"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.GlassCathedral, "Glass Techno"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.GlassCathedral, "Deep House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.GlassCathedral, "Electro"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.NightExchange, "Electro"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.NightExchange, "Acid House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.NightExchange, "Deep House"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.RoofRelay, "Industrial"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.RoofRelay, "Dub Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.RoofRelay, "Electro"));

            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Industrial"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Deep House"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Glass Techno"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Electro"));
            Assert.IsTrue(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Bass Tech"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Acid House"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Broken Techno"));
            Assert.IsFalse(RoomDemand.GenreFits(CampaignClubIds.FinalFestival, "Tech House"));

            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.NeonBasement, "House"));
            Assert.IsTrue(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Industrial"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Deep House"));
            Assert.IsFalse(RoomDemand.GenreFits(Act1ClubIds.TheSignal, "Electro"));
        }
    }
}
