namespace DeckRogue.Core.Time
{
    /// <summary>
    /// Test and tool clock. Assign <see cref="Now"/> to jump the timeline.
    /// </summary>
    public sealed class ManualTimeProvider : ITimeProvider
    {
        public double Now { get; set; }
    }
}
