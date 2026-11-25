using DeckRogue.Core.Audio;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class EqualPowerCrossfadeTests
    {
        private const double Tolerance = 1e-6d;

        [Test]
        public void Evaluate_AtZero_MutesDeckB()
        {
            EqualPowerCrossfade.Evaluate(0d, out double a, out double b);

            Assert.AreEqual(1d, a, Tolerance);
            Assert.AreEqual(0d, b, Tolerance);
        }

        [Test]
        public void Evaluate_AtOne_MutesDeckA()
        {
            EqualPowerCrossfade.Evaluate(1d, out double a, out double b);

            Assert.AreEqual(0d, a, Tolerance);
            Assert.AreEqual(1d, b, Tolerance);
        }

        [Test]
        public void Evaluate_AtCenter_IsEqualPower()
        {
            EqualPowerCrossfade.Evaluate(0.5d, out double a, out double b);

            Assert.AreEqual(a, b, Tolerance);
            Assert.AreEqual(1d, a * a + b * b, 1e-5d);
        }

        [Test]
        public void Evaluate_OutsideRange_ClampsToAnEndpoint()
        {
            EqualPowerCrossfade.Evaluate(-0.2d, out double lowA, out double lowB);
            EqualPowerCrossfade.Evaluate(1.4d, out double highA, out double highB);

            Assert.AreEqual(1d, lowA, Tolerance);
            Assert.AreEqual(0d, lowB, Tolerance);
            Assert.AreEqual(0d, highA, Tolerance);
            Assert.AreEqual(1d, highB, Tolerance);
        }
    }
}
