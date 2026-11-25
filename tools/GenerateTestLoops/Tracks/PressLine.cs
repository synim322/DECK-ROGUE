using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// press_line. Heavy even sub tool: kick every beat, long sub on 1 and 3.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Voice is 1.85 s and stays audible.
    /// </summary>
    internal static class PressLine
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double subVel = 0.65d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 42d, 1d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.42d, subVel);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.42d, subVel);
                }

                int under = chord.Root - 12;
                if (LoopKit.Diatonic(under))
                    Voice(low, LoopKit.At(bar, 0, false), under, 1.85d, 0.22d);

                for (int step = 2; step < LoopKit.StepsPerBar; step += 4)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.4d, true, 10300);

                int stab = chord.Fifth + 12;
                if (LoopKit.Diatonic(stab))
                    LoopKit.Stab(music, LoopKit.At(bar, 4, false), stab, 0.32d, 2);
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
                double pulse = 0.65d + 0.35d * (0.5d + 0.5d * Math.Cos(2d * Math.PI * 2d * t));
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double attack = t < 0.008d ? t / 0.008d : 1d;
                double remain = duration - t;
                double release = remain >= 0.02d ? 1d : (remain <= 0d ? 0d : remain / 0.02d);
                buffer[i] += sine * pulse * attack * release * velocity;
            }
        }
    }
}
