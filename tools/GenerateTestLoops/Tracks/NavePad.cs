using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// nave_pad. Cathedral nave, wide and even, still pulsing.
    /// 120 BPM, 8 bars, natural A minor. Pads stay within 1.85 s.
    /// </summary>
    internal static class NavePad
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
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 44d, 0.7d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.4d * lift;
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.36d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.36d, sub);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.2d, false, 1400);

                int root = chord.Root + 12;
                int third = chord.Third + 12;
                int fifth = chord.Fifth + 12;
                int choir = chord.Root + 24;
                if (LoopKit.Diatonic(root))
                    LoopKit.Pad(music, barStart, root, BedDuration, 0.36d, 2);
                if (LoopKit.Diatonic(third))
                    LoopKit.Pad(music, barStart, third, BedDuration, 0.36d, 2);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Pad(music, barStart, fifth, BedDuration, 0.36d, 2);
                if (LoopKit.Diatonic(choir))
                    LoopKit.Choir(music, barStart, choir, BedDuration, 0.2d);
                Voice(music, barStart, root, fifth, BedDuration, 0.14d);
            }
        }

        private static void Voice(double[] buffer, int start, int root, int fifth, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(root) || !LoopKit.Diatonic(fifth) || !LoopKit.Fits(start, duration))
                return;

            double barSeconds = (double)(LoopKit.StepsPerBar * LoopKit.Sixteenth) / LoopKit.SampleRate;
            double freqRoot = LoopKit.Hz(root);
            double freqFifth = LoopKit.Hz(fifth);
            int count = (int)(duration * LoopKit.SampleRate);
            const double release = 0.02d;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double rise = 0.5d * (1d - Math.Cos(Math.PI * t / barSeconds));
                double swell = 0.4d + 0.6d * rise;
                double tone = Math.Sin(2d * Math.PI * freqRoot * t) + Math.Sin(2d * Math.PI * freqFifth * t);
                double remain = duration - t;
                double tail = remain >= release ? 1d : (remain <= 0d ? 0d : remain / release);
                buffer[start + n] += tone * 0.5d * swell * tail * velocity;
            }
        }
    }
}
