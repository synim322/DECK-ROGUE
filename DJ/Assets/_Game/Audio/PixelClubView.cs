using DeckRogue.Client;
using DeckRogue.Core.Audio;
using DeckRogue.Core.Run;
using UnityEngine;
using UnityEngine.UI;

namespace DeckRogue.Audio
{
    /// <summary>
    /// Club visual themes for the 480×168 procedural hall.
    /// Keys match club ids and RunChrome.Venue display names.
    /// </summary>
    public enum ClubTheme
    {
        NeonBasement = 0,
        Warehouse = 1,
        BassFactory = 2,
        TheSignal = 3,
        DreamLounge = 4,
        RaveDistrict = 5,
        ChromePier = 6,
        TheStaticThrone = 7,
        GlassCathedral = 8,
        NightExchange = 9,
        RoofRelay = 10,
        FinalFestival = 11
    }

    public enum HallVisualStage
    {
        Solo = 0,
        Trickle = 1,
        Groove = 2,
        Rave = 3,
        Trash = 4,
        Slump = 5
    }

    /// <summary>
    /// 480×168 point-filtered hall. Twelve rooms: four Act1 floors and eight later clubs.
    /// Crowd climbs Solo → Trickle → Groove → Rave → Trash as the room fills.
    /// Fault → Slump. Pose, density, and the room silhouette carry the hit. No full-frame strobes.
    /// </summary>
    public sealed class PixelClubView
    {
        private const int Width = 480;
        private const int Height = 168;
        /// <summary>Two bars at 4/4 — temporary cheer after Clean/Drop.</summary>
        private const int CheerBumpBeats = 8;
        /// <summary>Quantize motionPhase so bob steps stay crisp (JRPG, no blur).</summary>
        private const int MotionStepsPerBeat = 4;

        /// <summary>Palette key → ClubTheme mapping for callers / docs.</summary>
        public static readonly string[] PaletteKeys =
        {
            "NeonBasement",
            "Warehouse",
            "BassFactory",
            "TheSignal",
            "DreamLounge",
            "RaveDistrict",
            "ChromePier",
            "TheStaticThrone",
            "GlassCathedral",
            "NightExchange",
            "RoofRelay",
            "FinalFestival"
        };

        private static readonly int[] PackedX = { 62, 112, 162, 212, 262, 312, 362, 412 };
        private static readonly int[] PeakX =
        {
            48, 78, 108, 138, 168, 198, 228, 258, 288, 318, 348, 378, 408, 438
        };
        private static readonly int[] FrontX = { 72, 132, 192, 252, 312, 372, 432 };
        private static readonly int[] FrontPeakX =
        {
            56, 96, 136, 176, 216, 256, 296, 336, 376, 416
        };
        private static readonly int[] DistrictTrickleX = { 56, 128, 200, 272, 344, 416 };
        private static readonly int[] DistrictPackedX = { 40, 80, 120, 160, 200, 240, 280, 320, 360, 400, 440 };
        private static readonly int[] DistrictPeakX =
        {
            30, 58, 86, 114, 142, 170, 198, 226, 254, 282, 310, 338, 366, 394, 422, 450
        };
        private static readonly int[] DistrictFrontX = { 48, 96, 144, 192, 240, 288, 336, 384, 432 };
        private static readonly int[] DistrictFrontPeakX =
        {
            36, 72, 108, 144, 180, 216, 252, 288, 324, 360, 396, 432
        };
        private static readonly int[] FestivalTrickleX = { 48, 104, 160, 216, 272, 328, 384, 440 };
        private static readonly int[] FestivalPackedX =
        {
            32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448
        };
        private static readonly int[] FestivalPeakX =
        {
            24, 48, 72, 96, 120, 144, 168, 192, 216, 240, 264, 288, 312, 336, 360, 384, 408, 432
        };
        private static readonly int[] FestivalFrontX =
        {
            32, 70, 108, 146, 184, 222, 260, 298, 336, 374, 412, 450
        };
        private static readonly int[] FestivalFrontPeakX =
        {
            28, 60, 92, 124, 156, 188, 220, 252, 284, 316, 348, 380, 412, 444
        };
        private static readonly int[] SoloX = { 236 };
        private static readonly int[] TrickleX = { 148, 204, 268, 332 };
        private static readonly int[] SlumpX = { 168, 236, 304 };
        private static readonly int[] ConfettiX = { 40, 90, 130, 180, 220, 270, 310, 360, 410, 450 };
        private static readonly int[] ConfettiY = { 48, 70, 58, 88, 64, 96, 52, 80, 60, 74 };
        private static readonly int[] AuraMoteX = { 40, 90, 140, 190, 250, 310, 370, 430 };
        private static readonly int[] AuraMoteY = { 52, 70, 58, 80, 64, 76, 50, 81 };

        private static readonly string[] GlyphN = { "10001", "11001", "10101", "10011", "10001" };
        private static readonly string[] GlyphE = { "11111", "10000", "11110", "10000", "11111" };
        private static readonly string[] GlyphO = { "01110", "10001", "10001", "10001", "01110" };

        private readonly Texture2D _texture;
        private readonly Color32[] _pixels = new Color32[Width * Height];
        private ClubTheme _theme = ClubTheme.NeonBasement;
        private ClubPalette _pal;
        private int _drawnBeat = -1;
        private int _band = -1;
        private int _cue = -1;
        private int _riser = -1;
        private int _intensity = -1;
        private int _impulse = -1;
        private int _population = -1;
        private readonly int[] _soloSlot = { 0 };
        private int _motionStep = -1;
        private bool _signFlash;
        private TrackMood _trackMood = TrackMood.None;
        private TrackMood _drawnMood = TrackMood.None;
        private int _prevBeat = -1;
        private int _bumpBeatsLeft;
        private int _bumpFloor;

        private struct ClubPalette
        {
            public Color32 Void;
            public Color32 Wall;
            public Color32 Floor;
            public Color32 FloorLine;
            public Color32 Shadow;
            public Color32 Panel;
            public Color32 Brick;
            public Color32 Stage;
            public Color32 StageEdge;
            public Color32 CrowdCold;
            public Color32 CrowdMid;
            public Color32 CrowdHot;
            public Color32 AccentA;
            public Color32 AccentADim;
            public Color32 AccentB;
            public Color32 AccentBDim;
            public Color32 Mint;
            public Color32 Amber;
            public Color32 Ray;
            public Color32 Fog;
            public Color32 Beam;
        }

        public PixelClubView(RawImage target)
        {
            _texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
            _texture.filterMode = FilterMode.Point;
            _texture.wrapMode = TextureWrapMode.Clamp;
            _texture.hideFlags = HideFlags.HideAndDontSave;
            target.texture = _texture;
            target.color = Color.white;
            target.raycastTarget = false;
            _pal = BuildPalette(ClubTheme.NeonBasement);
            _drawnBeat = 0;
            _band = 0;
            _cue = HallCue.None;
            _riser = 0;
            _intensity = 0;
            _population = 0;
            Draw(0, 0, HallCue.None, 0, false, 0, 0, 0f, 0, 0);
        }

        public ClubTheme Theme
        {
            get { return _theme; }
        }

        /// <summary>
        /// Switch hall palette. Accepts ClubTheme name, Act1ClubIds, or Venue display string.
        /// </summary>
        public void SetClubTheme(string clubId)
        {
            ClubTheme next = ResolveTheme(clubId);
            if (next == _theme)
                return;
            _theme = next;
            _pal = BuildPalette(next);
            _drawnBeat = -1;
        }

        public void SetClubTheme(ClubTheme theme)
        {
            if (theme == _theme)
                return;
            _theme = theme;
            _pal = BuildPalette(theme);
            _drawnBeat = -1;
        }

        /// <summary>
        /// Dominant deck. A special record washes the hall; anything else clears it.
        /// Call before <see cref="SetPerformance"/> on the same HUD refresh.
        /// </summary>
        public void SetActiveTrack(string trackId)
        {
            TrackMood next = TrackAura.MoodOf(trackId);
            if (next == _trackMood)
                return;
            _trackMood = next;
            _drawnBeat = -1;
        }

        /// <summary>
        /// Existing performance draw. Optional clubId refreshes the palette before paint.
        /// cheerIntensity −1 derives from band + cue; 0..3 drives pose / rays / Signal beams.
        /// crowdImpulse −1 derives from cue; motionPhase is continuous bar phase in beats [0,4).
        /// </summary>
        public void SetPerformance(int beatInBar, int crowdBand, int hallCue, int riserLevel, bool signFlash)
        {
            SetPerformance(beatInBar, crowdBand, hallCue, riserLevel, signFlash, null, -1, -1, beatInBar);
        }

        public void SetPerformance(
            int beatInBar,
            int crowdBand,
            int hallCue,
            int riserLevel,
            bool signFlash,
            string clubId)
        {
            SetPerformance(beatInBar, crowdBand, hallCue, riserLevel, signFlash, clubId, -1, -1, beatInBar);
        }

        public void SetPerformance(
            int beatInBar,
            int crowdBand,
            int hallCue,
            int riserLevel,
            bool signFlash,
            string clubId,
            int cheerIntensity)
        {
            SetPerformance(beatInBar, crowdBand, hallCue, riserLevel, signFlash, clubId, cheerIntensity, -1, beatInBar);
        }

        public void SetPerformance(
            int beatInBar,
            int crowdBand,
            int hallCue,
            int riserLevel,
            bool signFlash,
            string clubId,
            int cheerIntensity,
            int crowdImpulse,
            float motionPhase)
        {
            if (!string.IsNullOrEmpty(clubId))
                SetClubTheme(clubId);

            int beat = Mathf.Clamp(beatInBar, 0, 3);
            int band = Mathf.Clamp(crowdBand, 0, 2);
            int cue = hallCue;
            if (cue != HallCue.Clean && cue != HallCue.Drop && cue != HallCue.Fault)
                cue = HallCue.None;
            int riser = riserLevel < 0 ? 0 : (riserLevel > 2 ? 2 : riserLevel);
            int baseIntensity = cheerIntensity;
            if (baseIntensity < 0 || baseIntensity > 3)
                baseIntensity = MapIntensity(band, cue, riser);
            // Headcount is cheer before the bump. Impulse below 3 does not recruit bodies.
            int population = baseIntensity;

            AdvanceCheerBump(beat, cue);
            int intensity = baseIntensity;
            if (_bumpBeatsLeft > 0 && _bumpFloor > intensity)
                intensity = _bumpFloor;

            int impulse = crowdImpulse;
            if (impulse < 0 || impulse > 3)
                impulse = MapImpulse(cue, intensity);
            // Fault freezes motion even if cheer band is high.
            if (cue == HallCue.Fault)
                impulse = 0;
            // Successful cues keep density floor while impulse holds.
            if (impulse >= 2 && intensity < impulse)
                intensity = impulse;

            float phase = motionPhase;
            if (float.IsNaN(phase) || phase < 0f)
                phase = beat;
            int motionStep = QuantizeMotion(phase);

            // DJ-017: reduced flashing softens sign strobe + mint downbeat flash.
            if (GameSettings.ReducedFlashing)
                signFlash = false;

            if (beat == _drawnBeat
                && band == _band
                && cue == _cue
                && riser == _riser
                && intensity == _intensity
                && impulse == _impulse
                && population == _population
                && motionStep == _motionStep
                && signFlash == _signFlash
                && _trackMood == _drawnMood)
                return;
            _drawnBeat = beat;
            _band = band;
            _cue = cue;
            _riser = riser;
            _intensity = intensity;
            _impulse = impulse;
            _population = population;
            _motionStep = motionStep;
            _signFlash = signFlash;
            _drawnMood = _trackMood;
            Draw(beat, band, cue, riser, signFlash, intensity, impulse, phase, population, motionStep);
        }

        /// <summary>
        /// Population sets headcount: 0 Solo, 1 Trickle, 2 Groove, 3 Rave.
        /// Impulse below 3 does not add bodies. Trash needs impulse ≥ 3 and population ≥ 2.
        /// Fault is always Slump.
        /// </summary>
        public static HallVisualStage ResolveStage(int intensity, int impulse, bool fault)
        {
            if (fault)
                return HallVisualStage.Slump;
            if (intensity < 0)
                intensity = 0;
            else if (intensity > 3)
                intensity = 3;
            if (impulse < 0)
                impulse = 0;
            else if (impulse > 3)
                impulse = 3;
            if (impulse >= 3 && intensity >= 2)
                return HallVisualStage.Trash;
            if (intensity <= 0)
                return HallVisualStage.Solo;
            if (intensity == 1)
                return HallVisualStage.Trickle;
            if (intensity == 2)
                return HallVisualStage.Groove;
            return HallVisualStage.Rave;
        }

        /// <summary>Cue / cheer → impulse when session left CrowdImpulse unset.</summary>
        public static int MapImpulse(int hallCue, int cheerIntensity)
        {
            if (hallCue == HallCue.Fault)
                return 0;
            if (hallCue == HallCue.Drop)
                return 3;
            if (hallCue == HallCue.Clean)
                return 2;
            if (cheerIntensity < 0)
                return 0;
            return cheerIntensity > 3 ? 3 : cheerIntensity;
        }

        private static int QuantizeMotion(float motionPhase)
        {
            float wrapped = motionPhase % 4f;
            if (wrapped < 0f)
                wrapped += 4f;
            return Mathf.Clamp(Mathf.FloorToInt(wrapped * MotionStepsPerBeat), 0, 4 * MotionStepsPerBeat - 1);
        }

        /// <summary>Public mapping for docs / HUD fallbacks.</summary>
        public static int MapIntensity(int crowdBand, int hallCue, int riserLevel)
        {
            if (hallCue == HallCue.Fault)
                return 0;
            int intensity = crowdBand < 0 ? 0 : (crowdBand > 2 ? 2 : crowdBand);
            if (riserLevel >= 2 && intensity < 2)
                intensity = 2;
            else if (riserLevel >= 1 && intensity < 1)
                intensity = 1;
            if (hallCue == HallCue.Drop)
                return 3;
            if (hallCue == HallCue.Clean && intensity < 2)
                intensity = 2;
            return intensity;
        }

        private void AdvanceCheerBump(int beat, int cue)
        {
            if (cue == HallCue.Fault)
            {
                _bumpBeatsLeft = 0;
                _bumpFloor = 0;
            }
            else if (cue == HallCue.Clean || cue == HallCue.Drop)
            {
                int floor = cue == HallCue.Drop ? 3 : 2;
                if (cue != _cue || _bumpBeatsLeft <= 0)
                {
                    _bumpBeatsLeft = CheerBumpBeats;
                    _bumpFloor = floor;
                }
                else if (floor > _bumpFloor)
                    _bumpFloor = floor;
            }

            if (_prevBeat >= 0 && beat != _prevBeat && _bumpBeatsLeft > 0)
                _bumpBeatsLeft--;
            _prevBeat = beat;
        }

        public static ClubTheme ResolveTheme(string clubId)
        {
            if (string.IsNullOrEmpty(clubId))
                return ClubTheme.NeonBasement;

            string key = clubId.Trim();
            int boss = key.IndexOf(" · ", System.StringComparison.Ordinal);
            if (boss >= 0)
                key = key.Substring(0, boss).Trim();

            string lower = key.ToLowerInvariant();
            if (lower == "neonbasement"
                || lower == "club_neon_basement"
                || lower == "neon basement")
                return ClubTheme.NeonBasement;
            if (lower == "warehouse"
                || lower == "club_warehouse_pulse"
                || lower == "warehouse pulse")
                return ClubTheme.Warehouse;
            if (lower == "bassfactory"
                || lower == "club_bass_factory"
                || lower == "bass factory")
                return ClubTheme.BassFactory;
            if (lower == "thesignal"
                || lower == "club_the_signal"
                || lower == "the signal")
                return ClubTheme.TheSignal;
            if (lower == "dreamlounge"
                || lower == "club_dream_lounge"
                || lower == "dream lounge")
                return ClubTheme.DreamLounge;
            if (lower == "ravedistrict"
                || lower == "club_rave_district"
                || lower == "rave district")
                return ClubTheme.RaveDistrict;
            if (lower == "chromepier"
                || lower == "club_chrome_pier"
                || lower == "chrome pier")
                return ClubTheme.ChromePier;
            if (lower == "thestaticthrone"
                || lower == "club_the_static_throne"
                || lower == "the static throne")
                return ClubTheme.TheStaticThrone;
            if (lower == "glasscathedral"
                || lower == "club_glass_cathedral"
                || lower == "glass cathedral")
                return ClubTheme.GlassCathedral;
            if (lower == "nightexchange"
                || lower == "club_night_exchange"
                || lower == "night exchange")
                return ClubTheme.NightExchange;
            if (lower == "roofrelay"
                || lower == "club_roof_relay"
                || lower == "roof relay")
                return ClubTheme.RoofRelay;
            if (lower == "finalfestival"
                || lower == "club_final_festival"
                || lower == "final festival")
                return ClubTheme.FinalFestival;

            return ClubTheme.NeonBasement;
        }

        private static ClubPalette BuildPalette(ClubTheme theme)
        {
            switch (theme)
            {
                case ClubTheme.Warehouse:
                    return new ClubPalette
                    {
                        Void = new Color32(6, 8, 12, 255),
                        Wall = new Color32(18, 22, 28, 255),
                        Floor = new Color32(10, 12, 16, 255),
                        FloorLine = new Color32(40, 48, 56, 255),
                        Shadow = new Color32(8, 6, 10, 255),
                        Panel = new Color32(42, 48, 58, 255),
                        Brick = new Color32(58, 64, 72, 255),
                        Stage = new Color32(14, 16, 22, 255),
                        StageEdge = new Color32(72, 84, 96, 255),
                        CrowdCold = new Color32(118, 128, 140, 255),
                        CrowdMid = new Color32(148, 156, 168, 255),
                        CrowdHot = new Color32(188, 198, 210, 255),
                        AccentA = new Color32(140, 168, 196, 255),
                        AccentADim = new Color32(58, 72, 88, 255),
                        AccentB = new Color32(160, 176, 184, 255),
                        AccentBDim = new Color32(64, 72, 80, 255),
                        Mint = new Color32(120, 196, 188, 255),
                        Amber = new Color32(196, 180, 120, 255),
                        Ray = new Color32(120, 140, 160, 28),
                        Fog = new Color32(70, 80, 92, 36),
                        Beam = new Color32(150, 170, 190, 40)
                    };
                case ClubTheme.BassFactory:
                    return new ClubPalette
                    {
                        Void = new Color32(8, 4, 4, 255),
                        Wall = new Color32(28, 14, 12, 255),
                        Floor = new Color32(12, 6, 6, 255),
                        FloorLine = new Color32(56, 28, 18, 255),
                        Shadow = new Color32(14, 6, 4, 255),
                        Panel = new Color32(48, 24, 18, 255),
                        Brick = new Color32(86, 42, 28, 255),
                        Stage = new Color32(18, 8, 6, 255),
                        StageEdge = new Color32(120, 52, 28, 255),
                        CrowdCold = new Color32(148, 96, 84, 255),
                        CrowdMid = new Color32(196, 120, 88, 255),
                        CrowdHot = new Color32(240, 168, 96, 255),
                        AccentA = new Color32(232, 120, 48, 255),
                        AccentADim = new Color32(112, 48, 22, 255),
                        AccentB = new Color32(244, 168, 72, 255),
                        AccentBDim = new Color32(128, 64, 28, 255),
                        Mint = new Color32(220, 148, 72, 255),
                        Amber = new Color32(255, 196, 88, 255),
                        Ray = new Color32(220, 110, 40, 32),
                        Fog = new Color32(90, 36, 18, 30),
                        Beam = new Color32(255, 140, 48, 44)
                    };
                case ClubTheme.TheSignal:
                    return new ClubPalette
                    {
                        Void = new Color32(2, 4, 8, 255),
                        Wall = new Color32(6, 10, 16, 255),
                        Floor = new Color32(4, 6, 10, 255),
                        FloorLine = new Color32(20, 40, 48, 255),
                        Shadow = new Color32(2, 4, 8, 255),
                        Panel = new Color32(12, 22, 30, 255),
                        Brick = new Color32(18, 36, 44, 255),
                        Stage = new Color32(4, 8, 12, 255),
                        StageEdge = new Color32(40, 180, 200, 255),
                        CrowdCold = new Color32(72, 110, 128, 255),
                        CrowdMid = new Color32(96, 160, 176, 255),
                        CrowdHot = new Color32(160, 230, 240, 255),
                        AccentA = new Color32(64, 230, 255, 255),
                        AccentADim = new Color32(24, 72, 88, 255),
                        AccentB = new Color32(48, 200, 220, 255),
                        AccentBDim = new Color32(20, 60, 72, 255),
                        Mint = new Color32(80, 240, 255, 255),
                        Amber = new Color32(120, 220, 240, 255),
                        Ray = new Color32(40, 200, 230, 36),
                        Fog = new Color32(8, 20, 28, 40),
                        Beam = new Color32(80, 240, 255, 56)
                    };
                case ClubTheme.DreamLounge:
                    return new ClubPalette
                    {
                        Void = new Color32(12, 8, 18, 255),
                        Wall = new Color32(42, 28, 56, 255),
                        Floor = new Color32(20, 14, 32, 255),
                        FloorLine = new Color32(78, 52, 96, 255),
                        Shadow = new Color32(16, 10, 24, 255),
                        Panel = new Color32(58, 40, 72, 255),
                        Brick = new Color32(88, 60, 96, 255),
                        Stage = new Color32(24, 16, 36, 255),
                        StageEdge = new Color32(120, 84, 132, 255),
                        CrowdCold = new Color32(132, 108, 148, 255),
                        CrowdMid = new Color32(176, 132, 160, 255),
                        CrowdHot = new Color32(214, 168, 176, 255),
                        AccentA = new Color32(196, 120, 168, 255),
                        AccentADim = new Color32(92, 48, 84, 255),
                        AccentB = new Color32(140, 112, 196, 255),
                        AccentBDim = new Color32(64, 48, 96, 255),
                        Mint = new Color32(168, 140, 186, 255),
                        Amber = new Color32(196, 140, 120, 255),
                        Ray = new Color32(160, 100, 180, 24),
                        Fog = new Color32(80, 48, 96, 28),
                        Beam = new Color32(180, 120, 200, 32)
                    };
                case ClubTheme.RaveDistrict:
                    return new ClubPalette
                    {
                        Void = new Color32(6, 2, 10, 255),
                        Wall = new Color32(28, 6, 32, 255),
                        Floor = new Color32(12, 4, 14, 255),
                        FloorLine = new Color32(64, 16, 48, 255),
                        Shadow = new Color32(10, 2, 12, 255),
                        Panel = new Color32(48, 12, 40, 255),
                        Brick = new Color32(72, 16, 56, 255),
                        Stage = new Color32(18, 4, 20, 255),
                        StageEdge = new Color32(180, 32, 120, 255),
                        CrowdCold = new Color32(110, 36, 96, 255),
                        CrowdMid = new Color32(180, 40, 140, 255),
                        CrowdHot = new Color32(240, 64, 168, 255),
                        AccentA = new Color32(255, 40, 140, 255),
                        AccentADim = new Color32(120, 16, 64, 255),
                        AccentB = new Color32(64, 240, 120, 255),
                        AccentBDim = new Color32(20, 88, 48, 255),
                        Mint = new Color32(80, 230, 140, 255),
                        Amber = new Color32(255, 80, 160, 255),
                        Ray = new Color32(255, 40, 140, 28),
                        Fog = new Color32(80, 12, 48, 32),
                        Beam = new Color32(64, 255, 140, 36)
                    };
                case ClubTheme.ChromePier:
                    return new ClubPalette
                    {
                        Void = new Color32(4, 10, 14, 255),
                        Wall = new Color32(16, 32, 40, 255),
                        Floor = new Color32(8, 18, 24, 255),
                        FloorLine = new Color32(40, 72, 80, 255),
                        Shadow = new Color32(4, 12, 16, 255),
                        Panel = new Color32(28, 48, 56, 255),
                        Brick = new Color32(48, 72, 80, 255),
                        Stage = new Color32(10, 22, 28, 255),
                        StageEdge = new Color32(120, 168, 176, 255),
                        CrowdCold = new Color32(96, 120, 128, 255),
                        CrowdMid = new Color32(150, 170, 168, 255),
                        CrowdHot = new Color32(198, 214, 206, 255),
                        AccentA = new Color32(72, 220, 210, 255),
                        AccentADim = new Color32(24, 80, 84, 255),
                        AccentB = new Color32(240, 176, 64, 255),
                        AccentBDim = new Color32(120, 84, 28, 255),
                        Mint = new Color32(120, 210, 200, 255),
                        Amber = new Color32(240, 180, 72, 255),
                        Ray = new Color32(80, 180, 190, 26),
                        Fog = new Color32(20, 48, 56, 32),
                        Beam = new Color32(160, 220, 210, 36)
                    };
                case ClubTheme.TheStaticThrone:
                    return new ClubPalette
                    {
                        Void = new Color32(8, 8, 10, 255),
                        Wall = new Color32(22, 22, 26, 255),
                        Floor = new Color32(12, 12, 14, 255),
                        FloorLine = new Color32(48, 48, 56, 255),
                        Shadow = new Color32(6, 6, 8, 255),
                        Panel = new Color32(36, 36, 42, 255),
                        Brick = new Color32(58, 56, 64, 255),
                        Stage = new Color32(14, 14, 18, 255),
                        StageEdge = new Color32(140, 140, 150, 255),
                        CrowdCold = new Color32(96, 96, 104, 255),
                        CrowdMid = new Color32(132, 130, 140, 255),
                        CrowdHot = new Color32(176, 174, 184, 255),
                        AccentA = new Color32(196, 196, 204, 255),
                        AccentADim = new Color32(72, 72, 80, 255),
                        AccentB = new Color32(132, 104, 168, 255),
                        AccentBDim = new Color32(60, 44, 80, 255),
                        Mint = new Color32(180, 180, 188, 255),
                        Amber = new Color32(160, 140, 120, 255),
                        Ray = new Color32(160, 160, 170, 22),
                        Fog = new Color32(40, 40, 48, 30),
                        Beam = new Color32(180, 180, 190, 32)
                    };
                case ClubTheme.GlassCathedral:
                    return new ClubPalette
                    {
                        Void = new Color32(6, 10, 20, 255),
                        Wall = new Color32(18, 32, 56, 255),
                        Floor = new Color32(10, 16, 32, 255),
                        FloorLine = new Color32(48, 72, 110, 255),
                        Shadow = new Color32(6, 10, 22, 255),
                        Panel = new Color32(28, 48, 78, 255),
                        Brick = new Color32(70, 96, 130, 255),
                        Stage = new Color32(12, 20, 40, 255),
                        StageEdge = new Color32(160, 196, 220, 255),
                        CrowdCold = new Color32(150, 176, 200, 255),
                        CrowdMid = new Color32(186, 206, 220, 255),
                        CrowdHot = new Color32(220, 230, 236, 255),
                        AccentA = new Color32(186, 224, 255, 255),
                        AccentADim = new Color32(48, 80, 112, 255),
                        AccentB = new Color32(232, 196, 120, 255),
                        AccentBDim = new Color32(112, 84, 40, 255),
                        Mint = new Color32(200, 230, 240, 255),
                        Amber = new Color32(232, 196, 120, 255),
                        Ray = new Color32(180, 210, 240, 26),
                        Fog = new Color32(40, 60, 90, 28),
                        Beam = new Color32(200, 230, 255, 36)
                    };
                case ClubTheme.NightExchange:
                    return new ClubPalette
                    {
                        Void = new Color32(4, 6, 12, 255),
                        Wall = new Color32(10, 16, 32, 255),
                        Floor = new Color32(6, 10, 18, 255),
                        FloorLine = new Color32(36, 48, 32, 255),
                        Shadow = new Color32(4, 6, 10, 255),
                        Panel = new Color32(18, 28, 22, 255),
                        Brick = new Color32(32, 44, 36, 255),
                        Stage = new Color32(8, 14, 16, 255),
                        StageEdge = new Color32(196, 154, 64, 255),
                        CrowdCold = new Color32(72, 96, 88, 255),
                        CrowdMid = new Color32(164, 140, 72, 255),
                        CrowdHot = new Color32(220, 184, 88, 255),
                        AccentA = new Color32(72, 200, 128, 255),
                        AccentADim = new Color32(24, 72, 48, 255),
                        AccentB = new Color32(210, 88, 64, 255),
                        AccentBDim = new Color32(96, 36, 28, 255),
                        Mint = new Color32(88, 196, 140, 255),
                        Amber = new Color32(232, 176, 64, 255),
                        Ray = new Color32(200, 150, 48, 24),
                        Fog = new Color32(16, 24, 20, 30),
                        Beam = new Color32(232, 180, 72, 32)
                    };
                case ClubTheme.RoofRelay:
                    return new ClubPalette
                    {
                        Void = new Color32(2, 6, 16, 255),
                        Wall = new Color32(8, 18, 40, 255),
                        Floor = new Color32(4, 10, 22, 255),
                        FloorLine = new Color32(24, 48, 88, 255),
                        Shadow = new Color32(2, 6, 14, 255),
                        Panel = new Color32(16, 32, 64, 255),
                        Brick = new Color32(28, 48, 84, 255),
                        Stage = new Color32(6, 14, 28, 255),
                        StageEdge = new Color32(64, 120, 180, 255),
                        CrowdCold = new Color32(64, 88, 140, 255),
                        CrowdMid = new Color32(96, 128, 176, 255),
                        CrowdHot = new Color32(150, 180, 210, 255),
                        AccentA = new Color32(255, 186, 48, 255),
                        AccentADim = new Color32(112, 72, 16, 255),
                        AccentB = new Color32(96, 168, 255, 255),
                        AccentBDim = new Color32(32, 64, 120, 255),
                        Mint = new Color32(120, 190, 255, 255),
                        Amber = new Color32(255, 186, 48, 255),
                        Ray = new Color32(80, 140, 220, 26),
                        Fog = new Color32(8, 20, 48, 34),
                        Beam = new Color32(255, 190, 64, 36)
                    };
                case ClubTheme.FinalFestival:
                    return new ClubPalette
                    {
                        Void = new Color32(14, 4, 6, 255),
                        Wall = new Color32(40, 12, 16, 255),
                        Floor = new Color32(20, 8, 8, 255),
                        FloorLine = new Color32(88, 36, 24, 255),
                        Shadow = new Color32(16, 4, 6, 255),
                        Panel = new Color32(64, 24, 20, 255),
                        Brick = new Color32(110, 36, 32, 255),
                        Stage = new Color32(28, 10, 10, 255),
                        StageEdge = new Color32(220, 160, 48, 255),
                        CrowdCold = new Color32(150, 64, 56, 255),
                        CrowdMid = new Color32(200, 96, 48, 255),
                        CrowdHot = new Color32(255, 168, 64, 255),
                        AccentA = new Color32(255, 204, 72, 255),
                        AccentADim = new Color32(128, 84, 24, 255),
                        AccentB = new Color32(255, 64, 84, 255),
                        AccentBDim = new Color32(120, 24, 36, 255),
                        Mint = new Color32(255, 196, 96, 255),
                        Amber = new Color32(255, 180, 48, 255),
                        Ray = new Color32(255, 140, 48, 28),
                        Fog = new Color32(80, 20, 16, 30),
                        Beam = new Color32(255, 200, 72, 40)
                    };
                default:
                    return new ClubPalette
                    {
                        Void = new Color32(6, 6, 16, 255),
                        Wall = new Color32(16, 14, 36, 255),
                        Floor = new Color32(8, 8, 18, 255),
                        FloorLine = new Color32(36, 32, 64, 255),
                        Shadow = new Color32(10, 6, 14, 255),
                        Panel = new Color32(34, 36, 64, 255),
                        Brick = new Color32(58, 40, 78, 255),
                        Stage = new Color32(12, 10, 28, 255),
                        StageEdge = new Color32(72, 56, 120, 255),
                        CrowdCold = new Color32(120, 128, 176, 255),
                        CrowdMid = new Color32(188, 128, 168, 255),
                        CrowdHot = new Color32(236, 188, 156, 255),
                        AccentA = new Color32(88, 196, 255, 255),
                        AccentADim = new Color32(40, 72, 128, 255),
                        AccentB = new Color32(168, 120, 255, 255),
                        AccentBDim = new Color32(72, 48, 120, 255),
                        Mint = new Color32(94, 212, 177, 255),
                        Amber = new Color32(242, 196, 96, 255),
                        Ray = new Color32(72, 140, 255, 30),
                        Fog = new Color32(40, 28, 80, 28),
                        Beam = new Color32(96, 200, 255, 40)
                    };
            }
        }

        private void Draw(
            int beat,
            int band,
            int cue,
            int riser,
            bool signFlash,
            int intensity,
            int impulse,
            float motionPhase,
            int population,
            int motionStep)
        {
            ClubPalette p = _pal;
            Fill(p.Void);
            FillRect(0, 40, Width, Height - 40, p.Wall);
            FillRect(0, 0, Width, 34, p.Floor);
            FillRect(0, 32, Width, 5, p.FloorLine);

            bool fault = cue == HallCue.Fault;
            bool down = beat == 0;
            HallVisualStage stage = ResolveStage(population, impulse, fault);
            int mood = MoodFor(stage);
            // Lamps and the shell follow the room, not a cheer bump on an empty floor.
            DrawAtmosphere(mood);
            DrawStageShell(p, mood);
            bool hotCones = !fault && down
                && (stage == HallVisualStage.Rave || stage == HallVisualStage.Trash);
            DrawArchitecture(p, hotCones);

            bool mintPeak = !fault && down
                && (stage == HallVisualStage.Rave || stage == HallVisualStage.Trash);
            Color32 leftBulb = fault
                ? p.AccentADim
                : (mintPeak ? p.Mint : (down ? p.AccentA : p.AccentADim));
            Color32 rightBulb = fault
                ? p.Amber
                : (mintPeak ? p.Mint : (down ? p.AccentB : p.AccentBDim));
            if (!fault && !mintPeak && riser > 0)
            {
                leftBulb = riser >= 2 ? p.Amber : p.AccentBDim;
                rightBulb = riser >= 2 ? p.Amber : p.AccentB;
            }

            // Solo keeps a foot pool, not shafts. Slump stays dark.
            if (mood > 0)
                DrawGodrays(leftBulb, rightBulb, down, mood);
            DrawLamp(116, 108, leftBulb, p);
            DrawLamp(340, 108, rightBulb, p);
            DrawSign(156, 92, leftBulb, signFlash, p);

            DrawCrowd(beat, impulse, motionPhase, motionStep, stage, p);

            if (mintPeak && !GameSettings.ReducedFlashing && _theme != ClubTheme.RaveDistrict)
                DrawMintBeatFlash(p);

            if (Has(HallLook(), ClubSilhouette.Antenna))
                DrawSignalBeams(beat, down, GameSettings.ReducedFlashing ? Mathf.Min(mood, 1) : mood, p);

            DrawTrackWash(beat);

            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
        }

        /// <summary>
        /// Special-record wash over the finished hall. Alphas stay low so bodies keep their silhouette.
        /// Reduced flashing holds sweeps (rain, siren, hush motes, hardline, ledger, anthem squares).
        /// No full-frame blink.
        /// </summary>
        private void DrawTrackWash(int beat)
        {
            switch (_trackMood)
            {
                case TrackMood.AmberBloom:
                {
                    BlendRect(0, 0, Width, 34, new Color32(255, 176, 64, 110));
                    for (int i = 0; i < AuraMoteX.Length; i++)
                        FillRect(AuraMoteX[i], AuraMoteY[i] + beat * 3, 2, 2, new Color32(255, 210, 90, 200));
                    break;
                }
                case TrackMood.VoidStatic:
                {
                    for (int y = 40; y <= 160; y += 4)
                        BlendRect(0, y, Width, 1, new Color32(180, 190, 210, 80));
                    BlendRect(0, 140, Width, Height - 140, new Color32(20, 24, 40, 90));
                    break;
                }
                case TrackMood.GlassRain:
                {
                    bool hold = GameSettings.ReducedFlashing;
                    for (int i = 0; i < 8; i++)
                    {
                        int drift = hold ? i * 11 : beat * 10 + i * 11;
                        BlendRect(24 + i * 58, 48 + (drift % 70), 2, 36, new Color32(120, 220, 255, 130));
                    }
                    break;
                }
                case TrackMood.RedlineSiren:
                {
                    int sweepX = GameSettings.ReducedFlashing ? 180 : (beat * 120) % (Width - 72);
                    BlendRect(sweepX, 148, 72, 5, new Color32(255, 48, 56, 160));
                    BlendRect(96, 120, 8, 4, new Color32(255, 64, 72, 200));
                    BlendRect(360, 120, 8, 4, new Color32(255, 64, 72, 200));
                    break;
                }
                case TrackMood.VelvetHush:
                {
                    BlendRect(0, Height - 28, Width, 28, new Color32(112, 48, 150, 96));
                    int drift = GameSettings.ReducedFlashing ? 0 : beat * 2;
                    for (int i = 0; i < 6; i++)
                        BlendRect(48 + i * 72 + drift, 146 + (i & 1) * 6, 2, 2, new Color32(198, 150, 220, 90));
                    break;
                }
                case TrackMood.Hardline:
                {
                    int step = GameSettings.ReducedFlashing ? 0 : beat * 8;
                    BlendRect(100 + step, 44, 4, 84, new Color32(255, 210, 40, 96));
                    BlendRect(236 + step, 44, 4, 84, new Color32(255, 210, 40, 96));
                    BlendRect(364 + step, 44, 4, 84, new Color32(255, 196, 32, 80));
                    break;
                }
                case TrackMood.SaltChrome:
                {
                    BlendRect(0, 78, Width, 1, new Color32(80, 220, 230, 72));
                    BlendRect(0, 110, Width, 1, new Color32(160, 236, 240, 56));
                    break;
                }
                case TrackMood.StaticCrown:
                {
                    for (int y = 52; y <= 128; y += 8)
                        BlendRect(0, y, Width, 1, new Color32(148, 148, 156, 36));
                    BlendRect((Width - 36) / 2, Height - 3, 36, 3, new Color32(236, 236, 242, 100));
                    break;
                }
                case TrackMood.PrismChoir:
                {
                    Color32 shard = new Color32(214, 220, 240, 76);
                    for (int i = 0; i < 6; i++)
                    {
                        int x0 = 40 + i * 72;
                        int y0 = 64 + (i & 1) * 18;
                        for (int s = 0; s < 5; s++)
                            BlendRect(x0 + s * 2, y0 + s * 2, 2, 2, shard);
                    }
                    break;
                }
                case TrackMood.Ledger:
                {
                    int slide = GameSettings.ReducedFlashing ? 0 : beat * 4;
                    Color32 tick = new Color32(232, 168, 48, 100);
                    for (int i = 0; i < 12; i++)
                        BlendRect(16 + i * 38 + slide, 32, 2, 2, tick);
                    break;
                }
                case TrackMood.Skywire:
                {
                    for (int i = 0; i < 8; i++)
                        BlendRect(36 + i * 56, Height - 64, 1, 64, new Color32(150, 196, 230, 64));
                    break;
                }
                case TrackMood.FestivalAnthem:
                {
                    BlendRect(0, 0, Width, 8, new Color32(255, 188, 48, 88));
                    int lift = GameSettings.ReducedFlashing ? 0 : beat * 2;
                    BlendRect(70, 16 + lift, 2, 2, new Color32(255, 210, 96, 100));
                    BlendRect(150, 20 + lift, 2, 2, new Color32(255, 210, 96, 100));
                    BlendRect(240, 16 + lift, 2, 2, new Color32(255, 196, 72, 96));
                    BlendRect(330, 20 + lift, 2, 2, new Color32(255, 210, 96, 100));
                    BlendRect(410, 16 + lift, 2, 2, new Color32(255, 210, 96, 100));
                    break;
                }
            }
        }

        /// <summary>Ray / shell level. Slump and Solo stay at 0.</summary>
        private static int MoodFor(HallVisualStage stage)
        {
            if (stage == HallVisualStage.Trickle)
                return 1;
            if (stage == HallVisualStage.Groove || stage == HallVisualStage.Rave)
                return 2;
            if (stage == HallVisualStage.Trash)
                return 3;
            return 0;
        }

        private static string ThemeClubId(ClubTheme theme)
        {
            switch (theme)
            {
                case ClubTheme.Warehouse:
                    return Act1ClubIds.WarehousePulse;
                case ClubTheme.BassFactory:
                    return Act1ClubIds.BassFactory;
                case ClubTheme.TheSignal:
                    return Act1ClubIds.TheSignal;
                case ClubTheme.DreamLounge:
                    return CampaignClubIds.DreamLounge;
                case ClubTheme.RaveDistrict:
                    return CampaignClubIds.RaveDistrict;
                case ClubTheme.ChromePier:
                    return CampaignClubIds.ChromePier;
                case ClubTheme.TheStaticThrone:
                    return CampaignClubIds.TheStaticThrone;
                case ClubTheme.GlassCathedral:
                    return CampaignClubIds.GlassCathedral;
                case ClubTheme.NightExchange:
                    return CampaignClubIds.NightExchange;
                case ClubTheme.RoofRelay:
                    return CampaignClubIds.RoofRelay;
                case ClubTheme.FinalFestival:
                    return CampaignClubIds.FinalFestival;
                default:
                    return Act1ClubIds.NeonBasement;
            }
        }

        private ClubSilhouette HallLook()
        {
            return ClubLook.Mask(ThemeClubId(_theme));
        }

        private static bool Has(ClubSilhouette mask, ClubSilhouette bit)
        {
            return (mask & bit) != 0;
        }

        private void DrawAtmosphere(int intensity)
        {
            ClubPalette p = _pal;
            ClubSilhouette look = HallLook();
            // Deep night void strip at the top — Octopath-style black sky.
            FillRect(0, 150, Width, 18, p.Void);
            BlendRect(0, 140, Width, 16, p.Fog);

            if (Has(look, ClubSilhouette.Truss))
            {
                BlendRect(0, 60, Width, 40, p.Fog);
                BlendRect(20, 90, 80, 28, p.Fog);
                BlendRect(380, 90, 80, 28, p.Fog);
            }
            else if (Has(look, ClubSilhouette.EmberFloor))
            {
                // Static soot. Ember glow is the floor, not a downbeat flash.
                BlendRect(0, 34, Width, 16, p.Fog);
            }
            else if (Has(look, ClubSilhouette.LowCeiling))
            {
                BlendRect(0, 120, Width, 24, p.Fog);
            }

            if (intensity >= 2)
            {
                Color32 ray = p.Ray;
                ray.a = (byte)(intensity >= 3 ? 48 : 36);
                BlendRect(100, 50, 280, 60, ray);
            }
        }

        private void DrawStageShell(ClubPalette p, int intensity)
        {
            // Raised deck is the rig. Side frame changes with the room.
            FillRect(96, 70, 288, 42, p.Stage);
            FillRect(88, 68, 304, 4, p.StageEdge);
            if (intensity >= 1)
                FillRect(86, 66, 308, 2, p.StageEdge);
            FillRect(108, 74, 264, 3, p.Shadow);
            if (intensity >= 2)
                FillRect(112, 76, 256, 5, p.Shadow);
            FillRect(120, 78, 240, 28, p.Panel);

            ClubSilhouette look = HallLook();
            if (Has(look, ClubSilhouette.Towers))
                DrawLightTowers(p);
            else if (Has(look, ClubSilhouette.LowCeiling))
                DrawBasementPillars(p, intensity);

            if (Has(look, ClubSilhouette.BossAxis))
            {
                // Symmetric boss frame. Diamond sits under the marquee, on the deck.
                FillRect(236, 70, 8, 52, p.AccentADim);
                FillRect(232, 118, 16, 4, p.AccentA);
                FillRect(100, 66, 280, 2, p.AccentADim);
                DrawDiamondOutline(240, 80, 8, p.AccentA);
            }
        }

        /// <summary>Short basement columns. Top stays near y=122 — below the signal towers.</summary>
        private void DrawBasementPillars(ClubPalette p, int intensity)
        {
            FillRect(72, 70, 18, 52, p.Stage);
            FillRect(390, 70, 18, 52, p.Stage);
            FillRect(76, 112, 10, 4, p.StageEdge);
            FillRect(394, 112, 10, 4, p.StageEdge);
            if (intensity >= 2)
            {
                FillRect(70, 68, 2, 56, p.StageEdge);
                FillRect(408, 68, 2, 56, p.StageEdge);
            }
        }

        /// <summary>Edge light towers. Cap pixel clears the basement pillar tops.</summary>
        private void DrawLightTowers(ClubPalette p)
        {
            DrawLightTower(0, 0, p);
            DrawLightTower(470, 464, p);
        }

        private void DrawLightTower(int x, int headX, ClubPalette p)
        {
            FillRect(x, 52, 10, 100, p.Panel);
            FillRect(headX, 148, 16, 8, p.StageEdge);
            FillRect(x + 2, 136, 6, 6, p.AccentA);
            FillRect(x + 4, 156, 2, 6, p.AccentA);
        }

        /// <summary>2px diamond outline. Hollow — no label.</summary>
        private void DrawDiamondOutline(int cx, int cy, int radius, Color32 color)
        {
            for (int i = 0; i <= radius; i++)
            {
                int rise = radius - i;
                FillRect(cx + i, cy + rise, 2, 2, color);
                FillRect(cx - i, cy + rise, 2, 2, color);
                FillRect(cx + i, cy - rise, 2, 2, color);
                FillRect(cx - i, cy - rise, 2, 2, color);
            }
        }

        /// <summary>
        /// Intensity 3 downbeat: mint rim ticks only — not a full-screen strobe.
        /// </summary>
        private void DrawMintBeatFlash(ClubPalette p)
        {
            Color32 mint = p.Mint;
            mint.a = 90;
            BlendRect(118, 68, 36, 2, mint);
            BlendRect(326, 68, 36, 2, mint);
            BlendRect(232, 66, 16, 2, mint);
            mint.a = 56;
            BlendRect(140, 110, 8, 4, mint);
            BlendRect(332, 110, 8, 4, mint);
        }

        private void DrawArchitecture(ClubPalette p, bool hotCones)
        {
            ClubSilhouette look = HallLook();

            if (Has(look, ClubSilhouette.LowCeiling))
                DrawPlasterCeiling(p);
            if (Has(look, ClubSilhouette.Racks))
                DrawRacks(p);
            if (Has(look, ClubSilhouette.Truss))
                DrawTruss(p);
            if (Has(look, ClubSilhouette.Pipes))
                DrawPipes(p);
            if (Has(look, ClubSilhouette.Press))
                DrawPress(p);
            if (Has(look, ClubSilhouette.DockDoor))
                DrawDockDoor(p);
            if (Has(look, ClubSilhouette.Antenna))
                DrawHallMast(p);

            // Rig, every room. Speakers sit in front of wall racks.
            FillRect(48, 86, 32, 36, p.Panel);
            FillRect(52, 108, 24, 6, p.AccentADim);
            FillRect(52, 98, 24, 6, p.CrowdMid);
            FillRect(52, 88, 24, 6, p.AccentBDim);
            DrawSpeaker(8, 36, hotCones ? p.AccentA : p.AccentADim, p);
            DrawSpeaker(436, 36, hotCones ? p.AccentB : p.AccentBDim, p);

            if (Has(look, ClubSilhouette.Curtain))
                DrawCurtain(p);
            if (Has(look, ClubSilhouette.Bar))
                DrawBar(p);
            if (Has(look, ClubSilhouette.LowCeiling))
                DrawNeonTubes(p);
            if (Has(look, ClubSilhouette.EmberFloor))
                DrawEmberFloor(p);
        }

        /// <summary>Low plaster courses, y=142–167. No truss.</summary>
        private void DrawPlasterCeiling(ClubPalette p)
        {
            FillRect(0, 142, Width, 26, p.Brick);
            FillRect(0, 142, Width, 4, p.Shadow);
            FillRect(0, 151, Width, 3, p.Panel);
            FillRect(0, 159, Width, 3, p.Shadow);
            FillRect(0, 164, Width, 4, p.Wall);
            FillRect(48, 146, 26, 6, p.Wall);
            FillRect(200, 154, 36, 4, p.Panel);
            FillRect(360, 145, 22, 5, p.Shadow);
        }

        /// <summary>Two short tubes. Not a word.</summary>
        private void DrawNeonTubes(ClubPalette p)
        {
            FillRect(176, 148, 36, 2, p.AccentA);
            FillRect(268, 148, 36, 2, p.AccentB);
        }

        /// <summary>Left folds only, x=4–18, y=90–150.</summary>
        private void DrawCurtain(ClubPalette p)
        {
            FillRect(4, 90, 15, 60, p.Shadow);
            FillRect(4, 90, 3, 60, p.Brick);
            FillRect(8, 90, 2, 60, p.AccentB);
            FillRect(11, 90, 3, 60, p.Panel);
            FillRect(15, 90, 4, 60, p.Brick);
            FillRect(6, 90, 2, 5, p.Wall);
            FillRect(12, 90, 2, 7, p.Wall);
            FillRect(4, 146, 15, 4, p.AccentBDim);
        }

        /// <summary>Right counter, x=430–468, y=100–150. Four 2px bottles, no letters.</summary>
        private void DrawBar(ClubPalette p)
        {
            FillRect(430, 100, 38, 28, p.Panel);
            FillRect(430, 126, 38, 4, p.StageEdge);
            FillRect(434, 104, 30, 4, p.Shadow);
            FillRect(436, 130, 2, 12, p.AccentA);
            FillRect(444, 130, 2, 20, p.AccentB);
            FillRect(452, 130, 2, 10, p.Amber);
            FillRect(460, 130, 2, 16, p.Mint);
        }

        /// <summary>Diagonal 2px steps between chords, y=148–164.</summary>
        private void DrawTruss(ClubPalette p)
        {
            FillRect(0, 148, Width, 2, p.Brick);
            FillRect(0, 162, Width, 2, p.StageEdge);
            for (int x = 0; x < Width; x += 32)
                FillRect(x, 148, 2, 16, p.Brick);
            FillRect(478, 148, 2, 16, p.Brick);

            bool rise = true;
            for (int bay = 0; bay < Width; bay += 16)
            {
                for (int s = 0; s < 8; s++)
                {
                    int y = rise ? 148 + s * 2 : 162 - s * 2;
                    FillRect(bay + s * 2, y, 2, 2, p.FloorLine);
                }

                rise = !rise;
            }
        }

        /// <summary>Centered roll-up, x=200–280, y=124–164, 4px amber slit.</summary>
        private void DrawDockDoor(ClubPalette p)
        {
            FillRect(200, 124, 80, 40, p.Panel);
            FillRect(200, 160, 80, 4, p.StageEdge);
            FillRect(200, 124, 80, 3, p.Shadow);
            for (int i = 0; i < 4; i++)
                FillRect(204, 130 + i * 8, 72, 2, p.Shadow);
            FillRect(238, 128, 4, 32, p.Amber);
        }

        private void DrawRacks(ClubPalette p)
        {
            DrawRackBay(2, p);
            DrawRackBay(448, p);
        }

        /// <summary>Shelf ticks inside the side margins, y=80–150.</summary>
        private void DrawRackBay(int x, ClubPalette p)
        {
            FillRect(x, 80, 3, 70, p.Brick);
            FillRect(x + 26, 80, 3, 70, p.Panel);
            for (int i = 0; i < 5; i++)
            {
                int y = 84 + i * 12;
                FillRect(x, y, 29, 2, p.StageEdge);
                if ((i & 1) == 0)
                    FillRect(x + 6, y + 2, 10, 8, p.Shadow);
                else
                    FillRect(x + 14, y + 2, 8, 6, p.Brick);
            }
        }

        /// <summary>Chunky inverted U in the sky band. Legs flank the marquee, clear of the lamps.</summary>
        private void DrawPress(ClubPalette p)
        {
            FillRect(96, 124, 18, 32, p.Brick);
            FillRect(366, 124, 18, 32, p.Brick);
            FillRect(96, 144, 288, 12, p.Panel);
            FillRect(114, 148, 252, 4, p.Shadow);
        }

        /// <summary>Margin risers, elbow blocks, one ceiling run.</summary>
        private void DrawPipes(ClubPalette p)
        {
            FillRect(0, 40, 6, 122, p.Brick);
            FillRect(474, 40, 6, 122, p.Brick);
            FillRect(0, 154, 16, 12, p.Panel);
            FillRect(464, 154, 16, 12, p.Panel);
            FillRect(6, 162, 468, 4, p.AccentADim);
            FillRect(0, 78, 8, 4, p.Shadow);
            FillRect(0, 110, 8, 4, p.Shadow);
            FillRect(472, 78, 8, 4, p.Shadow);
            FillRect(472, 110, 8, 4, p.Shadow);
        }

        /// <summary>Static floor glow. Not beat-gated, no sparks.</summary>
        private void DrawEmberFloor(ClubPalette p)
        {
            Color32 glow = p.Amber;
            glow.a = 78;
            BlendRect(150, 28, 180, 12, glow);
            Color32 core = p.AccentA;
            core.a = 46;
            BlendRect(190, 31, 100, 6, core);
        }

        /// <summary>Mast and dish on the center axis. Not a word.</summary>
        private void DrawHallMast(ClubPalette p)
        {
            FillRect(238, 140, 4, 26, p.StageEdge);
            FillRect(242, 158, 12, 2, p.AccentA);
            FillRect(254, 152, 6, 10, p.Panel);
            FillRect(252, 154, 2, 6, p.AccentA);
        }

        private void DrawGodrays(Color32 leftBulb, Color32 rightBulb, bool down, int intensity)
        {
            ClubPalette p = _pal;
            byte strength = (byte)(28 + intensity * 8 + (down ? 8 : 0));
            if (strength > 72)
                strength = 72;
            Color32 leftRay = leftBulb;
            leftRay.a = strength;
            Color32 rightRay = rightBulb;
            rightRay.a = strength;

            // Soft vertical shafts — godray-lite rectangles, not full-frame flash.
            int shaft = intensity >= 2 ? 12 : 10;
            BlendRect(118, 40, shaft, 68, leftRay);
            BlendRect(124, 50, intensity >= 2 ? 22 : 18, 50, p.Ray);
            BlendRect(342, 40, shaft, 68, rightRay);
            BlendRect(330, 50, intensity >= 2 ? 22 : 18, 50, p.Ray);

            if (Has(HallLook(), ClubSilhouette.LowCeiling))
            {
                BlendRect(200, 48, 8, 56, p.Beam);
                BlendRect(272, 48, 8, 56, p.Beam);
            }
        }

        private void DrawSignalBeams(int beat, bool down, int intensity, ClubPalette p)
        {
            // Boss motif: centered cyan beam + mirrored side shafts; pulse scales with cheer.
            int pulse = (down ? 10 : 2) + intensity * 6;
            Color32 core = p.AccentA;
            core.a = (byte)Mathf.Clamp(56 + pulse * 3, 40, 140);
            Color32 wing = p.Beam;
            wing.a = (byte)Mathf.Clamp(28 + pulse * 2, 24, 100);

            int coreW = intensity >= 2 ? 10 : 8;
            int wingW = 20 + intensity * 4;
            BlendRect(240 - coreW / 2, 34, coreW, 90, core);
            BlendRect(240 - wingW / 2, 50, wingW, 60, wing);
            BlendRect(180 - beat - intensity, 56, 6 + intensity / 2, 50, wing);
            BlendRect(294 + beat + intensity, 56, 6 + intensity / 2, 50, wing);
            FillRect(234, 124, 12, 2, p.AccentA);
            FillRect(220, 126, 40, 2, p.AccentADim);
            if (intensity >= 3 && down)
            {
                Color32 mint = p.Mint;
                mint.a = 70;
                BlendRect(238, 36, 4, 20, mint);
            }
        }

        private enum DancerKind
        {
            Hood = 0,
            Phones = 1,
            Scarf = 2,
            Vest = 3,
            Visor = 4
        }

        private enum BodyPose
        {
            Weak = 0,
            Groove = 1,
            Peak = 2,
            Trash = 3,
            Slump = 4,
            GrooveRow = 5,
            TrickleRow = 6
        }

        private void DrawCrowd(
            int beat,
            int impulse,
            float motionPhase,
            int motionStep,
            HallVisualStage stage,
            ClubPalette p)
        {
            bool signal = _theme == ClubTheme.TheSignal;
            bool trashGesture = impulse >= 3
                && (stage == HallVisualStage.Solo || stage == HallVisualStage.Trickle);
            bool cheer = beat == 0
                && (stage == HallVisualStage.Rave
                    || stage == HallVisualStage.Trash
                    || trashGesture);

            int[] trickleRow = TrickleX;
            int[] packedRow = PackedX;
            int[] peakRow = PeakX;
            int[] frontRow = FrontX;
            int[] frontPeakRow = FrontPeakX;
            if (_theme == ClubTheme.RaveDistrict)
            {
                trickleRow = DistrictTrickleX;
                packedRow = DistrictPackedX;
                peakRow = DistrictPeakX;
                frontRow = DistrictFrontX;
                frontPeakRow = DistrictFrontPeakX;
            }
            else if (_theme == ClubTheme.FinalFestival)
            {
                trickleRow = FestivalTrickleX;
                packedRow = FestivalPackedX;
                peakRow = FestivalPeakX;
                frontRow = FestivalFrontX;
                frontPeakRow = FestivalFrontPeakX;
            }

            if (_theme == ClubTheme.ChromePier)
                FillRect(0, 8, Width, 2, p.AccentB);

            if (stage == HallVisualStage.Groove
                || stage == HallVisualStage.Rave
                || stage == HallVisualStage.Trash)
                DrawFloorTicks(stage, beat, p);

            if (stage == HallVisualStage.Slump)
            {
                DrawBodies(SlumpX, 34, beat, motionPhase, 0, BodyPose.Slump, false, false, false, true, stage, p);
                return;
            }

            if (stage == HallVisualStage.Solo)
            {
                int x = SoloX[0];
                if (InSignalGap(x))
                    x = 200;
                if (_theme == ClubTheme.NightExchange)
                    x = 150;
                // Foot pool only — not the ceiling band and not the room wash.
                BlendRect(x - 24, 18, 48, 16, p.Fog);
                _soloSlot[0] = x;
                BodyPose pose = BodyPose.Weak;
                int amp = 1;
                if (trashGesture)
                {
                    pose = BodyPose.Trash;
                    amp = 2;
                }
                else if (impulse >= 2)
                {
                    pose = BodyPose.Groove;
                    amp = 2;
                }
                else if (impulse >= 1)
                    amp = 2;
                DrawBodies(_soloSlot, 34, beat, motionPhase, amp, pose, cheer, trashGesture, false, false, stage, p);
                if (trashGesture && CrowdSparkle())
                    DrawConfettiPinch(x, motionStep, p);
                return;
            }

            if (stage == HallVisualStage.Trickle)
            {
                BodyPose pose = trashGesture ? BodyPose.Trash : BodyPose.TrickleRow;
                DrawBodies(trickleRow, 34, beat, motionPhase, 2, pose, cheer, trashGesture, false, true, stage, p);
                if (trashGesture && CrowdSparkle())
                    DrawConfettiPinch(trickleRow[1], motionStep, p);
                return;
            }

            if (stage == HallVisualStage.Groove)
            {
                DrawBodies(packedRow, 34, beat, motionPhase, 2, BodyPose.GrooveRow, false, false, false, true, stage, p);
                return;
            }

            if (stage == HallVisualStage.Rave)
            {
                DrawBodies(peakRow, 34, beat, motionPhase, 2, BodyPose.Peak, cheer, false, false, false, stage, p);
                DrawBodies(frontRow, 12, beat, motionPhase, 2, BodyPose.Peak, cheer, false, true, true, stage, p);
                return;
            }

            // Trash: back row stays low. Only five front bodies jump.
            bool syncedKick = signal;
            DrawBodies(peakRow, 34, beat, motionPhase, 2, BodyPose.Trash, cheer, syncedKick, false, false, stage, p);
            DrawBodies(frontPeakRow, 12, beat, motionPhase, 2, BodyPose.Trash, cheer, syncedKick, true, false, stage, p);
            if (CrowdSparkle())
                DrawConfetti(motionStep, p);
        }

        /// <summary>Spark shower. Signal, the throne, and Rave District stay unflashed.</summary>
        private bool CrowdSparkle()
        {
            if (GameSettings.ReducedFlashing)
                return false;
            if (_theme == ClubTheme.TheSignal)
                return false;
            if (_theme == ClubTheme.TheStaticThrone)
                return false;
            if (_theme == ClubTheme.RaveDistrict)
                return false;
            return true;
        }

        private void DrawBodies(
            int[] xs,
            int foot,
            int beat,
            float motionPhase,
            int bobAmp,
            BodyPose pose,
            bool cheer,
            bool kick,
            bool front,
            bool shiftGap,
            HallVisualStage stage,
            ClubPalette p)
        {
            bool signal = _theme == ClubTheme.TheSignal;
            for (int i = 0; i < xs.Length; i++)
            {
                int x = xs[i];
                if (InSignalGap(x))
                {
                    if (!shiftGap)
                        continue;
                    int landing = x < (Width / 2) ? 200 : 280;
                    if (InSignalGap(landing) || LandsOnNeighbor(xs, i, landing))
                        continue;
                    x = landing;
                }

                if (_theme == ClubTheme.DreamLounge && xs.Length > 2 && (i & 1) != 0)
                    continue;
                if (_theme == ClubTheme.NightExchange && x > 188 && x < 292)
                {
                    if (xs.Length <= 2)
                        x = x < (Width / 2) ? 150 : 330;
                    else
                        continue;
                }
                if (_theme == ClubTheme.RoofRelay)
                    x += GameSettings.ReducedFlashing ? 2 : beat * 2;

                int amp = bobAmp;
                bool doKick = kick;
                if (stage == HallVisualStage.Trash && front && !signal)
                {
                    bool jump = ((i + beat) & 1) == 0;
                    amp = jump ? 6 : 2;
                    doKick = jump;
                }

                if (_theme == ClubTheme.DreamLounge && amp > 1)
                    amp = 1;
                if (_theme == ClubTheme.TheStaticThrone)
                    amp = 0;

                float phase = motionPhase;
                if (!signal)
                    phase += (i % 5) * 0.5f;
                int grounded = foot + BobStep(phase, amp);

                BodyPose drawn = pose;
                if (pose == BodyPose.GrooveRow)
                    drawn = (i == 2 || i == 5) ? BodyPose.Peak : BodyPose.Groove;
                else if (pose == BodyPose.TrickleRow)
                    drawn = i == 1 ? BodyPose.Groove : BodyPose.Weak;

                // Signal is one organism: no per-index arm flip.
                bool leftUp = signal || ((i + beat) & 1) == 0;
                bool cathedral = _theme == ClubTheme.GlassCathedral;
                bool chrome = _theme == ClubTheme.ChromePier;
                if (_theme == ClubTheme.TheStaticThrone && drawn != BodyPose.Slump)
                {
                    drawn = BodyPose.Weak;
                    leftUp = true;
                    doKick = false;
                }
                else if ((_theme == ClubTheme.RaveDistrict || _theme == ClubTheme.FinalFestival)
                    && drawn != BodyPose.Slump)
                {
                    drawn = BodyPose.Peak;
                    if (_theme == ClubTheme.RaveDistrict)
                        cheer = false;
                }

                Color32 color = CrowdPaint(stage, i, front, i == 1, p);
                FillRect(x - 5, foot - 2, 10, 2, p.Shadow);

                int headTop;
                int dressX = x;
                if (drawn == BodyPose.Slump)
                    DrawSlump(x, grounded, color, out headTop);
                else if (cathedral)
                    DrawTallThin(x, grounded, color, p, out headTop);
                else if (chrome)
                {
                    DrawLean(x, grounded, color, out headTop);
                    dressX = x + 3;
                }
                else if (drawn == BodyPose.Weak)
                    DrawWeak(x, grounded, color, leftUp, out headTop);
                else if (drawn == BodyPose.Groove)
                    DrawGroove(x, grounded, color, leftUp, out headTop);
                else if (drawn == BodyPose.Trash)
                    DrawTrash(x, grounded, color, i + beat, doKick, cheer, signal, p, out headTop);
                else
                    DrawPeak(x, grounded, color, cheer, p, out headTop);

                if (drawn == BodyPose.Slump || cathedral)
                    continue;

                ClubSilhouette look = HallLook();
                if (Has(look, ClubSilhouette.Antenna))
                {
                    DrawAntenna(dressX, headTop, p);
                }
                else
                {
                    DancerKind kind = (DancerKind)((i * 2 + beat) % 5);
                    if (Has(look, ClubSilhouette.Press))
                        kind = DancerKind.Visor;
                    else if (Has(look, ClubSilhouette.Truss))
                        kind = ((i + beat) & 1) == 0 ? DancerKind.Hood : DancerKind.Vest;

                    bool rim = stage == HallVisualStage.Rave || stage == HallVisualStage.Trash;
                    int headWidth = drawn == BodyPose.Weak ? 4 : 6;
                    DrawArchetype(dressX, grounded, headTop, headWidth, kind, rim, p);
                }
            }
        }

        /// <summary>
        /// Beam column x=232–248, plus 8px of air. Body span used here is the packed 18px.
        /// </summary>
        private bool InSignalGap(int x)
        {
            if (_theme != ClubTheme.TheSignal)
                return false;
            int left = x - 9;
            int right = x + 8;
            return right >= 224 && left <= 256;
        }

        /// <summary>18px body plus 8px of air needs 26px between centers.</summary>
        private bool LandsOnNeighbor(int[] xs, int index, int landing)
        {
            for (int j = 0; j < xs.Length; j++)
            {
                if (j == index)
                    continue;
                int other = xs[j];
                int otherX = other;
                if (InSignalGap(other))
                {
                    otherX = other < (Width / 2) ? 200 : 280;
                    if (otherX == landing)
                        return true;
                }

                if (Mathf.Abs(otherX - landing) < 26)
                    return true;
            }

            return false;
        }

        private static Color32 CrowdPaint(HallVisualStage stage, int index, bool front, bool lead, ClubPalette p)
        {
            if (stage == HallVisualStage.Slump)
                return p.CrowdCold;
            if (stage == HallVisualStage.Solo)
                return p.CrowdHot;
            if (stage == HallVisualStage.Trickle)
            {
                if (lead)
                    return p.CrowdHot;
                return (index & 1) == 0 ? p.CrowdCold : p.CrowdMid;
            }

            if (front)
                return p.CrowdHot;
            return (index & 1) == 0 ? p.CrowdMid : p.CrowdHot;
        }

        private void DrawFloorTicks(HallVisualStage stage, int beat, ClubPalette p)
        {
            int count = stage == HallVisualStage.Trash ? 16 : 8;
            int step = count == 16 ? 28 : 56;
            int origin = count == 16 ? 12 : 16;
            Color32 lit = stage == HallVisualStage.Trash ? p.AccentA : p.FloorLine;
            for (int i = 0; i < count; i++)
            {
                if (((i + beat) & 1) != 0)
                    continue;
                FillRect(origin + i * step, 30, 8, 2, lit);
            }
        }

        /// <summary>Two sparks by the body. Street drop on a small floor, not a hall-wide shower.</summary>
        private void DrawConfettiPinch(int anchorX, int motionStep, ClubPalette p)
        {
            if (_theme == ClubTheme.TheSignal || GameSettings.ReducedFlashing)
                return;
            int hop = (motionStep < 0 ? 0 : motionStep & 3) * 2;
            FillRect(anchorX - 10, 46 + hop, 2, 2, p.Mint);
            FillRect(anchorX + 8, 54 + hop, 2, 2, p.Amber);
        }

        private void DrawConfetti(int motionStep, ClubPalette p)
        {
            int step = motionStep < 0 ? 0 : motionStep;
            for (int i = 0; i < ConfettiX.Length; i++)
            {
                int y = ConfettiY[i] + ((step + i * 3) % 5) * 2;
                Color32 color = p.Mint;
                int slot = i % 3;
                if (slot == 1)
                    color = p.Amber;
                else if (slot == 2)
                    color = p.AccentA;
                FillRect(ConfettiX[i], y, 2, 2, color);
            }
        }

        /// <summary>Legacy discrete bob (beat index). Prefer <see cref="BobPhase"/>.</summary>
        private static int Bob(int beat, int intensity)
        {
            return BobPhase(beat, intensity);
        }

        /// <summary>
        /// Vertical bob amplitude scales with impulse/intensity.
        /// motionPhase in beat units; Abs(Sin(π·phase)) peaks on each beat — crisp JRPG hops.
        /// </summary>
        private static int BobPhase(float motionPhase, int impulse)
        {
            if (impulse <= 0)
                return 0;

            float wave = Mathf.Abs(Mathf.Sin(motionPhase * Mathf.PI));
            int amp;
            if (impulse == 1)
                amp = 2;
            else if (impulse == 2)
                amp = 4;
            else
                amp = 6; // Drop jump frames

            int bob = Mathf.RoundToInt(wave * amp);
            // Keep a tiny off-beat lift so the hall never reads as frozen while hot.
            if (bob == 0 && impulse >= 2)
                bob = 1;
            return bob;
        }

        /// <summary>Crisp hop. Amp is already the stage cap (never above 8).</summary>
        private static int BobStep(float phase, int amp)
        {
            if (amp <= 0)
                return 0;
            if (amp > 8)
                amp = 8;
            float wave = Mathf.Abs(Mathf.Sin(phase * Mathf.PI));
            return Mathf.RoundToInt(wave * amp);
        }

        /// <summary>Feet stay put. Torso and head shift 3px so the pier crowd leans.</summary>
        private void DrawLean(int x, int foot, Color32 color, out int headTop)
        {
            FillRect(x - 2, foot, 2, 7, color);
            FillRect(x + 1, foot, 2, 7, color);
            int lean = x + 3;
            FillRect(lean - 3, foot + 6, 6, 9, color);
            FillRect(lean - 2, foot + 14, 4, 6, color);
            headTop = foot + 20;
            FillRect(lean - 5, foot + 10, 2, 10, color);
            FillRect(lean + 3, foot + 10, 2, 10, color);
        }

        /// <summary>Narrow column, taller than a weak dancer. A 2px glass spike, no wide hat.</summary>
        private void DrawTallThin(int x, int foot, Color32 color, ClubPalette p, out int headTop)
        {
            FillRect(x - 2, foot, 2, 14, color);
            FillRect(x + 1, foot, 2, 14, color);
            FillRect(x - 2, foot + 13, 4, 12, color);
            FillRect(x - 1, foot + 24, 3, 6, color);
            headTop = foot + 30;
            FillRect(x - 4, foot + 16, 2, 14, color);
            FillRect(x + 3, foot + 16, 2, 14, color);
            FillRect(x, headTop, 2, 4, p.AccentA);
        }

        /// <summary>Hunched, feet together, crown at least 4px under the weak dancer.</summary>
        private void DrawSlump(int x, int foot, Color32 color, out int headTop)
        {
            FillRect(x - 2, foot, 2, 5, color);
            FillRect(x, foot, 2, 5, color);
            FillRect(x - 4, foot + 1, 2, 7, color);
            FillRect(x + 2, foot + 1, 2, 7, color);
            FillRect(x - 3, foot + 4, 6, 6, color);
            FillRect(x - 2, foot + 8, 4, 6, color);
            headTop = foot + 14;
        }

        /// <summary>Trying, not dancing. Arms stay inside the ribs. Width ≤ 12, height ≤ 26.</summary>
        private void DrawWeak(int x, int foot, Color32 color, bool leadLeft, out int headTop)
        {
            FillRect(x - 3, foot, 2, 7, color);
            FillRect(x + 1, foot, 2, 7, color);
            FillRect(x - 3, foot + 6, 6, 9, color);
            FillRect(x - 2, foot + 7, 2, 5, color);
            if (leadLeft)
                FillRect(x - 2, foot + 9, 2, 6, color);
            else
                FillRect(x, foot + 9, 2, 6, color);
            FillRect(x - 2, foot + 15, 4, 6, color);
            headTop = foot + 21;
        }

        /// <summary>One arm up. Column stays ≤ 16px so a trickle pocket does not become a packed row.</summary>
        private void DrawGroove(int x, int foot, Color32 color, bool leftUp, out int headTop)
        {
            FillRect(x - 5, foot, 3, 8, color);
            FillRect(x + 2, foot, 3, 8, color);
            FillRect(x - 4, foot + 7, 8, 7, color);
            if (leftUp)
                FillRect(x + 4, foot + 8, 3, 6, color);
            else
                FillRect(x - 7, foot + 8, 3, 6, color);

            FillRect(x - 3, foot + 12, 6, 6, color);
            headTop = foot + 18;
            int armBase = foot + 10;
            int armH = 16;
            if (leftUp)
                FillRect(x - 8, armBase, 3, armH, color);
            else
                FillRect(x + 5, armBase, 3, armH, color);
        }

        /// <summary>Both arms clear the crown by 8px. Outer pixels stay inside x−9..x+8.</summary>
        private void DrawPeak(int x, int foot, Color32 color, bool cheer, ClubPalette p, out int headTop)
        {
            FillRect(x - 5, foot, 3, 8, color);
            FillRect(x + 2, foot, 3, 8, color);
            FillRect(x - 4, foot + 7, 8, 7, color);
            FillRect(x - 3, foot + 12, 6, 6, color);
            headTop = foot + 18;
            int armBase = foot + 10;
            int armH = 16;
            FillRect(x - 9, armBase, 3, armH, color);
            FillRect(x + 6, armBase, 3, armH, color);
            if (cheer)
            {
                FillRect(x - 9, armBase + armH - 4, 4, 4, p.Mint);
                FillRect(x + 5, armBase + armH - 4, 4, 4, p.Mint);
            }
        }

        /// <summary>
        /// Unequal arms (about 16 vs 22). Kick is 4px out and up.
        /// Head shift is 0 when the hall is one organism.
        /// </summary>
        private void DrawTrash(
            int x,
            int foot,
            Color32 color,
            int variant,
            bool kick,
            bool cheer,
            bool synced,
            ClubPalette p,
            out int headTop)
        {
            int shift = 0;
            bool kickLeft = true;
            bool longLeft = true;
            if (!synced)
            {
                shift = ((variant % 3) - 1) * 2;
                kickLeft = (variant & 1) == 0;
                longLeft = ((variant >> 1) & 1) == 0;
            }

            if (kick && kickLeft)
            {
                FillRect(x - 9, foot + 4, 3, 6, color);
                FillRect(x + 2, foot, 3, 8, color);
            }
            else if (kick)
            {
                FillRect(x - 5, foot, 3, 8, color);
                FillRect(x + 6, foot + 4, 3, 6, color);
            }
            else if (longLeft)
            {
                FillRect(x - 6, foot, 3, 8, color);
                FillRect(x + 2, foot, 3, 7, color);
            }
            else
            {
                FillRect(x - 5, foot, 3, 7, color);
                FillRect(x + 3, foot, 3, 8, color);
            }

            FillRect(x - 4, foot + 7, 8, 7, color);
            int headY = foot + 12 + shift;
            FillRect(x - 3, headY, 6, 6, color);
            headTop = headY + 6;

            int crown = headTop - 1;
            int armBase = foot + 10;
            int shortH = (crown + 8) - armBase + 1;
            int longH = (crown + 14) - armBase + 1;
            if (longLeft)
            {
                FillRect(x - 9, armBase, 3, longH, color);
                FillRect(x + 6, armBase, 3, shortH, color);
                if (cheer)
                {
                    FillRect(x - 9, armBase + longH - 4, 4, 4, p.Mint);
                    FillRect(x + 5, armBase + shortH - 4, 4, 4, p.Mint);
                }
            }
            else
            {
                FillRect(x + 6, armBase, 3, longH, color);
                FillRect(x - 9, armBase, 3, shortH, color);
                if (cheer)
                {
                    FillRect(x + 5, armBase + longH - 4, 4, 4, p.Mint);
                    FillRect(x - 9, armBase + shortH - 4, 4, 4, p.Mint);
                }
            }
        }

        private void DrawAntenna(int x, int headTop, ClubPalette p)
        {
            // 1px of empty air, then 2×6. Same offset on every head in the row.
            FillRect(x - 1, headTop + 1, 2, 6, p.AccentA);
        }

        private void DrawArchetype(
            int x,
            int foot,
            int headTop,
            int headWidth,
            DancerKind kind,
            bool rim,
            ClubPalette p)
        {
            if (kind == DancerKind.Hood)
            {
                FillRect(x - 4, headTop, 8, 2, p.AccentADim);
                FillRect(x - 2, headTop + 2, 4, 2, p.AccentADim);
                FillRect(x - 1, headTop + 4, 2, 1, p.AccentADim);
            }
            else if (kind == DancerKind.Phones)
            {
                int skullLeft = headWidth <= 4 ? x - 2 : x - 3;
                int skullRight = skullLeft + headWidth - 1;
                FillRect(skullLeft - 4, headTop - 4, 4, 4, p.AccentA);
                FillRect(skullRight + 1, headTop - 4, 4, 4, p.AccentA);
                FillRect(skullLeft, headTop - 1, headWidth, 1, p.AccentA);
            }
            else if (kind == DancerKind.Scarf)
            {
                FillRect(x - 6, headTop - 9, 12, 3, p.AccentB);
            }
            else if (kind == DancerKind.Vest)
            {
                FillRect(x - 3, foot + 9, 3, 3, p.Brick);
                FillRect(x + 1, foot + 9, 3, 3, p.Brick);
            }
            else
            {
                FillRect(x - 4, headTop - 2, 8, 2, p.Amber);
            }

            if (rim && (kind == DancerKind.Phones || kind == DancerKind.Scarf))
                FillRect(x - 5, foot + 8, 1, 8, p.AccentA);
        }

        private void DrawSpeaker(int x, int y, Color32 cone, ClubPalette p)
        {
            FillRect(x, y, 36, 58, p.Panel);
            FillRect(x + 6, y + 32, 24, 18, p.Void);
            FillRect(x + 10, y + 36, 16, 10, cone);
            FillRect(x + 14, y + 39, 8, 4, p.Void);
            FillRect(x + 8, y + 10, 20, 8, p.Brick);
        }

        private void DrawLamp(int x, int y, Color32 bulb, ClubPalette p)
        {
            FillRect(x, y, 20, 16, p.Panel);
            FillRect(x + 4, y + 4, 12, 8, bulb);
        }

        private void DrawSign(int x, int y, Color32 neon, bool flash, ClubPalette p)
        {
            const int boardWidth = 168;
            const int boardHeight = 44;
            const int scale = 4;
            FillRect(x, y, boardWidth, boardHeight, p.Panel);
            // Reduced flash: border / glyph accent only — no full board fill strobe.
            Color32 glow = neon;
            if (flash)
            {
                FillRect(x + 4, y + 4, boardWidth - 8, 2, p.AccentA);
                FillRect(x + 4, y + boardHeight - 6, boardWidth - 8, 2, p.AccentA);
                glow = p.Mint;
            }

            FillRect(x, y, boardWidth, 4, glow);
            FillRect(x, y + boardHeight - 4, boardWidth, 4, glow);
            FillRect(x, y, 4, boardHeight, glow);
            FillRect(x + boardWidth - 4, y, 4, boardHeight, glow);

            Color32 ink = flash ? p.AccentA : neon;
            ClubSilhouette look = HallLook();
            if (Has(look, ClubSilhouette.LowCeiling))
            {
                int glyphWidth = 5 * scale;
                int gap = 4;
                int rowWidth = glyphWidth * 4 + gap * 3;
                int textX = x + (boardWidth - rowWidth) / 2;
                int textY = y + (boardHeight - 5 * scale) / 2;
                DrawGlyph(textX, textY, GlyphN, scale, ink);
                DrawGlyph(textX + glyphWidth + gap, textY, GlyphE, scale, ink);
                DrawGlyph(textX + 2 * (glyphWidth + gap), textY, GlyphO, scale, ink);
                DrawGlyph(textX + 3 * (glyphWidth + gap), textY, GlyphN, scale, ink);
                return;
            }

            const int markW = 72;
            const int markH = 20;
            int mx = x + (boardWidth - markW) / 2;
            int my = y + (boardHeight - markH) / 2;
            if (Has(look, ClubSilhouette.DockDoor))
                DrawMarkBars(mx, my, ink);
            else if (Has(look, ClubSilhouette.Press))
                DrawMarkChevron(mx, my, ink);
            else
                DrawMarkRing(mx, my, ink);
        }

        /// <summary>Warehouse mark. Five bars, no spine, no letters.</summary>
        private void DrawMarkBars(int x, int y, Color32 ink)
        {
            int[] heights = { 20, 10, 16, 8, 14 };
            for (int i = 0; i < heights.Length; i++)
            {
                int h = heights[i];
                FillRect(x + 4 + i * 14, y + (20 - h) / 2, 8, h, ink);
            }
        }

        /// <summary>Factory mark. One chevron, no crossbar.</summary>
        private void DrawMarkChevron(int x, int y, Color32 ink)
        {
            for (int s = 0; s < 9; s++)
            {
                FillRect(x + s * 4, y + s * 2, 8, 4, ink);
                FillRect(x + 64 - s * 4, y + s * 2, 8, 4, ink);
            }
        }

        /// <summary>Signal mark. Ring plus reticle ticks, not a letter.</summary>
        private void DrawMarkRing(int x, int y, Color32 ink)
        {
            int cx = x + 36;
            int cy = y + 10;
            FillRect(cx - 6, cy + 6, 12, 2, ink);
            FillRect(cx - 6, cy - 8, 12, 2, ink);
            FillRect(cx - 10, cy - 4, 2, 8, ink);
            FillRect(cx + 8, cy - 4, 2, 8, ink);
            FillRect(cx - 8, cy + 4, 2, 2, ink);
            FillRect(cx + 6, cy + 4, 2, 2, ink);
            FillRect(cx - 8, cy - 6, 2, 2, ink);
            FillRect(cx + 6, cy - 6, 2, 2, ink);
            FillRect(x, cy - 1, 26, 2, ink);
            FillRect(x + 46, cy - 1, 26, 2, ink);
            FillRect(cx - 1, y, 2, 4, ink);
            FillRect(cx - 1, y + 16, 2, 4, ink);
        }

        private void DrawGlyph(int x, int y, string[] rows, int scale, Color32 color)
        {
            for (int row = 0; row < rows.Length; row++)
            {
                string line = rows[row];
                int py = y + (rows.Length - 1 - row) * scale;
                for (int col = 0; col < line.Length; col++)
                {
                    if (line[col] == '1')
                        FillRect(x + col * scale, py, scale, scale, color);
                }
            }
        }

        private void Fill(Color32 color)
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = color;
        }

        private void FillRect(int x, int y, int width, int height, Color32 color)
        {
            int x1 = Mathf.Clamp(x, 0, Width);
            int y1 = Mathf.Clamp(y, 0, Height);
            int x2 = Mathf.Clamp(x + width, 0, Width);
            int y2 = Mathf.Clamp(y + height, 0, Height);
            for (int py = y1; py < y2; py++)
            {
                int row = py * Width;
                for (int px = x1; px < x2; px++)
                    _pixels[row + px] = color;
            }
        }

        private void BlendRect(int x, int y, int width, int height, Color32 color)
        {
            int x1 = Mathf.Clamp(x, 0, Width);
            int y1 = Mathf.Clamp(y, 0, Height);
            int x2 = Mathf.Clamp(x + width, 0, Width);
            int y2 = Mathf.Clamp(y + height, 0, Height);
            int srcA = color.a;
            if (srcA <= 0)
                return;
            int inv = 255 - srcA;
            for (int py = y1; py < y2; py++)
            {
                int row = py * Width;
                for (int px = x1; px < x2; px++)
                {
                    Color32 dst = _pixels[row + px];
                    _pixels[row + px] = new Color32(
                        (byte)((color.r * srcA + dst.r * inv) / 255),
                        (byte)((color.g * srcA + dst.g * inv) / 255),
                        (byte)((color.b * srcA + dst.b * inv) / 255),
                        255);
                }
            }
        }
    }
}
