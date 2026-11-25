using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// phase_fault. Tense even electro: the phase beat is held across the bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class PhaseFault
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);
                int stab = chord.Root + 12;
                bool stabOk = LoopKit.Diatonic(stab);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 54d, 0.95d);
                    if (stabOk)
                        LoopKit.Square(music, at, stab, 0.12d, 0.32d, 0.6d);
                }

                Bass(low, bar, chord, bassVel);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;
                    double hat = offbeat ? 0.55d : 0.3d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hat, false, 11700);
                }

                int fifth = chord.Fifth + 12;
                if (LoopKit.Diatonic(fifth))
                    Voice(music, LoopKit.At(bar, 0, false), fifth);
            }
        }

        private static void Bass(double[] buffer, int bar, LoopKit.Chord chord, double velocity)
        {
            int root = chord.Root;
            int third = chord.Third;
            int fifth = chord.Fifth;
            if (LoopKit.Diatonic(root))
            {
                LoopKit.Bass(buffer, LoopKit.At(bar, 0, false), root, 0.12d, velocity, 1);
                LoopKit.Bass(buffer, LoopKit.At(bar, 14, false), root, 0.12d, velocity, 1);
            }

            if (LoopKit.Diatonic(third))
                LoopKit.Bass(buffer, LoopKit.At(bar, 6, false), third, 0.12d, velocity, 1);
            if (LoopKit.Diatonic(fifth))
                LoopKit.Bass(buffer, LoopKit.At(bar, 8, false), fifth, 0.12d, velocity, 1);
        }

        private static void Voice(double[] buffer, int start, int midi)
        {
            const double duration = 1.85d;
            const double velocity = 0.14d;
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            double sharp = freq * Math.Pow(2d, 6d / 1200d);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double a = Math.Sin(2d * Math.PI * freq * t);
                double b = Math.Sin(2d * Math.PI * sharp * t);
                double attack = t < 0.01d ? t / 0.01d : 1d;
                double remain = duration - t;
                double release = remain >= 0.03d ? 1d : (remain <= 0d ? 0d : remain / 0.03d);
                buffer[i] += (a + b) * 0.5d * attack * release * velocity;
            }
        }
    }
}
