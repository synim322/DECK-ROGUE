using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// torque_drum. Mechanical even drums on a straight grid.
    /// Kick every beat, tom on fixed cells, eighth hats, one stab, 2 kHz tick.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class TorqueDrum
    {
        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double bassVel = 0.48d * LoopKit.Lift(bar);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 54d, 0.95d);

                if (LoopKit.Diatonic(chord.Root))
                    LoopKit.Bass(low, LoopKit.At(bar, 0, false), chord.Root, 0.12d, bassVel, 1);
                if (LoopKit.Diatonic(chord.Fifth))
                    LoopKit.Bass(low, LoopKit.At(bar, 8, false), chord.Fifth, 0.12d, bassVel, 1);

                LoopKit.Tom(perc, LoopKit.At(bar, 4, false), 140d, 0.45d);
                LoopKit.Tom(perc, LoopKit.At(bar, 10, false), 140d, 0.45d);
                LoopKit.Tom(perc, LoopKit.At(bar, 14, false), 140d, 0.45d);

                for (int eighth = 0; eighth < 8; eighth++)
                {
                    int at = LoopKit.At(bar, eighth * 2, false);
                    LoopKit.Hat(perc, at, 0.4d, false, 10400);
                    Voice(perc, at, 0.14d);
                }

                int stab = chord.Root + 12;
                if (LoopKit.Diatonic(stab))
                    LoopKit.Stab(music, LoopKit.At(bar, 0, false), stab, 0.3d, 0);
            }
        }

        private static void Voice(double[] buffer, int start, double velocity)
        {
            const double duration = 0.015d;
            const double freq = 2000d;
            if (!LoopKit.Fits(start, duration))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double metal = Math.Sin(2d * Math.PI * freq * t)
                    + 0.35d * Math.Sin(2d * Math.PI * freq * 2.73d * t);
                double attack = t < 0.0004d ? t / 0.0004d : 1d;
                double env = Math.Exp(-t * 280d);
                double remain = duration - t;
                if (remain < 0.0015d)
                    env *= remain <= 0d ? 0d : remain / 0.0015d;
                buffer[i] += metal * attack * env * velocity;
            }
        }
    }
}
