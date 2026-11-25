using System;
using DeckRogue.Core.Audio;

namespace DeckRogue.Audio
{
    /// <summary>
    /// Resolves MVP stems from Resources/TestLoops. A Web locator can replace this
    /// without changing the library.
    /// </summary>
    public sealed class UnityResourceLocator : IAudioAssetLocator
    {
        public string Locate(TrackDefinition track, StemId stem)
        {
            if (track == null)
                throw new ArgumentNullException(nameof(track));
            return "TestLoops/" + track.StemFile(stem);
        }
    }
}
