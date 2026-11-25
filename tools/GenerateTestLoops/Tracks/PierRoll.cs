using System;
using GenerateTestLoops;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// pier_roll. Even techno roll: the same 16th cell every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing off.
    /// </summary>
    internal static class PierRoll
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

                    bool offbeat = (step & 1) == 1;
                    LoopKit.Hat(perc, at, offbeat ? 0.48d : 0.3d, false, 2700);
                    Voice(perc, at, 0.1d);

                    int pluck = midi + 12;
                    if (LoopKit.Diatonic(pluck))
                        LoopKit.Pluck(music, at, pluck, 0.08d, 0.26d, 0.6d);
                }
            }
        }

        private static int Cell(LoopKit.Chord chord, int step)
        {
            int slot = step & 3;
            if (slot == 2)
                return chord.Third;
            if (slot == 3)
                return chord.Fifth;
            return chord.Root;
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.003d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double noise = LoopKit.Hash(i);
                double bright = noise - previous;
                previous = noise;
                double attack = t < 0.00025d ? t / 0.00025d : 1d;
                double env = Math.Exp(-t * 1600d);
                buffer[i] += bright * attack * env * velocity;
            }
        }
    }
}
