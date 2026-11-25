using System;
using System.IO;
using DeckRogue.Core.Run;
using DeckRogue.Core.Save;
using UnityEngine;

namespace DeckRogue.Client
{
    /// <summary>
    /// Unity IO adapter for <see cref="SaveGameDocument"/> (client half of DJ-116).
    ///
    /// Persist choice (Win + Web):
    /// Primary write is a JSON file under <see cref="Application.persistentDataPath"/> named
    /// <c>{SaveSchema.DocumentKey}.json</c>. That path is durable on Windows Standalone and
    /// maps to IndexedDB on WebGL. If file IO throws (sandbox, disk full, first-run race),
    /// the same payload is stored as a PlayerPrefs string under key
    /// <see cref="SaveSchema.DocumentKey"/>. Load tries the file first, then PlayerPrefs.
    /// Domain stays engine-free; this class owns all serialization.
    /// </summary>
    public static class LocalSaveStore
    {
        private static string FilePath =>
            Path.Combine(Application.persistentDataPath, SaveSchema.DocumentKey + ".json");

        public static SaveGameDocument Load()
        {
            string json = TryReadFile();
            if (string.IsNullOrEmpty(json))
                json = PlayerPrefs.GetString(SaveSchema.DocumentKey, string.Empty);

            if (string.IsNullOrEmpty(json))
                return SaveGameDocument.Migrate(null);

            try
            {
                SaveGameDto dto = JsonUtility.FromJson<SaveGameDto>(json);
                return SaveGameDocument.Migrate(FromDto(dto));
            }
            catch (Exception)
            {
                return SaveGameDocument.Migrate(null);
            }
        }

        public static void Save(SaveGameDocument document, SaveFlushTrigger trigger)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            document.Settings?.Normalize();
            document.SchemaVersion = SaveSchema.CurrentVersion;
            string json = JsonUtility.ToJson(ToDto(document), false);
            bool wroteFile = TryWriteFile(json);
            PlayerPrefs.SetString(SaveSchema.DocumentKey, json);
            PlayerPrefs.Save();
            if (!wroteFile)
                Debug.LogWarning(
                    "[LocalSaveStore] File write failed; persisted via PlayerPrefs ("
                    + trigger + ").");
        }

        private static string TryReadFile()
        {
            try
            {
                string path = FilePath;
                if (!File.Exists(path))
                    return null;
                return File.ReadAllText(path);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool TryWriteFile(string json)
        {
            try
            {
                string path = FilePath;
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static SaveGameDto ToDto(SaveGameDocument doc)
        {
            SettingsData s = doc.Settings ?? SettingsData.CreateDefault();
            return new SaveGameDto
            {
                schemaVersion = doc.SchemaVersion,
                settings = new SettingsDto
                {
                    language = s.Language ?? SettingsData.DefaultLanguage,
                    musicVolume = s.MusicVolume,
                    sfxVolume = s.SfxVolume,
                    uiScale = s.UiScale,
                    reducedFlashing = s.ReducedFlashing,
                    tutorialCompleted = s.TutorialCompleted,
                    boothCoachCompleted = s.BoothCoachCompleted,
                    fullscreen = s.Fullscreen,
                    screenWidth = s.ScreenWidth,
                    screenHeight = s.ScreenHeight
                },
                unlockedTrackIds = ToArray(doc.UnlockedTrackIds),
                unlockedGearIds = ToArray(doc.UnlockedGearIds),
                unlockedClubIds = ToArray(doc.UnlockedClubIds),
                bestReputation = doc.BestReputation,
                metaCoins = doc.MetaCoins,
                ownedDeckIds = ToArray(doc.OwnedDeckIds),
                equippedDeckId = doc.EquippedDeckId ?? "",
                ownedItemIds = ToArray(doc.OwnedItemIds),
                hubDay = doc.HubDay,
                hubUnlocked = doc.HubUnlocked,
                walkUsedThisDay = doc.WalkUsedThisDay,
                settledRunId = doc.SettledRunId ?? "",
                claimedGigKeys = ToArray(doc.ClaimedGigKeys),
                checkpoint = ToCheckpointDto(doc.Checkpoint)
            };
        }

        private static SaveGameDocument FromDto(SaveGameDto dto)
        {
            if (dto == null)
                return null;

            var doc = new SaveGameDocument
            {
                // On-disk version. Load migrates only after FromDto returns.
                SchemaVersion = dto.schemaVersion,
                BestReputation = dto.bestReputation,
                UnlockedTrackIds = new System.Collections.Generic.List<string>(),
                UnlockedGearIds = new System.Collections.Generic.List<string>(),
                UnlockedClubIds = new System.Collections.Generic.List<string>(),
                Checkpoint = FromCheckpointDto(dto.checkpoint),
                MetaCoins = dto.metaCoins < 0 ? 0 : dto.metaCoins,
                OwnedDeckIds = new System.Collections.Generic.List<string>(),
                EquippedDeckId = dto.equippedDeckId ?? "",
                OwnedItemIds = new System.Collections.Generic.List<string>(),
                HubDay = dto.hubDay < 0 ? 0 : dto.hubDay,
                // On-disk flag, before Migrate. A missing field stays false; Migrate
                // still opens the door when coins, a gig key, or HubDay > 0 are already there.
                // Decks stay on OwnedDeckIds — UnlockedGearIds is reserved and stays empty.
                HubUnlocked = dto.hubUnlocked,
                WalkUsedThisDay = dto.walkUsedThisDay,
                SettledRunId = dto.settledRunId ?? "",
                ClaimedGigKeys = new System.Collections.Generic.List<string>()
            };

            if (dto.settings != null)
            {
                doc.Settings = new SettingsData
                {
                    Language = dto.settings.language,
                    MusicVolume = dto.settings.musicVolume,
                    SfxVolume = dto.settings.sfxVolume,
                    UiScale = dto.settings.uiScale,
                    ReducedFlashing = dto.settings.reducedFlashing,
                    TutorialCompleted = dto.settings.tutorialCompleted,
                    BoothCoachCompleted = dto.settings.boothCoachCompleted,
                    Fullscreen = dto.settings.fullscreen,
                    ScreenWidth = dto.settings.screenWidth,
                    ScreenHeight = dto.settings.screenHeight
                };
            }

            CopyArray(dto.unlockedTrackIds, doc.UnlockedTrackIds);
            CopyArray(dto.unlockedGearIds, doc.UnlockedGearIds);
            CopyArray(dto.unlockedClubIds, doc.UnlockedClubIds);
            CopyArray(dto.ownedDeckIds, doc.OwnedDeckIds);
            CopyArray(dto.ownedItemIds, doc.OwnedItemIds);
            CopyArray(dto.claimedGigKeys, doc.ClaimedGigKeys);
            return doc;
        }

        private static CheckpointDto ToCheckpointDto(RunCheckpoint checkpoint)
        {
            if (checkpoint == null || !checkpoint.Active)
                return null;

            return new CheckpointDto
            {
                active = true,
                seed = checkpoint.Seed,
                clubIndex = checkpoint.ClubIndex,
                phase = (int)checkpoint.Phase,
                outcome = (int)checkpoint.Outcome,
                reasonId = checkpoint.ReasonId ?? "",
                elapsedSeconds = checkpoint.ElapsedSeconds,
                hype = checkpoint.Hype,
                combo = checkpoint.Combo,
                reputation = checkpoint.Reputation,
                crateTrackIds = ToArray(checkpoint.CrateTrackIds),
                offeredTrackIds = ToArray(checkpoint.OfferedTrackIds),
                chosenTrackId = checkpoint.ChosenTrackId ?? "",
                cleanCount = checkpoint.CleanCount,
                bassConflicts = checkpoint.BassConflicts,
                cleanPrepCount = checkpoint.CleanPrepCount,
                cueReadyHonorCount = checkpoint.CueReadyHonorCount,
                hadDrop = checkpoint.HadDrop,
                hadStale = checkpoint.HadStale,
                signalLockChecked = checkpoint.SignalLockChecked,
                signalLockOk = checkpoint.SignalLockOk,
                deckId = checkpoint.DeckId ?? ""
            };
        }

        private static RunCheckpoint FromCheckpointDto(CheckpointDto dto)
        {
            if (dto == null || !dto.active)
                return RunCheckpoint.CreateEmpty();

            var checkpoint = RunCheckpoint.CreateEmpty();
            checkpoint.Active = true;
            checkpoint.Seed = dto.seed;
            checkpoint.ClubIndex = dto.clubIndex;
            checkpoint.Phase = (RunPhase)dto.phase;
            checkpoint.Outcome = (SetOutcome)dto.outcome;
            checkpoint.ReasonId = dto.reasonId ?? "";
            checkpoint.ElapsedSeconds = dto.elapsedSeconds;
            checkpoint.Hype = dto.hype;
            checkpoint.Combo = dto.combo;
            checkpoint.Reputation = dto.reputation;
            checkpoint.ChosenTrackId = dto.chosenTrackId ?? "";
            checkpoint.CleanCount = dto.cleanCount;
            checkpoint.BassConflicts = dto.bassConflicts;
            checkpoint.CleanPrepCount = dto.cleanPrepCount;
            checkpoint.CueReadyHonorCount = dto.cueReadyHonorCount;
            checkpoint.HadDrop = dto.hadDrop;
            checkpoint.HadStale = dto.hadStale;
            checkpoint.SignalLockChecked = dto.signalLockChecked;
            checkpoint.SignalLockOk = dto.signalLockOk;
            checkpoint.DeckId = dto.deckId ?? "";
            CopyArray(dto.crateTrackIds, checkpoint.CrateTrackIds);
            CopyArray(dto.offeredTrackIds, checkpoint.OfferedTrackIds);
            return checkpoint;
        }

        private static string[] ToArray(System.Collections.Generic.List<string> list)
        {
            if (list == null || list.Count == 0)
                return Array.Empty<string>();
            return list.ToArray();
        }

        private static void CopyArray(string[] source, System.Collections.Generic.List<string> target)
        {
            target.Clear();
            if (source == null)
                return;
            for (int i = 0; i < source.Length; i++)
            {
                if (!string.IsNullOrEmpty(source[i]))
                    target.Add(source[i]);
            }
        }

        [Serializable]
        private sealed class SaveGameDto
        {
            public int schemaVersion;
            public SettingsDto settings;
            public string[] unlockedTrackIds;
            public string[] unlockedGearIds;
            public string[] unlockedClubIds;
            public int bestReputation;
            public int metaCoins;
            public string[] ownedDeckIds;
            public string equippedDeckId;
            public string[] ownedItemIds;
            public int hubDay;
            public bool hubUnlocked;
            public bool walkUsedThisDay;
            public string settledRunId;
            public string[] claimedGigKeys;
            public CheckpointDto checkpoint;
        }

        [Serializable]
        private sealed class SettingsDto
        {
            public string language;
            public float musicVolume;
            public float sfxVolume;
            public float uiScale;
            public bool reducedFlashing;
            public bool tutorialCompleted;
            public bool boothCoachCompleted;
            public bool fullscreen;
            public int screenWidth;
            public int screenHeight;
        }

        [Serializable]
        private sealed class CheckpointDto
        {
            public bool active;
            public int seed;
            public int clubIndex;
            public int phase;
            public int outcome;
            public string reasonId;
            public double elapsedSeconds;
            public double hype;
            public int combo;
            public double reputation;
            public string[] crateTrackIds;
            public string[] offeredTrackIds;
            public string chosenTrackId;
            public int cleanCount;
            public int bassConflicts;
            public int cleanPrepCount;
            public int cueReadyHonorCount;
            public bool hadDrop;
            public bool hadStale;
            public bool signalLockChecked;
            public bool signalLockOk;
            public string deckId;
        }
    }
}
