using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// void_static — hollow even electro, static bed, one pluck on every beat.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off. Wash is 1.85 s.
    /// </summary>
    internal static class VoidStatic
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.4d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                {
                    int at = LoopKit.At(bar, beat * 4, false);
                    LoopKit.Kick(low, at, 48d, 0.84d);

                    int pluck = chord.Root + 12;
                    if (LoopKit.Diatonic(pluck))
                        LoopKit.Pluck(music, at, pluck, 0.16d, 0.26d, 0.25d);

                    int voice = chord.Root + 24;
                    if (LoopKit.Diatonic(voice))
                        Voice(music, at, LoopKit.Hz(voice), 0.12d);
                }

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.16d, bassVel, 2);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.16d, bassVel, 2);

                for (int eighth = 0; eighth < 8; eighth++)
                    LoopKit.Hat(perc, LoopKit.At(bar, eighth * 2, false), 0.24d, false, 12000);

                LoopKit.NoiseWash(music, LoopKit.At(bar, 0, false), 1.85d, 0.1d, 11, 1000d);
            }
        }

        private static void Voice(double[] buffer, int start, double freq, double velocity)
        {
            const double duration = 0.2d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double body = Math.Sin(2d * Math.PI * freq * t);
                double formant = Math.Sin(2d * Math.PI * freq * 2.5d * t);
                double hollow = 0.5d * body + 0.5d * formant;
                double attack = t < 0.004d ? t / 0.004d : 1d;
                double decay = Math.Exp(-t * 8d);
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += hollow * attack * decay * release * velocity;
            }
        }
    }
}
