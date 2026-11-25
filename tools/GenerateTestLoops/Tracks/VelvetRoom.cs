using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// velvet_room — velvet Rhodes on an even four-on-the-floor grid.
    /// </summary>
    internal static class VelvetRoom
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 50d, 0.82d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.3d, 0.5d * lift);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.3d, 0.5d * lift);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.28d, 4400);

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.42d, 4412);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.42d, 4412);

                for (int beat = 0; beat < 4; beat++)
                {
                    int note = beat == 0 ? chord.Root + 12 : beat == 2 ? chord.Fifth + 12 : chord.Third + 12;
                    if (!LoopKit.Diatonic(note))
                        continue;
                    int start = LoopKit.AtEighth(bar, beat * 2);
                    Voice(music, start, 0.15d);
                    LoopKit.Fm(music, start, note, 0.22d, 0.4d * lift, 1d, 1.05d);
                }
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.03d;
            const double cutoff = 900d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double lp = 0d;
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / LoopKit.SampleRate;
            double alpha = dt / (rc + dt);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 8800);
                lp += alpha * (noise - lp);
                double attack = t < 0.0008d ? t / 0.0008d : 1d;
                double decay = Math.Exp(-t * 70d);
                double remain = duration - t;
                double release = 1d;
                if (remain < 0.004d)
                    release = remain <= 0d ? 0d : remain / 0.004d;
                buffer[i] += lp * attack * decay * release * velocity;
            }
        }
    }
}
