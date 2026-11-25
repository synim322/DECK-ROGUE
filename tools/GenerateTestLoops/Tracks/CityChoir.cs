using System;
using GenerateTestLoops;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// city_choir — городской хор поверх ровного house.
    /// </summary>
    internal static class CityChoir
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int root = chord.Root + 12;
                int fifth = chord.Fifth + 12;

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 52d, 0.9d);

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.16d, 0.52d * lift, 0);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.16d, 0.52d * lift, 0);

                for (int step = 2; step < LoopKit.StepsPerBar; step += 4)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.5d, false, 3100);

                LoopKit.Hat(perc, LoopKit.At(bar, 0, false), 0.28d, false, 3100);
                LoopKit.Hat(perc, LoopKit.At(bar, 8, false), 0.28d, false, 3100);
                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.72d, 3100);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.72d, 3100);

                if (LoopKit.Diatonic(root))
                    LoopKit.Choir(music, LoopKit.At(bar, 0, false), root, 1.85d, 0.22d);

                Stab(music, bar, 4, root, fifth);
                Stab(music, bar, 12, root, fifth);
            }
        }

        private static void Stab(double[] music, int bar, int step, int root, int fifth)
        {
            int at = LoopKit.At(bar, step, false);
            if (LoopKit.Diatonic(root))
            {
                LoopKit.Choir(music, at, root, 0.4d, 0.48d);
                Voice(music, at, root, 0.35d, 0.2d);
            }

            if (LoopKit.Diatonic(fifth))
            {
                LoopKit.Choir(music, at, fifth, 0.4d, 0.48d);
                Voice(music, at, fifth, 0.35d, 0.2d);
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            const double attack = 0.012d;
            const double release = 0.028d;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double cycle = 2d * Math.PI * freq * t;
                double tone = Math.Sin(cycle) + 0.5d * Math.Sin(2d * cycle) + 0.25d * Math.Sin(3d * cycle);
                double env = t < attack ? t / attack : 1d;
                double remain = duration - t;
                if (remain < release)
                    env *= remain <= 0d ? 0d : remain / release;
                buffer[start + n] += tone * env * velocity;
            }
        }
    }
}
