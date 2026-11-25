using System;
using DeckRogue.Core.Music;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class MusicClockTests
    {
        private const double Tolerance = 1e-6d;

        [Test]
        public void ReferenceGrid_At120Bpm_IsHalfSecondBeat_TwoSecondBar_SixteenSecondPhrase()
        {
            MusicClock clock = Create(0d, 0d);

            Assert.AreEqual(120d, clock.Bpm, Tolerance);
            Assert.AreEqual(0.5d, clock.BeatDuration, Tolerance);
            Assert.AreEqual(2d, clock.BarDuration, Tolerance);
            Assert.AreEqual(16d, clock.PhraseDuration, Tolerance);
            Assert.AreEqual(4, MusicClock.BeatsPerBar);
            Assert.AreEqual(8, MusicClock.BarsPerPhrase);
        }

        [TestCase(0d, 0, 0, 0, 0, 0)]
        [TestCase(0.499d, 0, 0, 0, 0, 0)]
        [TestCase(0.5d, 1, 0, 0, 1, 0)]
        [TestCase(1.999d, 3, 0, 0, 3, 0)]
        [TestCase(2d, 4, 1, 0, 0, 1)]
        [TestCase(15.999d, 31, 7, 0, 3, 7)]
        [TestCase(16d, 32, 8, 1, 0, 0)]
        public void Position_AroundBoundaries_Matches120BpmGrid(
            double audioTime,
            int beat,
            int bar,
            int phrase,
            int beatInBar,
            int barInPhrase)
        {
            MusicClock clock = Create(audioTime, 0d);
            MusicPosition position = clock.GetPosition();

            Assert.AreEqual(beat, position.Beat);
            Assert.AreEqual(bar, position.Bar);
            Assert.AreEqual(phrase, position.Phrase);
            Assert.AreEqual(beatInBar, position.BeatInBar);
            Assert.AreEqual(barInPhrase, position.BarInPhrase);
            Assert.AreEqual(beat, clock.GetBeat());
            Assert.AreEqual(bar, clock.GetBar());
            Assert.AreEqual(phrase, clock.GetPhrase());
        }

        [TestCase(0.49d, 0.05d, MusicalBoundary.Beat, 1d)]
        [TestCase(0.40d, 0.05d, MusicalBoundary.Beat, 0.5d)]
        [TestCase(0.50d, 0.05d, MusicalBoundary.Beat, 1d)]
        [TestCase(0.50d, 0d, MusicalBoundary.Beat, 0.5d)]
        [TestCase(1.90d, 0.05d, MusicalBoundary.Bar, 2d)]
        [TestCase(1.96d, 0.05d, MusicalBoundary.Bar, 4d)]
        [TestCase(15.90d, 0.05d, MusicalBoundary.Phrase, 16d)]
        [TestCase(15.97d, 0.05d, MusicalBoundary.Phrase, 32d)]
        [TestCase(7.90d, 0.05d, MusicalBoundary.HalfPhrase, 8d)]
        [TestCase(7.97d, 0.05d, MusicalBoundary.HalfPhrase, 16d)]
        [TestCase(3.90d, 0.05d, MusicalBoundary.TwoBars, 4d)]
        [TestCase(3.97d, 0.05d, MusicalBoundary.TwoBars, 8d)]
        [TestCase(0d, 0d, MusicalBoundary.Beat, 0d)]
        [TestCase(16d, 0d, MusicalBoundary.Phrase, 16d)]
        public void GetNextBoundary_TooClose_MovesToTheFollowingGridPoint(
            double requestedAt,
            double lead,
            MusicalBoundary boundary,
            double expected)
        {
            MusicClock clock = Create(requestedAt, lead);

            Assert.AreEqual(expected, clock.GetNextBoundary(boundary), Tolerance);
            Assert.GreaterOrEqual(clock.GetNextBoundary(boundary, requestedAt) + Tolerance, requestedAt + lead);
        }

        [TestCase(1.0d, 0.05d, 1, 2d)]
        [TestCase(1.0d, 0.05d, 2, 4d)]
        [TestCase(1.0d, 0.05d, 4, 8d)]
        [TestCase(1.0d, 0.05d, 8, 16d)]
        public void GetNextBoundaryBars_MatchesBarStride(
            double requestedAt,
            double lead,
            int bars,
            double expected)
        {
            MusicClock clock = Create(requestedAt, lead);
            Assert.AreEqual(expected, clock.GetNextBoundaryBars(bars, requestedAt), Tolerance);
        }

        [Test]
        public void GetNextBoundary_BeforeOrigin_ReturnsOrigin()
        {
            var time = new ManualTimeProvider { Now = -2d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, origin: 10d);

            Assert.AreEqual(0, clock.GetBeat());
            Assert.AreEqual(10d, clock.GetNextBoundary(MusicalBoundary.Beat), Tolerance);
            Assert.AreEqual(10d, clock.GetNextBoundary(MusicalBoundary.Bar), Tolerance);
            Assert.AreEqual(10d, clock.GetNextBoundary(MusicalBoundary.Phrase), Tolerance);
        }

        [Test]
        public void ToAudioTime_MapsBeatIndexOntoTheGrid()
        {
            var time = new ManualTimeProvider { Now = 3d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, origin: 1.5d);

            Assert.AreEqual(1.5d, clock.ToAudioTime(0), Tolerance);
            Assert.AreEqual(3.5d, clock.ToAudioTime(4), Tolerance);
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.ToAudioTime(-1));
        }

        [Test]
        public void Position_FollowsAbsoluteTime_AndDoesNotAccumulateBetweenCalls()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0d);

            time.Now = 4d;
            Assert.AreEqual(8, clock.GetBeat());
            Assert.AreEqual(2, clock.GetBar());

            time.Now = 4d;
            Assert.AreEqual(8, clock.GetBeat());

            time.Now = 3.9d;
            Assert.AreEqual(7, clock.GetBeat());
        }

        [Test]
        public void ResumeKeepingElapsed_RestoresPhaseAfterAudioClockJump()
        {
            var time = new ManualTimeProvider { Now = 2.5d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);

            Assert.AreEqual(5, clock.GetBeat());
            double elapsed = clock.Elapsed;

            time.Now = 100d;
            clock.ResumeKeepingElapsed(elapsed, time.Now);

            Assert.AreEqual(5, clock.GetBeat());
            Assert.AreEqual(1, clock.GetBar());
            Assert.AreEqual(0, clock.GetPhrase());
            Assert.AreEqual(100.5d, clock.GetNextBoundary(MusicalBoundary.Beat), Tolerance);
            Assert.AreEqual(101.5d, clock.GetNextBoundary(MusicalBoundary.Bar), Tolerance);
        }

        [Test]
        public void NextBoundary_OverASweep_IsNeverInThePast_AndHasNoEarlierLegalPoint()
        {
            var boundaries = new[]
            {
                MusicalBoundary.Beat,
                MusicalBoundary.Bar,
                MusicalBoundary.Phrase,
                MusicalBoundary.HalfPhrase
            };

            var clock = new MusicClock(new ManualTimeProvider(), MusicClock.ReferenceBpm, 0.05d);
            for (double requested = 0d; requested <= 48d; requested += 0.037d)
            {
                double earliest = requested + clock.SchedulingLeadTime;
                foreach (MusicalBoundary boundary in boundaries)
                {
                    double next = clock.GetNextBoundary(boundary, requested);
                    double interval = Interval(clock, boundary);

                    Assert.GreaterOrEqual(next + Tolerance, earliest, boundary + " at " + requested);
                    double steps = (next - clock.Origin) / interval;
                    Assert.AreEqual(Math.Round(steps), steps, Tolerance, boundary + " at " + requested);

                    double previous = next - interval;
                    Assert.Less(previous, earliest - Tolerance, boundary + " skipped a legal boundary at " + requested);
                }
            }
        }

        [Test]
        public void Constructor_RejectsInvalidTempoAndLead()
        {
            var time = new ManualTimeProvider();
            Assert.Throws<ArgumentNullException>(() => new MusicClock(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MusicClock(time, bpm: 0d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MusicClock(time, bpm: -10d));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MusicClock(time, schedulingLeadTime: -0.01d));
        }

        private static MusicClock Create(double now, double lead)
        {
            return new MusicClock(new ManualTimeProvider { Now = now }, MusicClock.ReferenceBpm, lead);
        }

        private static double Interval(MusicClock clock, MusicalBoundary boundary)
        {
            switch (boundary)
            {
                case MusicalBoundary.Beat:
                    return clock.BeatDuration;
                case MusicalBoundary.Bar:
                    return clock.BarDuration;
                case MusicalBoundary.Phrase:
                    return clock.PhraseDuration;
                case MusicalBoundary.HalfPhrase:
                    return clock.HalfPhraseDuration;
                default:
                    throw new ArgumentOutOfRangeException(nameof(boundary));
            }
        }
    }
}
