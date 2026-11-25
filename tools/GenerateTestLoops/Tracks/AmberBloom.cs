using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// amber_bloom. Warm even bloom, soft pulse, swell stays above 40%.
    /// 120 BPM, 8 bars, natural A minor. Pads stay within 1.85 s.
    /// </summary>
    internal static class AmberBloom
    {
        private const double BedDuration = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int barStart = LoopKit.At(bar, 0, false);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 46d, 0.74d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.42d * lift;
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.3d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.3d, sub);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.28d, 11900);

                int root = chord.Root + 12;
                int third = chord.Third + 12;
                int fifth = chord.Fifth + 12;
                int bell = chord.Fifth + 24;
                if (LoopKit.Diatonic(root))
                    LoopKit.Pad(music, barStart, root, BedDuration, 0.38d, 0);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Pad(music, barStart, fifth, BedDuration, 0.38d, 0);
                if (LoopKit.Diatonic(bell))
                    LoopKit.Bell(music, LoopKit.AtEighth(bar, 3), bell, 0.3d, 2.4d);
                Voice(music, barStart, root, third, BedDuration, 0.14d);
            }
        }

        private static void Voice(double[] buffer, int start, int root, int third, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(root) || !LoopKit.Diatonic(third) || !LoopKit.Fits(start, duration))
                return;

            double freqRoot = LoopKit.Hz(root);
            double freqThird = LoopKit.Hz(third);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double rise = 0.5d * (1d - Math.Cos(Math.PI * t / duration));
                double swell = 0.4d + 0.6d * rise;
                double tone = Math.Sin(2d * Math.PI * freqRoot * t) + Math.Sin(2d * Math.PI * freqThird * t);
                buffer[i] += tone * 0.5d * swell * velocity;
            }
        }
    }
}
