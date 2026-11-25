using DeckRogue.Core.Music;
using DeckRogue.Core.Run;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    /// <summary>
    /// MixerExpressionDetector invariants: FilterPhrase / StemPunch / LoopSnap + CrowdMotionImpulse.
    /// </summary>
    public sealed class MixerExpressionTests
    {
        private readonly ScoreChange[] _changes = new ScoreChange[16];
        private readonly ScoreEvent[] _events = new ScoreEvent[8];

        [Test]
        public void FilterPhrase_TravelOverOneBarThenSettle_PaysPlus4Plus50_Once()
        {
            SetJudge judge = Judge();
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
            Assert.AreEqual(CrowdMotionImpulse.Bounce, judge.CrowdMotionImpulse);
            Assert.AreEqual(0, Feed(judge, Mix(15.9d, filterA: 0.20d)));
        }

        [Test]
        public void StemPunch_DipOneBarThenReturnOnBar_PaysPlus3Plus40_CooldownTwoBars()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d, percA: 1d));
            Feed(judge, Mix(0.5d, percA: 0.2d));
            Feed(judge, Mix(2.5d, percA: 0.2d));
            int count = Feed(judge, Mix(4d, percA: 0.9d));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.StemPunch, _changes[0].Kind);
            Assert.AreEqual(3d, _changes[0].HypeDelta, 1e-9d);
            Assert.AreEqual(40d, _changes[0].ReputationDelta, 1e-9d);
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(40d, judge.Reputation, 1e-9d);
            Assert.AreEqual(CrowdMotionImpulse.Bounce, judge.CrowdMotionImpulse);

            Feed(judge, Mix(4.5d, percA: 0.15d));
            Assert.AreEqual(0, Feed(judge, Mix(6d, percA: 0.9d)));
            Assert.AreEqual(38d, judge.Hype, 1e-9d);

            Feed(judge, Mix(6.5d, percA: 0.1d));
            Feed(judge, Mix(8.5d, percA: 0.1d));
            int again = Feed(judge, Mix(10d, percA: 0.85d));
            Assert.AreEqual(1, again);
            Assert.AreEqual(ScoreKind.StemPunch, _changes[0].Kind);
            Assert.AreEqual(41d, judge.Hype, 1e-9d);
        }

        [Test]
        public void LoopSnap_NonDefaultAfterChange_PaysPlus2Plus20_Once()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d, loopBarsA: 8));
            Feed(judge, Mix(1.0d, loopBarsA: 4));
            int count = Feed(judge, Mix(2.0d, loopBarsA: 4, startA: true));

            Assert.AreEqual(1, count);
            Assert.AreEqual(ScoreKind.LoopSnap, _changes[0].Kind);
            Assert.AreEqual(2d, _changes[0].HypeDelta, 1e-9d);
            Assert.AreEqual(20d, _changes[0].ReputationDelta, 1e-9d);
            Assert.AreEqual(37d, judge.Hype, 1e-9d);
            Assert.AreEqual(20d, judge.Reputation, 1e-9d);
            Assert.AreEqual(CrowdMotionImpulse.Nod, judge.CrowdMotionImpulse);
            Assert.AreEqual(0, Feed(judge, Mix(2.1d, loopBarsA: 4, startA: true)));
        }

        [Test]
        public void CrowdMotionImpulse_Api_NoneNodBouncePeak()
        {
            Assert.AreEqual(0, (int)CrowdMotionImpulse.None);
            Assert.AreEqual(1, (int)CrowdMotionImpulse.Nod);
            Assert.AreEqual(2, (int)CrowdMotionImpulse.Bounce);
            Assert.AreEqual(3, (int)CrowdMotionImpulse.Peak);
            Assert.AreEqual(CrowdMotionImpulse.Peak, CrowdMotion.FromKind(ScoreKind.Drop));
            Assert.AreEqual(CrowdMotionImpulse.Bounce, CrowdMotion.FromKind(ScoreKind.FilterPhrase));
            Assert.AreEqual(CrowdMotionImpulse.Bounce, CrowdMotion.FromKind(ScoreKind.StemPunch));
            Assert.AreEqual(CrowdMotionImpulse.Nod, CrowdMotion.FromKind(ScoreKind.LoopSnap));
        }

        [Test]
        public void Act1_CollectMixerExpressions_AndReadoutImpulse()
        {
            var director = new Act1RunDirector(11);
            director.BeginClub();
            Assert.AreEqual(0, director.Readout.CrowdMotionImpulse);

            director.Tick(Mix(0d, filterA: 0.85d));
            director.Tick(Mix(2.0d, filterA: 0.45d));
            director.Tick(Mix(13.5d, filterA: 0.20d));
            director.Tick(Mix(15.8d, filterA: 0.20d));

            Assert.AreEqual((int)CrowdMotionImpulse.Bounce, director.Readout.CrowdMotionImpulse);
            // Opening room starts at target-4 and forgives the soft penalty this gesture used to pay.
            Assert.AreEqual(50d, director.Readout.Hype, 1e-9d);

            int n = director.CollectMixerExpressions(Mix(15.9d, filterA: 0.20d), _events, 0);
            Assert.AreEqual(0, n);
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
            bool startA = false)
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
                startA,
                false,
                1d,
                1d);
        }
    }
}
