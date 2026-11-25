using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// prism_choir: even glass arpeggio over a choir bed.
    /// </summary>
    internal static class PrismChoir
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 50d, 0.78d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.4d * lift;
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.24d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.24d, sub);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.3d, false, 1600);

                int barStart = LoopKit.At(bar, 0, false);
                int choirRoot = chord.Root + 12;
                int choirFifth = chord.Fifth + 12;
                if (LoopKit.Diatonic(choirRoot))
                    LoopKit.Choir(music, barStart, choirRoot, 1.85d, 0.28d);
                if (LoopKit.Diatonic(choirFifth))
                    LoopKit.Choir(music, barStart, choirFifth, 1.85d, 0.28d);

                for (int eighth = 0; eighth < 8; eighth += 2)
                {
                    int midi = eighth == 2 ? chord.Third + 24 : eighth == 4 ? chord.Fifth + 24 : chord.Root + 24;
                    if (!LoopKit.Diatonic(midi))
                        continue;
                    int at = LoopKit.AtEighth(bar, eighth);
                    LoopKit.Bell(music, at, midi, 0.36d, 3.5d);
                    Voice(music, at, midi);
                }
            }
        }

        private static void Voice(double[] buffer, int start, int midi)
        {
            const double duration = 0.2d;
            const double velocity = 0.12d;
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = 2.76d * LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double decay = Math.Exp(-t * 16d);
                double remain = duration - t;
                if (remain < 0.012d)
                    decay *= remain <= 0d ? 0d : remain / 0.012d;
                buffer[start + n] += Math.Sin(2d * Math.PI * freq * t) * decay * velocity;
            }
        }
    }
}
