using System;
using DeckRogue.Core.Music;

namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// Mixer depth expressions: FilterPhrase, StemPunch, LoopSnap (GDD §3.4).
    /// Anti-farm: filter/stem need audible Δ over ≥1 bar; LoopSnap once per bar on quantize.
    /// </summary>
    public sealed class MixerExpressionDetector
    {
        public const double AudibleGain = 0.35d;
        public const double FilterTravelMin = 0.35d;
        public const double FilterSettleEpsilon = 0.05d;
        public const double PhraseWindowSeconds = 0.5d;
        public const double StemDipThreshold = 0.3d;
        public const double StemReturnThreshold = 0.75d;
        public const int StemPunchCooldownBars = 2;
        public const int MinAudibleChangeBars = 1;
        public const int BarsPerPhrase = 8;

        private bool _haveSample;
        private double _filterA;
        private double _filterB;
        private double _percA;
        private double _percB;
        private double _musicA;
        private double _musicB;
        private int _loopBarsA = MixSnapshot.DefaultLoopBars;
        private int _loopBarsB = MixSnapshot.DefaultLoopBars;

        private int _phrase = -1;
        private double _phraseOriginA;
        private double _phraseOriginB;
        private double _phraseTravelA;
        private double _phraseTravelB;
        private double _travelStartA = -1d;
        private double _travelStartB = -1d;
        private int _filterPhrase = -1;

        private bool _percDipped;
        private bool _musicDipped;
        private double _stemDipStart = -1d;
        private int _punchCooldownUntilBar = -1;

        private bool _loopChangedA;
        private bool _loopChangedB;
        private int _loopSnapBar = -1;
        private int _loopSnapPhrase = -1;

        public void Reset()
        {
            _haveSample = false;
            _filterA = 0d;
            _filterB = 0d;
            _percA = 1d;
            _percB = 1d;
            _musicA = 1d;
            _musicB = 1d;
            _loopBarsA = MixSnapshot.DefaultLoopBars;
            _loopBarsB = MixSnapshot.DefaultLoopBars;
            _phrase = -1;
            _phraseOriginA = 0d;
            _phraseOriginB = 0d;
            _phraseTravelA = 0d;
            _phraseTravelB = 0d;
            _travelStartA = -1d;
            _travelStartB = -1d;
            _filterPhrase = -1;
            _percDipped = false;
            _musicDipped = false;
            _stemDipStart = -1d;
            _punchCooldownUntilBar = -1;
            _loopChangedA = false;
            _loopChangedB = false;
            _loopSnapBar = -1;
            _loopSnapPhrase = -1;
        }

        /// <summary>
        /// Optional entry when audio feeds a dedicated expression struct instead of MixSnapshot.
        /// </summary>
        public int Collect(
            MixerExpressionSnapshot expression,
            MusicPosition position,
            double phraseDuration,
            ScoreEvent[] results,
            int offset)
        {
            return Collect(expression.ToMix(), position, phraseDuration, results, offset);
        }

        /// <summary>
        /// Writes zero or more ScoreEvents into <paramref name="results"/> starting at
        /// <paramref name="offset"/>. Returns how many were written.
        /// </summary>
        public int Collect(
            MixSnapshot snapshot,
            MusicPosition position,
            double phraseDuration,
            ScoreEvent[] results,
            int offset)
        {
            if (results == null || offset < 0 || offset >= results.Length)
                return 0;
            if (snapshot.Paused)
                return 0;

            SyncPhraseWindow(snapshot, position);

            int written = 0;
            written += TryFilterPhrase(snapshot, position, phraseDuration, results, offset + written);
            written += TryStemPunch(snapshot, position, phraseDuration, results, offset + written);
            written += TryLoopSnap(snapshot, position, results, offset + written);
            Remember(snapshot);
            return written;
        }

        private void SyncPhraseWindow(MixSnapshot snapshot, MusicPosition position)
        {
            if (_phrase != position.Phrase)
            {
                _phrase = position.Phrase;
                _phraseOriginA = snapshot.FilterA;
                _phraseOriginB = snapshot.FilterB;
                _phraseTravelA = 0d;
                _phraseTravelB = 0d;
                _travelStartA = -1d;
                _travelStartB = -1d;
            }

            double stepA = Math.Abs(snapshot.FilterA - _phraseOriginA);
            double stepB = Math.Abs(snapshot.FilterB - _phraseOriginB);
            if (stepA > _phraseTravelA + 1e-9d && _travelStartA < 0d)
                _travelStartA = snapshot.AudioTime;
            if (stepB > _phraseTravelB + 1e-9d && _travelStartB < 0d)
                _travelStartB = snapshot.AudioTime;
            _phraseTravelA = Math.Max(_phraseTravelA, stepA);
            _phraseTravelB = Math.Max(_phraseTravelB, stepB);
        }

        private int TryFilterPhrase(
            MixSnapshot snapshot,
            MusicPosition position,
            double phraseDuration,
            ScoreEvent[] results,
            int index)
        {
            if (index >= results.Length)
                return 0;
            if (_filterPhrase == position.Phrase)
                return 0;
            if (!NearPhraseBoundary(position, phraseDuration))
                return 0;

            double minSeconds = BarSeconds(phraseDuration) * MinAudibleChangeBars;
            bool a = CanFilterPhrase(
                snapshot.FilterA,
                _filterA,
                _phraseTravelA,
                _travelStartA,
                snapshot.AudioTime,
                minSeconds,
                snapshot.DeckAPlaying,
                snapshot.GainA);
            bool b = CanFilterPhrase(
                snapshot.FilterB,
                _filterB,
                _phraseTravelB,
                _travelStartB,
                snapshot.AudioTime,
                minSeconds,
                snapshot.DeckBPlaying,
                snapshot.GainB);
            if (!a && !b)
                return 0;

            _filterPhrase = position.Phrase;
            results[index] = ScoreEvent.FilterPhrase(position.Phrase);
            return 1;
        }

        private bool CanFilterPhrase(
            double filter,
            double previous,
            double travel,
            double travelStart,
            double audioTime,
            double minSeconds,
            bool playing,
            double gain)
        {
            if (!_haveSample)
                return false;
            if (travel + 1e-9d < FilterTravelMin)
                return false;
            if (travelStart < 0d || audioTime - travelStart + 1e-9d < minSeconds)
                return false;
            if (!playing || gain < AudibleGain)
                return false;

            // Settled: little motion on this sample after the travel accumulated in-phrase.
            if (Math.Abs(filter - previous) > FilterSettleEpsilon)
                return false;

            return true;
        }

        private int TryStemPunch(
            MixSnapshot snapshot,
            MusicPosition position,
            double phraseDuration,
            ScoreEvent[] results,
            int index)
        {
            if (index >= results.Length)
                return 0;

            double perc = DominantStem(snapshot, true);
            double music = DominantStem(snapshot, false);
            bool audible = (snapshot.DeckAPlaying && snapshot.GainA >= AudibleGain)
                || (snapshot.DeckBPlaying && snapshot.GainB >= AudibleGain);

            if (!audible)
            {
                _percDipped = false;
                _musicDipped = false;
                _stemDipStart = -1d;
                return 0;
            }

            if (perc < StemDipThreshold || music < StemDipThreshold)
            {
                if (_stemDipStart < 0d)
                    _stemDipStart = snapshot.AudioTime;
                if (perc < StemDipThreshold)
                    _percDipped = true;
                if (music < StemDipThreshold)
                    _musicDipped = true;
            }

            if (position.BeatInBar != 0)
                return 0;

            bool percReturn = _percDipped && perc > StemReturnThreshold;
            bool musicReturn = _musicDipped && music > StemReturnThreshold;
            if (!percReturn && !musicReturn)
                return 0;

            double minSeconds = BarSeconds(phraseDuration) * MinAudibleChangeBars;
            if (_stemDipStart < 0d || snapshot.AudioTime - _stemDipStart + 1e-9d < minSeconds)
            {
                _percDipped = false;
                _musicDipped = false;
                _stemDipStart = -1d;
                return 0;
            }

            bool cooled = _punchCooldownUntilBar < 0 || position.Bar >= _punchCooldownUntilBar;
            if (!cooled)
            {
                _percDipped = false;
                _musicDipped = false;
                _stemDipStart = -1d;
                return 0;
            }

            _punchCooldownUntilBar = position.Bar + StemPunchCooldownBars;
            _percDipped = false;
            _musicDipped = false;
            _stemDipStart = -1d;
            results[index] = ScoreEvent.StemPunch(position.Bar);
            return 1;
        }

        private int TryLoopSnap(MixSnapshot snapshot, MusicPosition position, ScoreEvent[] results, int index)
        {
            if (index >= results.Length)
                return 0;

            if (_haveSample)
            {
                if (snapshot.LoopBarsA != _loopBarsA)
                    _loopChangedA = snapshot.LoopBarsA != MixSnapshot.DefaultLoopBars;
                if (snapshot.LoopBarsB != _loopBarsB)
                    _loopChangedB = snapshot.LoopBarsB != MixSnapshot.DefaultLoopBars;
            }

            bool snapA = snapshot.QuantizedStartA
                && _loopChangedA
                && snapshot.LoopBarsA != MixSnapshot.DefaultLoopBars;
            bool snapB = snapshot.QuantizedStartB
                && _loopChangedB
                && snapshot.LoopBarsB != MixSnapshot.DefaultLoopBars;
            if (!snapA && !snapB)
                return 0;
            if (_loopSnapBar == position.Bar || _loopSnapPhrase == position.Phrase)
                return 0;

            int bars = snapA ? snapshot.LoopBarsA : snapshot.LoopBarsB;
            if (snapA)
                _loopChangedA = false;
            if (snapB)
                _loopChangedB = false;
            _loopSnapBar = position.Bar;
            _loopSnapPhrase = position.Phrase;
            results[index] = ScoreEvent.LoopSnap(position.Bar, bars);
            return 1;
        }

        private static double BarSeconds(double phraseDuration)
        {
            if (phraseDuration <= 0d)
                return 2d;
            return phraseDuration / BarsPerPhrase;
        }

        private void Remember(MixSnapshot snapshot)
        {
            _haveSample = true;
            _filterA = snapshot.FilterA;
            _filterB = snapshot.FilterB;
            _percA = snapshot.PercA;
            _percB = snapshot.PercB;
            _musicA = snapshot.MusicA;
            _musicB = snapshot.MusicB;
            _loopBarsA = snapshot.LoopBarsA;
            _loopBarsB = snapshot.LoopBarsB;
        }

        private static double DominantStem(MixSnapshot snapshot, bool perc)
        {
            bool aLead = snapshot.DeckAPlaying && snapshot.GainA >= AudibleGain
                && snapshot.GainA >= snapshot.GainB;
            bool bLead = snapshot.DeckBPlaying && snapshot.GainB >= AudibleGain
                && snapshot.GainB > snapshot.GainA;
            if (aLead)
                return perc ? snapshot.PercA : snapshot.MusicA;
            if (bLead)
                return perc ? snapshot.PercB : snapshot.MusicB;

            // Blend / single audible: take the louder audible deck's stem.
            if (snapshot.DeckAPlaying && snapshot.GainA >= AudibleGain)
                return perc ? snapshot.PercA : snapshot.MusicA;
            if (snapshot.DeckBPlaying && snapshot.GainB >= AudibleGain)
                return perc ? snapshot.PercB : snapshot.MusicB;
            return perc ? 1d : 1d;
        }

        private static bool NearPhraseBoundary(MusicPosition position, double phraseDuration)
        {
            if (phraseDuration <= 0d)
                return false;

            double phraseStart = position.Phrase * phraseDuration;
            double into = position.AudioTime - phraseStart;
            if (into < 0d)
                into = 0d;
            double toEnd = phraseDuration - into;
            if (toEnd < 0d)
                toEnd = 0d;
            return into <= PhraseWindowSeconds || toEnd <= PhraseWindowSeconds;
        }
    }

    /// <summary>
    /// Compact mixer expression view when audio prefers not to widen MixSnapshot mid-wire.
    /// </summary>
    public readonly struct MixerExpressionSnapshot
    {
        public MixerExpressionSnapshot(
            double audioTime,
            bool paused,
            bool deckAPlaying,
            bool deckBPlaying,
            double gainA,
            double gainB,
            double filterA,
            double filterB,
            double percA,
            double percB,
            double musicA,
            double musicB,
            int loopBarsA,
            int loopBarsB,
            bool quantizedStartA,
            bool quantizedStartB)
        {
            AudioTime = audioTime;
            Paused = paused;
            DeckAPlaying = deckAPlaying;
            DeckBPlaying = deckBPlaying;
            GainA = gainA;
            GainB = gainB;
            FilterA = filterA;
            FilterB = filterB;
            PercA = percA;
            PercB = percB;
            MusicA = musicA;
            MusicB = musicB;
            LoopBarsA = loopBarsA;
            LoopBarsB = loopBarsB;
            QuantizedStartA = quantizedStartA;
            QuantizedStartB = quantizedStartB;
        }

        public double AudioTime { get; }
        public bool Paused { get; }
        public bool DeckAPlaying { get; }
        public bool DeckBPlaying { get; }
        public double GainA { get; }
        public double GainB { get; }
        public double FilterA { get; }
        public double FilterB { get; }
        public double PercA { get; }
        public double PercB { get; }
        public double MusicA { get; }
        public double MusicB { get; }
        public int LoopBarsA { get; }
        public int LoopBarsB { get; }
        public bool QuantizedStartA { get; }
        public bool QuantizedStartB { get; }

        public static MixerExpressionSnapshot FromMix(MixSnapshot mix)
        {
            return new MixerExpressionSnapshot(
                mix.AudioTime,
                mix.Paused,
                mix.DeckAPlaying,
                mix.DeckBPlaying,
                mix.GainA,
                mix.GainB,
                mix.FilterA,
                mix.FilterB,
                mix.PercA,
                mix.PercB,
                mix.MusicA,
                mix.MusicB,
                mix.LoopBarsA,
                mix.LoopBarsB,
                mix.QuantizedStartA,
                mix.QuantizedStartB);
        }

        public MixSnapshot ToMix()
        {
            return new MixSnapshot(
                AudioTime,
                Paused,
                DeckAPlaying,
                DeckBPlaying,
                GainA,
                GainB,
                1d,
                0d,
                "",
                "",
                true,
                FilterA,
                FilterB,
                PercA,
                PercB,
                MusicA,
                MusicB,
                LoopBarsA,
                LoopBarsB,
                QuantizedStartA,
                QuantizedStartB,
                1d,
                1d);
        }
    }
}
