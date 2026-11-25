using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// salt_chrome — shaker salt and a chrome bell on an even mid-energy grid.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class SaltChrome
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 52d, 0.88d);

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.14d, bassVel, 2);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.14d, bassVel, 2);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    LoopKit.Shaker(perc, at, 0.4d, 2800);
                    Voice(perc, at, 0.1d);
                }

                int bell = chord.Fifth + 24;
                if (!LoopKit.Diatonic(bell))
                    continue;

                LoopKit.Bell(music, LoopKit.At(bar, 2, false), bell, 0.38d, 5.0d);
                LoopKit.Bell(music, LoopKit.At(bar, 6, false), bell, 0.38d, 5.0d);
                LoopKit.Bell(music, LoopKit.At(bar, 10, false), bell, 0.38d, 5.0d);
                LoopKit.Bell(music, LoopKit.At(bar, 14, false), bell, 0.38d, 5.0d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.006d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i * 5 + 2817);
                double grain = noise - previous;
                previous = noise;
                double attack = t < 0.0003d ? t / 0.0003d : 1d;
                double env = Math.Exp(-t * 700d);
                double remain = duration - t;
                if (remain < 0.001d)
                    env *= remain / 0.001d;
                buffer[i] += grain * attack * env * velocity;
            }
        }
    }
}
