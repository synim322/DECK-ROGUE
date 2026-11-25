using DeckRogue.Core.Audio;
using DeckRogue.Core.Music;
using DeckRogue.Core.Run;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class DeckDepthTests
    {
        [Test]
        public void RunCrate_StartsWithFourMvpIds_AndCapsAtSixteen()
        {
            var crate = new RunCrate();
            string[] mvp = RunCrate.MvpIds();

            Assert.AreEqual(4, crate.Count);
            Assert.AreEqual(4, mvp.Length);
            Assert.AreEqual("track_neon_pulse", mvp[0]);
            Assert.AreEqual("track_bass_reactor", mvp[1]);
            Assert.AreEqual("track_afterhours", mvp[2]);
            Assert.AreEqual("track_city_static", mvp[3]);
            CollectionAssert.AreEqual(mvp, crate.ToArray());
            Assert.IsTrue(crate.Contains("track_neon_pulse"));
            Assert.IsFalse(crate.Contains("track_missing"));

            Assert.IsFalse(crate.TryAdd("track_neon_pulse"));
            Assert.IsFalse(crate.TryAdd(null));
            Assert.IsFalse(crate.TryAdd(""));
            Assert.AreEqual(4, crate.Count);

            string[] picked =
            {
                "track_voltage_alley",
                "track_mutabor_pulse",
                "track_foundry_drop",
                "track_afterglow_key",
                "track_mirror_shunt",
                "track_lounge_key",
                "track_district_hat",
                "track_siren_sub",
                "track_pier_roll",
                "track_signal_carrier",
                "track_static_crown",
                "track_festival_anthem"
            };
            for (int i = 0; i < picked.Length; i++)
                Assert.IsTrue(crate.TryAdd(picked[i]));
            Assert.AreEqual(16, RunCrate.Cap);
            Assert.AreEqual(RunCrate.Cap, crate.Count);
            Assert.IsFalse(crate.TryAdd("track_nave_pad"));
            Assert.IsFalse(crate.Contains("track_nave_pad"));
        }

        [Test]
        public void CueReady_RequiresLoadedQueuedNonPlaying_ForOneBar()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, 0d);
            var tracker = new CueReadyTracker();

            tracker.Tick(clock, DeckId.B, loaded: true, queued: true, playing: false);
            Assert.IsFalse(tracker.IsCueReady(DeckId.B));

            time.Now = clock.BarDuration - 0.01d;
            tracker.Tick(clock, DeckId.B, true, true, false);
            Assert.IsFalse(tracker.IsCueReady(DeckId.B));

            time.Now = clock.BarDuration;
            tracker.Tick(clock, DeckId.B, true, true, false);
            Assert.IsTrue(tracker.IsCueReady(DeckId.B));
            Assert.IsFalse(tracker.IsCueReady(DeckId.A));

            tracker.Tick(clock, DeckId.B, true, true, playing: true);
            Assert.IsFalse(tracker.IsCueReady(DeckId.B));
        }

        [Test]
        public void CueReady_ClearsWhenNotLoadedOrNotQueued()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, 0d);
            var tracker = new CueReadyTracker();

            time.Now = 3d;
            tracker.Tick(clock, DeckId.A, true, true, false);
            time.Now = 5d;
            tracker.Tick(clock, DeckId.A, true, true, false);
            Assert.IsTrue(tracker.IsCueReady(DeckId.A));

            tracker.Tick(clock, DeckId.A, loaded: false, queued: true, playing: false);
            Assert.IsFalse(tracker.IsCueReady(DeckId.A));

            tracker.Tick(clock, DeckId.A, true, true, false);
            Assert.IsFalse(tracker.IsCueReady(DeckId.A));
            time.Now = 7d;
            tracker.Tick(clock, DeckId.A, true, true, false);
            Assert.IsTrue(tracker.IsCueReady(DeckId.A));

            tracker.Tick(clock, DeckId.A, true, queued: false, playing: false);
            Assert.IsFalse(tracker.IsCueReady(DeckId.A));
        }

        [Test]
        public void PhraseLoadQueue_HoldsUntilPhraseStart()
        {
            var time = new ManualTimeProvider { Now = 1d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, 0d);
            var queue = new PhraseLoadQueue();

            queue.QueueLoad(DeckId.B, "track_city_static");
            Assert.IsTrue(queue.HasPending(DeckId.B));

            string trackId;
            Assert.IsFalse(queue.TryApply(clock, DeckId.B, out trackId));
            Assert.IsNull(trackId);
            Assert.IsTrue(queue.HasPending(DeckId.B));

            time.Now = clock.PhraseDuration;
            Assert.IsTrue(queue.TryApply(clock, DeckId.B, out trackId));
            Assert.AreEqual("track_city_static", trackId);
            Assert.IsFalse(queue.HasPending(DeckId.B));
            Assert.IsFalse(queue.TryApply(clock, DeckId.B, out trackId));
        }

        [Test]
        public void PhraseLoadQueue_AppliesAtOriginPhraseStart()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, 0d);
            var queue = new PhraseLoadQueue();

            queue.QueueLoad(DeckId.A, "track_neon_pulse");
            string trackId;
            Assert.IsTrue(queue.TryApply(clock, DeckId.A, out trackId));
            Assert.AreEqual("track_neon_pulse", trackId);

            queue.QueueLoad(DeckId.A, "track_bass_reactor");
            Assert.IsFalse(queue.TryApply(clock, DeckId.A, out trackId));
            Assert.IsTrue(queue.HasPending(DeckId.A));

            time.Now = clock.PhraseDuration;
            Assert.IsTrue(queue.TryApply(clock, DeckId.A, out trackId));
            Assert.AreEqual("track_bass_reactor", trackId);
        }

        [Test]
        public void DeckPresence_DeadAir_AfterMoreThanTwoQuietBeats()
        {
            var time = new ManualTimeProvider { Now = 0d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, 0d);
            var presence = new DeckPresence();

            Assert.IsFalse(presence.Tick(Quiet(0d), clock.BeatDuration));
            Assert.IsFalse(presence.IsDeadAir);

            Assert.IsFalse(presence.Tick(Quiet(clock.BeatDuration * 2d), clock.BeatDuration));
            Assert.IsFalse(presence.IsDeadAir);

            Assert.IsTrue(presence.Tick(Quiet(clock.BeatDuration * 2d + 0.01d), clock.BeatDuration));
            Assert.IsTrue(presence.IsDeadAir);

            Assert.IsFalse(presence.Tick(AudibleA(4d), clock.BeatDuration));
            Assert.IsFalse(presence.IsDeadAir);
        }

        [Test]
        public void DeckPresence_HotSwapFumble_MidPhraseWithoutHandoff()
        {
            var time = new ManualTimeProvider { Now = 3d };
            var clock = new MusicClock(time, MusicClock.ReferenceBpm, 0d, 0d);
            MusicPosition mid = clock.GetPosition();
            Assert.IsTrue(mid.BarInPhrase > 0 || mid.BeatInBar > 0);

            Assert.IsTrue(DeckPresence.IsHotSwapFumble(mid, stoppedDominant: true, otherDeckAudible: false));
            Assert.IsFalse(DeckPresence.IsHotSwapFumble(mid, stoppedDominant: true, otherDeckAudible: true));
            Assert.IsFalse(DeckPresence.IsHotSwapFumble(mid, stoppedDominant: false, otherDeckAudible: false));

            time.Now = 0d;
            MusicPosition start = clock.GetPosition();
            Assert.IsFalse(DeckPresence.IsHotSwapFumble(start, stoppedDominant: true, otherDeckAudible: false));
        }

        private static MixSnapshot Quiet(double audioTime)
        {
            return new MixSnapshot(
                audioTime,
                false,
                false,
                false,
                0d,
                0d,
                0d,
                0d,
                "",
                "",
                true);
        }

        private static MixSnapshot AudibleA(double audioTime)
        {
            return new MixSnapshot(
                audioTime,
                false,
                true,
                false,
                1d,
                0d,
                1d,
                0d,
                "track_neon_pulse",
                "",
                true);
        }
    }
}
