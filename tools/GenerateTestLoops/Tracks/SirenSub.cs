using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// siren_sub — the sub holds, the stab answers.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Drone is 1.85 s.
    /// </summary>
    internal static class SirenSub
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int downbeat = LoopKit.At(bar, 0, false);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 42d, 1d);

                if (LoopKit.Diatonic(chord.Root))
                {
                    double sub = 0.6d * lift;
                    LoopKit.Sub(low, downbeat, chord.Root, 0.42d, sub);
                    LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.42d, sub);
                }

                int drone = chord.Root - 12;
                if (drone < 28)
                    drone = chord.Root;
                Voice(low, downbeat, drone, 1.85d, 0.2d);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.4d, false, 8200);

                int root = chord.Root + 12;
                int third = chord.Third + 12;
                int fifth = chord.Fifth + 12;
                if (LoopKit.Diatonic(root))
                    LoopKit.Stab(music, LoopKit.At(bar, 0, false), root, 0.4d, 2);
                if (LoopKit.Diatonic(third))
                    LoopKit.Stab(music, LoopKit.At(bar, 4, false), third, 0.4d, 2);
                if (LoopKit.Diatonic(fifth))
                    LoopKit.Stab(music, LoopKit.At(bar, 8, false), fifth, 0.4d, 2);
                if (LoopKit.Diatonic(third))
                    LoopKit.Stab(music, LoopKit.At(bar, 12, false), third, 0.4d, 2);
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
                double tremolo = 1d - 0.35d * (0.5d - 0.5d * Math.Cos(2d * Math.PI * 4d * t));
                double attack = t < 0.008d ? t / 0.008d : 1d;
                double remain = duration - t;
                double release = remain >= 0.02d ? 1d : (remain <= 0d ? 0d : remain / 0.02d);
                double sine = Math.Sin(2d * Math.PI * freq * t);
                buffer[i] += sine * tremolo * attack * release * velocity;
            }
        }
    }
}
