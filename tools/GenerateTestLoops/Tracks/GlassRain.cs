using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// glass_rain. Icy even rain: kick every beat, shaker sixteenths, glass on the eighths.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class GlassRain
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
                    LoopKit.Kick(low, at, 52d, 0.86d);
                    if (LoopKit.Diatonic(chord.Root))
                        LoopKit.Bass(low, at, chord.Root, 0.12d, bassVel, 2);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                    LoopKit.Shaker(perc, LoopKit.At(bar, step, false), 0.36d, 12100);

                for (int place = 0; place < 4; place++)
                {
                    int midi = place == 1 ? chord.Third + 24 : place == 2 ? chord.Fifth + 24 : chord.Root + 24;
                    if (!LoopKit.Diatonic(midi))
                        continue;

                    int at = LoopKit.AtEighth(bar, place * 2);
                    LoopKit.Bell(music, at, midi, 0.32d, 3.8d);
                    Voice(music, at, midi, 0.1d);
                }
            }
        }

        private static void Voice(double[] buffer, int start, int midi, double velocity)
        {
            const double duration = 0.02d;
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = 3.3d * LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double tick = Math.Sin(2d * Math.PI * freq * t);
                double decay = Math.Exp(-t * 160d);
                double remain = duration - t;
                if (remain < 0.003d)
                    decay *= remain > 0d ? remain / 0.003d : 0d;
                buffer[i] += tick * decay * velocity;
            }
        }
    }
}
