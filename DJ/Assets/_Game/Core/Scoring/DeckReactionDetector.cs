namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Club reaction cues for deck management (GDD §3.1).
    /// Caller owns MixSnapshot; this layer maps one-shot flags to ScoreEvents.
    /// HypeSystem still dedupes by event id.
    /// </summary>
    public readonly struct DeckReactionCues
    {
        public DeckReactionCues(
            int phrase,
            int requestId,
            bool isCueReadyB,
            bool requestActive,
            bool requestSatisfied,
            bool deadAir,
            bool hotSwapFumble,
            bool cleanPrepEligible,
            string transitionEventId)
        {
            Phrase = phrase;
            RequestId = requestId;
            IsCueReadyB = isCueReadyB;
            RequestActive = requestActive;
            RequestSatisfied = requestSatisfied;
            DeadAir = deadAir;
            HotSwapFumble = hotSwapFumble;
            CleanPrepEligible = cleanPrepEligible;
            TransitionEventId = transitionEventId ?? "";
        }

        public int Phrase { get; }
        public int RequestId { get; }
        public bool IsCueReadyB { get; }
        public bool RequestActive { get; }
        public bool RequestSatisfied { get; }
        public bool DeadAir { get; }
        public bool HotSwapFumble { get; }
        public bool CleanPrepEligible { get; }
        public string TransitionEventId { get; }
    }

    public sealed class DeckReactionDetector
    {
        private bool _requestSeen;
        private int _trackedRequestId = -1;
        private bool _honoredRequest;
        private int _deadAirPhrase = -1;
        private int _hotSwapPhrase = -1;
        private int _honorRequestId = -1;
        private string _prepTransitionId = "";

        public void Reset()
        {
            _requestSeen = false;
            _trackedRequestId = -1;
            _honoredRequest = false;
            _deadAirPhrase = -1;
            _hotSwapPhrase = -1;
            _honorRequestId = -1;
            _prepTransitionId = "";
        }

        /// <summary>
        /// Writes zero or more ScoreEvents into <paramref name="results"/> starting at
        /// <paramref name="offset"/>. Returns how many were written.
        /// </summary>
        public int Collect(MixSnapshot snapshot, DeckReactionCues cues, ScoreEvent[] results, int offset)
        {
            if (results == null || offset < 0 || offset >= results.Length)
                return 0;
            if (snapshot.Paused)
                return 0;

            int written = 0;
            written += TryHonor(cues, results, offset + written);
            written += TryLateLoad(cues, results, offset + written);
            written += TryDeadAir(cues, results, offset + written);
            written += TryHotSwap(cues, results, offset + written);
            written += TryPrepBonus(cues, results, offset + written);
            TrackRequest(cues);
            return written;
        }

        private int TryHonor(DeckReactionCues cues, ScoreEvent[] results, int index)
        {
            if (index >= results.Length)
                return 0;
            // Satisfied at or before window end; requestActive may already be false.
            if (!cues.RequestSatisfied || !cues.IsCueReadyB)
                return 0;
            if (_honorRequestId == cues.RequestId)
                return 0;

            _honorRequestId = cues.RequestId;
            _honoredRequest = true;
            _requestSeen = true;
            _trackedRequestId = cues.RequestId;
            results[index] = ScoreEvent.CueHonor(cues.RequestId);
            return 1;
        }

        private int TryLateLoad(DeckReactionCues cues, ScoreEvent[] results, int index)
        {
            if (index >= results.Length)
                return 0;
            if (!_requestSeen || _honoredRequest)
                return 0;
            if (cues.RequestActive)
                return 0;
            if (_trackedRequestId < 0)
                return 0;

            int requestId = _trackedRequestId;
            _requestSeen = false;
            _trackedRequestId = -1;
            results[index] = ScoreEvent.Late(requestId);
            return 1;
        }

        private int TryDeadAir(DeckReactionCues cues, ScoreEvent[] results, int index)
        {
            if (index >= results.Length)
                return 0;
            if (!cues.DeadAir)
                return 0;
            if (_deadAirPhrase == cues.Phrase)
                return 0;

            _deadAirPhrase = cues.Phrase;
            results[index] = ScoreEvent.DeadAir(cues.Phrase);
            return 1;
        }

        private int TryHotSwap(DeckReactionCues cues, ScoreEvent[] results, int index)
        {
            if (index >= results.Length)
                return 0;
            if (!cues.HotSwapFumble)
                return 0;
            if (_hotSwapPhrase == cues.Phrase)
                return 0;

            _hotSwapPhrase = cues.Phrase;
            results[index] = ScoreEvent.HotSwap(cues.Phrase);
            return 1;
        }

        private int TryPrepBonus(DeckReactionCues cues, ScoreEvent[] results, int index)
        {
            if (index >= results.Length)
                return 0;
            if (!cues.CleanPrepEligible)
                return 0;
            if (cues.TransitionEventId.Length == 0)
                return 0;
            if (_prepTransitionId == cues.TransitionEventId)
                return 0;

            _prepTransitionId = cues.TransitionEventId;
            results[index] = ScoreEvent.PrepBonus(cues.TransitionEventId);
            return 1;
        }

        private void TrackRequest(DeckReactionCues cues)
        {
            if (cues.RequestActive)
            {
                if (!_requestSeen || _trackedRequestId != cues.RequestId)
                {
                    _requestSeen = true;
                    _trackedRequestId = cues.RequestId;
                    _honoredRequest = _honorRequestId == cues.RequestId;
                }

                return;
            }

            if (_requestSeen && !_honoredRequest)
                return;

            _requestSeen = false;
            _trackedRequestId = -1;
            _honoredRequest = false;
        }
    }
}
