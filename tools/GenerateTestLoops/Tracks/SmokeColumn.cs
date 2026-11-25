using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// smoke_column. Hypnotic smoke, even from the first bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Pads are 1.85 s.
    /// </summary>
    internal static class SmokeColumn
    {
        private const double Bed = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int downbeat = LoopKit.At(bar, 0, false);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 44d, 0.78d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.45d * lift;
                    LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.4d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.4d, sub);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.AtEighth(bar, eighth), 0.22d, false, 10200);

                int root = chord.Root + 12;
                int fifth = chord.Fifth + 12;
                if (LoopKit.Diatonic(root))
                    LoopKit.Pad(music, downbeat, root, Bed, 0.4d, 2);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Pad(music, downbeat, fifth, Bed, 0.4d, 2);

                int bell = chord.Fifth + 24;
                if (LoopKit.Diatonic(bell))
                    LoopKit.Bell(music, LoopKit.AtEighth(bar, 4), bell, 0.28d, 2.0d);

                Voice(music, downbeat, root, fifth, 0.16d);
            }
        }

        private static void Voice(double[] buffer, int start, int root, int fifth, double velocity)
        {
            if (fifth - root != 7 || !LoopKit.Diatonic(root) || !LoopKit.Diatonic(fifth) || !LoopKit.Fits(start, Bed))
                return;

            double lowHz = LoopKit.Hz(root) * Math.Pow(2d, -7d / 1200d);
            double highHz = LoopKit.Hz(fifth) * Math.Pow(2d, 7d / 1200d);
            int count = (int)(Bed * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double tremolo = 0.75d + 0.25d * Math.Sin(2d * Math.PI * 0.2d * t);
                double low = Math.Sin(2d * Math.PI * lowHz * t);
                double high = Math.Sin(2d * Math.PI * highHz * t);
                double attack = t < 0.06d ? t / 0.06d : 1d;
                double remain = Bed - t;
                double release = remain >= 0.05d ? 1d : (remain <= 0d ? 0d : remain / 0.05d);
                buffer[i] += (low + high) * 0.5d * tremolo * attack * release * velocity;
            }
        }
    }
}
