using System;

namespace GenerateTestLoops
{
    /// <summary>
    /// Shared grid and voices for 16-second test loops.
    /// 120 BPM, 4/4, 8 bars, natural A minor. Callers fill the buffers they are given.
    /// </summary>
    internal static class LoopKit
    {
        public const int SampleRate = 48000;
        public const int Length = SampleRate * 16;
        public const int Sixteenth = SampleRate / 8;
        public const int Eighth = SampleRate / 4;
        public const int Bars = 8;
        public const int StepsPerBar = 16;
        public const int TailGuard = 576;

        public struct Chord
        {
            public Chord(int root, int third, int fifth, int below)
            {
                Root = root;
                Third = third;
                Fifth = fifth;
                Below = below;
            }

            public int Root;
            public int Third;
            public int Fifth;
            public int Below;

            public static Chord AMinor => new Chord(45, 48, 52, 43);
            public static Chord FMajor => new Chord(41, 45, 48, 40);
            public static Chord GMajor => new Chord(43, 47, 50, 41);
        }

        public static Chord Bar(int bar)
        {
            switch (bar & 7)
            {
                case 2:
                case 5:
                    return Chord.FMajor;
                case 3:
                case 6:
                    return Chord.GMajor;
                default:
                    return Chord.AMinor;
            }
        }

        public static double Lift(int bar)
        {
            return bar >= 4 ? 1.08d : 1d;
        }

        public static int At(int bar, int step, bool swing)
        {
            int sample = (bar * StepsPerBar + step) * Sixteenth;
            if (swing && (step & 1) == 1)
                sample += (int)(0.012d * SampleRate);
            return sample;
        }

        public static int AtEighth(int bar, int eighth)
        {
            return (bar * 8 + eighth) * Eighth;
        }

        public static double Hz(int midi)
        {
            return 440d * Math.Pow(2d, (midi - 69) / 12d);
        }

        public static bool Diatonic(int midi)
        {
            int pc = midi % 12;
            if (pc < 0)
                pc += 12;
            return pc == 0 || pc == 2 || pc == 4 || pc == 5 || pc == 7 || pc == 9 || pc == 11;
        }

        public static bool Fits(int start, double duration)
        {
            if (start < 0)
                return false;
            int end = start + (int)(duration * SampleRate);
            return end <= Length - TailGuard;
        }

        public static double Hash(int index)
        {
            uint x = (uint)index * 747796405u + 2891336453u;
            uint w = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
            w = (w >> 22) ^ w;
            return (w / (double)uint.MaxValue) * 2d - 1d;
        }

        public static void Kick(double[] buffer, int start, double bodyHz, double velocity)
        {
            const double duration = 0.16d;
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            double phase = 0d;
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double hz = bodyHz + 190d * Math.Exp(-t * 36d);
                phase += 2d * Math.PI * hz / SampleRate;
                double body = Math.Sin(phase) * Math.Exp(-t * 11d);
                double noise = Hash(i + 17) - previous;
                previous = Hash(i + 17);
                double click = noise * Math.Exp(-t * 260d);
                if (t < 0.001d)
                    click *= t / 0.001d;
                double sample = Math.Tanh((body * 1.15d + click * 0.38d) * 1.25d);
                buffer[i] += sample * Release(t, duration, 0.012d) * velocity;
            }
        }

        public static void Sub(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double attack = t < 0.008d ? t / 0.008d : 1d;
                double body = Math.Sin(2d * Math.PI * freq * t);
                buffer[i] += Math.Tanh(body * 1.4d) * attack * Release(t, duration, 0.03d) * velocity;
            }
        }

        public static void Bass(double[] buffer, int start, int midi, double duration, double velocity, int color)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double slide = freq * Math.Pow(2d, Math.Exp(-t * 55d) / 18d);
                double cycle = slide * t;
                double wave;
                if (color == 1)
                {
                    wave = Math.Sin(2d * Math.PI * cycle) >= 0d ? 0.8d : -0.8d;
                    wave += 0.25d * Math.Sin(2d * Math.PI * cycle * 3d);
                }
                else if (color == 2)
                {
                    wave = Math.Sin(2d * Math.PI * cycle) + 0.18d * Math.Sin(2d * Math.PI * cycle * 3d);
                }
                else
                {
                    wave = 0d;
                    for (int h = 1; h <= 6; h++)
                        wave += Math.Sin(2d * Math.PI * cycle * h) / h;
                }

                double cutoff = 180d + 900d * Math.Exp(-t * 10d);
                lp += Alpha(cutoff) * (wave - lp);
                double sub = Math.Sin(2d * Math.PI * freq * 0.5d * t);
                double env = t < 0.006d ? t / 0.006d : 1d;
                double tone = Math.Tanh((lp * 0.9d + sub * 0.85d) * 1.3d);
                buffer[i] += tone * env * Release(t, duration, 0.02d) * velocity;
            }
        }

        public static void Acid(double[] buffer, int start, int midi, double duration, double velocity, double accent)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            double bp = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double slide = freq * Math.Pow(2d, accent * 0.18d * Math.Exp(-t * 14d));
                double cycle = slide * t;
                double saw = 0d;
                for (int h = 1; h <= 7; h++)
                    saw += Math.Sin(2d * Math.PI * cycle * h) / h;
                double cutoff = 220d + (900d + accent * 1400d) * Math.Exp(-t * (6d + accent * 4d));
                double a = Alpha(cutoff);
                lp += a * (saw - lp);
                bp += a * (lp - bp);
                double env = t < 0.004d ? t / 0.004d : 1d;
                buffer[i] += Math.Tanh(bp * 2.4d) * env * Release(t, duration, 0.018d) * velocity;
            }
        }

        public static void Hat(double[] buffer, int start, double velocity, bool open, int salt)
        {
            double duration = open ? 0.14d : 0.028d;
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            double previous = 0d;
            double band = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double noise = Hash(i + salt);
                double high = noise - previous;
                previous = noise;
                band += Alpha(open ? 3200d : 6200d) * (high - band);
                double air = high - band;
                double env = open ? Math.Exp(-t * 16d) : Math.Exp(-t * 90d);
                if (t < 0.0008d)
                    env *= t / 0.0008d;
                buffer[i] += Math.Tanh(air * 1.8d) * env * Release(t, duration, open ? 0.016d : 0.004d) * velocity;
            }
        }

        public static void Shaker(double[] buffer, int start, double velocity, int salt)
        {
            const double duration = 0.045d;
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double noise = Hash(i * 3 + salt);
                double high = noise - previous;
                previous = noise;
                double env = Math.Exp(-t * 48d);
                buffer[i] += high * env * Release(t, duration, 0.006d) * velocity * 0.85d;
            }
        }

        public static void Clap(double[] buffer, int start, double velocity, int salt)
        {
            const double duration = 0.12d;
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double noise = Hash(i + salt);
                double high = noise - previous;
                previous = noise;
                double bursts = Burst(t, 0d, 0.006d) + 0.72d * Burst(t, 0.009d, 0.007d) + 0.4d * Burst(t, 0.02d, 0.028d);
                double tone = Math.Sin(2d * Math.PI * 240d * t) * Math.Exp(-t * 40d);
                buffer[i] += Math.Tanh((high * bursts + tone * 0.28d) * 1.6d) * Release(t, duration, 0.01d) * velocity;
            }
        }

        public static void Rim(double[] buffer, int start, double velocity)
        {
            const double duration = 0.04d;
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double tone = Math.Sin(2d * Math.PI * 640d * t) * Math.Exp(-t * 80d);
                double tick = Math.Sin(2d * Math.PI * 1850d * t) * Math.Exp(-t * 220d);
                buffer[i] += (tone * 0.65d + tick * 0.35d) * Release(t, duration, 0.004d) * velocity;
            }
        }

        public static void Tom(double[] buffer, int start, double hz, double velocity)
        {
            const double duration = 0.18d;
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            double phase = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double freq = hz * (1d + 0.35d * Math.Exp(-t * 30d));
                phase += 2d * Math.PI * freq / SampleRate;
                buffer[i] += Math.Sin(phase) * Math.Exp(-t * 10d) * Release(t, duration, 0.016d) * velocity;
            }
        }

        public static void Pluck(double[] buffer, int start, int midi, double duration, double velocity, double brightness)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            double phase = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                phase += 2d * Math.PI * freq / SampleRate;
                double wave = Math.Sin(phase) + 0.55d * Math.Sin(phase * 2d) + 0.22d * Math.Sin(phase * 3d);
                double cutoff = 500d + (1600d + brightness * 2800d) * Math.Exp(-t * 8d);
                lp += Alpha(cutoff) * (wave - lp);
                double env = t < 0.004d ? t / 0.004d : Math.Exp(-t * 5.5d);
                buffer[i] += lp * env * Release(t, duration, 0.012d) * velocity;
            }
        }

        public static void Square(double[] buffer, int start, int midi, double duration, double velocity, double pwm)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            double width = 0.35d + 0.3d * pwm;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double phase = freq * t;
                phase -= Math.Floor(phase);
                double wave = phase < width ? 0.7d : -0.7d;
                wave += 0.2d * Math.Sin(2d * Math.PI * freq * t);
                lp += Alpha(1400d + 1800d * Math.Exp(-t * 6d)) * (wave - lp);
                double env = t < 0.005d ? t / 0.005d : 1d;
                buffer[i] += lp * env * Release(t, duration, 0.016d) * velocity;
            }
        }

        public static void Stab(double[] buffer, int start, int midi, double velocity, int flavor)
        {
            const double duration = 0.2d;
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            double detune = freq * Math.Pow(2d, flavor == 1 ? 0.012d : 0.006d);
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double wave = 0d;
                for (int h = 1; h <= 5; h++)
                    wave += (Math.Sin(2d * Math.PI * freq * h * t) + 0.6d * Math.Sin(2d * Math.PI * detune * h * t)) / h;
                double cutoff = flavor == 2 ? 400d + 700d * Math.Exp(-t * 12d) : 700d + 2200d * Math.Exp(-t * 9d);
                lp += Alpha(cutoff) * (wave - lp);
                double env = t < 0.003d ? t / 0.003d : Math.Exp(-t * 9d);
                buffer[i] += Math.Tanh(lp * 0.8d) * env * Release(t, duration, 0.014d) * velocity * 0.55d;
            }
        }

        public static void ChordStab(double[] buffer, int start, Chord chord, int add, double velocity, int flavor)
        {
            Stab(buffer, start, chord.Root + add, velocity, flavor);
            Stab(buffer, start, chord.Third + add, velocity * 0.82d, flavor);
            Stab(buffer, start, chord.Fifth + add, velocity * 0.74d, flavor);
        }

        public static void Pad(double[] buffer, int start, int midi, double duration, double velocity, int flavor)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            double freqB = freq * Math.Pow(2d, flavor == 1 ? 0.012d : 0.006d);
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double vib = 1d + 0.003d * Math.Sin(2d * Math.PI * 5d * t);
                double a = Math.Sin(2d * Math.PI * freq * vib * t);
                double b = Math.Sin(2d * Math.PI * freqB * t);
                double wave = a + 0.7d * b;
                if (flavor == 1)
                    wave += 0.25d * Math.Sin(2d * Math.PI * freq * 2d * t);
                double cutoff = flavor == 2 ? 380d : 900d;
                lp += Alpha(cutoff) * (wave - lp);
                double tone = flavor == 2 ? lp : wave * 0.65d + lp * 0.35d;
                double attack = flavor == 2 ? 0.12d : 0.04d;
                double env = t < attack ? t / attack : 1d;
                buffer[i] += tone * env * Release(t, duration, 0.18d) * velocity * 0.34d;
            }
        }

        public static void Bell(double[] buffer, int start, int midi, double velocity, double ratio)
        {
            const double duration = 0.55d;
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double carrier = Math.Sin(2d * Math.PI * freq * t + Math.Sin(2d * Math.PI * freq * ratio * t) * Math.Exp(-t * 4d));
                double shimmer = 0.35d * Math.Sin(2d * Math.PI * freq * (ratio + 0.5d) * t) * Math.Exp(-t * 6d);
                double env = t < 0.004d ? t / 0.004d : Math.Exp(-t * 3.2d);
                buffer[i] += (carrier + shimmer) * env * Release(t, duration, 0.04d) * velocity * 0.45d;
            }
        }

        public static void Choir(double[] buffer, int start, int midi, double duration, double velocity)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double vib = 1d + 0.006d * Math.Sin(2d * Math.PI * 4.6d * t);
                double fundamental = Math.Sin(2d * Math.PI * freq * vib * t);
                double formant = 0.45d * Math.Sin(2d * Math.PI * freq * 2d * vib * t);
                double air = 0.18d * Math.Sin(2d * Math.PI * freq * 3.02d * t);
                double drift = 0.55d * Math.Sin(2d * Math.PI * freq * Math.Pow(2d, 0.01d) * t);
                double attack = t < 0.05d ? t / 0.05d : 1d;
                buffer[i] += (fundamental + formant + air + drift) * attack * Release(t, duration, 0.16d) * velocity * 0.22d;
            }
        }

        public static void Fm(double[] buffer, int start, int midi, double duration, double velocity, double ratio, double index)
        {
            if (!Fits(start, duration))
                return;
            double freq = Hz(midi);
            int count = (int)(duration * SampleRate);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double mod = Math.Sin(2d * Math.PI * freq * ratio * t) * index * Math.Exp(-t * 3.5d);
                double wave = Math.Sin(2d * Math.PI * freq * t + mod);
                double env = t < 0.006d ? t / 0.006d : 1d;
                buffer[i] += wave * env * Release(t, duration, 0.02d) * velocity * 0.55d;
            }
        }

        public static void NoiseWash(double[] buffer, int start, double duration, double velocity, int salt, double cutoff)
        {
            if (!Fits(start, duration))
                return;
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                double t = n / (double)SampleRate;
                double noise = Hash(n + salt * 13);
                double high = noise - previous;
                previous = noise;
                lp += Alpha(cutoff) * (high - lp);
                double attack = t < 0.08d ? t / 0.08d : 1d;
                buffer[i] += lp * attack * Release(t, duration, 0.12d) * velocity;
            }
        }

        public static void Skank(double[] buffer, int start, Chord chord, double velocity)
        {
            ChordStab(buffer, start, chord, 12, velocity, 2);
        }

        private static double Burst(double t, double at, double width)
        {
            double local = t - at;
            if (local < 0d || local > width)
                return 0d;
            return Math.Exp(-local * 32d);
        }

        private static double Release(double t, double duration, double seconds)
        {
            double remain = duration - t;
            if (remain >= seconds)
                return 1d;
            if (remain <= 0d)
                return 0d;
            return remain / seconds;
        }

        private static double Alpha(double cutoffHz)
        {
            double cutoff = Math.Max(40d, Math.Min(14000d, cutoffHz));
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / SampleRate;
            return dt / (rc + dt);
        }
    }
}
