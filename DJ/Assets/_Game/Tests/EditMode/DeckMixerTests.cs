using DeckRogue.Core.Audio;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class DeckMixerTests
    {
        [Test]
        public void Crossfader_EndpointsMuteTheOppositeDeck()
        {
            var mixer = new DeckMixer();

            Assert.IsTrue(mixer.TryApply(MixerCommand.Crossfader(1, 0d)));
            Assert.AreEqual(1d, mixer.StemGain(DeckId.A, StemId.Music), 1e-6d);
            Assert.AreEqual(0d, mixer.StemGain(DeckId.B, StemId.Low), 1e-6d);

            Assert.IsTrue(mixer.TryApply(MixerCommand.Crossfader(2, 1d)));
            Assert.AreEqual(0d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-6d);
            Assert.AreEqual(1d, mixer.StemGain(DeckId.B, StemId.Music), 1e-6d);
        }

        [Test]
        public void BassKill_MutesOnlyTheLowStem()
        {
            var mixer = new DeckMixer();

            Assert.IsTrue(mixer.TryApply(MixerCommand.BassKill(1, DeckId.A, true)));

            Assert.AreEqual(0d, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
            Assert.AreEqual(1d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-9d);
            Assert.AreEqual(1d, mixer.StemGain(DeckId.A, StemId.Music), 1e-9d);
            Assert.AreEqual(0d, mixer.StemGain(DeckId.B, StemId.Low), 1e-9d);
            Assert.IsFalse(mixer.IsBassKilled(DeckId.B));
        }

        [Test]
        public void Filter_StoresPerDeckAmount_WithoutChangingStemGain()
        {
            var mixer = new DeckMixer();

            Assert.IsTrue(mixer.TryApply(MixerCommand.Filter(1, DeckId.A, 0.75d)));
            Assert.AreEqual(0.75d, mixer.FilterAmount(DeckId.A), 1e-9d);
            Assert.AreEqual(0d, mixer.FilterAmount(DeckId.B), 1e-9d);
            Assert.AreEqual(1d, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
            Assert.AreEqual(1d, mixer.StemGain(DeckId.A, StemId.Music), 1e-9d);
        }

        [Test]
        public void PercAndMusicGain_ScaleOnlyTheirStems()
        {
            var mixer = new DeckMixer();

            Assert.IsTrue(mixer.TryApply(MixerCommand.PercGain(1, DeckId.A, 0.5d)));
            Assert.IsTrue(mixer.TryApply(MixerCommand.MusicGain(2, DeckId.A, 0.25d)));

            Assert.AreEqual(1d, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
            Assert.AreEqual(0.5d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-9d);
            Assert.AreEqual(0.25d, mixer.StemGain(DeckId.A, StemId.Music), 1e-9d);
            Assert.AreEqual(1d, mixer.PercTrim(DeckId.B), 1e-9d);
            Assert.AreEqual(0d, mixer.StemGain(DeckId.B, StemId.Perc), 1e-9d);
        }

        [Test]
        public void PercGain_MultipliesEqualPowerDeckGain()
        {
            var mixer = new DeckMixer();
            Assert.IsTrue(mixer.TryApply(MixerCommand.Crossfader(1, 0.5d)));
            Assert.IsTrue(mixer.TryApply(MixerCommand.PercGain(2, DeckId.A, 0.5d)));

            double deck = mixer.DeckGain(DeckId.A);
            Assert.AreEqual(deck * 0.5d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-9d);
            Assert.AreEqual(deck, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
        }

        [Test]
        public void BassKill_StillMutesLow_WhenPercAndMusicTrimmed()
        {
            var mixer = new DeckMixer();
            Assert.IsTrue(mixer.TryApply(MixerCommand.PercGain(1, DeckId.A, 0.4d)));
            Assert.IsTrue(mixer.TryApply(MixerCommand.MusicGain(2, DeckId.A, 0.6d)));
            Assert.IsTrue(mixer.TryApply(MixerCommand.BassKill(3, DeckId.A, true)));

            Assert.AreEqual(0d, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
            Assert.AreEqual(0.4d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-9d);
            Assert.AreEqual(0.6d, mixer.StemGain(DeckId.A, StemId.Music), 1e-9d);
        }

        [Test]
        public void InvalidFilterOrGain_LeavesPreviousValue()
        {
            var mixer = new DeckMixer();
            Assert.IsTrue(mixer.TryApply(MixerCommand.Filter(1, DeckId.A, 0.3d)));
            Assert.IsTrue(mixer.TryApply(MixerCommand.PercGain(2, DeckId.A, 0.8d)));

            Assert.IsFalse(mixer.TryApply(MixerCommand.Filter(3, DeckId.A, double.NaN)));
            Assert.IsFalse(mixer.TryApply(MixerCommand.MusicGain(4, DeckId.A, double.PositiveInfinity)));

            Assert.AreEqual(0.3d, mixer.FilterAmount(DeckId.A), 1e-9d);
            Assert.AreEqual(0.8d, mixer.PercTrim(DeckId.A), 1e-9d);
            Assert.AreEqual(1d, mixer.MusicTrim(DeckId.A), 1e-9d);
        }

        [Test]
        public void RepeatedCommandId_DoesNotApplyTwice()
        {
            var mixer = new DeckMixer();
            MixerCommand kill = MixerCommand.BassKill(7, DeckId.B, true);

            Assert.IsTrue(mixer.TryApply(kill));
            Assert.IsFalse(mixer.TryApply(MixerCommand.BassKill(7, DeckId.B, false)));
            Assert.IsTrue(mixer.IsBassKilled(DeckId.B));

            Assert.IsTrue(mixer.TryApply(MixerCommand.Filter(8, DeckId.A, 1d)));
            Assert.IsFalse(mixer.TryApply(MixerCommand.Filter(8, DeckId.A, 0d)));
            Assert.AreEqual(1d, mixer.FilterAmount(DeckId.A), 1e-9d);
        }

        [Test]
        public void OneHundredMoves_StayInsideUnityGain()
        {
            var mixer = new DeckMixer();
            for (int i = 0; i < 100; i++)
            {
                double position = i / 99d;
                Assert.IsTrue(mixer.TryApply(MixerCommand.Crossfader(i + 1, position)));
                Assert.IsTrue(mixer.TryApply(MixerCommand.BassKill(1000 + i, DeckId.A, i % 2 == 0)));
                Assert.IsTrue(mixer.TryApply(MixerCommand.Filter(2000 + i, DeckId.A, position)));
                Assert.IsTrue(mixer.TryApply(MixerCommand.PercGain(3000 + i, DeckId.B, 1d - position)));
                Assert.IsTrue(mixer.TryApply(MixerCommand.MusicGain(4000 + i, DeckId.B, position)));

                AssertGain(mixer.StemGain(DeckId.A, StemId.Low));
                AssertGain(mixer.StemGain(DeckId.A, StemId.Music));
                AssertGain(mixer.StemGain(DeckId.B, StemId.Perc));
                AssertGain(mixer.FilterAmount(DeckId.A));
                double sum = mixer.DeckGain(DeckId.A) * mixer.DeckGain(DeckId.A)
                    + mixer.DeckGain(DeckId.B) * mixer.DeckGain(DeckId.B);
                Assert.AreEqual(1d, sum, 1e-5d);
            }
        }

        [Test]
        public void InvalidCrossfader_LeavesThePreviousPosition()
        {
            var mixer = new DeckMixer();
            mixer.TryApply(MixerCommand.Crossfader(1, 0.25d));

            Assert.IsFalse(mixer.TryApply(MixerCommand.Crossfader(2, double.NaN)));
            Assert.AreEqual(0.25d, mixer.Crossfader, 1e-9d);
        }

        [Test]
        public void LineGain_DefaultsToOne_AndDeckGainAtCrossfaderZeroMatchesDeckA()
        {
            var mixer = new DeckMixer();

            Assert.AreEqual(1d, mixer.LineGain(DeckId.A), 1e-9d);
            Assert.AreEqual(1d, mixer.LineGain(DeckId.B), 1e-9d);
            Assert.AreEqual(0d, mixer.Crossfader, 1e-9d);

            EqualPowerCrossfade.Evaluate(0d, out double gainA, out double gainB);
            Assert.AreEqual(gainA, mixer.DeckGain(DeckId.A), 1e-9d);
            Assert.AreEqual(gainB, mixer.DeckGain(DeckId.B), 1e-9d);
        }

        [Test]
        public void LineGain_HalfOnA_ScalesDeckAndLowPerc_LeavesB()
        {
            var mixer = new DeckMixer();
            Assert.IsTrue(mixer.TryApply(MixerCommand.Crossfader(1, 0.25d)));

            double deckA = mixer.DeckGain(DeckId.A);
            double deckB = mixer.DeckGain(DeckId.B);
            double lowA = mixer.StemGain(DeckId.A, StemId.Low);
            double percA = mixer.StemGain(DeckId.A, StemId.Perc);
            double musicA = mixer.StemGain(DeckId.A, StemId.Music);
            double lowB = mixer.StemGain(DeckId.B, StemId.Low);
            double percB = mixer.StemGain(DeckId.B, StemId.Perc);

            Assert.IsTrue(mixer.TryApply(MixerCommand.LineGain(2, DeckId.A, 0.5d)));

            Assert.AreEqual(0.5d, mixer.LineGain(DeckId.A), 1e-9d);
            Assert.AreEqual(1d, mixer.LineGain(DeckId.B), 1e-9d);
            Assert.AreEqual(deckA * 0.5d, mixer.DeckGain(DeckId.A), 1e-9d);
            Assert.AreEqual(lowA * 0.5d, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
            Assert.AreEqual(percA * 0.5d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-9d);
            Assert.AreEqual(musicA * 0.5d, mixer.StemGain(DeckId.A, StemId.Music), 1e-9d);
            Assert.AreEqual(deckB, mixer.DeckGain(DeckId.B), 1e-9d);
            Assert.AreEqual(lowB, mixer.StemGain(DeckId.B, StemId.Low), 1e-9d);
            Assert.AreEqual(percB, mixer.StemGain(DeckId.B, StemId.Perc), 1e-9d);
        }

        [Test]
        public void LineGain_RepeatedCommandId_DoesNotApplyTwice()
        {
            var mixer = new DeckMixer();

            Assert.IsTrue(mixer.TryApply(MixerCommand.LineGain(9, DeckId.A, 0.5d)));
            Assert.IsFalse(mixer.TryApply(MixerCommand.LineGain(9, DeckId.A, 0.2d)));
            Assert.AreEqual(0.5d, mixer.LineGain(DeckId.A), 1e-9d);
            Assert.AreEqual(1d, mixer.LineGain(DeckId.B), 1e-9d);
        }

        [Test]
        public void LineGain_ZeroMutesDeckGain_WithoutResettingCrossfader()
        {
            var mixer = new DeckMixer();
            Assert.IsTrue(mixer.TryApply(MixerCommand.Crossfader(1, 0.35d)));
            Assert.IsTrue(mixer.TryApply(MixerCommand.LineGain(2, DeckId.A, 0d)));

            Assert.AreEqual(0d, mixer.LineGain(DeckId.A), 1e-9d);
            Assert.AreEqual(0d, mixer.DeckGain(DeckId.A), 1e-9d);
            Assert.AreEqual(0d, mixer.StemGain(DeckId.A, StemId.Low), 1e-9d);
            Assert.AreEqual(0d, mixer.StemGain(DeckId.A, StemId.Perc), 1e-9d);
            Assert.AreEqual(0d, mixer.StemGain(DeckId.A, StemId.Music), 1e-9d);
            Assert.AreEqual(0.35d, mixer.Crossfader, 1e-9d);
            Assert.Greater(mixer.DeckGain(DeckId.B), 0d);
        }

        [Test]
        public void LineGain_NonFinite_DoesNotConsumeCommandId()
        {
            var mixer = new DeckMixer();

            Assert.IsFalse(mixer.TryApply(MixerCommand.LineGain(5, DeckId.A, double.NaN)));
            Assert.AreEqual(1d, mixer.LineGain(DeckId.A), 1e-9d);
            Assert.IsTrue(mixer.TryApply(MixerCommand.LineGain(5, DeckId.A, 0.4d)));
            Assert.AreEqual(0.4d, mixer.LineGain(DeckId.A), 1e-9d);

            Assert.IsFalse(mixer.TryApply(MixerCommand.LineGain(6, DeckId.B, double.PositiveInfinity)));
            Assert.AreEqual(1d, mixer.LineGain(DeckId.B), 1e-9d);
            Assert.IsTrue(mixer.TryApply(MixerCommand.LineGain(6, DeckId.B, 0.25d)));
            Assert.AreEqual(0.25d, mixer.LineGain(DeckId.B), 1e-9d);
        }

        private static void AssertGain(double gain)
        {
            Assert.GreaterOrEqual(gain, 0d);
            Assert.LessOrEqual(gain, 1d);
        }
    }
}
