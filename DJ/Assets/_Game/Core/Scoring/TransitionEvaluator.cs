using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Scores a real change of the dominant deck, once.
    /// A deck is dominant at gain ≥ 0.65 while the other is ≤ 0.35 and actually playing.
    /// The outgoing deck must have held that for a beat, and the incoming deck must
    /// stay there for a beat, so a slider bounce does not pay.
    /// </summary>
    public sealed class TransitionEvaluator
    {
        public const double DominantGain = 0.65d;
        public const double QuietGain = 0.35d;
        public const double BassOverlap = 0.45d;

        private int _side = -1;
        private double _sideSince;
        private bool _primed;
        private bool _tracking;
        private int _from = -1;
        private double _leftAt;
        private bool _pending;
        private int _to = -1;
        private double _arrivedAt;
        private int _arrivalPhrase;
        private int _arrivalBeatInBar;
        private int _arrivalBarInPhrase;
        private bool _incomingPlaying;
        private bool _bassDirty;
        private double _overlapSince = -1d;
        private int _closedPhrase = -1;
        private int _fussPhrase = -1;
        private int _missPhrase = -1;

        public void Reset()
        {
            _side = -1;
            _sideSince = 0d;
            _primed = false;
            _tracking = false;
            _pending = false;
            _bassDirty = false;
            _overlapSince = -1d;
        }

        public void ResetForNewSet()
        {
            Reset();
            _from = -1;
            _leftAt = 0d;
            _to = -1;
            _arrivedAt = 0d;
            _arrivalPhrase = 0;
            _arrivalBeatInBar = 0;
            _arrivalBarInPhrase = 0;
            _incomingPlaying = false;
            _closedPhrase = -1;
            _fussPhrase = -1;
            _missPhrase = -1;
        }

        public bool TryRead(MixSnapshot snapshot, MusicPosition position, double beatDuration, double barDuration, out ScoreEvent scoreEvent)
        {
            scoreEvent = default;
            int side = SideOf(snapshot);
            if (!_primed)
            {
                _primed = true;
                _side = side;
                _sideSince = snapshot.AudioTime;
                return false;
            }

            TrackOverlap(snapshot, beatDuration);

            if (_pending && side == _to && snapshot.AudioTime - _arrivedAt >= beatDuration)
                return Finish(snapshot, beatDuration, barDuration, out scoreEvent);

            if (_pending && side != _to)
                Cancel();

            if (side == _side)
                return false;

            bool held = snapshot.AudioTime - _sideSince >= beatDuration;
            if (side < 0)
            {
                if (_side >= 0 && held)
                    Begin(_side, snapshot.AudioTime);
                _side = -1;
                _sideSince = snapshot.AudioTime;
                return false;
            }

            if (_side >= 0 && side != _side && held)
                Begin(_side, snapshot.AudioTime);

            if (_tracking && side != _from)
                Arrive(side, snapshot, position);
            else
                Cancel();

            _side = side;
            _sideSince = snapshot.AudioTime;
            return false;
        }

        private void Begin(int from, double audioTime)
        {
            _tracking = true;
            _from = from;
            _leftAt = audioTime;
            _pending = false;
            _bassDirty = false;
            _overlapSince = -1d;
        }

        private void Arrive(int side, MixSnapshot snapshot, MusicPosition position)
        {
            _pending = true;
            _to = side;
            _arrivedAt = snapshot.AudioTime;
            _arrivalPhrase = position.Phrase;
            _arrivalBeatInBar = position.BeatInBar;
            _arrivalBarInPhrase = position.BarInPhrase;
            _incomingPlaying = side == 0 ? snapshot.DeckAPlaying : snapshot.DeckBPlaying;
        }

        private bool Finish(MixSnapshot snapshot, double beatDuration, double barDuration, out ScoreEvent scoreEvent)
        {
            _pending = false;
            _tracking = false;
            double duration = _arrivedAt - _leftAt;
            bool onBar = _arrivalBeatInBar == 0;
            bool onPhrase = onBar && _arrivalBarInPhrase == 0;
            bool hard = duration <= beatDuration + 1e-9d && onPhrase;
            bool smooth = duration + 1e-9d >= barDuration && onBar;
            int timing = hard || smooth ? 100 : 0;
            int bass = _bassDirty ? 0 : 100;
            int continuity = _incomingPlaying ? 100 : 0;
            int club = snapshot.ClubMatch ? 100 : 0;
            int score = (timing * 35 + bass * 35 + continuity * 20 + club * 10) / 100;
            bool clean = score >= 70;

            if (_closedPhrase == _arrivalPhrase)
            {
                if (_fussPhrase == _arrivalPhrase)
                {
                    scoreEvent = default;
                    return false;
                }

                _fussPhrase = _arrivalPhrase;
                scoreEvent = ScoreEvent.Fuss(_arrivalPhrase);
                return true;
            }

            if (!clean)
            {
                if (_missPhrase == _arrivalPhrase)
                {
                    scoreEvent = default;
                    return false;
                }

                _missPhrase = _arrivalPhrase;
                scoreEvent = ScoreEvent.Miss(_arrivalPhrase);
                return true;
            }

            _closedPhrase = _arrivalPhrase;
            scoreEvent = ScoreEvent.Clean(_arrivalPhrase, _from, _to, hard);
            return true;
        }

        private void TrackOverlap(MixSnapshot snapshot, double beatDuration)
        {
            if (!_tracking)
                return;

            bool overlap = snapshot.LowA > BassOverlap && snapshot.LowB > BassOverlap;
            if (!overlap)
            {
                _overlapSince = -1d;
                return;
            }

            if (_overlapSince < 0d)
                _overlapSince = snapshot.AudioTime;
            if (snapshot.AudioTime - _overlapSince >= beatDuration * 2d)
                _bassDirty = true;
        }

        private void Cancel()
        {
            _tracking = false;
            _pending = false;
            _bassDirty = false;
            _overlapSince = -1d;
        }

        private static int SideOf(MixSnapshot snapshot)
        {
            if (snapshot.DeckAPlaying && snapshot.GainA >= DominantGain && snapshot.GainB <= QuietGain)
                return 0;
            if (snapshot.DeckBPlaying && snapshot.GainB >= DominantGain && snapshot.GainA <= QuietGain)
                return 1;
            return -1;
        }
    }
}
