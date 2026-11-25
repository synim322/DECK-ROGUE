using System;

namespace GenerateTestLoops.Tracks
{
    internal static class NeonPulse
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            int[] intervals = { 0, 7, 3, 7 };
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.62d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 52d, 0.95d);

                LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.15d, bassVel, 0);
                LoopKit.Bass(low, LoopKit.At(bar, 6, false), chord.Third, 0.15d, bassVel, 0);
                LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.15d, bassVel, 0);
                LoopKit.Bass(low, LoopKit.At(bar, 12, false), chord.Root, 0.15d, bassVel, 0);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                {
                    int hatAt = LoopKit.At(bar, step, false);
                    bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;
                    double hatVel = offbeat ? 0.62d : 0.32d;
                    if (step == 14)
                        LoopKit.Hat(perc, hatAt, hatVel, true, 4500 + bar);
                    else
                        LoopKit.Hat(perc, hatAt, hatVel, false, 4200 + bar * 16 + step);
                }

                int clapSalt = 4100 + bar;
                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.78d, clapSalt);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.78d, clapSalt);

                LoopKit.ChordStab(music, LoopKit.At(bar, 0, false), chord, 12, 0.35d, 1);
                for (int n = 0; n < intervals.Length; n++)
                {
                    int midi = HookMidi(chord, intervals[n]);
                    int noteAt = LoopKit.AtEighth(bar, n * 2);
                    LoopKit.Square(music, noteAt, midi, 0.18d, 0.62d, 0.4d);
                    Voice(music, noteAt, midi, 0.16d, 0.22d);
                }
            }
        }

        private static int HookMidi(LoopKit.Chord chord, int interval)
        {
            int midi = chord.Root + 12 + interval;
            if (LoopKit.Diatonic(midi))
                return midi;

            int root = chord.Root + 12;
            int third = chord.Third + 12;
            int fifth = chord.Fifth + 12;
            int nearest = root;
            int distance = Math.Abs(midi - root);
            int thirdDistance = Math.Abs(midi - third);
            if (thirdDistance < distance)
            {
                nearest = third;
                distance = thirdDistance;
            }

            if (Math.Abs(midi - fifth) < distance)
                nearest = fifth;
            return nearest;
        }

        private static void Voice(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!LoopKit.Fits(start, duration))
                return;

            double freq = LoopKit.Hz(midi);
            double detuned = freq * Math.Pow(2d, 8d / 1200d);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double phase = freq * t;
                phase -= Math.Floor(phase);
                double phaseB = detuned * t;
                phaseB -= Math.Floor(phaseB);
                double square = phase < 0.5d ? 0.62d : -0.62d;
                double squareB = phaseB < 0.5d ? 0.62d : -0.62d;
                double saw = (phase * 2d - 1d) * 0.28d;
                double sawB = (phaseB * 2d - 1d) * 0.28d;
                double wave = Math.Tanh(square + saw + squareB + sawB);
                double attack = t < 0.003d ? t / 0.003d : 1d;
                double decay = Math.Exp(-t * 24d);
                double remain = duration - t;
                double release = remain < 0.008d ? (remain > 0d ? remain / 0.008d : 0d) : 1d;
                buffer[i] += wave * attack * decay * release * velocity;
            }
        }
    }
}
