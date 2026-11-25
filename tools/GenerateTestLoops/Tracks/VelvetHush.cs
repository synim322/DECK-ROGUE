using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// velvet_hush — бархат и шёпот, пульс не пропадает.
    /// </summary>
    internal static class VelvetHush
    {
        private const double Bed = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 46d, 0.74d);
                    if (LoopKit.Diatonic(chord.Root))
                        LoopKit.Sub(low, at, chord.Root, 0.28d, 0.32d * lift);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.AtEighth(bar, eighth), 0.22d, false, 4500);

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.36d, 4512);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.36d, 4512);

                int downbeat = LoopKit.At(bar, 0, false);
                int root = chord.Root + 12;
                if (LoopKit.Diatonic(root))
                    LoopKit.Pad(music, downbeat, root, Bed, 0.3d, 0);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Pad(music, downbeat, chord.Fifth, Bed, 0.3d, 0);

                LoopKit.Chord pair = LoopKit.Bar(bar - (bar % 2));
                int third = pair.Third + 24;
                int fifth = pair.Fifth + 24;
                if (LoopKit.Diatonic(third))
                    LoopKit.Fm(music, LoopKit.AtEighth(bar, 0), third, 0.2d, 0.28d, 2d, 0.35d);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Fm(music, LoopKit.AtEighth(bar, 4), fifth, 0.2d, 0.28d, 2d, 0.35d);

                Voice(music, downbeat, Bed, 0.08d);
            }
        }

        private static void Voice(double[] buffer, int start, double duration, double velocity)
        {
            const double cutoff = 700d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double lp = 0d;
            double previous = 0d;
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / LoopKit.SampleRate;
            double alpha = dt / (rc + dt);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i + 4700);
                double breath = noise - previous;
                previous = noise;
                lp += alpha * (breath - lp);
                double swell = 0.75d + 0.25d * Math.Sin(2d * Math.PI * 0.5d * t);
                double attack = t < 0.04d ? t / 0.04d : 1d;
                double remain = duration - t;
                double release = 1d;
                if (remain < 0.12d)
                    release = remain <= 0d ? 0d : remain / 0.12d;
                buffer[i] += lp * swell * attack * release * velocity;
            }
        }
    }
}
