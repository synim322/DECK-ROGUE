using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// relay_dub — offbeat skank and a short delay, even on the grid.
    /// </summary>
    internal static class RelayDub
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    double velocity = (beat & 1) == 0 ? 1d : 0.75d;
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 46d, velocity);
                }

                double bass = 0.55d * lift;
                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.34d, bass, 2);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.34d, bass, 2);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.28d, false, 4700);

                LoopKit.Hat(perc, LoopKit.At(bar, 14, false), 0.35d, true, 4700);

                for (int hit = 0; hit < 4; hit++)
                {
                    int step = 2 + hit * 4;
                    int start = LoopKit.At(bar, step, false);
                    if (LoopKit.Fits(start, 0.2d))
                    {
                        LoopKit.Skank(music, start, chord, 0.45d);
                        Voice(music, start, 0.12d);
                    }

                    Delay(music, bar, step + 1, chord, 0.45d * 0.45d);
                    Delay(music, bar, step + 2, chord, 0.45d * 0.22d);
                }
            }
        }

        private static void Delay(double[] buffer, int bar, int step, LoopKit.Chord chord, double velocity)
        {
            int start = LoopKit.At(bar, step, false);
            if (!LoopKit.Fits(start, 0.2d))
                return;

            LoopKit.Skank(buffer, start, chord, velocity);
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.18d;
            const double ringHz = 900d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 4800);
                double metal = noise - previous;
                previous = noise;
                double ring = Math.Sin(2d * Math.PI * ringHz * t);
                double splash = Math.Exp(-t * 34d);
                double ringEnv = Math.Exp(-t * 12d);
                double attack = t < 0.0015d ? t / 0.0015d : 1d;
                double remain = duration - t;
                double release = 1d;
                if (remain < 0.012d)
                    release = remain <= 0d ? 0d : remain / 0.012d;
                buffer[i] += (metal * 0.55d * splash + ring * ringEnv) * attack * release * velocity;
            }
        }
    }
}
