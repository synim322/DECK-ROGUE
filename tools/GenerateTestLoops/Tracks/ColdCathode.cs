using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// cold_cathode — icy even glass techno. Same bell arp every bar.
    /// 120 BPM, 8 bars, natural A minor. Swing stays off.
    /// </summary>
    internal static class ColdCathode
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
                    LoopKit.Kick(low, at, 56d, 0.9d);
                    if (LoopKit.Diatonic(chord.Root))
                        LoopKit.Bass(low, at, chord.Root, 0.12d, 0.42d * lift, 2);
                }

                for (int step = 0; step < LoopKit.StepsPerBar; step++)
                {
                    bool offbeat = (step & 3) == 2;
                    double hat = offbeat ? 0.5d : 0.28d;
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), hat, false, 11200);
                }

                int pad = chord.Root + 12;
                if (LoopKit.Diatonic(pad))
                    LoopKit.Pad(music, LoopKit.At(bar, 0, false), pad, 1.85d, 0.22d, 1);

                for (int eighth = 0; eighth < 8; eighth += 2)
                {
                    int midi = eighth == 2 ? chord.Root + 24 : eighth == 4 ? chord.Third + 24 : chord.Fifth + 24;
                    if (!LoopKit.Diatonic(midi))
                        continue;
                    int at = LoopKit.AtEighth(bar, eighth);
                    LoopKit.Bell(music, at, midi, 0.34d, 4.5d);
                    Voice(music, at, midi);
                }
            }

            Guard(low);
            Guard(perc);
            Guard(music);
        }

        private static void Voice(double[] buffer, int start, int midi)
        {
            const double duration = 0.16d;
            const double velocity = 0.12d;
            if (!LoopKit.Diatonic(midi) || !LoopKit.Fits(start, duration))
                return;

            double freq = 2.9d * LoopKit.Hz(midi);
            int count = (int)(duration * LoopKit.SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)LoopKit.SampleRate;
                double partial = Math.Sin(2d * Math.PI * freq * t);
                double decay = Math.Exp(-t * 18d);
                double attack = t < 0.002d ? t / 0.002d : 1d;
                double remain = duration - t;
                double release = remain >= 0.012d ? 1d : (remain <= 0d ? 0d : remain / 0.012d);
                buffer[i] += partial * decay * attack * release * velocity;
            }
        }

        private static void Guard(double[] buffer)
        {
            double peak = 0d;
            for (int i = 0; i < buffer.Length; i++)
            {
                double sample = buffer[i];
                if (double.IsNaN(sample) || double.IsInfinity(sample))
                    throw new InvalidOperationException("cold_cathode");
                double abs = Math.Abs(sample);
                if (abs > peak)
                    peak = abs;
            }

            if (peak <= 0d || peak >= 12d)
                throw new InvalidOperationException("cold_cathode");
        }
    }
}
