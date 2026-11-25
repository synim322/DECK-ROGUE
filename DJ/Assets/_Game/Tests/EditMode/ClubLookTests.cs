using DeckRogue.Core.Run;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class ClubLookTests
    {
        const ClubSilhouette Basement =
            ClubSilhouette.LowCeiling | ClubSilhouette.Curtain | ClubSilhouette.Bar;

        const ClubSilhouette Warehouse =
            ClubSilhouette.Truss | ClubSilhouette.DockDoor | ClubSilhouette.Racks;

        const ClubSilhouette Factory =
            ClubSilhouette.Press | ClubSilhouette.Pipes | ClubSilhouette.EmberFloor;

        const ClubSilhouette Signal =
            ClubSilhouette.Antenna | ClubSilhouette.Towers | ClubSilhouette.BossAxis;

        [Test]
        public void Act1Ids_ReturnFourDifferentMasks()
        {
            ClubSilhouette neon = ClubLook.Mask(Act1ClubIds.NeonBasement);
            ClubSilhouette warehouse = ClubLook.Mask(Act1ClubIds.WarehousePulse);
            ClubSilhouette factory = ClubLook.Mask(Act1ClubIds.BassFactory);
            ClubSilhouette signal = ClubLook.Mask(Act1ClubIds.TheSignal);

            Assert.AreEqual(Basement, neon);
            Assert.AreEqual(Warehouse, warehouse);
            Assert.AreEqual(Factory, factory);
            Assert.AreEqual(Signal, signal);

            Assert.AreNotEqual(neon, warehouse);
            Assert.AreNotEqual(neon, factory);
            Assert.AreNotEqual(neon, signal);
            Assert.AreNotEqual(warehouse, factory);
            Assert.AreNotEqual(warehouse, signal);
            Assert.AreNotEqual(factory, signal);
        }

        [Test]
        public void OrdinaryMasks_ArePairwiseDisjoint()
        {
            ClubSilhouette neon = ClubLook.Mask(Act1ClubIds.NeonBasement);
            ClubSilhouette warehouse = ClubLook.Mask(Act1ClubIds.WarehousePulse);
            ClubSilhouette factory = ClubLook.Mask(Act1ClubIds.BassFactory);

            Assert.AreEqual(ClubSilhouette.None, neon & warehouse);
            Assert.AreEqual(ClubSilhouette.None, neon & factory);
            Assert.AreEqual(ClubSilhouette.None, warehouse & factory);
        }

        [Test]
        public void SignalMask_IsDisjointFromEachOrdinaryMask()
        {
            ClubSilhouette signal = ClubLook.Mask(Act1ClubIds.TheSignal);

            Assert.AreEqual(ClubSilhouette.None, signal & ClubLook.Mask(Act1ClubIds.NeonBasement));
            Assert.AreEqual(ClubSilhouette.None, signal & ClubLook.Mask(Act1ClubIds.WarehousePulse));
            Assert.AreEqual(ClubSilhouette.None, signal & ClubLook.Mask(Act1ClubIds.BassFactory));
        }

        [Test]
        public void Signal_HasAntennaTowersAndBossAxis()
        {
            ClubSilhouette signal = ClubLook.Mask(Act1ClubIds.TheSignal);

            Assert.IsTrue((signal & ClubSilhouette.Antenna) == ClubSilhouette.Antenna);
            Assert.IsTrue((signal & ClubSilhouette.Towers) == ClubSilhouette.Towers);
            Assert.IsTrue((signal & ClubSilhouette.BossAxis) == ClubSilhouette.BossAxis);
        }

        [Test]
        public void Neon_HasBasementBits_WithoutTrussPressOrAntenna()
        {
            ClubSilhouette neon = ClubLook.Mask(Act1ClubIds.NeonBasement);

            Assert.IsTrue((neon & ClubSilhouette.LowCeiling) == ClubSilhouette.LowCeiling);
            Assert.IsTrue((neon & ClubSilhouette.Curtain) == ClubSilhouette.Curtain);
            Assert.IsTrue((neon & ClubSilhouette.Bar) == ClubSilhouette.Bar);
            Assert.IsFalse((neon & ClubSilhouette.Truss) == ClubSilhouette.Truss);
            Assert.IsFalse((neon & ClubSilhouette.Press) == ClubSilhouette.Press);
            Assert.IsFalse((neon & ClubSilhouette.Antenna) == ClubSilhouette.Antenna);
        }

        [Test]
        public void UnknownIds_ReturnBasementMask()
        {
            ClubSilhouette basement = ClubLook.Mask(Act1ClubIds.NeonBasement);

            Assert.AreEqual(basement, ClubLook.Mask(null));
            Assert.AreEqual(basement, ClubLook.Mask(""));
            Assert.AreEqual(basement, ClubLook.Mask("club_not_a_room"));
            Assert.AreNotEqual(basement, ClubLook.Mask("club_dream_lounge"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_dream_lounge"));
        }

        [Test]
        public void LaterClubIds_AreNotBasement()
        {
            Assert.AreEqual(
                ClubSilhouette.LowCeiling | ClubSilhouette.Curtain,
                ClubLook.Mask("club_dream_lounge"));
            Assert.AreEqual(
                ClubSilhouette.Truss | ClubSilhouette.EmberFloor,
                ClubLook.Mask("club_rave_district"));
            Assert.AreEqual(
                ClubSilhouette.DockDoor | ClubSilhouette.Towers,
                ClubLook.Mask("club_chrome_pier"));
            Assert.AreEqual(
                ClubSilhouette.Antenna | ClubSilhouette.BossAxis | ClubSilhouette.Towers,
                ClubLook.Mask("club_the_static_throne"));
            Assert.AreEqual(
                ClubSilhouette.Curtain | ClubSilhouette.Towers,
                ClubLook.Mask("club_glass_cathedral"));
            Assert.AreEqual(
                ClubSilhouette.Bar | ClubSilhouette.Racks,
                ClubLook.Mask("club_night_exchange"));
            Assert.AreEqual(
                ClubSilhouette.Antenna | ClubSilhouette.Truss,
                ClubLook.Mask("club_roof_relay"));
            Assert.AreEqual(
                ClubSilhouette.Truss | ClubSilhouette.EmberFloor | ClubSilhouette.BossAxis,
                ClubLook.Mask("club_final_festival"));

            Assert.AreNotEqual(Basement, ClubLook.Mask("club_dream_lounge"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_rave_district"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_chrome_pier"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_the_static_throne"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_glass_cathedral"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_night_exchange"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_roof_relay"));
            Assert.AreNotEqual(Basement, ClubLook.Mask("club_final_festival"));
        }

        [Test]
        public void Mask_IsStableAcrossCalls()
        {
            Assert.AreEqual(ClubLook.Mask(Act1ClubIds.NeonBasement), ClubLook.Mask(Act1ClubIds.NeonBasement));
            Assert.AreEqual(ClubLook.Mask(Act1ClubIds.WarehousePulse), ClubLook.Mask(Act1ClubIds.WarehousePulse));
            Assert.AreEqual(ClubLook.Mask(Act1ClubIds.BassFactory), ClubLook.Mask(Act1ClubIds.BassFactory));
            Assert.AreEqual(ClubLook.Mask(Act1ClubIds.TheSignal), ClubLook.Mask(Act1ClubIds.TheSignal));
            Assert.AreEqual(ClubLook.Mask(null), ClubLook.Mask(null));
            Assert.AreEqual(ClubLook.Mask(""), ClubLook.Mask(""));
            Assert.AreEqual(ClubLook.Mask("club_dream_lounge"), ClubLook.Mask("club_dream_lounge"));
        }
    }
}
