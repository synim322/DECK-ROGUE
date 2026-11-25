using DeckRogue.Core.Run;
using NUnit.Framework;

namespace DeckRogue.Core.Tests
{
    public sealed class FirstNightCoachTests
    {
        [Test]
        public void Story_HoldsTheLesson()
        {
            FirstNightCoach coach = new FirstNightCoach();
            CoachSignals signals = Visible();
            signals.StoryOpen = true;
            signals.Performing = true;
            signals.PreClub = false;

            coach.Observe(signals);
            coach.Observe(signals);

            CoachCard card;
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.StartSet, card.Spotlight);
            Assert.IsFalse(coach.IsFinished);
        }

        [Test]
        public void StartSet_AdvancesWhenTheNightBegins()
        {
            FirstNightCoach coach = new FirstNightCoach();
            coach.Observe(Visible());

            CoachSignals live = Visible();
            live.PreClub = false;
            live.Performing = true;
            coach.Observe(live);

            CoachCard card;
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.Crate, card.Spotlight);
        }

        [Test]
        public void AlreadyDone_StaysUntilDismissed()
        {
            FirstNightCoach coach = new FirstNightCoach();
            CoachSignals live = Visible();
            live.PreClub = false;
            live.Performing = true;
            coach.Observe(live);

            CoachCard card;
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.StartSet, card.Spotlight);
            Assert.IsFalse(card.RequiresAction);
            StringAssert.Contains("already running", card.Body);

            coach.Observe(live);
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.StartSet, card.Spotlight);

            live.Acknowledged = true;
            coach.Observe(live);
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.Crate, card.Spotlight);
        }

        [Test]
        public void Crossfader_NeedsARealMoveFromWhereItStarted()
        {
            FirstNightCoach coach = new FirstNightCoach();
            AckUntil(coach, CoachSpotlight.Crossfader);

            CoachSignals parked = Visible();
            parked.Performing = true;
            parked.PreClub = false;
            parked.Crossfader = 0.4f;
            coach.Observe(parked);

            parked.Crossfader = 0.45f;
            coach.Observe(parked);
            CoachCard card;
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.Crossfader, card.Spotlight);

            parked.Crossfader = 0.6f;
            coach.Observe(parked);
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.DeckBStart, card.Spotlight);
        }

        [Test]
        public void HiddenControl_SkipsAfterTheLimit()
        {
            FirstNightCoach coach = new FirstNightCoach();
            CoachSignals hidden = Visible();
            hidden.SpotlightVisible = false;

            for (int i = 0; i < FirstNightCoach.MissingSpotlightLimit - 1; i++)
                coach.Observe(hidden);

            CoachCard card;
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.StartSet, card.Spotlight);

            coach.Observe(hidden);
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.Crate, card.Spotlight);
        }

        [Test]
        public void Pause_CompletesWhenTheMenuOpens()
        {
            FirstNightCoach coach = new FirstNightCoach();
            AckUntil(coach, CoachSpotlight.Pause);
            coach.Observe(Visible());

            CoachSignals paused = Visible();
            paused.Paused = true;
            coach.Observe(paused);
            CoachCard card;
            Assert.IsTrue(coach.TryGetCard(out card));
            Assert.AreEqual(CoachSpotlight.Pause, card.Spotlight);

            coach.Observe(Visible());
            Assert.IsTrue(coach.IsFinished);
        }

        [Test]
        public void LeavingTheBooth_EndsTheLesson()
        {
            FirstNightCoach coach = new FirstNightCoach();
            coach.Observe(Visible());
            CoachSignals gone = Visible();
            gone.InBooth = false;
            gone.PreClub = false;
            coach.Observe(gone);
            Assert.IsTrue(coach.IsFinished);
            Assert.IsTrue(coach.WasShown);
            CoachCard card;
            Assert.IsFalse(coach.TryGetCard(out card));
        }

        [Test]
        public void LeavingBeforeTheFirstCard_DoesNotCountAsShown()
        {
            FirstNightCoach coach = new FirstNightCoach();
            CoachSignals gone = Visible();
            gone.InBooth = false;
            gone.PreClub = false;
            coach.Observe(gone);
            Assert.IsTrue(coach.IsFinished);
            Assert.IsFalse(coach.WasShown);
        }

        [Test]
        public void HiddenSkip_DoesNotCountAsShown()
        {
            // A control that never appears must not burn the lesson: the session
            // persists only when WasShown, and the player never saw a card.
            FirstNightCoach coach = new FirstNightCoach();
            CoachSignals hidden = Visible();
            hidden.SpotlightVisible = false;

            for (int step = 0; step < 9; step++)
            {
                for (int i = 0; i < FirstNightCoach.MissingSpotlightLimit; i++)
                    coach.Observe(hidden);
                Assert.IsFalse(coach.WasShown, "after hidden step " + step);
            }

            Assert.IsTrue(coach.IsFinished);
            Assert.IsFalse(coach.WasShown);
        }

        [Test]
        public void Skip_EndsImmediately()
        {
            FirstNightCoach coach = new FirstNightCoach();
            coach.Skip();
            Assert.IsTrue(coach.IsFinished);
        }

        [Test]
        public void Acknowledge_WalksEverySpotlight()
        {
            FirstNightCoach coach = new FirstNightCoach();
            CoachSpotlight[] order =
            {
                CoachSpotlight.StartSet,
                CoachSpotlight.Crate,
                CoachSpotlight.DeckAStart,
                CoachSpotlight.Crossfader,
                CoachSpotlight.DeckBStart,
                CoachSpotlight.Bass,
                CoachSpotlight.Filter,
                CoachSpotlight.Hype,
                CoachSpotlight.Pause
            };

            for (int i = 0; i < order.Length; i++)
            {
                CoachCard card;
                Assert.IsTrue(coach.TryGetCard(out card), "before show " + i);
                Assert.AreEqual(order[i], card.Spotlight);
                Assert.AreEqual(i + 1, card.Number);
                Assert.AreEqual(order.Length, card.Count);

                coach.Observe(Visible());
                CoachSignals next = Visible();
                next.Acknowledged = true;
                coach.Observe(next);
            }

            Assert.IsTrue(coach.IsFinished);
        }

        private static void AckUntil(FirstNightCoach coach, CoachSpotlight spotlight)
        {
            for (int guard = 0; guard < 12; guard++)
            {
                CoachCard card;
                if (!coach.TryGetCard(out card))
                    Assert.Fail("lesson ended before " + spotlight);
                if (card.Spotlight == spotlight)
                    return;
                coach.Observe(Visible());
                CoachSignals next = Visible();
                next.Acknowledged = true;
                coach.Observe(next);
            }

            Assert.Fail("did not reach " + spotlight);
        }

        private static CoachSignals Visible()
        {
            return new CoachSignals
            {
                InBooth = true,
                SpotlightVisible = true,
                PreClub = true
            };
        }
    }
}
