using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// ratchet_works. Rolling even ratchet, the same click every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class RatchetWorks
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.4d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 52d, 0.94d);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    int midi = Cell(chord, step);
                    if (LoopKit.Diatonic(midi))
                        LoopKit.Bass(low, at, midi, 0.06d, bassVel, 0);

                    LoopKit.Hat(perc, at, 0.36d, false, 11600);
                    if (step == 4 || step == 12)
                        LoopKit.Rim(perc, at, 0.5d);

                    int half = step & 7;
                    if (half == 0 || half == 2 || half == 3 || half == 4 || half == 6)
                        Voice(perc, at, 0.16d);

                    int top = midi + 24;
                    if (LoopKit.Diatonic(top))
                        Tick(music, at, top, 0.04d, 0.2d);
                }
            }
        }

        private static int Cell(LoopKit.Chord chord, int step)
        {
            switch (step & 3)
            {
                case 2:
                    return chord.Third;
                case 3:
                    return chord.Fifth;
                default:
                    return chord.Root;
            }
        }

        private static void Tick(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.002d ? t / 0.002d : 1d;
                double remain = duration - t;
                double release = remain >= 0.006d ? 1d : (remain <= 0d ? 0d : remain / 0.006d);
                buffer[start + n] += Math.Sin(2d * Math.PI * freq * t) * attack * release * velocity;
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.005d;
            const double freq = 1500d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double sine = Math.Sin(2d * Math.PI * freq * t);
                double noise = LoopKit.Hash(i + 1500);
                double attack = t < 0.0004d ? t / 0.0004d : 1d;
                double env = Math.Exp(-t * 620d);
                double remain = duration - t;
                if (remain < 0.0008d)
                    env *= remain <= 0d ? 0d : remain / 0.0008d;
                buffer[i] += (sine + noise) * 0.5d * attack * env * velocity;
            }
        }
    }
}
