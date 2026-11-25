using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// static_crown — static bed and a crown of fifths, even boss groove.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Wash is 1.85 s.
    /// </summary>
    internal static class StaticCrown
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double subVel = 0.58d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 40d, 1d);
                    int fifth = chord.Fifth + 12;
                    if (LoopKit.Diatonic(fifth))
                        LoopKit.Bell(music, at, fifth, 0.36d, 2.5d);
                    Voice(music, at);
                }

                if (LoopKit.Diatonic(chord.Root))
                {
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.36d, subVel);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.36d, subVel);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.AtEighth(bar, eighth), 0.46d, false, 9300);

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.75d, 9300);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.75d, 9300);

                LoopKit.NoiseWash(music, LoopKit.At(bar, 0, false), 1.85d, 0.1d, 42, 1800d);
            }
        }

        private static void Voice(double[] buffer, int start)
        {
            const double duration = 0.04d;
            const double velocity = 0.14d;
            const double cutoff = 2400d;
            const double decay = 80d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / LoopKit.SampleRate;
            double alpha = dt / (rc + dt);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(n + 2400);
                lp += alpha * (noise - lp);
                double env = Math.Exp(-t * decay);
                double remain = duration - t;
                if (remain < 0.003d)
                    env *= remain > 0d ? remain / 0.003d : 0d;
                buffer[start + n] += lp * env * velocity;
            }
        }
    }
}
