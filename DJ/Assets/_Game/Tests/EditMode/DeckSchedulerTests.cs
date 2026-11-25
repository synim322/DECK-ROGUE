using DeckRogue.Core.Audio;
using DeckRogue.Core.Music;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class DeckSchedulerTests
    {
        private const double Tolerance = 1e-6d;

        [Test]
        public void TryScheduleStart_QuantizesToTheNextBar_AndNeverInThePast()
        {
            var time = new ManualTimeProvider { Now = 1.0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            ScheduleResult result = scheduler.TryScheduleStart(DeckId.A, 1L, MusicalBoundary.Bar, time.Now);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(2d, result.StartAudioTime, Tolerance);
            Assert.GreaterOrEqual(result.StartAudioTime + Tolerance, time.Now + clock.SchedulingLeadTime);
            Assert.IsTrue(scheduler.IsActive(DeckId.A));
        }

        [Test]
        public void TryScheduleStart_SecondStartOnSameDeck_IsRejected()
        {
            var time = new ManualTimeProvider { Now = 0.2d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            Assert.IsTrue(scheduler.TryScheduleStart(DeckId.A, 1L, MusicalBoundary.Bar, time.Now).Accepted);
            ScheduleResult again = scheduler.TryScheduleStart(DeckId.A, 2L, MusicalBoundary.Bar, time.Now);

            Assert.IsFalse(again.Accepted);
            Assert.AreEqual("deck-already-started", again.Reason);
        }

        [Test]
        public void TryScheduleStart_RepeatedCommandId_IsRejected()
        {
            var time = new ManualTimeProvider { Now = 0.2d };
            var clock = new MusicClock(time, schedulingLeadTime: 0d);
            var scheduler = new DeckScheduler(clock);

            Assert.IsTrue(scheduler.TryScheduleStart(DeckId.A, 7L, MusicalBoundary.Beat, time.Now).Accepted);
            ScheduleResult duplicate = scheduler.TryScheduleStart(DeckId.B, 7L, MusicalBoundary.Beat, time.Now);

            Assert.IsFalse(duplicate.Accepted);
            Assert.AreEqual("command-already-used", duplicate.Reason);
            Assert.IsFalse(scheduler.IsActive(DeckId.B));
        }

        [Test]
        public void TryScheduleStart_OtherDeck_CanStartOnALaterBar()
        {
            var time = new ManualTimeProvider { Now = 0.2d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            ScheduleResult a = scheduler.TryScheduleStart(DeckId.A, 1L, MusicalBoundary.Bar, 0.2d);
            time.Now = 2.1d;
            ScheduleResult b = scheduler.TryScheduleStart(DeckId.B, 2L, MusicalBoundary.Bar, time.Now);

            Assert.IsTrue(a.Accepted);
            Assert.IsTrue(b.Accepted);
            Assert.AreEqual(2d, a.StartAudioTime, Tolerance);
            Assert.AreEqual(4d, b.StartAudioTime, Tolerance);
            Assert.Greater(b.StartAudioTime, a.StartAudioTime);
        }

        [Test]
        public void MarkStopped_AllowsANewCommand_ToScheduleAgain()
        {
            var time = new ManualTimeProvider { Now = 3.1d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            Assert.IsTrue(scheduler.TryScheduleStart(DeckId.B, 1L, MusicalBoundary.Bar, time.Now).Accepted);
            scheduler.MarkStopped(DeckId.B);
            ScheduleResult next = scheduler.TryScheduleStart(DeckId.B, 2L, MusicalBoundary.Bar, time.Now);

            Assert.IsTrue(next.Accepted);
            Assert.AreEqual(4d, next.StartAudioTime, Tolerance);
        }

        [Test]
        public void LoopLength_Half4_SchedulesOnEightSecondBoundaries()
        {
            var time = new ManualTimeProvider { Now = 1.0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            Assert.AreEqual(LoopLengthMode.Phrase8, scheduler.GetLoopLength(DeckId.A));
            Assert.AreEqual(MusicalBoundary.Phrase, scheduler.LoopBoundary(DeckId.A));
            Assert.IsTrue(scheduler.TrySetLoopLength(1L, DeckId.A, LoopLengthMode.Half4));
            Assert.AreEqual(LoopLengthMode.Half4, scheduler.GetLoopLength(DeckId.A));
            Assert.AreEqual(MusicalBoundary.HalfPhrase, scheduler.LoopBoundary(DeckId.A));

            ScheduleResult result = scheduler.TryScheduleLoopStart(DeckId.A, 2L, time.Now);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(8d, result.StartAudioTime, Tolerance);
            Assert.AreEqual(8d, clock.HalfPhraseDuration, Tolerance);
        }

        [Test]
        public void LoopLength_Phrase8_SchedulesOnPhraseBoundary()
        {
            var time = new ManualTimeProvider { Now = 1.0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            ScheduleResult result = scheduler.TryScheduleLoopStart(DeckId.B, 1L, time.Now);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(16d, result.StartAudioTime, Tolerance);
        }

        [Test]
        public void TrySetLoopLength_RepeatedCommandId_IsRejected()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0d);
            var scheduler = new DeckScheduler(clock);

            Assert.IsTrue(scheduler.TrySetLoopLength(9L, DeckId.A, LoopLengthMode.Half4));
            Assert.IsFalse(scheduler.TrySetLoopLength(9L, DeckId.A, LoopLengthMode.Phrase8));
            Assert.AreEqual(LoopLengthMode.Half4, scheduler.GetLoopLength(DeckId.A));
        }

        [TestCase(1, 2d)]
        [TestCase(2, 4d)]
        [TestCase(4, 8d)]
        [TestCase(8, 16d)]
        public void LoopLengthBars_SchedulesOnMatchingBarStride(int bars, double expectedStart)
        {
            var time = new ManualTimeProvider { Now = 1.0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0.05d);
            var scheduler = new DeckScheduler(clock);

            Assert.IsTrue(scheduler.TrySetLoopLengthBars(1L, DeckId.A, bars));
            Assert.AreEqual(bars, scheduler.GetLoopLengthBars(DeckId.A));
            Assert.AreEqual(LoopLength.FromBars(bars), scheduler.GetLoopLength(DeckId.A));

            ScheduleResult result = scheduler.TryScheduleLoopStart(DeckId.A, 2L, time.Now);

            Assert.IsTrue(result.Accepted);
            Assert.AreEqual(expectedStart, result.StartAudioTime, Tolerance);
            Assert.AreEqual(expectedStart, clock.GetNextBoundaryBars(bars, time.Now), Tolerance);
        }

        [Test]
        public void TrySetLoopLengthBars_RejectsInvalidStride()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, schedulingLeadTime: 0d);
            var scheduler = new DeckScheduler(clock);

            Assert.IsFalse(scheduler.TrySetLoopLengthBars(1L, DeckId.A, 3));
            Assert.AreEqual(8, scheduler.GetLoopLengthBars(DeckId.A));
        }
    }
}
