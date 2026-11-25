using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// signal_carrier — uneasy even radio pulse. Same static bed every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Wash is 1.85 s.
    /// </summary>
    internal static class SignalCarrier
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double subVel = 0.5d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 46d, 0.9d);
                    Voice(music, at, 0.22d);
                }

                if (LoopKit.Diatonic(chord.Root))
                {
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.3d, subVel);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.3d, subVel);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.At(bar, eighth * 2, false), 0.36d, false, 10900);

                LoopKit.NoiseWash(music, LoopKit.At(bar, 0, false), 1.85d, 0.1d, 7, 1400d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.06d;
            double freq = LoopKit.Hz(81);
            if (Math.Abs(freq - 880d) > 0.5d || !LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double env = Math.Exp(-t * 64d);
                double remain = duration - t;
                if (remain < 0.003d)
                    env *= remain > 0d ? remain / 0.003d : 0d;
                buffer[start + n] += sine * env * velocity;
            }
        }
    }
}
