using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// yellow_siren — bright stabs and one musical fifth glide, not a two-tone wail.
    /// 120 BPM, 8 bars, the same cell every bar. Events end before Length-576.
    /// </summary>
    internal static class YellowSiren
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
                    LoopKit.Kick(low, at, 50d, 0.95d);
                    int stab = StabMidi(chord, beat);
                    if (!LoopKit.Diatonic(stab))
                        throw new InvalidOperationException("yellow_siren stab left A minor");
                    LoopKit.Stab(music, at, stab, 0.42d, 1);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int acid = AcidMidi(chord, eighth);
                    if (!LoopKit.Diatonic(acid))
                        throw new InvalidOperationException("yellow_siren acid left A minor");
                    LoopKit.Acid(low, LoopKit.AtEighth(bar, eighth), acid, 0.11d, 0.5d * lift, 0.7d);
                    LoopKit.Hat(perc, LoopKit.At(bar, eighth * 2, false), 0.45d, false, 8800);
                }

                LoopKit.Hat(perc, LoopKit.At(bar, 14, false), 0.55d, true, 8814);
                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.75d, 8840);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.75d, 8840);

                if ((bar & 1) == 0)
                    Voice(music, LoopKit.At(bar, 0, false), chord.Fifth + 24, chord.Root + 24, 1.6d, 0.18d);
            }
        }

        private static int AcidMidi(LoopKit.Chord chord, int eighth)
        {
            switch (eighth & 3)
            {
                case 2:
                    return chord.Fifth;
                case 3:
                    return chord.Third;
                default:
                    return chord.Root;
            }
        }

        private static int StabMidi(LoopKit.Chord chord, int beat)
        {
            switch (beat & 3)
            {
                case 1:
                case 3:
                    return chord.Third + 12;
                case 2:
                    return chord.Fifth + 12;
                default:
                    return chord.Root + 12;
            }
        }

        private static void Voice(double[] buffer, int start, int fromMidi, int toMidi, double duration, double velocity)
        {
            if (!LoopKit.Diatonic(fromMidi) || !LoopKit.Diatonic(toMidi))
                throw new InvalidOperationException("yellow_siren siren left A minor");
            if (Math.Abs(toMidi - fromMidi) > 12)
                throw new InvalidOperationException("yellow_siren siren exceeds an octave");
            if (!LoopKit.Fits(start, duration))
                return;

            double hz0 = LoopKit.Hz(fromMidi);
            double hz1 = LoopKit.Hz(toMidi);
            int count = (int)(duration * LoopKit.SampleRate);
            int guard = LoopKit.Length - LoopKit.TailGuard;
            if (start + count > guard)
                count = guard - start;
            if (count <= 1)
                return;

            double phase = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if ((uint)i >= (uint)buffer.Length || i >= guard)
                    break;
                double u = n / (double)(count - 1);
                double hz = hz0 * Math.Pow(hz1 / hz0, u);
                phase += 2d * Math.PI * hz / LoopKit.SampleRate;
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.018d ? t / 0.018d : 1d;
                double remain = duration - t;
                double release = remain < 0.036d ? Math.Max(0d, remain / 0.036d) : 1d;
                buffer[i] += Math.Sin(phase) * attack * release * velocity;
            }
        }
    }
}
