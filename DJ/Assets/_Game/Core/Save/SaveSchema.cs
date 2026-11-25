namespace DeckRogue.Core.Save
{
    /// <summary>
    /// Local SaveGame schema for shell / Act1 meta / mid-run checkpoint.
    /// </summary>
    public static class SaveSchema
    {
        /// <summary>Current on-disk document version. Bump only with a migrator step.</summary>
        public const int CurrentVersion = 5;

        /// <summary>Stable file / prefs key for the single local document.</summary>
        public const string DocumentKey = "deck_rogue_save_v1";
    }

    /// <summary>
    /// Why the IO adapter must flush the document. Domain never writes I/O itself.
    /// </summary>
    public enum SaveFlushTrigger
    {
        /// <summary>Any confirmed Settings field change from the Settings screen.</summary>
        SettingsChanged = 0,

        /// <summary>Meta unlock set grew (e.g. boss clear → track_signal_carrier).</summary>
        MetaUnlockChanged = 1,

        /// <summary>Run reached FinalResult and Reputation beat BestReputation.</summary>
        RunEndedBestReputation = 2,

        /// <summary>Pause-menu mid-run checkpoint written or cleared.</summary>
        RunCheckpointChanged = 3,

        /// <summary>
        /// Hub wallet, owned deck/item, equipped deck, hub day, or a claimed gig key changed.
        /// Domain does not flush; the client writes the same document.
        /// </summary>
        MetaWalletChanged = 4
    }
}
