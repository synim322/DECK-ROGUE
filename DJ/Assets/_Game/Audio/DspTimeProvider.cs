using DeckRogue.Core.Time;
using UnityEngine;

namespace DeckRogue.Audio
{
    /// <summary>
    /// Audio device clock. Musical phase reads this value directly.
    /// </summary>
    public sealed class DspTimeProvider : ITimeProvider
    {
        public double Now
        {
            get { return AudioSettings.dspTime; }
        }
    }
}
