using System;

namespace GenerateTestLoops.Tracks
{
    internal static class ThroneHum
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 40d, 1d);

                LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.4d, 0.62d * lift);
                LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.4d, 0.62d * lift);
                Voice(low, LoopKit.At(bar, 0, false), chord.Root, 1.85d, 0.22d);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.42d, false, 9001);

                LoopKit.Hat(perc, LoopKit.At(bar, 14, false), 0.5d, true, 9001);

                int choir = chord.Root + 12;
                int stab = chord.Fifth + 12;
                if (LoopKit.Diatonic(choir))
                    LoopKit.Choir(music, LoopKit.At(bar, 0, false), choir, 1.85d, 0.45d);
                if (LoopKit.Diatonic(stab))
                    LoopKit.Stab(music, LoopKit.At(bar, 0, false), stab, 0.4d, 2);
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            double detuned = freq * Math.Pow(2d, 8d / 1200d);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.06d ? t / 0.06d : 1d;
                double a = Math.Sin(2d * Math.PI * freq * t);
                double b = Math.Sin(2d * Math.PI * detuned * t);
                double remain = duration - t;
                double release = remain >= 0.08d ? 1d : (remain <= 0d ? 0d : remain / 0.08d);
                buffer[i] += (a + b) * 0.5d * attack * release * velocity;
            }
        }
    }
}
