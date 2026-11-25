using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Both LOW stems louder than 0.45 for two beats.
    /// One overlap frame is ignored. The penalty repeats at most once per phrase.
    /// </summary>
    public sealed class BassConflictDetector
    {
        public const double Threshold = 0.45d;

        private double _hotSince = -1d;
        private int _lastPhrase = -1;

        public void ResetSustain()
        {
            _hotSince = -1d;
        }

        public void Reset()
        {
            ResetSustain();
            _lastPhrase = -1;
        }

        public bool TryRead(MixSnapshot snapshot, MusicPosition position, double beatDuration, out ScoreEvent scoreEvent)
        {
            scoreEvent = default;
            bool hot = snapshot.DeckAPlaying
                && snapshot.DeckBPlaying
                && snapshot.LowA > Threshold
                && snapshot.LowB > Threshold;
            if (!hot)
            {
                _hotSince = -1d;
                return false;
            }

            if (_hotSince < 0d)
                _hotSince = snapshot.AudioTime;
            if (snapshot.AudioTime - _hotSince < beatDuration * 2d)
                return false;
            if (position.Phrase == _lastPhrase)
                return false;

            _lastPhrase = position.Phrase;
            scoreEvent = ScoreEvent.Bass(position.Phrase);
            return true;
        }
    }
}
