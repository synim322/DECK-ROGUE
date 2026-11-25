using DeckRogue.Core.Music;
using DeckRogue.Core.Run;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class MixGestureTests
    {
        private readonly ScoreChange[] _changes = new ScoreChange[12];
        private readonly ScoreEvent[] _events = new ScoreEvent[8];

        [Test]
        public void FilterPhrase_NearBoundary_PaysOnce()
        {
            SetJudge judge = Judge();
            // ≥1 bar of travel (bar=2s @120), settle near phrase end (16s).
            Feed(judge, Mix(0d, filterA: 0.85d));
            Feed(judge, Mix(2.0d, filterA: 0.45d));
            Feed(judge, Mix(13.5d, filterA: 0.20d));
            int count = Feed(judge, Mix(15.8d, filterA: 0.20d));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.FilterPhrase, _changes[0].Kind);
            Assert.AreEqual(4d, _changes[0].HypeDelta, 1e-9d);
            Assert.AreEqual(50d, _changes[0].ReputationDelta, 1e-9d);
            Assert.AreEqual(39d, judge.Hype, 1e-9d);
            Assert.AreEqual(50d, judge.Reputation, 1e-9d);
            Assert.AreEqual(1, judge.CheerIntensity);
            Assert.AreEqual(CrowdMotionImpulse.Bounce, judge.CrowdMotionImpulse);

            Assert.AreEqual(0, Feed(judge, Mix(15.9d, filterA: 0.20d)));
            Assert.AreEqual(39d, judge.Hype, 1e-9d);
            Assert.AreEqual(1, judge.CheerIntensity);
        }

        [Test]
        public void FilterPhrase_WithoutTravel_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d, filterA: 0.1d));
            Feed(judge, Mix(0.2d, filterA: 0.12d));
            int count = Feed(judge, Mix(0.4d, filterA: 0.13d));
            Assert.AreEqual(0, count);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Assert.AreEqual(0, judge.CheerIntensity);
            Assert.AreEqual(CrowdMotionImpulse.None, judge.CrowdMotionImpulse);
        }

        [Test]
        public void FilterPhrase_ShortKnobSpam_NoScoreWithoutOneBar()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d, filterA: 0d));
            Feed(judge, Mix(15.2d, filterA: 0.4d));
            int count = Feed(judge, Mix(15.8d, filterA: 0.55d));
            Assert.AreEqual(0, count);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
        }

        [Test]
        public void StemPunch_AfterBarOfDipThenReturn_PaysAndBumpsCrowd()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d, percA: 0.9d));
            Feed(judge, Mix(0.1d, percA: 0.2d));
            Feed(judge, Mix(2.1d, percA: 0.2d));
            // Next bar downbeat after ≥1 bar of dip (bar = 2s @ 120 BPM).
            int count = Feed(judge, Mix(4.0d, percA: 0.85d));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.StemPunch, _changes[0].Kind);
            Assert.AreEqual(3d, _changes[0].HypeDelta, 1e-9d);
            Assert.AreEqual(40d, _changes[0].ReputationDelta, 1e-9d);
            Assert.AreEqual(1, judge.CheerIntensity);
            Assert.AreEqual(CrowdMotionImpulse.Bounce, judge.CrowdMotionImpulse);
        }

        [Test]
        public void LoopSnap_OnQuantizedStartAfterLengthChange_PaysOnce()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d, loopBarsA: 8));
            Feed(judge, Mix(1.0d, loopBarsA: 4));
            int count = Feed(judge, Mix(2.0d, loopBarsA: 4, quantizedA: true));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.LoopSnap, _changes[0].Kind);
            Assert.AreEqual(2d, _changes[0].HypeDelta, 1e-9d);
            Assert.AreEqual(20d, _changes[0].ReputationDelta, 1e-9d);
            Assert.AreEqual(1, judge.CheerIntensity);
            Assert.AreEqual(CrowdMotionImpulse.Nod, judge.CrowdMotionImpulse);

            Assert.AreEqual(0, Feed(judge, Mix(2.1d, loopBarsA: 4, quantizedA: true)));
        }

        [Test]
        public void CrowdEnergy_BumpsOnListedKinds_AndDecaysAfterHoldBars()
        {
            var crowd = new CrowdEnergy();
            Assert.AreEqual(0, crowd.CheerIntensity);

            crowd.Note(ScoreKind.CleanTransition);
            Assert.AreEqual(1, crowd.CheerIntensity);
            crowd.Note(ScoreKind.Drop);
            Assert.AreEqual(2, crowd.CheerIntensity);
            crowd.Note(ScoreKind.StemPunch);
            Assert.AreEqual(3, crowd.CheerIntensity);
            crowd.Note(ScoreKind.LoopSnap);
            Assert.AreEqual(3, crowd.CheerIntensity);

            crowd.AdvanceBar();
            crowd.AdvanceBar();
            Assert.AreEqual(3, crowd.CheerIntensity);
            crowd.AdvanceBar();
            Assert.AreEqual(2, crowd.CheerIntensity);
            crowd.AdvanceBar();
            Assert.AreEqual(1, crowd.CheerIntensity);
            crowd.AdvanceBar();
            Assert.AreEqual(0, crowd.CheerIntensity);
        }

        [Test]
        public void CrowdMotion_MapsMixerDepthKinds()
        {
            Assert.AreEqual(CrowdMotionImpulse.Bounce, CrowdMotion.FromKind(ScoreKind.FilterPhrase));
            Assert.AreEqual(CrowdMotionImpulse.Bounce, CrowdMotion.FromKind(ScoreKind.StemPunch));
            Assert.AreEqual(CrowdMotionImpulse.Nod, CrowdMotion.FromKind(ScoreKind.LoopSnap));
            Assert.AreEqual(CrowdMotionImpulse.Peak, CrowdMotion.FromKind(ScoreKind.Drop));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.FilterPhrase));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.StemPunch));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.LoopSnap));
        }

        [Test]
        public void Act1Readout_ExposesCheerIntensity_AfterFilterPhrase()
        {
            var director = new Act1RunDirector(11);
            director.BeginClub();
            Assert.AreEqual(0, director.Readout.CheerIntensity);

            director.Tick(Mix(0d));
            Assert.AreEqual(0, director.Readout.CheerIntensity);

            var hype = new HypeSystem();
            Assert.IsTrue(hype.TryApply(ScoreEvent.FilterPhrase(0), out _));
            Assert.AreEqual(1, hype.CheerIntensity);
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.FilterPhrase));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.CueReadyHonor));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.CleanPrepTransition));
        }

        [Test]
        public void MixerExpressionDetector_CollectsFilterPhraseDirectly()
        {
            var detector = new MixerExpressionDetector();
            var time = new ManualTimeProvider();
            var clock = new MusicClock(time, 120d, 0d, 0d);

            Assert.AreEqual(0, detector.Collect(Mix(0d, filterA: 0.85d), clock.GetPosition(0d), clock.PhraseDuration, _events, 0));
            Assert.AreEqual(0, detector.Collect(Mix(2.0d, filterA: 0.40d), clock.GetPosition(2.0d), clock.PhraseDuration, _events, 0));
            Assert.AreEqual(0, detector.Collect(Mix(13.5d, filterA: 0.20d), clock.GetPosition(13.5d), clock.PhraseDuration, _events, 0));

            int n = detector.Collect(Mix(15.8d, filterA: 0.20d), clock.GetPosition(15.8d), clock.PhraseDuration, _events, 0);
            Assert.AreEqual(1, n);
            Assert.AreEqual(ScoreKind.FilterPhrase, _events[0].Kind);
            Assert.AreEqual(4d, _events[0].HypeDelta, 1e-9d);
            Assert.AreEqual(50d, _events[0].ReputationBase, 1e-9d);
        }

        private int Feed(SetJudge judge, MixSnapshot snapshot)
        {
            return judge.Observe(snapshot, _changes);
        }

        private static SetJudge Judge()
        {
            var time = new ManualTimeProvider();
            var clock = new MusicClock(time, 120d, 0d, 0d);
            return new SetJudge(clock);
        }

        private static MixSnapshot Mix(
            double time,
            double lowA = 1d,
            double filterA = 0d,
            double percA = 1d,
            double musicA = 1d,
            int loopBarsA = MixSnapshot.DefaultLoopBars,
            bool quantizedA = false)
        {
            return new MixSnapshot(
                time,
                false,
                true,
                false,
                1d,
                0d,
                lowA,
                0d,
                "track_neon_pulse",
                "track_afterhours",
                true,
                filterA,
                0d,
                percA,
                1d,
                musicA,
                1d,
                loopBarsA,
                MixSnapshot.DefaultLoopBars,
                quantizedA,
                false,
                1d,
                1d);
        }
    }
}
