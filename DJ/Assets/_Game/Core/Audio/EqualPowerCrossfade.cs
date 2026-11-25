using System;

namespace DeckRogue.Core.Audio
{
    /// <summary>
    /// Equal-power crossfader. Position 0 is deck A only, 1 is deck B only.
    /// The endpoints mute the opposite deck exactly.
    /// </summary>
    public static class EqualPowerCrossfade
    {
        public static void Evaluate(double position, out double gainA, out double gainB)
        {
            if (double.IsNaN(position))
                throw new ArgumentOutOfRangeException(nameof(position));

            if (position <= 0d)
            {
                gainA = 1d;
                gainB = 0d;
                return;
            }

            if (position >= 1d)
            {
                gainA = 0d;
                gainB = 1d;
                return;
            }

            double angle = Math.PI * position * 0.5d;
            gainA = Math.Cos(angle);
            gainB = Math.Sin(angle);
        }
    }
}
