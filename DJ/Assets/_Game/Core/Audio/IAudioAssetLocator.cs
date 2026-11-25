namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// Maps a logical stem onto the path the active backend can load.
    /// Web and native builds may resolve the same record differently.
    /// </summary>
    public interface IAudioAssetLocator
    {
        string Locate(TrackDefinition track, StemId stem);
    }
}
