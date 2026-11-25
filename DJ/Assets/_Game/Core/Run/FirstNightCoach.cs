using System;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Which booth control the first-night lesson is pointing at.
    /// </summary>
    public enum CoachSpotlight
    {
        None = 0,
        StartSet = 1,
        Crate = 2,
        DeckAStart = 3,
        Crossfader = 4,
        DeckBStart = 5,
        Bass = 6,
        Filter = 7,
        Hype = 8,
        Pause = 9
    }

    /// <summary>
    /// One card of the first-club lesson. The view only draws this.
    /// </summary>
    public readonly struct CoachCard
    {
        public CoachCard(
            CoachSpotlight spotlight,
            string title,
            string body,
            bool requiresAction,
            int number,
            int count)
        {
            Spotlight = spotlight;
            Title = title ?? "";
            Body = body ?? "";
            RequiresAction = requiresAction;
            Number = number;
            Count = count;
        }

        public CoachSpotlight Spotlight { get; }
        public string Title { get; }
        public string Body { get; }

        /// <summary>
        /// The highlighted control still needs to be used. False means NEXT dismisses the card
        /// (explanation only, or the player already did it).
        /// </summary>
        public bool RequiresAction { get; }

        public int Number { get; }
        public int Count { get; }
    }

    /// <summary>
    /// What the booth observed this frame. The coach does not read Unity or the mixer.
    /// </summary>
    public struct CoachSignals
    {
        public bool InBooth;
        public bool PreClub;
        public bool Performing;
        public bool StoryOpen;
        public bool Paused;
        public bool SpotlightVisible;
        public bool CratePicked;
        public bool DeckALive;
        public bool DeckBLive;
        public float Crossfader;
        public bool BassKilled;
        public bool MixMoved;
        public bool Acknowledged;
    }

    /// <summary>
    /// First club, first visit: a short lesson that advances when the player does the highlighted action.
    /// Steps already satisfied when they appear stay up until the player dismisses them, so the explanation is still read.
    /// </summary>
    public sealed class FirstNightCoach
    {
        public const int MissingSpotlightLimit = 12;
        private const float FaderMove = 0.12f;

        private readonly Step[] _steps;
        private int _index;
        private bool _finished;
        private bool _shown;
        private bool _everShown;
        private bool _satisfiedOnEntry;
        private bool _faderArmed;
        private float _faderOrigin;
        private bool _pauseUsed;
        private int _missing;

        public FirstNightCoach()
        {
            _steps = BuildSteps();
        }

        public bool IsFinished
        {
            get { return _finished; }
        }

        /// <summary>True once a card has been shown, so a result screen we never taught does not burn the lesson.</summary>
        public bool WasShown
        {
            get { return _everShown; }
        }

        public bool TryGetCard(out CoachCard card)
        {
            if (_finished || _index < 0 || _index >= _steps.Length)
            {
                card = default(CoachCard);
                return false;
            }

            Step step = _steps[_index];
            bool already = _shown && _satisfiedOnEntry;
            string body = already && step.Already.Length > 0 ? step.Already : step.Body;
            card = new CoachCard(
                step.Spotlight,
                step.Title,
                body,
                step.RequiresAction && !already,
                _index + 1,
                _steps.Length);
            return true;
        }

        public void Skip()
        {
            _finished = true;
        }

        public void Observe(CoachSignals signals)
        {
            if (_finished)
                return;

            if (!signals.InBooth)
            {
                _finished = true;
                return;
            }

            if (_index < 0 || _index >= _steps.Length)
            {
                _finished = true;
                return;
            }

            if (signals.StoryOpen)
                return;

            if (signals.Paused)
            {
                if (_shown && _steps[_index].Spotlight == CoachSpotlight.Pause)
                    _pauseUsed = true;
                return;
            }

            if (!signals.SpotlightVisible)
            {
                _missing++;
                if (_missing >= MissingSpotlightLimit)
                    Advance();
                return;
            }

            _missing = 0;
            Step step = _steps[_index];
            if (!_shown)
            {
                _shown = true;
                _everShown = true;
                _satisfiedOnEntry = IsSatisfied(step, signals);
                if (step.Spotlight == CoachSpotlight.Crossfader)
                {
                    _faderOrigin = signals.Crossfader;
                    _faderArmed = true;
                }

                return;
            }

            if (signals.Acknowledged || (!_satisfiedOnEntry && IsSatisfied(step, signals)))
                Advance();
        }

        private void Advance()
        {
            _index++;
            _shown = false;
            _satisfiedOnEntry = false;
            _faderArmed = false;
            _pauseUsed = false;
            _missing = 0;
            if (_index >= _steps.Length)
                _finished = true;
        }

        private bool IsSatisfied(Step step, CoachSignals signals)
        {
            switch (step.Spotlight)
            {
                case CoachSpotlight.StartSet:
                    return signals.Performing;
                case CoachSpotlight.Crate:
                    return signals.CratePicked;
                case CoachSpotlight.DeckAStart:
                    return signals.DeckALive;
                case CoachSpotlight.Crossfader:
                    return _faderArmed && Math.Abs(signals.Crossfader - _faderOrigin) >= FaderMove;
                case CoachSpotlight.DeckBStart:
                    return signals.DeckBLive;
                case CoachSpotlight.Bass:
                    return signals.BassKilled;
                case CoachSpotlight.Filter:
                    return signals.MixMoved;
                case CoachSpotlight.Pause:
                    return _pauseUsed;
                default:
                    return false;
            }
        }

        private static Step[] BuildSteps()
        {
            return new[]
            {
                new Step(
                    CoachSpotlight.StartSet,
                    "Open the night",
                    "START SET starts the clock on this club. Raise HYPE to the target before time runs out. This first room forgives messy mixes.",
                    "The set is already running. START SET is what opens the clock.",
                    true),
                new Step(
                    CoachSpotlight.Crate,
                    "Pick a record",
                    "Tap a chip in the record row. It queues onto Deck A and lands on the next phrase. The crossfader stays where you left it.",
                    "A record is already queued. Those chips are how you pick the next one.",
                    true),
                new Step(
                    CoachSpotlight.DeckAStart,
                    "Put Deck A on the air",
                    "START queues this deck for the next bar. It does not cut Deck B. STOP takes it off. The disc and the name chip swap the record on this deck only.",
                    "Deck A is already on the air or waiting for the bar. START is what queues a silent deck.",
                    true),
                new Step(
                    CoachSpotlight.Crossfader,
                    "Blend the two decks",
                    "Drag the crossfader toward B to bring Deck B up, toward A to pull it back. It stays where you leave it. Loading a record never moves it.",
                    "You already moved the crossfader. Nudge it again if you want, or skip ahead.",
                    true),
                new Step(
                    CoachSpotlight.DeckBStart,
                    "Bring Deck B in",
                    "START on B queues that record for the next bar. Deck A keeps playing until the fader moves over. One deck comes in, the other goes out.",
                    "Deck B is already in. START is how a silent deck joins on the next bar.",
                    true),
                new Step(
                    CoachSpotlight.Bass,
                    "Mute one low end",
                    "BASS kills only the bass on that deck. Two lows at once muddy the floor. Mute the quieter deck. Tap it again to bring the low end back.",
                    "Bass is already muted on a deck. Each BASS button belongs to that deck only.",
                    true),
                new Step(
                    CoachSpotlight.Filter,
                    "Shape the mix",
                    "FILTER rolls off the top of that deck. PERC, MUSIC, and LINE trim those stems. LOOP locks 1, 2, 4, or 8 bars so you can ride a slice.",
                    "You already shaped a deck. FILTER, the stems, and LOOP are the same on both sides.",
                    true),
                new Step(
                    CoachSpotlight.Hype,
                    "Hype is the goal",
                    "The number on the right is HYPE. Clean transitions and drops raise it. A record this room knows lifts the crowd while it is the loudest deck. Hit the target before the clock dies.",
                    "",
                    false),
                new Step(
                    CoachSpotlight.Pause,
                    "Pause whenever",
                    "PAUSE or Esc opens the menu. You can save the night and come back later. You are set. Mix the floor.",
                    "",
                    false)
            };
        }

        private readonly struct Step
        {
            public Step(CoachSpotlight spotlight, string title, string body, string already, bool requiresAction)
            {
                Spotlight = spotlight;
                Title = title ?? "";
                Body = body ?? "";
                Already = already ?? "";
                RequiresAction = requiresAction;
            }

            public CoachSpotlight Spotlight { get; }
            public string Title { get; }
            public string Body { get; }
            public string Already { get; }
            public bool RequiresAction { get; }
        }
    }
}
