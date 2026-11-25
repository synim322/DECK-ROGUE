using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// night_tram — warm even tram roll, wheels on every 16th.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Pads are 1.85 s.
    /// </summary>
    internal static class NightTram
    {
        private const double Bed = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int downbeat = LoopKit.At(bar, 0, false);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 48d, 0.8d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double bass = 0.45d * lift;
                    for (int eighth = 0; eighth < 8; eighth++)
                        LoopKit.Bass(low, LoopKit.At(bar, eighth * 2, false), chord.Root, 0.14d, bass, 2);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.32d, 11300);

                int root = chord.Root + 12;
                int fifth = chord.Fifth + 12;
                if (LoopKit.Diatonic(root))
                    LoopKit.Pad(music, downbeat, root, Bed, 0.36d, 0);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Pad(music, downbeat, fifth, Bed, 0.36d, 0);

                Voice(low, downbeat, Bed, 0.1d);
            }
        }

        private static void Voice(double[] buffer, int start, double duration, double velocity)
        {
            const double cutoff = 300d;
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
                double noise = LoopKit.Hash(n + 11300);
                lp += alpha * (noise - lp);
                double pulse = 0.75d + 0.25d * Math.Sin(2d * Math.PI * 2d * t);
                double attack = t < 0.008d ? t / 0.008d : 1d;
                double remain = duration - t;
                double release = remain >= 0.03d ? 1d : (remain <= 0d ? 0d : remain / 0.03d);
                buffer[i] += lp * pulse * attack * release * velocity;
            }
        }
    }
}
