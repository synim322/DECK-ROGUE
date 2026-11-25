using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// wind_hat. Even house: shaker sixteenths, kick every beat, air pad and a 2 kHz wind.
    /// Wash is 1.85 s and repeats the same phrase every bar.
    /// </summary>
    internal static class WindHat
    {
        private const double Wash = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int downbeat = LoopKit.At(bar, 0, false);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    if ((step & 3) == 0)
                    {
                        LoopKit.Kick(low, at, 48d, 0.86d);
                        if (LoopKit.Diatonic(chord.Root))
                            LoopKit.Bass(low, at, chord.Root, 0.12d, 0.42d * lift, 2);
                    }

                    LoopKit.Shaker(perc, at, 0.48d, 7300);
                    if (step == 2 || step == 6 || step == 10 || step == 14)
                        LoopKit.Hat(perc, at, 0.4d, true, 7400);
                }

                int air = chord.Root + 12;
                if (LoopKit.Diatonic(air))
                    LoopKit.Pad(music, downbeat, air, Wash, 0.28d, 1);
                Voice(music, downbeat, Wash, 0.12d);
            }
        }

        private static void Voice(double[] buffer, int start, double duration, double velocity)
        {
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double below = 0d;
            double band = 0d;
            double slow = Alpha(1000d);
            double fast = Alpha(4000d);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(n + 7300);
                below += slow * (noise - below);
                double high = noise - below;
                band += fast * (high - band);
                double amp = 1d + 0.4d * Math.Sin(2d * Math.PI * 2d * t);
                double attack = t < 0.02d ? t / 0.02d : 1d;
                double remain = duration - t;
                double release = remain >= 0.05d ? 1d : (remain <= 0d ? 0d : remain / 0.05d);
                buffer[i] += band * amp * attack * release * velocity;
            }
        }

        private static double Alpha(double cutoffHz)
        {
            double cutoff = Math.Max(40d, Math.Min(14000d, cutoffHz));
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / LoopKit.SampleRate;
            return dt / (rc + dt);
        }
    }
}
