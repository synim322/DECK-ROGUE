using System;
using GenerateTestLoops;

namespace GenerateTestLoops.Tracks
{
    internal static class PierSalt
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 48d, 0.8d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.45d * lift;
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.22d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.22d, sub);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.26d, 2500 + bar);

                int root = chord.Root + 12;
                int third = chord.Third + 12;
                int fifth = chord.Fifth + 12;
                Place(music, bar, 0, root);
                Place(music, bar, 2, fifth);
                Place(music, bar, 4, third);
                Place(music, bar, 6, fifth);
            }
        }

        private static void Place(double[] music, int bar, int eighth, int midi)
        {
            if (!LoopKit.Diatonic(midi))
                return;

            int at = LoopKit.AtEighth(bar, eighth);
            LoopKit.Pluck(music, at, midi, 0.16d, 0.4d, 0.35d);
            Voice(music, at, midi, 0.2d, 0.22d);
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            double sharp = freq * Math.Pow(2d, 12d / 1200d);
            int count = (int)(duration * LoopKit.SampleRate);
            const double releaseSeconds = 0.03d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double time = i / (double)LoopKit.SampleRate;
                double a = Math.Sin(2d * Math.PI * freq * t);
                double b = Math.Sin(2d * Math.PI * sharp * t);
                double chorus = 0.64d + 0.36d * Math.Sin(2d * Math.PI * 0.5d * time);
                double attack = t < 0.018d ? t / 0.018d : 1d;
                double remain = duration - t;
                double release = remain >= releaseSeconds ? 1d : remain / releaseSeconds;
                buffer[i] += (a + b) * 0.5d * chorus * attack * release * velocity;
            }
        }
    }
}
