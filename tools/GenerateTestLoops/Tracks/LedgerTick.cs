using System;

namespace GenerateTestLoops.Tracks
{
    internal static class LedgerTick
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 50d, 0.88d);
                    int bass = beat == 2 ? chord.Third : beat == 0 ? chord.Root : chord.Fifth;
                    if (LoopKit.Diatonic(bass))
                        LoopKit.Bass(low, at, bass, 0.14d, 0.5d * lift, 2);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    int at = LoopKit.At(bar, step, false);
                    if ((step & 1) == 0)
                        LoopKit.Hat(perc, at, 0.22d, false, 1700);
                    Voice(perc, at, (step & 3) == 0 ? 0.28d : 0.16d);
                }

                for (int eighth = 0; eighth < 8; eighth += 2)
                {
                    int midi = eighth == 0 ? chord.Root + 12 : eighth == 4 ? chord.Fifth + 12 : chord.Third + 12;
                    if (!LoopKit.Diatonic(midi))
                        continue;
                    LoopKit.Square(music, LoopKit.AtEighth(bar, eighth), midi, 0.14d, 0.36d, 0.25d);
                }
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.018d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double decay = Math.Exp(-t / duration);
                double remain = duration - t;
                if (remain < 0.002d)
                    decay *= remain / 0.002d;
                buffer[start + n] += Math.Sin(2d * Math.PI * 1800d * t) * decay * velocity;
            }
        }
    }
}
