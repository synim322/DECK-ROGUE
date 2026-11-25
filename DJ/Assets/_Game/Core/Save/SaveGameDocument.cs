using System;
using System.Collections.Generic;
using DeckRogue.Core.Meta;
using DeckRogue.Core.Run;

namespace DeckRogue.Core.Save
{
    /// <summary>
    /// Single local SaveGame for Main Menu / Settings / Act1 meta / mid-run checkpoint.
    /// </summary>
    public sealed class SaveGameDocument
    {
        public int SchemaVersion { get; set; }

        public SettingsData Settings { get; set; }

        /// <summary>Persistent track unlocks (Act1: track_signal_carrier after boss).</summary>
        public List<string> UnlockedTrackIds { get; set; }

        /// <summary>
        /// Reserved list. <see cref="Migrate"/> always clears it.
        /// Hub decks and items live on <see cref="OwnedDeckIds"/> and <see cref="OwnedItemIds"/>.
        /// </summary>
        public List<string> UnlockedGearIds { get; set; }

        /// <summary>Reserved for future club gates. Act1 pool is always available — stays empty.</summary>
        public List<string> UnlockedClubIds { get; set; }

        /// <summary>Best finished-run Reputation (≥ 0). Updated only at FinalResult when higher.</summary>
        public int BestReputation { get; set; }

        /// <summary>Optional mid-run Act1 checkpoint from the pause menu.</summary>
        public RunCheckpoint Checkpoint { get; set; }

        /// <summary>Flat hub coins. Not a Hype percent.</summary>
        public int MetaCoins { get; set; }

        /// <summary>Purchased deck ids. Default owns <c>deck_starter</c>. One equipped deck enters a run.</summary>
        public List<string> OwnedDeckIds { get; set; }

        /// <summary>Deck worn in the hub. Copied into a run by <c>RunDeckLock</c> at start.</summary>
        public string EquippedDeckId { get; set; }

        /// <summary>Purchased non-deck ids (spare leash). Never equipped.</summary>
        public List<string> OwnedItemIds { get; set; }

        /// <summary>Hub day counter. Advances on <c>SettleNight</c>, not on the OS clock.</summary>
        public int HubDay { get; set; }

        /// <summary>
        /// Basement door. False until the first finished club.
        /// Same v5 blob: a missing field stays false, and coins, a gig key, or
        /// <see cref="HubDay"/> &gt; 0 open it so an older night does not lose the door.
        /// </summary>
        public bool HubUnlocked { get; set; }

        /// <summary>True after a completed walk until the next settled night.</summary>
        public bool WalkUsedThisDay { get; set; }

        /// <summary>Run id already settled. The same id does not advance <see cref="HubDay"/>.</summary>
        public string SettledRunId { get; set; }

        /// <summary>Gig payout keys <c>gig:{runId}:{setIndex}</c>. A repeat pays nothing.</summary>
        public List<string> ClaimedGigKeys { get; set; }

        public static SaveGameDocument CreateDefault()
        {
            return new SaveGameDocument
            {
                SchemaVersion = SaveSchema.CurrentVersion,
                Settings = SettingsData.CreateDefault(),
                UnlockedTrackIds = new List<string>(),
                UnlockedGearIds = new List<string>(),
                UnlockedClubIds = new List<string>(),
                BestReputation = 0,
                Checkpoint = RunCheckpoint.CreateEmpty(),
                MetaCoins = 0,
                OwnedDeckIds = new List<string> { DeckCatalog.StarterId },
                EquippedDeckId = DeckCatalog.StarterId,
                OwnedItemIds = new List<string>(),
                HubDay = 0,
                HubUnlocked = false,
                WalkUsedThisDay = false,
                SettledRunId = "",
                ClaimedGigKeys = new List<string>()
            };
        }

        /// <summary>
        /// Bring any loaded blob to <see cref="SaveSchema.CurrentVersion"/> with safe defaults.
        /// Unknown future versions: keep readable known fields, rewrite version to current.
        /// </summary>
        public static SaveGameDocument Migrate(SaveGameDocument loaded)
        {
            if (loaded == null)
                return CreateDefault();

            var result = CreateDefault();
            // v0 / missing / future: copy known fields, then normalize to CurrentVersion.
            if (loaded.Settings != null)
                result.Settings = loaded.Settings.Clone();
            CopyIds(loaded.UnlockedTrackIds, result.UnlockedTrackIds);
            if (loaded.BestReputation > 0)
                result.BestReputation = loaded.BestReputation;
            if (loaded.Checkpoint != null && loaded.Checkpoint.Active)
                result.Checkpoint = loaded.Checkpoint.Clone();

            // Schema versions before 3 used TutorialCompleted as a stub "started a run" flag —
            // re-offer the real pre-run tutorial once after this migrator.
            if (loaded.SchemaVersion < 3 && result.Settings != null)
                result.Settings.TutorialCompleted = false;

            // Display settings landed in v4 — older blobs had no fullscreen/resolution fields
            // (JsonUtility would leave Fullscreen=false), so force the intended defaults once.
            if (loaded.SchemaVersion < 4 && result.Settings != null)
            {
                result.Settings.Fullscreen = true;
                result.Settings.ScreenWidth = SettingsData.NativeResolutionSentinel;
                result.Settings.ScreenHeight = SettingsData.NativeResolutionSentinel;
            }

            // v5 adds the hub wallet. HubUnlocked rides in the same version: a missing bool
            // reads false, and coins, a gig key, or HubDay > 0 open the door on the way in.
            // Older blobs had no meta fields. The booth-coach flag could be stored without
            // the card ever showing, so the lesson opens once more.
            if (loaded.SchemaVersion < 5)
            {
                if (result.Settings != null)
                    result.Settings.BoothCoachCompleted = false;
            }
            else
            {
                CopyMeta(loaded, result);
            }

            result.SchemaVersion = SaveSchema.CurrentVersion;
            result.Settings.Normalize();
            if (result.BestReputation < 0)
                result.BestReputation = 0;

            DedupeOrdinal(result.UnlockedTrackIds);
            // Gear / club reserved empty until a real Act consumer exists.
            result.UnlockedGearIds.Clear();
            result.UnlockedClubIds.Clear();
            if (result.Checkpoint == null)
                result.Checkpoint = RunCheckpoint.CreateEmpty();
            if (!result.Checkpoint.CanResume)
                result.Checkpoint = RunCheckpoint.CreateEmpty();
            return result;
        }

        public bool HasResumableCheckpoint =>
            Checkpoint != null && Checkpoint.CanResume;

        public void SetCheckpoint(RunCheckpoint checkpoint)
        {
            Checkpoint = checkpoint == null || !checkpoint.CanResume
                ? RunCheckpoint.CreateEmpty()
                : checkpoint.Clone();
        }

        public void ClearCheckpoint()
        {
            Checkpoint = RunCheckpoint.CreateEmpty();
        }

        /// <summary>Boot hydration: push track ids into an existing MetaUnlocks instance.</summary>
        public void ApplyToMeta(MetaUnlocks meta)
        {
            if (meta == null)
                throw new ArgumentNullException(nameof(meta));
            meta.Hydrate(UnlockedTrackIds);
        }

        /// <summary>Capture current meta tracks into this document (does not flush I/O).</summary>
        public void CaptureMeta(MetaUnlocks meta)
        {
            if (meta == null)
                throw new ArgumentNullException(nameof(meta));
            UnlockedTrackIds.Clear();
            foreach (string id in meta.UnlockedTrackIds)
            {
                if (!string.IsNullOrEmpty(id))
                    UnlockedTrackIds.Add(id);
            }
        }

        /// <summary>
        /// At FinalResult: raise BestReputation if run rep is higher.
        /// Returns true when the document changed (caller should flush).
        /// </summary>
        public bool TryUpdateBestReputation(int runReputation)
        {
            if (runReputation < 0)
                runReputation = 0;
            if (runReputation <= BestReputation)
                return false;
            BestReputation = runReputation;
            return true;
        }

        private static void CopyMeta(SaveGameDocument loaded, SaveGameDocument result)
        {
            result.MetaCoins = loaded.MetaCoins < 0 ? 0 : loaded.MetaCoins;
            if (loaded.OwnedDeckIds != null)
                CopyIds(loaded.OwnedDeckIds, result.OwnedDeckIds);
            if (!ContainsOrdinal(result.OwnedDeckIds, DeckCatalog.StarterId))
                result.OwnedDeckIds.Insert(0, DeckCatalog.StarterId);
            DedupeOrdinal(result.OwnedDeckIds);

            if (!string.IsNullOrEmpty(loaded.EquippedDeckId))
                result.EquippedDeckId = loaded.EquippedDeckId;

            if (loaded.OwnedItemIds != null)
                CopyIds(loaded.OwnedItemIds, result.OwnedItemIds);
            DedupeOrdinal(result.OwnedItemIds);

            result.HubDay = loaded.HubDay < 0 ? 0 : loaded.HubDay;
            result.WalkUsedThisDay = loaded.WalkUsedThisDay;
            result.SettledRunId = loaded.SettledRunId ?? "";

            if (loaded.ClaimedGigKeys != null)
                CopyIds(loaded.ClaimedGigKeys, result.ClaimedGigKeys);
            DedupeOrdinal(result.ClaimedGigKeys);

            result.HubUnlocked = loaded.HubUnlocked;
            if (!result.HubUnlocked && HasPriorHubEarnings(result))
                result.HubUnlocked = true;
        }

        private static bool HasPriorHubEarnings(SaveGameDocument document)
        {
            if (document.MetaCoins > 0 || document.HubDay > 0)
                return true;
            return document.ClaimedGigKeys != null && document.ClaimedGigKeys.Count > 0;
        }

        private static bool ContainsOrdinal(List<string> ids, string id)
        {
            if (ids == null || string.IsNullOrEmpty(id))
                return false;
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void CopyIds(IEnumerable<string> source, List<string> target)
        {
            target.Clear();
            if (source == null)
                return;
            foreach (string id in source)
            {
                if (!string.IsNullOrEmpty(id))
                    target.Add(id);
            }
        }

        private static void DedupeOrdinal(List<string> ids)
        {
            if (ids == null || ids.Count <= 1)
                return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var unique = new List<string>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrEmpty(id) || !seen.Add(id))
                    continue;
                unique.Add(id);
            }

            ids.Clear();
            ids.AddRange(unique);
        }
    }
}
