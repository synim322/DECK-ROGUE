namespace DeckRogue.Core.Scoring
{
    /// <summary>
    /// What scoring is allowed to see: audible decks, gains, stems, filter, line faders, and the audio clock.
    /// Gains are already zero when that deck is not actually playing.
    /// Filter is 0..1 low-pass amount. Perc/Music are 0..1 trims (default full).
    /// Channel is the line fader. Default 1 keeps older snapshots off the line-lift gesture.
    /// </summary>
    public readonly struct MixSnapshot
    {
        public MixSnapshot(
            double audioTime,
            bool paused,
            bool deckAPlaying,
            bool deckBPlaying,
            double gainA,
            double gainB,
            double lowA,
            double lowB,
            string trackA,
            string trackB,
            bool clubMatch)
            : this(
                audioTime,
                paused,
                deckAPlaying,
                deckBPlaying,
                gainA,
                gainB,
                lowA,
                lowB,
                trackA,
                trackB,
                clubMatch,
                0d,
                0d,
                1d,
                1d,
                1d,
                1d,
                DefaultLoopBars,
                DefaultLoopBars,
                false,
                false,
                1d,
                1d)
        {
        }

        public MixSnapshot(
            double audioTime,
            bool paused,
            bool deckAPlaying,
            bool deckBPlaying,
            double gainA,
            double gainB,
            double lowA,
            double lowB,
            string trackA,
            string trackB,
            bool clubMatch,
            double filterA,
            double filterB,
            double percA,
            double percB,
            double musicA,
            double musicB)
            : this(
                audioTime,
                paused,
                deckAPlaying,
                deckBPlaying,
                gainA,
                gainB,
                lowA,
                lowB,
                trackA,
                trackB,
                clubMatch,
                filterA,
                filterB,
                percA,
                percB,
                musicA,
                musicB,
                DefaultLoopBars,
                DefaultLoopBars,
                false,
                false,
                1d,
                1d)
        {
        }

        public MixSnapshot(
            double audioTime,
            bool paused,
            bool deckAPlaying,
            bool deckBPlaying,
            double gainA,
            double gainB,
            double lowA,
            double lowB,
            string trackA,
            string trackB,
            bool clubMatch,
            double filterA,
            double filterB,
            double percA,
            double percB,
            double musicA,
            double musicB,
            int loopBarsA,
            int loopBarsB,
            bool quantizedStartA,
            bool quantizedStartB,
            double channelA,
            double channelB)
        {
            AudioTime = audioTime;
            Paused = paused;
            DeckAPlaying = deckAPlaying;
            DeckBPlaying = deckBPlaying;
            GainA = gainA;
            GainB = gainB;
            LowA = lowA;
            LowB = lowB;
            TrackA = trackA ?? "";
            TrackB = trackB ?? "";
            ClubMatch = clubMatch;
            FilterA = filterA;
            FilterB = filterB;
            PercA = percA;
            PercB = percB;
            MusicA = musicA;
            MusicB = musicB;
            LoopBarsA = loopBarsA <= 0 ? DefaultLoopBars : loopBarsA;
            LoopBarsB = loopBarsB <= 0 ? DefaultLoopBars : loopBarsB;
            QuantizedStartA = quantizedStartA;
            QuantizedStartB = quantizedStartB;
            ChannelA = channelA;
            ChannelB = channelB;
        }

        /// <summary>Default phrase loop length in bars (LoopLengthMode.Phrase8).</summary>
        public const int DefaultLoopBars = 8;

        public double AudioTime { get; }
        public bool Paused { get; }
        public bool DeckAPlaying { get; }
        public bool DeckBPlaying { get; }
        public double GainA { get; }
        public double GainB { get; }
        public double LowA { get; }
        public double LowB { get; }
        public string TrackA { get; }
        public string TrackB { get; }
        public bool ClubMatch { get; }
        public double FilterA { get; }
        public double FilterB { get; }
        public double PercA { get; }
        public double PercB { get; }
        public double MusicA { get; }
        public double MusicB { get; }

        /// <summary>Active loop slice length in bars (8 default, 4 half-phrase, etc.).</summary>
        public int LoopBarsA { get; }

        public int LoopBarsB { get; }

        /// <summary>True when a quantized start lands on this sample for the deck.</summary>
        public bool QuantizedStartA { get; }

        public bool QuantizedStartB { get; }

        /// <summary>Line fader for deck A. Detector reads the raw value; 1 is a resting line.</summary>
        public double ChannelA { get; }

        /// <summary>Line fader for deck B.</summary>
        public double ChannelB { get; }
    }

    /// <summary>
    /// Hall motion tier for PixelClubView / BoothSnapshot.CrowdImpulse.
    /// Derived from quality ScoreKinds applied this frame (max wins).
    /// </summary>
    public enum CrowdMotionImpulse
    {
        None = 0,
        Nod = 1,
        Bounce = 2,
        Peak = 3
    }

    public enum ScoreKind
    {
        CleanTransition = 0,
        MissedTransition = 1,
        Drop = 2,
        BassConflict = 3,
        Fuss = 4,
        PhraseDecay = 5,
        StaleGroove = 6,
        CueReadyHonor = 7,
        LateLoad = 8,
        DeadAir = 9,
        HotSwapFumble = 10,
        CleanPrepTransition = 11,
        FilterPhrase = 12,
        StemPunch = 13,
        LoopSnap = 14,
        PhraseStart = 15,
        BassHandoff = 16,
        LineLift = 17
    }

    public readonly struct ScoreEvent
    {
        public ScoreEvent(
            string id,
            ScoreKind kind,
            double hypeDelta,
            double reputationBase,
            bool isQuality,
            bool resetsCombo,
            bool hardCut)
        {
            Id = id;
            Kind = kind;
            HypeDelta = hypeDelta;
            ReputationBase = reputationBase;
            IsQuality = isQuality;
            ResetsCombo = resetsCombo;
            HardCut = hardCut;
        }

        public string Id { get; }
        public ScoreKind Kind { get; }
        public double HypeDelta { get; }
        public double ReputationBase { get; }
        public bool IsQuality { get; }
        public bool ResetsCombo { get; }
        public bool HardCut { get; }

        public ScoreEvent WithBonus(double hype, double reputation)
        {
            return new ScoreEvent(
                Id,
                Kind,
                HypeDelta + hype,
                ReputationBase + reputation,
                IsQuality,
                ResetsCombo,
                HardCut);
        }

        public static ScoreEvent Clean(int phrase, int from, int to, bool hardCut)
        {
            return new ScoreEvent(
                "transition-" + phrase + "-" + from + "-" + to,
                ScoreKind.CleanTransition,
                8d,
                100d,
                true,
                false,
                hardCut);
        }

        public static ScoreEvent Miss(int phrase)
        {
            return new ScoreEvent(
                "miss-" + phrase,
                ScoreKind.MissedTransition,
                0d,
                0d,
                false,
                false,
                false);
        }

        public static ScoreEvent Drop(int bar)
        {
            return new ScoreEvent("drop-" + bar, ScoreKind.Drop, 12d, 140d, true, false, false);
        }

        public static ScoreEvent Bass(int phrase)
        {
            return new ScoreEvent("bass-" + phrase, ScoreKind.BassConflict, -6d, 0d, false, true, false);
        }

        public static ScoreEvent Fuss(int phrase)
        {
            return new ScoreEvent("fuss-" + phrase, ScoreKind.Fuss, -4d, 0d, false, false, false);
        }

        public static ScoreEvent Decay(int phrase)
        {
            return new ScoreEvent("decay-" + phrase, ScoreKind.PhraseDecay, -2d, 0d, false, false, false);
        }

        public static ScoreEvent Stale(int phrase)
        {
            return new ScoreEvent("stale-" + phrase, ScoreKind.StaleGroove, -4d, 0d, false, false, false);
        }

        public static ScoreEvent CueHonor(int requestId)
        {
            return new ScoreEvent(
                "cue-honor-" + requestId,
                ScoreKind.CueReadyHonor,
                3d,
                40d,
                true,
                false,
                false);
        }

        public static ScoreEvent Late(int requestId)
        {
            return new ScoreEvent(
                "late-load-" + requestId,
                ScoreKind.LateLoad,
                -3d,
                0d,
                false,
                false,
                false);
        }

        public static ScoreEvent DeadAir(int phrase)
        {
            return new ScoreEvent(
                "dead-air-" + phrase,
                ScoreKind.DeadAir,
                -5d,
                0d,
                false,
                false,
                false);
        }

        public static ScoreEvent HotSwap(int phrase)
        {
            return new ScoreEvent(
                "hot-swap-" + phrase,
                ScoreKind.HotSwapFumble,
                -4d,
                0d,
                false,
                false,
                false);
        }

        /// <summary>
        /// Bonus on top of a clean transition. Id is derived from the transition event id.
        /// </summary>
        public static ScoreEvent PrepBonus(string transitionEventId)
        {
            string id = transitionEventId ?? "";
            return new ScoreEvent(
                "prep-" + id,
                ScoreKind.CleanPrepTransition,
                2d,
                30d,
                true,
                false,
                false);
        }

        /// <summary>
        /// Filter travel ≥0.35 over ≥1 bar, settled near phrase/bar boundary.
        /// event kind id: filter_phrase. Playtest hypothesis: +4 Hype / +50 Rep.
        /// </summary>
        public static ScoreEvent FilterPhrase(int phrase)
        {
            return new ScoreEvent(
                "filter_phrase-" + phrase,
                ScoreKind.FilterPhrase,
                4d,
                50d,
                true,
                false,
                false);
        }

        /// <summary>
        /// Stem contrast: PERC/MUSIC dip then punch-return on a bar downbeat.
        /// event kind id: stem_punch. Playtest hypothesis: +3 Hype / +40 Rep.
        /// </summary>
        public static ScoreEvent StemPunch(int bar)
        {
            return new ScoreEvent(
                "stem_punch-" + bar,
                ScoreKind.StemPunch,
                3d,
                40d,
                true,
                false,
                false);
        }

        /// <summary>
        /// Quantized start on a non-default loop length after a length change.
        /// event kind id: loop_snap. +2 Hype / +20 Rep.
        /// </summary>
        public static ScoreEvent LoopSnap(int bar, int bars = MixSnapshot.DefaultLoopBars)
        {
            return new ScoreEvent(
                "loop_snap-" + bar + "-" + bars,
                ScoreKind.LoopSnap,
                2d,
                20d,
                true,
                false,
                false);
        }

        /// <summary>
        /// Both decks audible, this deck's quantized start on the phrase downbeat.
        /// event kind id: phrase_start. Playtest hypothesis: +2 Hype / +25 Rep.
        /// </summary>
        public static ScoreEvent PhraseStart(int phrase, int deck)
        {
            return new ScoreEvent(
                "phrase_start-" + phrase + "-" + deck,
                ScoreKind.PhraseStart,
                2d,
                25d,
                true,
                false,
                false);
        }

        /// <summary>
        /// One deck keeps the low end for a bar while the other stays quiet. Both stay audible.
        /// event kind id: bass_handoff. Playtest hypothesis: +3 Hype / +35 Rep.
        /// </summary>
        public static ScoreEvent BassHandoff(int phrase)
        {
            return new ScoreEvent(
                "bass_handoff-" + phrase,
                ScoreKind.BassHandoff,
                3d,
                35d,
                true,
                false,
                false);
        }

        /// <summary>
        /// Line fader travels from down to up across a bar, on an audible deck.
        /// event kind id: line_lift. Playtest hypothesis: +3 Hype / +40 Rep.
        /// </summary>
        public static ScoreEvent LineLift(int phrase, int deck)
        {
            return new ScoreEvent(
                "line_lift-" + phrase + "-" + deck,
                ScoreKind.LineLift,
                3d,
                40d,
                true,
                false,
                false);
        }
    }

    /// <summary>
    /// Maps quality ScoreKinds to hall motion impulses for the client.
    /// </summary>
    public static class CrowdMotion
    {
        public static CrowdMotionImpulse FromKind(ScoreKind kind)
        {
            if (kind == ScoreKind.Drop)
                return CrowdMotionImpulse.Peak;
            if (kind == ScoreKind.CleanTransition
                || kind == ScoreKind.FilterPhrase
                || kind == ScoreKind.StemPunch
                || kind == ScoreKind.LineLift)
                return CrowdMotionImpulse.Bounce;
            if (kind == ScoreKind.CueReadyHonor
                || kind == ScoreKind.CleanPrepTransition
                || kind == ScoreKind.LoopSnap
                || kind == ScoreKind.PhraseStart
                || kind == ScoreKind.BassHandoff)
                return CrowdMotionImpulse.Nod;
            return CrowdMotionImpulse.None;
        }

        public static CrowdMotionImpulse Max(CrowdMotionImpulse a, CrowdMotionImpulse b)
        {
            return (int)a >= (int)b ? a : b;
        }
    }

    public readonly struct ScoreChange
    {
        public ScoreChange(
            string id,
            ScoreKind kind,
            double hypeDelta,
            double reputationDelta,
            double hype,
            int combo,
            double reputation,
            bool hardCut)
        {
            Id = id;
            Kind = kind;
            HypeDelta = hypeDelta;
            ReputationDelta = reputationDelta;
            Hype = hype;
            Combo = combo;
            Reputation = reputation;
            HardCut = hardCut;
        }

        public string Id { get; }
        public ScoreKind Kind { get; }
        public double HypeDelta { get; }
        public double ReputationDelta { get; }
        public double Hype { get; }
        public int Combo { get; }
        public double Reputation { get; }
        public bool HardCut { get; }
    }
}
