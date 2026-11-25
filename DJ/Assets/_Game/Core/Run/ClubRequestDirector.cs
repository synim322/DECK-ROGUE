using DeckRogue.Core.Scoring;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Act1 club request windows. Opens <see cref="RequestPrepB"/> for one phrase
    /// on every second phrase while Performing (parity from seed).
    /// </summary>
    public sealed class ClubRequestDirector
    {
        public const string RequestPrepB = "request_prep_b";

        private readonly int _openParity;
        private readonly int _seed;
        private int _serial;
        private int _requestId;
        private int _activePhrase = -1;
        private bool _active;

        public ClubRequestDirector(int seed)
        {
            _seed = seed;
            _openParity = seed & 1;
            Reset();
        }

        public bool IsActive => _active;

        public int RequestId => _requestId;

        public int ActivePhrase => _activePhrase;

        public string RequestKind => _active ? RequestPrepB : "";

        public void Reset()
        {
            _serial = 0;
            _requestId = 0;
            _activePhrase = -1;
            _active = false;
        }

        /// <summary>
        /// Advances the request window from the current musical phrase.
        /// Active only while <paramref name="performing"/>; lasts exactly one phrase.
        /// </summary>
        public void Tick(bool performing, int phrase)
        {
            if (!performing)
            {
                Close();
                return;
            }

            if (_active && phrase != _activePhrase)
                Close();

            bool shouldOpen = phrase >= 0 && (phrase % 2) == _openParity;
            if (!_active && shouldOpen)
                Open(phrase);
        }

        /// <summary>
        /// Builds cues for <see cref="DeckReactionDetector"/>.
        /// <paramref name="isCueReadyB"/> satisfies an active <see cref="RequestPrepB"/> window.
        /// </summary>
        public DeckReactionCues BuildCues(
            int phrase,
            bool isCueReadyB,
            bool deadAir = false,
            bool hotSwapFumble = false,
            bool cleanPrepEligible = false,
            string transitionEventId = "")
        {
            bool requestActive = _active && phrase == _activePhrase;
            bool satisfied = requestActive && isCueReadyB;
            return new DeckReactionCues(
                phrase,
                _requestId,
                isCueReadyB,
                requestActive,
                satisfied,
                deadAir,
                hotSwapFumble,
                cleanPrepEligible,
                transitionEventId ?? "");
        }

        private void Open(int phrase)
        {
            _serial++;
            _requestId = unchecked((_seed * 397) ^ (_serial * 31) ^ (phrase + 1));
            if (_requestId < 0)
                _requestId = -_requestId;
            if (_requestId == 0)
                _requestId = _serial;
            _activePhrase = phrase;
            _active = true;
        }

        private void Close()
        {
            _active = false;
            _activePhrase = -1;
        }
    }
}
