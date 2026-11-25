using System;

namespace GenerateTestLoops.Tracks
{
    internal static class Afterhours
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bass = 0.5d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 48d, 0.78d);

                LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.26d, bass, 2);
                LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.26d, bass, 2);

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.22d, 3300 + step);

                int downbeat = LoopKit.At(bar, 0, false);
                LoopKit.Pad(music, downbeat, chord.Root + 12, 1.85d, 0.4d, 0);
                LoopKit.Pad(music, downbeat, chord.Fifth + 12, 1.85d, 0.4d, 0);
                Voice(music, downbeat, chord.Root + 12, 1.85d, 0.2d);

                LoopKit.Fm(music, LoopKit.AtEighth(bar, 0), chord.Root + 24, 0.22d, 0.34d, 1d, 0.55d);
                LoopKit.Fm(music, LoopKit.AtEighth(bar, 2), chord.Third + 24, 0.22d, 0.34d, 1d, 0.55d);
                LoopKit.Fm(music, LoopKit.AtEighth(bar, 5), chord.Fifth + 24, 0.22d, 0.34d, 1d, 0.55d);
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            double detuned = freq * Math.Pow(2d, 8d / 1200d);
            int count = (int)(duration * LoopKit.SampleRate);
            double phase = 0d;
            double phaseB = 0d;
            const double release = 0.18d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)LoopKit.SampleRate;
                phase += 2d * Math.PI * freq / LoopKit.SampleRate;
                phaseB += 2d * Math.PI * detuned / LoopKit.SampleRate;
                double chorus = 0.5d * (Math.Sin(phase) + Math.Sin(phaseB));
                double lfo = 0.75d + 0.25d * Math.Sin(2d * Math.PI * 0.3d * t);
                double attack = t < 0.02d ? t / 0.02d : 1d;
                double remain = duration - t;
                double tail = 1d;
                if (remain < release)
                    tail = remain <= 0d ? 0d : remain / release;
                buffer[i] += chorus * lfo * attack * tail * velocity;
            }
        }
    }
}
