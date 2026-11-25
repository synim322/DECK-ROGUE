using System;

namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// Progression / pool markers for Act1+ tracks. Overlaps music rarity without replacing it.
    /// </summary>
    [Flags]
    public enum TrackFlags
    {
        None = 0,
        Reward = 1,
        BossDrop = 2,
        MetaUnlock = 4,
        BossExclusive = 8,
        ClubPool = 16,
        Special = 32
    }

    /// <summary>
    /// One fixed-tempo record. MVP loops are 120 BPM, 4/4, eight bars, A minor.
    /// Stem file names are logical; a platform locator turns them into asset paths.
    /// </summary>
    public sealed class TrackDefinition
    {
        public const double MvpBpm = 120d;
        public const int MvpBeatsPerBar = 4;
        public const int MvpBarsPerLoop = 8;
        public const double MvpLengthSeconds = 16d;

        private readonly string _lowStem;
        private readonly string _percStem;
        private readonly string _musicStem;

        public TrackDefinition(
            string id,
            string displayName,
            double bpm,
            int beatsPerBar,
            int barsPerLoop,
            string key,
            string genre,
            int energy,
            string mood,
            string lowStem,
            string percStem,
            string musicStem,
            string licenseId,
            TrackFlags flags = TrackFlags.None)
        {
            if (!IsTrackId(id))
                throw new ArgumentException("Track id must be track_snake_case.", nameof(id));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("Display name is required.", nameof(displayName));
            if (bpm <= 0d || double.IsNaN(bpm) || double.IsInfinity(bpm))
                throw new ArgumentOutOfRangeException(nameof(bpm));
            if (beatsPerBar <= 0)
                throw new ArgumentOutOfRangeException(nameof(beatsPerBar));
            if (barsPerLoop <= 0)
                throw new ArgumentOutOfRangeException(nameof(barsPerLoop));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key is required.", nameof(key));
            if (string.IsNullOrWhiteSpace(genre))
                throw new ArgumentException("Genre is required.", nameof(genre));
            if (energy < 1 || energy > 10)
                throw new ArgumentOutOfRangeException(nameof(energy));
            if (string.IsNullOrWhiteSpace(mood))
                throw new ArgumentException("Mood is required.", nameof(mood));
            if (string.IsNullOrWhiteSpace(lowStem) || string.IsNullOrWhiteSpace(percStem) || string.IsNullOrWhiteSpace(musicStem))
                throw new ArgumentException("All three stems are required.");
            if (string.IsNullOrWhiteSpace(licenseId))
                throw new ArgumentException("License id is required.", nameof(licenseId));

            Id = id;
            DisplayName = displayName;
            Bpm = bpm;
            BeatsPerBar = beatsPerBar;
            BarsPerLoop = barsPerLoop;
            Key = key;
            Genre = genre;
            Energy = energy;
            Mood = mood;
            LicenseId = licenseId;
            Flags = flags;
            _lowStem = lowStem;
            _percStem = percStem;
            _musicStem = musicStem;
            LengthSeconds = barsPerLoop * beatsPerBar * (60d / bpm);
        }

        public string Id { get; }
        public string DisplayName { get; }
        public double Bpm { get; }
        public int BeatsPerBar { get; }
        public int BarsPerLoop { get; }
        public string Key { get; }
        public string Genre { get; }
        public int Energy { get; }
        public string Mood { get; }
        public string LicenseId { get; }
        public TrackFlags Flags { get; }
        public double LengthSeconds { get; }

        public string StemFile(StemId stem)
        {
            if (stem == StemId.Low)
                return _lowStem;
            if (stem == StemId.Perc)
                return _percStem;
            if (stem == StemId.Music)
                return _musicStem;
            throw new ArgumentOutOfRangeException(nameof(stem));
        }

        private static bool IsTrackId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length < 7 || !id.StartsWith("track_", StringComparison.Ordinal))
                return false;
            if (id[id.Length - 1] == '_')
                return false;

            for (int i = 6; i < id.Length; i++)
            {
                char c = id[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                    return false;
            }

            return true;
        }
    }
}
