using System;

namespace GenerateTestLoops.Tracks
{
    internal static class ChoirDust
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 44d, 0.72d);

                LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.3d, 0.4d * lift);
                LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.3d, 0.4d * lift);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.26d, false, 1500);

                int root = chord.Root + 12;
                int fifth = chord.Fifth + 12;
                int third = chord.Third + 24;
                int barStart = LoopKit.At(bar, 0, false);
                if (LoopKit.Diatonic(root))
                    LoopKit.Choir(music, barStart, root, 1.85d, 0.4d);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Choir(music, barStart, fifth, 1.85d, 0.4d);
                if (LoopKit.Diatonic(third))
                {
                    LoopKit.Pluck(music, LoopKit.At(bar, 4, false), third, 0.1d, 0.22d, 0.2d);
                    LoopKit.Pluck(music, LoopKit.At(bar, 12, false), third, 0.1d, 0.22d, 0.2d);
                }

                Voice(music, barStart);
            }
        }

        private static void Voice(double[] buffer, int start)
        {
            const double duration = 0.12d;
            const double velocity = 0.12d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double rc = 1d / (2d * Math.PI * 1200d);
            double dt = 1d / LoopKit.SampleRate;
            double alpha = dt / (rc + dt);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(n + 2401);
                lp += alpha * (noise - lp);
                double env = Math.Exp(-t * 32d);
                if (t < 0.003d)
                    env *= t / 0.003d;
                double remain = duration - t;
                double release = remain >= 0.01d ? 1d : remain / 0.01d;
                buffer[start + n] += lp * env * release * velocity;
            }
        }
    }
}
