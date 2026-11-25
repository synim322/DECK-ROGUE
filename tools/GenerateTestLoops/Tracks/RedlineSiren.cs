using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// redline_siren — urgent even bass-tech. A musical fifth glide, not a police siren.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Glide is 1.6 s, then a gap.
    /// </summary>
    internal static class RedlineSiren
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double acidVel = 0.52d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 50d, 1d);
                    int stab = StabMidi(chord, beat);
                    if (!LoopKit.Diatonic(stab))
                        throw new InvalidOperationException("redline_siren stab left A minor");
                    LoopKit.Stab(music, at, stab, 0.4d, 1);
                }

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int acid = AcidMidi(chord, eighth);
                    if (!LoopKit.Diatonic(acid))
                        throw new InvalidOperationException("redline_siren acid left A minor");
                    LoopKit.Acid(low, LoopKit.At(bar, eighth * 2, false), acid, 0.1d, acidVel, 0.8d);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool offbeat = (step & 3) == 2;
                    double hat = offbeat ? 0.64d : 0.38d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hat, false, 12200);
                }

                LoopKit.Clap(perc, LoopKit.At(bar, 4, false), 0.8d, 12200);
                LoopKit.Clap(perc, LoopKit.At(bar, 12, false), 0.8d, 12200);

                Voice(music, LoopKit.At(bar, 0, false), chord.Fifth + 24, chord.Root + 24, 1.6d, 0.16d);
            }
        }

        private static int AcidMidi(LoopKit.Chord chord, int eighth)
        {
            switch (eighth & 3)
            {
                case 1:
                case 3:
                    return chord.Fifth;
                case 2:
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
                throw new InvalidOperationException("redline_siren glide left A minor");
            if (Math.Abs(toMidi - fromMidi) != 7)
                throw new InvalidOperationException("redline_siren glide is not a fifth");
            if (!LoopKit.Fits(start, duration))
                return;

            double hz0 = LoopKit.Hz(fromMidi);
            double hz1 = LoopKit.Hz(toMidi);
            int count = (int)(duration * LoopKit.SampleRate);
            if (count <= 1)
                return;

            double phase = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double u = n / (double)(count - 1);
                double hz = hz0 * Math.Pow(hz1 / hz0, u);
                phase += 2d * Math.PI * hz / LoopKit.SampleRate;
                double t = n / (double)LoopKit.SampleRate;
                double attack = t < 0.01d ? t / 0.01d : 1d;
                double remain = duration - t;
                double release = remain >= 0.028d ? 1d : (remain <= 0d ? 0d : remain / 0.028d);
                buffer[i] += Math.Sin(phase) * attack * release * velocity;
            }
        }
    }
}
