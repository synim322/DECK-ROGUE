using System;

namespace GenerateTestLoops.Tracks
{
    /// <summary>
    /// dusk_pad. Warm evening pad, even four-on-the-floor pulse.
    /// 120 BPM, 8 bars, natural A minor. Last pad is 1.85 s.
    /// </summary>
    internal static class DuskPad
    {
        private const double PadDuration = 1.85d;

        public static void Render(double[] low, double[] perc, double[] music)
        {
            for (int bar = 0; bar < LoopKit.Bars; bar++)
            {
                LoopKit.Chord chord = LoopKit.Bar(bar);
                double lift = LoopKit.Lift(bar);
                int barStart = LoopKit.At(bar, 0, false);

                for (int beat = 0; beat < 4; beat++)
                    LoopKit.Kick(low, LoopKit.At(bar, beat * 4, false), 46d, 0.76d);

                LoopKit.Sub(low, LoopKit.At(bar, 0, false), chord.Root, 0.34d, 0.48d * lift);
                LoopKit.Sub(low, LoopKit.At(bar, 8, false), chord.Root, 0.34d, 0.48d * lift);

                for (int step = 0; step < LoopKit.StepsPerBar; step += 2)
                    LoopKit.Hat(perc, LoopKit.At(bar, step, false), 0.3d, false, 2100);

                LoopKit.Pad(music, barStart, chord.Root + 12, PadDuration, 0.42d, 2);
                LoopKit.Pad(music, barStart, chord.Fifth + 12, PadDuration, 0.42d, 2);
                LoopKit.Bell(music, LoopKit.AtEighth(bar, 0), chord.Third + 24, 0.36d, 2.2d);
                LoopKit.Bell(music, LoopKit.AtEighth(bar, 4), chord.Third + 24, 0.36d, 2.2d);
                Voice(music, barStart, chord.Root + 12, chord.Fifth + 12, PadDuration, 0.16d);
            }
        }

        private static void Voice(double[] buffer, int start, int root, int fifth, double duration, double velocity)
        {
            if (start < 0 || duration <= 0d || !LoopKit.Diatonic(root) || !LoopKit.Diatonic(fifth))
                return;

            int count = (int)(duration * LoopKit.SampleRate);
            int room = buffer.Length - start;
            if (count > room)
                count = room;
            if (count <= 0)
                return;

            double freqRoot = LoopKit.Hz(root);
            double freqFifth = LoopKit.Hz(fifth);
            const double attack = 0.16d;
            const double release = 0.22d;
            for (int n = 0; n < count; n++)
            {
                double t = n / (double)LoopKit.SampleRate;
                double tremolo = 1d + 0.07d * Math.Sin(2d * Math.PI * 0.25d * t);
                double low = Math.Sin(2d * Math.PI * freqRoot * t);
                double high = Math.Sin(2d * Math.PI * freqFifth * t);
                double env = t < attack ? t / attack : 1d;
                double remain = duration - t;
                if (remain < release)
                    env *= remain <= 0d ? 0d : remain / release;
                buffer[start + n] += (0.5d * low + 0.5d * high) * tremolo * env * velocity;
            }
        }
    }
}
