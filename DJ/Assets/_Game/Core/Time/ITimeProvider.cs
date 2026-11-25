namespace DeckRogue.Core.Time
{
    /// <summary>
    /// Absolute timeline in seconds. Implementations may wrap the audio DSP clock.
    /// Frame delta is not a valid source: the clock reads <see cref="Now"/> on each query.
    /// </summary>
    public interface ITimeProvider
    {
        double Now { get; }
    }
}
