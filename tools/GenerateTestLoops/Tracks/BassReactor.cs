using System;

namespace GenerateTestLoops.Tracks
{
    internal static class BassReactor
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int growl = chord.Root - 12;
                if (growl < 28)
                    growl = chord.Root;

                for (int step = 0; step < LoopKit.StepsPerBar; step += 4)
                {
                    int at = LoopKit.At(bar, step, false);
                    LoopKit.Kick(low, at, 42d, 1d);
                    Voice(low, at, growl, 0.2d, 0.28d);
                    LoopKit.Fm(music, at, chord.Fifth + 12, 0.16d, 0.4d, 2d, 1.5d);
                }

                LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.42d, 0.7d);
                LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.42d, 0.7d);

                double acid = 0.55d * lift;
                LoopKit.Acid(low, LoopKit.At(bar, 4, false), chord.Root, 0.12d, acid, 0.85d);
                LoopKit.Acid(low, LoopKit.At(bar, 7, false), chord.Fifth, 0.12d, acid, 0.85d);
                LoopKit.Acid(low, LoopKit.At(bar, 12, false), chord.Third, 0.12d, acid, 0.85d);

                LoopKit.Hat(perc, LoopKit.At(bar, 2, false), 0.48d, true, 5200);
                LoopKit.Hat(perc, LoopKit.At(bar, 6, false), 0.48d, true, 5200);
                LoopKit.Hat(perc, LoopKit.At(bar, 10, false), 0.48d, true, 5200);
                LoopKit.Hat(perc, LoopKit.At(bar, 14, false), 0.48d, true, 5200);

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.7d, 5300);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.7d, 5300);
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.008d ? t / 0.008d : 1d;
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double remain = duration - t;
                double release = remain >= 0.02d ? 1d : (remain <= 0d ? 0d : remain / 0.02d);
                buffer[i] += Math.Tanh(sine * 1.15d) * attack * release * velocity;
            }
        }
    }
}
