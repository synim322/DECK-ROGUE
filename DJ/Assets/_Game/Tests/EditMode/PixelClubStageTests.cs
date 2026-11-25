using DeckRogue.Audio;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class PixelClubStageTests
    {
        [Test]
        public void ResolveStage_ZeroIntensityAndImpulse_IsSolo()
        {
            Assert.AreEqual(HallVisualStage.Solo, PixelClubView.ResolveStage(0, 0, false));
        }

        [Test]
        public void ResolveStage_Intensity1Impulse0_IsTrickle()
        {
            Assert.AreEqual(HallVisualStage.Trickle, PixelClubView.ResolveStage(1, 0, false));
        }

        [Test]
        public void ResolveStage_Impulse1Intensity0_IsSolo()
        {
            Assert.AreEqual(HallVisualStage.Solo, PixelClubView.ResolveStage(0, 1, false));
        }

        [Test]
        public void ResolveStage_Intensity2Impulse0_IsGroove()
        {
            Assert.AreEqual(HallVisualStage.Groove, PixelClubView.ResolveStage(2, 0, false));
        }

        [Test]
        public void ResolveStage_Impulse2Intensity0_IsSolo()
        {
            Assert.AreEqual(HallVisualStage.Solo, PixelClubView.ResolveStage(0, 2, false));
        }

        [Test]
        public void ResolveStage_Intensity2Impulse2_IsGroove()
        {
            Assert.AreEqual(HallVisualStage.Groove, PixelClubView.ResolveStage(2, 2, false));
        }

        [Test]
        public void ResolveStage_Intensity3Impulse0_IsRave()
        {
            Assert.AreEqual(HallVisualStage.Rave, PixelClubView.ResolveStage(3, 0, false));
        }

        [Test]
        public void ResolveStage_Intensity3Impulse2_IsRave()
        {
            Assert.AreEqual(HallVisualStage.Rave, PixelClubView.ResolveStage(3, 2, false));
        }

        [Test]
        public void ResolveStage_Intensity0Impulse3_IsSolo()
        {
            Assert.AreEqual(HallVisualStage.Solo, PixelClubView.ResolveStage(0, 3, false));
        }

        [Test]
        public void ResolveStage_Intensity1Impulse3_IsTrickle()
        {
            Assert.AreEqual(HallVisualStage.Trickle, PixelClubView.ResolveStage(1, 3, false));
        }

        [Test]
        public void ResolveStage_Intensity2Impulse3_IsTrash()
        {
            Assert.AreEqual(HallVisualStage.Trash, PixelClubView.ResolveStage(2, 3, false));
        }

        [Test]
        public void ResolveStage_Intensity3Impulse3_IsTrash()
        {
            Assert.AreEqual(HallVisualStage.Trash, PixelClubView.ResolveStage(3, 3, false));
        }

        [Test]
        public void ResolveStage_FaultAtPeak_IsSlump()
        {
            Assert.AreEqual(HallVisualStage.Slump, PixelClubView.ResolveStage(3, 3, true));
        }

        [Test]
        public void MapImpulse_AndMapIntensity_KeepCueRows()
        {
            Assert.AreEqual(3, PixelClubView.MapImpulse(HallCue.Drop, 0));
            Assert.AreEqual(0, PixelClubView.MapImpulse(HallCue.Fault, 3));
            Assert.AreEqual(2, PixelClubView.MapImpulse(HallCue.Clean, 0));
            Assert.AreEqual(1, PixelClubView.MapImpulse(HallCue.None, 1));
            Assert.AreEqual(3, PixelClubView.MapIntensity(0, HallCue.Drop, 0));
            Assert.AreEqual(0, PixelClubView.MapIntensity(2, HallCue.Fault, 2));
        }
    }
}
