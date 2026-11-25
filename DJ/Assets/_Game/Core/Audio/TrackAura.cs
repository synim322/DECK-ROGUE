namespace DeckRogue.Core.Audio
{
    public enum TrackMood
    {
        None = 0,
        AmberBloom = 1,
        VoidStatic = 2,
        GlassRain = 3,
        RedlineSiren = 4,
        VelvetHush = 5,
        Hardline = 6,
        SaltChrome = 7,
        StaticCrown = 8,
        PrismChoir = 9,
        Ledger = 10,
        Skywire = 11,
        FestivalAnthem = 12
    }

    /// <summary>
    /// Reward-track auras and the one-line card copy. Hype shifts are a playtest hypothesis.
    /// </summary>
    public static class TrackAura
    {
        public const string AmberBloomId = "track_amber_bloom";
        public const string VoidStaticId = "track_void_static";
        public const string GlassRainId = "track_glass_rain";
        public const string RedlineSirenId = "track_redline_siren";
        public const string VelvetHushId = "track_velvet_hush";
        public const string HardlineYellowId = "track_hardline_yellow";
        public const string SaltChromeId = "track_salt_chrome";
        public const string PrismChoirId = "track_prism_choir";
        public const string NightLedgerId = "track_night_ledger";
        public const string SkywireId = "track_skywire";
        public const string StaticCrownId = "track_static_crown";
        public const string FestivalAnthemId = "track_festival_anthem";

        public static TrackMood MoodOf(string trackId)
        {
            if (trackId == AmberBloomId)
                return TrackMood.AmberBloom;
            if (trackId == VoidStaticId)
                return TrackMood.VoidStatic;
            if (trackId == GlassRainId)
                return TrackMood.GlassRain;
            if (trackId == RedlineSirenId)
                return TrackMood.RedlineSiren;
            if (trackId == VelvetHushId)
                return TrackMood.VelvetHush;
            if (trackId == HardlineYellowId)
                return TrackMood.Hardline;
            if (trackId == SaltChromeId)
                return TrackMood.SaltChrome;
            if (trackId == PrismChoirId)
                return TrackMood.PrismChoir;
            if (trackId == NightLedgerId)
                return TrackMood.Ledger;
            if (trackId == SkywireId)
                return TrackMood.Skywire;
            if (trackId == StaticCrownId)
                return TrackMood.StaticCrown;
            if (trackId == FestivalAnthemId)
                return TrackMood.FestivalAnthem;
            return TrackMood.None;
        }

        public static bool IsSpecial(string trackId)
        {
            return MoodOf(trackId) != TrackMood.None;
        }

        public static string CardBody(string trackId)
        {
            TrackMood mood = MoodOf(trackId);
            if (mood == TrackMood.AmberBloom)
                return "Buff: cleans and drops pay +3 hype. Debuff: a bass clash hurts 3 more.";
            if (mood == TrackMood.VoidStatic)
                return "Buff: misses, late loads and dead air are forgiven. Debuff: cleans and drops pay 3 less.";
            if (mood == TrackMood.GlassRain)
                return "Buff: filter, stem and loop snaps pay +3. Debuff: fuss and hot-swap fumbles hurt 3 more.";
            if (mood == TrackMood.RedlineSiren)
                return "Buff: drops pay +5 hype. Debuff: a stale groove and phrase decay hurt 3 more.";
            if (mood == TrackMood.VelvetHush)
                return "Buff: a stale phrase and decay are forgiven. Debuff: drops pay 3 less.";
            if (mood == TrackMood.Hardline)
                return "Buff: drops pay +4 and stem punches +3. Debuff: a bass clash hurts 4 more.";
            if (mood == TrackMood.SaltChrome)
                return "Buff: cleans and bass handoffs pay +3. Debuff: a missed phrase hurts 3 more.";
            if (mood == TrackMood.PrismChoir)
                return "Buff: filter phrases pay +4 and loop snaps +3. Debuff: fuss hurts 4 more.";
            if (mood == TrackMood.Ledger)
                return "Buff: a prepped clean pays +4. Debuff: a hot-swap fumble hurts 4 more.";
            if (mood == TrackMood.Skywire)
                return "Buff: line lifts pay +4 and phrase starts +2. Debuff: phrase decay hurts 3 more.";
            if (mood == TrackMood.StaticCrown)
                return "Buff: an honored cue pays +4. Debuff: a stale groove hurts 4 more, dead air 3 more.";
            if (mood == TrackMood.FestivalAnthem)
                return "Buff: drops pay +4 and cleans +2. Debuff: a bass clash hurts 5 more.";
            return "";
        }

        public static string ShortTag(string trackId)
        {
            TrackMood mood = MoodOf(trackId);
            if (mood == TrackMood.AmberBloom)
                return "BLOOM +clean −bass";
            if (mood == TrackMood.VoidStatic)
                return "STATIC forgive −drop";
            if (mood == TrackMood.GlassRain)
                return "RAIN +snap −fuss";
            if (mood == TrackMood.RedlineSiren)
                return "SIREN +drop −stale";
            if (mood == TrackMood.VelvetHush)
                return "HUSH forgive −drop";
            if (mood == TrackMood.Hardline)
                return "HARD +drop −bass";
            if (mood == TrackMood.SaltChrome)
                return "SALT +clean −miss";
            if (mood == TrackMood.PrismChoir)
                return "PRISM +filter −fuss";
            if (mood == TrackMood.Ledger)
                return "LEDGER +prep −swap";
            if (mood == TrackMood.Skywire)
                return "WIRE +line −decay";
            if (mood == TrackMood.StaticCrown)
                return "CROWN +cue −stale";
            if (mood == TrackMood.FestivalAnthem)
                return "ANTHEM +drop −bass";
            return "";
        }
    }
}
