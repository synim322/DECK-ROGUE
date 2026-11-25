using System;

namespace GenerateTestLoops.Tracks
{
    internal static class TideClick
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.36d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 52d, 0.9d);
                    int pluck = chord.Fifth + 12;
                    if (LoopKit.Diatonic(pluck))
                        LoopKit.Pluck(music, at, pluck, 0.12d, 0.28d, 0.5d);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    int bass = (step & 1) == 0 ? chord.Root : chord.Fifth;
                    if (LoopKit.Diatonic(bass))
                        LoopKit.Bass(low, at, bass, 0.07d, bassVel, 2);
                    if ((step & 1) == 0)
                        LoopKit.Rim(perc, at, 0.55d);
                    if (step == 2 || step == 6 || step == 10 || step == 14)
                        LoopKit.Hat(perc, at, 0.3d, false, 2600);
                }

                Voice(music, LoopKit.At(bar, 0, false), 0.1d);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 1.85d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double cutoff = 400d;
            double lp = 0d;
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double cycle = t / duration;
                double target = 1100d - 700d * Math.Cos(2d * Math.PI * cycle);
                cutoff += Alpha(48d) * (target - cutoff);
                double noise = LoopKit.Hash(n + 26017);
                double high = noise - previous;
                previous = noise;
                lp += Alpha(cutoff) * (high - lp);
                double attack = t < 0.06d ? t / 0.06d : 1d;
                buffer[start + n] += lp * attack * Release(t, duration, 0.1d) * velocity;
            }
        }

        private static double Alpha(double cutoffHz)
        {
            double cutoff = Math.Max(40d, Math.Min(14000d, cutoffHz));
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / LoopKit.SampleRate;
            return dt / (rc + dt);
        }

        private static double Release(double t, double duration, double seconds)
        {
            double remain = duration - t;
            if (remain >= seconds)
                return 1d;
            if (remain <= 0d)
                return 0d;
            return remain / seconds;
        }
    }
}
