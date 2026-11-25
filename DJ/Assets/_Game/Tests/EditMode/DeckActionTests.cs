using DeckRogue.Core.Music;
using DeckRogue.Core.Scoring;
using DeckRogue.Core.Time;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    /// <summary>
    /// DeckActionDetector: PhraseStart, BassHandoff, LineLift.
    /// Stays inside phrase 0 (t &lt; 16) so PhraseDecay does not land in the batch.
    /// </summary>
    public sealed class DeckActionTests
    {
        private readonly ScoreChange[] _changes = new ScoreChange[8];

        [Test]
        public void PhraseStart_OnPhrase_PaysOnce()
        {
            SetJudge judge = Judge();
            int count = Feed(judge, Mix(0d, startB: true));

            ScoreChange change = Require(count, ScoreKind.PhraseStart);
            Assert.AreEqual("phrase_start-0-1", change.Id);
            Assert.AreEqual(2d, change.HypeDelta, 1e-9d);
            Assert.AreEqual(25d, change.ReputationDelta, 1e-9d);
            Assert.AreEqual(37d, judge.Hype, 1e-9d);
            Assert.AreEqual(25d, judge.Reputation, 1e-9d);
            Assert.AreEqual(CrowdMotionImpulse.Nod, judge.CrowdMotionImpulse);

            // 0.4s is still beat 0 of bar 0, so the flag is still a legal phrase start.
            Assert.AreEqual(0, Feed(judge, Mix(0.4d, startB: true)));
            Assert.AreEqual(37d, judge.Hype, 1e-9d);
            Assert.AreEqual(25d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void PhraseStart_OffPhrase_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0d));
            int count = Feed(judge, Mix(2d, startB: true));

            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Assert.AreEqual(0d, judge.Reputation, 1e-9d);
            Absent(count, ScoreKind.PhraseStart);
        }

        [Test]
        public void PhraseStart_SilentDeck_DoesNotScore()
        {
            SetJudge judge = Judge();
            int count = Feed(judge, Mix(0d, deckB: false, gainB: 0d, startB: true));

            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Assert.AreEqual(0d, judge.Reputation, 1e-9d);
            Absent(count, ScoreKind.PhraseStart);
        }

        [Test]
        public void BassHandoff_OneLowForABar_PaysOnce()
        {
            SetJudge judge = Judge();
            Feed(judge, Handoff(0.1d));
            int count = Feed(judge, Handoff(2d));

            ScoreChange change = Require(count, ScoreKind.BassHandoff);
            Assert.AreEqual("bass_handoff-0", change.Id);
            Assert.AreEqual(3d, change.HypeDelta, 1e-9d);
            Assert.AreEqual(35d, change.ReputationDelta, 1e-9d);
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(35d, judge.Reputation, 1e-9d);
            Assert.AreEqual(CrowdMotionImpulse.Nod, judge.CrowdMotionImpulse);

            Assert.AreEqual(0, Feed(judge, Handoff(4d)));
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(35d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void BassHandoff_SingleDeck_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Handoff(0.1d, deckB: false, gainB: 0d));
            int count = Feed(judge, Handoff(2d, deckB: false, gainB: 0d));

            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Absent(count, ScoreKind.BassHandoff);
        }

        [Test]
        public void LineLift_TravelOverBar_PaysOnce()
        {
            SetJudge judge = Judge();
            Feed(judge, Line(0.1d, 0.2d));
            Assert.AreEqual(35d, judge.Hype, 1e-9d);

            // Arm at 0.1s. A full bar is 2s, so 2.0s is still short; 2.1s is a full bar and still beat 0.
            Feed(judge, Line(2d, 1d));
            Assert.AreEqual(35d, judge.Hype, 1e-9d);

            int count = Feed(judge, Line(2.1d, 1d));
            ScoreChange change = Require(count, ScoreKind.LineLift);
            Assert.AreEqual("line_lift-0-0", change.Id);
            Assert.AreEqual(3d, change.HypeDelta, 1e-9d);
            Assert.AreEqual(40d, change.ReputationDelta, 1e-9d);
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(40d, judge.Reputation, 1e-9d);
            Assert.AreEqual(CrowdMotionImpulse.Bounce, judge.CrowdMotionImpulse);

            Assert.AreEqual(0, Feed(judge, Line(4d, 1d)));
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(40d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void LineLift_BlipThenWait_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Line(0.1d, 0.2d));
            Feed(judge, Line(0.2d, 1d));
            int count = Feed(judge, Line(2.1d, 1d));

            Absent(count, ScoreKind.LineLift);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
        }

        [Test]
        public void LineLift_CrossfaderOnly_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(0.1d, deckB: false, gainB: 0d, gainA: 0.1d, channelA: 1d));
            int count = Feed(judge, Mix(2.1d, deckB: false, gainB: 0d, gainA: 1d, channelA: 1d));

            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Absent(count, ScoreKind.LineLift);
        }

        [Test]
        public void PhraseStart_SameDeckAsLineLift_PaysLineOnly()
        {
            SetJudge judge = Judge();
            Feed(judge, Mix(14d, channelA: 0.2d, startB: false));
            int count = Feed(judge, Mix(16d, channelA: 1d, startA: true));

            Require(count, ScoreKind.LineLift);
            Absent(count, ScoreKind.PhraseStart);
            Assert.AreEqual(36d, judge.Hype, 1e-9d);
        }

        [Test]
        public void CrowdEnergy_BumpsOnDeckActions()
        {
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.PhraseStart));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.BassHandoff));
            Assert.IsTrue(CrowdEnergy.Bumps(ScoreKind.LineLift));
            Assert.AreEqual(CrowdMotionImpulse.Nod, CrowdMotion.FromKind(ScoreKind.PhraseStart));
            Assert.AreEqual(CrowdMotionImpulse.Nod, CrowdMotion.FromKind(ScoreKind.BassHandoff));
            Assert.AreEqual(CrowdMotionImpulse.Bounce, CrowdMotion.FromKind(ScoreKind.LineLift));
        }

        [Test]
        public void BassHandoff_OffBarBoundary_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Handoff(0.6d));

            // 0.6s is beat 1. 2.7s is a later bar but still beat 1 — not the downbeat.
            int early = Feed(judge, Handoff(2.7d));
            Absent(early, ScoreKind.BassHandoff);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);

            int count = Feed(judge, Handoff(4d));
            ScoreChange change = Require(count, ScoreKind.BassHandoff);
            Assert.AreEqual("bass_handoff-0", change.Id);
            Assert.AreEqual(3d, change.HypeDelta, 1e-9d);
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(1, CountKind(count, ScoreKind.BassHandoff));
        }

        [Test]
        public void LineLift_OffBarBoundary_DoesNotScore()
        {
            SetJudge judge = Judge();
            Feed(judge, Line(0.6d, 0.2d));

            int early = Feed(judge, Line(2.7d, 1d));
            Absent(early, ScoreKind.LineLift);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);

            int count = Feed(judge, Line(4d, 1d));
            ScoreChange change = Require(count, ScoreKind.LineLift);
            Assert.AreEqual("line_lift-0-0", change.Id);
            Assert.AreEqual(3d, change.HypeDelta, 1e-9d);
            Assert.AreEqual(38d, judge.Hype, 1e-9d);
            Assert.AreEqual(1, CountKind(count, ScoreKind.LineLift));
        }

        [Test]
        public void PausedHold_DoesNotPayAcrossResume()
        {
            SetJudge judge = Judge();
            Feed(judge, Handoff(0.1d));

            Assert.AreEqual(0, Feed(judge, Handoff(2d, paused: true)));
            Assert.AreEqual(35d, judge.Hype, 1e-9d);

            // Clock moved through the bar while the snapshot was paused. Resume must not bank it.
            int count = Feed(judge, Handoff(2.1d));
            Absent(count, ScoreKind.BassHandoff);
            Assert.AreEqual(35d, judge.Hype, 1e-9d);
            Assert.AreEqual(0d, judge.Reputation, 1e-9d);
        }

        [Test]
        public void PhraseStart_ClampsHypeAt100_AndRepeatIdDoesNotStack()
        {
            SetJudge judge = Judge();
            judge.BeginNextSet(99d);

            int count = Feed(judge, Mix(0d, startB: true));
            ScoreChange change = Require(count, ScoreKind.PhraseStart);
            Assert.AreEqual(2d, change.HypeDelta, 1e-9d);
            Assert.AreEqual(100d, judge.Hype, 1e-9d);
            Assert.GreaterOrEqual(judge.Hype, 0d);
            Assert.LessOrEqual(judge.Hype, 100d);

            Assert.AreEqual(0, Feed(judge, Mix(0.2d, startB: true)));
            Assert.AreEqual(100d, judge.Hype, 1e-9d);
        }

        private int Feed(SetJudge judge, MixSnapshot snapshot)
        {
            return judge.Observe(snapshot, _changes);
        }

        private ScoreChange Require(int count, ScoreKind kind)
        {
            for (int i = 0; i < count; i++)
            {
                if (_changes[i].Kind == kind)
                    return _changes[i];
            }

            Assert.Fail("Missing " + kind);
            return default(ScoreChange);
        }

        private void Absent(int count, ScoreKind kind)
        {
            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(kind, _changes[i].Kind);
        }

        private int CountKind(int count, ScoreKind kind)
        {
            int found = 0;
            for (int i = 0; i < count; i++)
            {
                if (_changes[i].Kind == kind)
                    found++;
            }

            return found;
        }

        private static SetJudge Judge()
        {
            var time = new ManualTimeProvider();
            var clock = new MusicClock(time, 120d, 0d, 0d);
            return new SetJudge(clock);
        }

        private static MixSnapshot Mix(
            double time,
            bool deckA = true,
            bool deckB = true,
            double gainA = 0.7d,
            double gainB = 0.7d,
            double lowA = 0.3d,
            double lowB = 0.3d,
            double channelA = 1d,
            double channelB = 1d,
            bool startA = false,
            bool startB = false,
            bool paused = false)
        {
            return new MixSnapshot(
                time,
                paused,
                deckA,
                deckB,
                gainA,
                gainB,
                lowA,
                lowB,
                "track_neon_pulse",
                "track_afterhours",
                true,
                0d,
                0d,
                1d,
                1d,
                1d,
                1d,
                MixSnapshot.DefaultLoopBars,
                MixSnapshot.DefaultLoopBars,
                startA,
                startB,
                channelA,
                channelB);
        }

        private static MixSnapshot Handoff(
            double time,
            bool deckB = true,
            double gainB = 0.7d,
            bool paused = false)
        {
            return Mix(time, deckB: deckB, gainB: gainB, lowA: 0.9d, lowB: 0d, paused: paused);
        }

        private static MixSnapshot Line(double time, double channelA)
        {
            return Mix(time, deckB: false, gainB: 0d, channelA: channelA);
        }
    }
}
