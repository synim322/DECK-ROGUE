using System;
using System.Collections.Generic;

namespace DeckRogue.Core.Run
{
    /// <summary>
    /// Persistent track unlocks (e.g. boss clear meta_unlock of track_signal_carrier).
    /// </summary>
    public sealed class MetaUnlocks
    {
        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyCollection<string> UnlockedTrackIds
        {
            get { return _unlocked; }
        }

        public void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("Track id is required.", nameof(id));
            _unlocked.Add(id);
        }

        public bool Contains(string id)
        {
            return id != null && _unlocked.Contains(id);
        }

        /// <summary>
        /// Boot / reload: replace in-memory set from SaveGame.UnlockedTrackIds.
        /// Does not touch run crate or gear. Empty/null clears to none unlocked.
        /// </summary>
        public void Hydrate(IEnumerable<string> unlockedTrackIds)
        {
            _unlocked.Clear();
            if (unlockedTrackIds == null)
                return;

            foreach (string id in unlockedTrackIds)
            {
                if (!string.IsNullOrEmpty(id))
                    _unlocked.Add(id);
            }
        }
    }
}
