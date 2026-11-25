using DeckRogue.Core.Save;
using UnityEngine;

namespace DeckRogue.Client
{
    /// <summary>
    /// DontDestroyOnLoad holder for the migrated <see cref="SaveGameDocument"/>.
    /// Load on Boot / MainMenu; flush on settings apply and Act1 FinalResult meta/best-rep.
    /// </summary>
    public sealed class GameSaveService : MonoBehaviour
    {
        private const string HostName = "GameSaveService";

        public static GameSaveService Instance { get; private set; }

        public SaveGameDocument Document { get; private set; }

        /// <summary>Creates or returns the singleton; loads + migrates once.</summary>
        public static GameSaveService Ensure()
        {
            if (Instance != null)
                return Instance;

            var existing = FindAnyObjectByType<GameSaveService>();
            if (existing != null)
            {
                Instance = existing;
                if (existing.Document == null)
                    existing.Reload();
                return existing;
            }

            var host = new GameObject(HostName);
            DontDestroyOnLoad(host);
            var service = host.AddComponent<GameSaveService>();
            service.Reload();
            Instance = service;
            return service;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (Document == null)
                Reload();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Reload()
        {
            Document = LocalSaveStore.Load();
            ApplyRuntimeSettings();
        }

        /// <summary>Push current document settings into <see cref="GameSettings"/> / AudioListener.</summary>
        public void ApplyRuntimeSettings()
        {
            if (Document == null)
                Document = SaveGameDocument.CreateDefault();
            GameSettings.ApplyFrom(Document.Settings);
        }

        public void Flush(SaveFlushTrigger trigger)
        {
            if (Document == null)
                Document = SaveGameDocument.CreateDefault();
            LocalSaveStore.Save(Document, trigger);
            GameSettings.ApplyFrom(Document.Settings);
        }

        /// <summary>Copy settings into the document, apply runtime, flush.</summary>
        public void ApplySettings(SettingsData settings)
        {
            if (Document == null)
                Document = SaveGameDocument.CreateDefault();
            if (settings == null)
                settings = SettingsData.CreateDefault();
            settings.Normalize();
            Document.Settings = settings.Clone();
            Flush(SaveFlushTrigger.SettingsChanged);
        }

        /// <summary>
        /// Persist that the first-night booth lesson was finished or skipped.
        /// </summary>
        public void MarkTutorialCompletedOnFirstRun()
        {
            if (Document == null)
                Document = SaveGameDocument.CreateDefault();
            if (Document.Settings == null)
                Document.Settings = SettingsData.CreateDefault();
            if (Document.Settings.TutorialCompleted && Document.Settings.BoothCoachCompleted)
                return;
            Document.Settings.TutorialCompleted = true;
            Document.Settings.BoothCoachCompleted = true;
            Flush(SaveFlushTrigger.SettingsChanged);
        }

        /// <summary>MainMenu → AudioSpike should restore the pause-menu checkpoint once.</summary>
        public bool PendingResume { get; set; }

        public bool HasResumableCheckpoint =>
            Document != null && Document.HasResumableCheckpoint;

        public void SaveRunCheckpoint(RunCheckpoint checkpoint)
        {
            if (Document == null)
                Document = SaveGameDocument.CreateDefault();
            Document.SetCheckpoint(checkpoint);
            Flush(SaveFlushTrigger.RunCheckpointChanged);
        }

        public void ClearRunCheckpoint()
        {
            if (Document == null)
                Document = SaveGameDocument.CreateDefault();
            if (Document.Checkpoint == null || !Document.Checkpoint.Active)
                return;
            Document.ClearCheckpoint();
            Flush(SaveFlushTrigger.RunCheckpointChanged);
        }

        /// <summary>Consume the one-shot resume flag from MainMenu Continue.</summary>
        public bool ConsumePendingResume()
        {
            if (!PendingResume)
                return false;
            PendingResume = false;
            return HasResumableCheckpoint;
        }
    }
}
