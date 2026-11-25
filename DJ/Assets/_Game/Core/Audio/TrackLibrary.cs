using System;
using System.Collections.Generic;

namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// The four MVP records from the content bible. Every loop shares 120 BPM and A minor,
    /// so any pair can be mixed. Stem files are checked by the audio session at load.
    /// </summary>
    public sealed class TrackLibrary
    {
        public const double LengthToleranceSeconds = 0.05d;

        private readonly Dictionary<string, TrackDefinition> _byId;

        public TrackLibrary(IReadOnlyList<TrackDefinition> tracks)
        {
            if (tracks == null || tracks.Count == 0)
                throw new ArgumentException("Library needs at least one track.", nameof(tracks));

            _byId = new Dictionary<string, TrackDefinition>(StringComparer.Ordinal);
            double bpm = tracks[0].Bpm;
            var copy = new List<TrackDefinition>(tracks.Count);
            for (int i = 0; i < tracks.Count; i++)
            {
                TrackDefinition track = tracks[i];
                if (track == null)
                    throw new ArgumentException("Track is missing.", nameof(tracks));
                if (_byId.ContainsKey(track.Id))
                    throw new ArgumentException("Duplicate track id " + track.Id + ".");
                if (Math.Abs(track.Bpm - bpm) > 0.001d)
                    throw new ArgumentException("Library tracks must share one BPM.");
                if (Math.Abs(track.LengthSeconds - TrackDefinition.MvpLengthSeconds) > LengthToleranceSeconds)
                    throw new ArgumentException("Loop length is outside 16s tolerance: " + track.Id + ".");
                _byId.Add(track.Id, track);
                copy.Add(track);
            }

            Tracks = copy.AsReadOnly();
        }

        public IReadOnlyList<TrackDefinition> Tracks { get; }

        public static TrackLibrary CreateMvp()
        {
            return new TrackLibrary(new[]
            {
                Record("track_neon_pulse", "Neon Pulse", "House", 5, "Bright", "neon_pulse", "LIC-001"),
                Record("track_bass_reactor", "Bass Reactor", "Tech House", 9, "Dark", "bass_reactor", "LIC-002"),
                Record("track_afterhours", "Afterhours", "Deep House", 3, "Dreamy", "afterhours", "LIC-003"),
                Record("track_city_static", "City Static", "Electro House", 7, "Edgy", "city_static", "LIC-004")
            });
        }

        /// <summary>
        /// Original Act 1 recordings on the shared eight-bar grid.
        /// </summary>
        public static TrackLibrary CreateAct1Catalog()
        {
            return new TrackLibrary(new[]
            {
                Record("track_warehouse_line", "Warehouse Line", "Warehouse Techno", 6, "Dusty", "warehouse_line", "DR-CLUB-010", TrackFlags.ClubPool),
                Record("track_smoke_column", "Smoke Column", "Warehouse Techno", 4, "Hypnotic", "smoke_column", "DR-CLUB-011", TrackFlags.ClubPool),
                Record("track_press_line", "Press Line", "Bass Tech", 7, "Heavy", "press_line", "DR-CLUB-020", TrackFlags.ClubPool),
                Record("track_torque_drum", "Torque Drum", "Bass Tech", 6, "Mechanical", "torque_drum", "DR-CLUB-021", TrackFlags.ClubPool),
                Record("track_voltage_alley", "Voltage Alley", "Acid House", 6, "Acid", "voltage_alley", "DR-CLUB-001", TrackFlags.Reward),
                Record("track_mutabor_pulse", "Mutabor Pulse", "Warehouse Techno", 7, "Dense", "mutabor_pulse", "DR-CLUB-012", TrackFlags.Reward),
                Record("track_foundry_drop", "Foundry Drop", "Bass Tech", 10, "Brutal", "foundry_drop", "DR-CLUB-022", TrackFlags.Reward),
                Record("track_afterglow_key", "Afterglow Key", "Deep House", 4, "Warm", "afterglow_key", "DR-CLUB-032", TrackFlags.Reward),
                Record("track_signal_carrier", "Signal Carrier", "Industrial", 7, "Uneasy", "signal_carrier", "DR-CLUB-050", TrackFlags.Reward | TrackFlags.BossDrop | TrackFlags.MetaUnlock),
                Record("track_signal_breach", "Signal Breach", "Industrial", 10, "Critical", "signal_breach", "DR-CLUB-051", TrackFlags.BossExclusive),
                Record("track_carbon_copy", "Carbon Copy", "Electro", 6, "Dry", "carbon_copy", "DR-CLUB-060", TrackFlags.ClubPool),
                Record("track_cold_cathode", "Cold Cathode", "Glass Techno", 5, "Icy", "cold_cathode", "DR-CLUB-061", TrackFlags.ClubPool),
                Record("track_night_tram", "Night Tram", "Deep House", 4, "Warm", "night_tram", "DR-CLUB-062", TrackFlags.ClubPool),
                Record("track_freight_lift", "Freight Lift", "Industrial", 8, "Heavy", "freight_lift", "DR-CLUB-063", TrackFlags.ClubPool),
                Record("track_return_duct", "Return Duct", "Dub Techno", 4, "Hazy", "return_duct", "DR-CLUB-064", TrackFlags.ClubPool),
                Record("track_ratchet_works", "Ratchet Works", "Broken Techno", 8, "Rolling", "ratchet_works", "DR-CLUB-065", TrackFlags.ClubPool),
                Record("track_phase_fault", "Phase Fault", "Electro", 8, "Tense", "phase_fault", "DR-CLUB-066", TrackFlags.ClubPool),
                Record("track_mirror_shunt", "Mirror Shunt", "Electro", 9, "Prismatic", "mirror_shunt", "DR-CLUB-067", TrackFlags.Reward),
                Record("track_amber_bloom", "Amber Bloom", "Deep House", 5, "Warm", "amber_bloom", "DR-CLUB-070", TrackFlags.Reward | TrackFlags.Special),
                Record("track_void_static", "Void Static", "Electro", 4, "Hollow", "void_static", "DR-CLUB-071", TrackFlags.Reward | TrackFlags.Special),
                Record("track_glass_rain", "Glass Rain", "Glass Techno", 6, "Icy", "glass_rain", "DR-CLUB-072", TrackFlags.Reward | TrackFlags.Special),
                Record("track_redline_siren", "Redline Siren", "Bass Tech", 9, "Urgent", "redline_siren", "DR-CLUB-073", TrackFlags.Reward | TrackFlags.Special),
                Record("track_dusk_pad", "Dusk Pad", "Deep House", 4, "Warm", "dusk_pad", "DR-CLUB-080", TrackFlags.ClubPool),
                Record("track_velvet_room", "Velvet Room", "Deep House", 3, "Soft", "velvet_room", "DR-CLUB-081", TrackFlags.ClubPool),
                Record("track_strobe_grid", "Strobe Grid", "Broken Techno", 8, "Hard", "strobe_grid", "DR-CLUB-082", TrackFlags.ClubPool),
                Record("track_yellow_siren", "Yellow Siren", "Bass Tech", 7, "Urgent", "yellow_siren", "DR-CLUB-083", TrackFlags.ClubPool),
                Record("track_pier_salt", "Pier Salt", "Electro", 6, "Bright", "pier_salt", "DR-CLUB-084", TrackFlags.ClubPool),
                Record("track_tide_click", "Tide Click", "Electro House", 5, "Crisp", "tide_click", "DR-CLUB-085", TrackFlags.ClubPool),
                Record("track_throne_hum", "Throne Hum", "Industrial", 8, "Static", "throne_hum", "DR-CLUB-086", TrackFlags.BossExclusive),
                Record("track_crown_loop", "Crown Loop", "Industrial", 9, "Rigid", "crown_loop", "DR-CLUB-087", TrackFlags.BossExclusive),
                Record("track_prism_pane", "Prism Pane", "Glass Techno", 5, "Icy", "prism_pane", "DR-CLUB-088", TrackFlags.ClubPool),
                Record("track_choir_dust", "Choir Dust", "Deep House", 4, "Airy", "choir_dust", "DR-CLUB-089", TrackFlags.ClubPool),
                Record("track_ledger_tick", "Ledger Tick", "Electro", 6, "Dry", "ledger_tick", "DR-CLUB-090", TrackFlags.ClubPool),
                Record("track_favor_bass", "Favor Bass", "Acid House", 7, "Sly", "favor_bass", "DR-CLUB-091", TrackFlags.ClubPool),
                Record("track_sky_cable", "Sky Cable", "Industrial", 7, "Windy", "sky_cable", "DR-CLUB-092", TrackFlags.ClubPool),
                Record("track_wind_hat", "Wind Hat", "Dub Techno", 5, "Open", "wind_hat", "DR-CLUB-093", TrackFlags.ClubPool),
                Record("track_festival_grid", "Festival Grid", "Industrial", 9, "Wide", "festival_grid", "DR-CLUB-094", TrackFlags.BossExclusive),
                Record("track_city_choir", "City Choir", "Deep House", 8, "Choral", "city_choir", "DR-CLUB-095", TrackFlags.BossExclusive),
                Record("track_lounge_key", "Lounge Key", "Deep House", 4, "Warm", "lounge_key", "DR-CLUB-100", TrackFlags.Reward),
                Record("track_district_hat", "District Hat", "Broken Techno", 8, "Hard", "district_hat", "DR-CLUB-101", TrackFlags.Reward),
                Record("track_siren_sub", "Siren Sub", "Bass Tech", 9, "Heavy", "siren_sub", "DR-CLUB-102", TrackFlags.Reward),
                Record("track_pier_roll", "Pier Roll", "Electro", 6, "Bright", "pier_roll", "DR-CLUB-103", TrackFlags.Reward),
                Record("track_chrome_wake", "Chrome Wake", "Electro House", 5, "Crisp", "chrome_wake", "DR-CLUB-104", TrackFlags.Reward),
                Record("track_nave_pad", "Nave Pad", "Glass Techno", 5, "Icy", "nave_pad", "DR-CLUB-110", TrackFlags.Reward),
                Record("track_market_click", "Market Click", "Electro", 6, "Dry", "market_click", "DR-CLUB-111", TrackFlags.Reward),
                Record("track_roof_hum", "Roof Hum", "Industrial", 7, "Windy", "roof_hum", "DR-CLUB-112", TrackFlags.Reward),
                Record("track_exchange_acid", "Exchange Acid", "Acid House", 7, "Sly", "exchange_acid", "DR-CLUB-113", TrackFlags.Reward),
                Record("track_relay_dub", "Relay Dub", "Dub Techno", 4, "Hazy", "relay_dub", "DR-CLUB-114", TrackFlags.Reward),
                Record("track_velvet_hush", "Velvet Hush", "Deep House", 4, "Soft", "velvet_hush", "DR-CLUB-120", TrackFlags.Reward | TrackFlags.Special),
                Record("track_hardline_yellow", "Hardline Yellow", "Bass Tech", 9, "Urgent", "hardline_yellow", "DR-CLUB-121", TrackFlags.Reward | TrackFlags.Special),
                Record("track_salt_chrome", "Salt Chrome", "Electro", 6, "Bright", "salt_chrome", "DR-CLUB-122", TrackFlags.Reward | TrackFlags.Special),
                Record("track_prism_choir", "Prism Choir", "Glass Techno", 6, "Icy", "prism_choir", "DR-CLUB-123", TrackFlags.Reward | TrackFlags.Special),
                Record("track_night_ledger", "Night Ledger", "Electro", 6, "Dry", "night_ledger", "DR-CLUB-124", TrackFlags.Reward | TrackFlags.Special),
                Record("track_skywire", "Skywire", "Industrial", 8, "Windy", "skywire", "DR-CLUB-125", TrackFlags.Reward | TrackFlags.Special),
                Record("track_static_crown", "Static Crown", "Industrial", 8, "Static", "static_crown", "DR-CLUB-126", TrackFlags.Reward | TrackFlags.BossDrop | TrackFlags.MetaUnlock | TrackFlags.Special),
                Record("track_festival_anthem", "Festival Anthem", "Deep House", 9, "Choral", "festival_anthem", "DR-CLUB-127", TrackFlags.Reward | TrackFlags.BossDrop | TrackFlags.MetaUnlock | TrackFlags.Special)
            });
        }

        /// <summary>
        /// Starter crate plus all authored club and reward records.
        /// </summary>
        public static TrackLibrary CreateAct1Playable()
        {
            var tracks = new List<TrackDefinition>(CreateMvp().Tracks);
            tracks.AddRange(CreateAct1Catalog().Tracks);
            return new TrackLibrary(tracks);
        }

        public bool TryGet(string id, out TrackDefinition track)
        {
            if (id == null)
            {
                track = null;
                return false;
            }

            return _byId.TryGetValue(id, out track);
        }

        public TrackDefinition Get(string id)
        {
            TrackDefinition track;
            if (!TryGet(id, out track))
                throw new KeyNotFoundException(id);
            return track;
        }

        public int IndexOf(string id)
        {
            for (int i = 0; i < Tracks.Count; i++)
            {
                if (Tracks[i].Id == id)
                    return i;
            }

            return -1;
        }

        private static TrackDefinition Record(
            string id,
            string displayName,
            string genre,
            int energy,
            string mood,
            string fileStem,
            string licenseId,
            TrackFlags flags = TrackFlags.None)
        {
            return new TrackDefinition(
                id,
                displayName,
                TrackDefinition.MvpBpm,
                TrackDefinition.MvpBeatsPerBar,
                TrackDefinition.MvpBarsPerLoop,
                "A_minor",
                genre,
                energy,
                mood,
                fileStem + "_low",
                fileStem + "_perc",
                fileStem + "_music",
                licenseId,
                flags);
        }
    }
}
