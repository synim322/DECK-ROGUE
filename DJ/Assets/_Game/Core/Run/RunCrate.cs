using System;
using System.Collections.Generic;
using DeckRogue.Core.Audio;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Run crate of track ids. Starts with the four MVP records.
    /// Cap is 16: 4 starters + 9 ordinary picks + 3 boss drops.
    /// </summary>
    public sealed class RunCrate
    {
        public const int Cap = 16;

        private readonly List<string> _ids = new List<string>(Cap);

        public RunCrate()
            : this(MvpIds())
        {
        }

        public RunCrate(IReadOnlyList<string> seedIds)
        {
            if (seedIds == null)
                throw new ArgumentNullException(nameof(seedIds));

            for (int i = 0; i < seedIds.Count; i++)
            {
                string id = seedIds[i];
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentException("Track id is missing.", nameof(seedIds));
                if (_ids.Contains(id))
                    continue;
                if (_ids.Count >= Cap)
                    throw new ArgumentException("Seed exceeds crate cap.", nameof(seedIds));
                _ids.Add(id);
            }
        }

        public int Count => _ids.Count;

        public bool TryAdd(string trackId)
        {
            if (string.IsNullOrEmpty(trackId))
                return false;
            if (_ids.Count >= Cap)
                return false;
            if (_ids.Contains(trackId))
                return false;

            _ids.Add(trackId);
            return true;
        }

        public bool Contains(string trackId)
        {
            return trackId != null && _ids.Contains(trackId);
        }

        public string[] ToArray()
        {
            return _ids.ToArray();
        }

        /// <summary>
        /// Clears the crate and reseeds the four MVP ids (run reset / lose).
        /// </summary>
        public void ResetToMvp()
        {
            _ids.Clear();
            string[] mvp = MvpIds();
            for (int i = 0; i < mvp.Length; i++)
                _ids.Add(mvp[i]);
        }

        /// <summary>
        /// Replace crate contents from a checkpoint (capped, deduped, empty → MVP).
        /// </summary>
        public void ReplaceFrom(IReadOnlyList<string> trackIds)
        {
            _ids.Clear();
            if (trackIds == null || trackIds.Count == 0)
            {
                ResetToMvp();
                return;
            }

            for (int i = 0; i < trackIds.Count && _ids.Count < Cap; i++)
            {
                string id = trackIds[i];
                if (string.IsNullOrEmpty(id) || _ids.Contains(id))
                    continue;
                _ids.Add(id);
            }

            if (_ids.Count == 0)
                ResetToMvp();
        }

        /// <summary>
        /// Same four ids as <see cref="TrackLibrary.CreateMvp"/> (no public constants there).
        /// </summary>
        public static string[] MvpIds()
        {
            IReadOnlyList<TrackDefinition> tracks = TrackLibrary.CreateMvp().Tracks;
            var ids = new string[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
                ids[i] = tracks[i].Id;
            return ids;
        }
    }
}
