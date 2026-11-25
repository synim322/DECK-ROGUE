using DeckRogue.Core.Scoring;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class DeckReactionTests
    {
        private readonly ScoreEvent[] _events = new ScoreEvent[8];

        [Test]
        public void CueReadyHonor_PaysOncePerRequest_ThenLateIsSkipped()
        {
            var detector = new DeckReactionDetector();
            var hype = new HypeSystem();
            MixSnapshot mix = Playing(0d);

            int n = detector.Collect(
                mix,
                Cues(phrase: 0, requestId: 1, cueReady: true, active: true, satisfied: true),
                _events,
                0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(ScoreKind.CueReadyHonor, _events[0].Kind);
            Assert.AreEqual(3d, _events[0].HypeDelta, 1e-9d);
            Assert.AreEqual(40d, _events[0].ReputationBase, 1e-9d);
            Assert.IsTrue(hype.TryApply(_events[0], out ScoreChange honor));
            Assert.AreEqual(38d, honor.Hype, 1e-9d);
            Assert.AreEqual(40d, honor.Reputation, 1e-9d);

            n = detector.Collect(
                mix,
                Cues(phrase: 0, requestId: 1, cueReady: true, active: true, satisfied: true),
                _events,
                0);
            Assert.AreEqual(0, n);
            Assert.IsFalse(hype.TryApply(ScoreEvent.CueHonor(1), out _));

            n = detector.Collect(
                mix,
                Cues(phrase: 0, requestId: 1, cueReady: true, active: false, satisfied: false),
                _events,
                0);
            Assert.AreEqual(0, n);
            Assert.AreEqual(38d, hype.Hype, 1e-9d);
        }

        [Test]
        public void LateLoad_WhenRequestWindowClosesUnsatisfied()
        {
            var detector = new DeckReactionDetector();
            var hype = new HypeSystem();
            MixSnapshot mix = Playing(0d);

            Assert.AreEqual(
                0,
                detector.Collect(
                    mix,
                    Cues(phrase: 1, requestId: 4, cueReady: false, active: true, satisfied: false),
                    _events,
                    0));

            int n = detector.Collect(
                mix,
                Cues(phrase: 1, requestId: 4, cueReady: false, active: false, satisfied: false),
                _events,
                0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(ScoreKind.LateLoad, _events[0].Kind);
            Assert.AreEqual(-3d, _events[0].HypeDelta, 1e-9d);
            Assert.IsTrue(hype.TryApply(_events[0], out ScoreChange late));
            Assert.AreEqual(32d, late.Hype, 1e-9d);
            Assert.AreEqual(0d, late.Reputation, 1e-9d);

            Assert.AreEqual(
                0,
                detector.Collect(
                    mix,
                    Cues(phrase: 1, requestId: 4, cueReady: false, active: false, satisfied: false),
                    _events,
                    0));
            Assert.IsFalse(hype.TryApply(ScoreEvent.Late(4), out _));
        }

        [Test]
        public void DeadAir_OncePerPhrase_HotSwapOncePerPhrase()
        {
            var detector = new DeckReactionDetector();
            var hype = new HypeSystem();
            MixSnapshot mix = Playing(1d);

            int n = detector.Collect(
                mix,
                Cues(phrase: 2, requestId: 0, deadAir: true, hotSwap: true),
                _events,
                0);
            Assert.AreEqual(2, n);
            Assert.AreEqual(ScoreKind.DeadAir, _events[0].Kind);
            Assert.AreEqual(-5d, _events[0].HypeDelta, 1e-9d);
            Assert.AreEqual(ScoreKind.HotSwapFumble, _events[1].Kind);
            Assert.AreEqual(-4d, _events[1].HypeDelta, 1e-9d);

            Assert.IsTrue(hype.TryApply(_events[0], out _));
            Assert.IsTrue(hype.TryApply(_events[1], out ScoreChange swap));
            Assert.AreEqual(26d, swap.Hype, 1e-9d);

            n = detector.Collect(
                mix,
                Cues(phrase: 2, requestId: 0, deadAir: true, hotSwap: true),
                _events,
                0);
            Assert.AreEqual(0, n);

            n = detector.Collect(
                mix,
                Cues(phrase: 3, requestId: 0, deadAir: true, hotSwap: false),
                _events,
                0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(ScoreKind.DeadAir, _events[0].Kind);
            Assert.AreEqual("dead-air-3", _events[0].Id);
        }

        [Test]
        public void CleanPrep_IsSeparateBonus_TiedToTransitionId()
        {
            var detector = new DeckReactionDetector();
            var hype = new HypeSystem();
            MixSnapshot mix = Playing(2d);
            ScoreEvent clean = ScoreEvent.Clean(0, 0, 1, false);
            Assert.IsTrue(hype.TryApply(clean, out ScoreChange baseClean));
            Assert.AreEqual(43d, baseClean.Hype, 1e-9d);
            Assert.AreEqual(100d, baseClean.Reputation, 1e-9d);

            int n = detector.Collect(
                mix,
                Cues(
                    phrase: 0,
                    requestId: 0,
                    cleanPrep: true,
                    transitionId: clean.Id),
                _events,
                0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(ScoreKind.CleanPrepTransition, _events[0].Kind);
            Assert.AreEqual("prep-" + clean.Id, _events[0].Id);
            Assert.AreEqual(2d, _events[0].HypeDelta, 1e-9d);
            Assert.AreEqual(30d, _events[0].ReputationBase, 1e-9d);

            Assert.IsTrue(hype.TryApply(_events[0], out ScoreChange prep));
            Assert.AreEqual(45d, prep.Hype, 1e-9d);
            Assert.AreEqual(160d, prep.Reputation, 1e-9d);

            n = detector.Collect(
                mix,
                Cues(phrase: 0, requestId: 0, cleanPrep: true, transitionId: clean.Id),
                _events,
                0);
            Assert.AreEqual(0, n);
            Assert.IsFalse(hype.TryApply(ScoreEvent.PrepBonus(clean.Id), out _));
        }

        [Test]
        public void PausedSnapshot_EmitsNothing()
        {
            var detector = new DeckReactionDetector();
            var paused = new MixSnapshot(1d, true, false, false, 0d, 0d, 0d, 0d, "", "", true);
            int n = detector.Collect(
                paused,
                Cues(phrase: 0, requestId: 1, cueReady: true, active: true, satisfied: true, deadAir: true),
                _events,
                0);
            Assert.AreEqual(0, n);
        }

        [Test]
        public void HonorAtWindowEnd_BeatsLateLoad()
        {
            var detector = new DeckReactionDetector();
            MixSnapshot mix = Playing(0d);
            detector.Collect(
                mix,
                Cues(phrase: 0, requestId: 9, cueReady: false, active: true, satisfied: false),
                _events,
                0);

            int n = detector.Collect(
                mix,
                Cues(phrase: 0, requestId: 9, cueReady: true, active: false, satisfied: true),
                _events,
                0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(ScoreKind.CueReadyHonor, _events[0].Kind);
        }

        private static MixSnapshot Playing(double time)
        {
            return new MixSnapshot(time, false, true, false, 1d, 0d, 1d, 0d, "a", "b", true);
        }

        private static DeckReactionCues Cues(
            int phrase,
            int requestId,
            bool cueReady = false,
            bool active = false,
            bool satisfied = false,
            bool deadAir = false,
            bool hotSwap = false,
            bool cleanPrep = false,
            string transitionId = "")
        {
            return new DeckReactionCues(
                phrase,
                requestId,
                cueReady,
                active,
                satisfied,
                deadAir,
                hotSwap,
                cleanPrep,
                transitionId);
        }
    }
}
