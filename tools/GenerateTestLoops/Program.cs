using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GenerateTestLoops
{
    /// <summary>
    /// Four 8-bar loops at 120 BPM in A minor: Neon Pulse, Bass Reactor,
    /// Afterhours and City Static. Bass patterns leave room for each other.
    /// Every note ends before the loop point; the last 12 ms fade to zero.
    /// </summary>
    internal static class Program
    {
        private const int SampleRate = 48000;
        private const int Length = SampleRate * 16;
        private const int Sixteenth = SampleRate / 8;
        private const int Eighth = SampleRate / 4;
        private const int Bars = 8;
        private const int StepsPerBar = 16;

        private static int _wroteCount;
        private static double _rawPeakMin = double.MaxValue;
        private static double _rawPeakMax;
        private static double _scaledPeakMin = double.MaxValue;
        private static double _scaledPeakMax;
        private static double _tailMax;

        private static int Main(string[] args)
        {
            string folder = args.Length > 0
                ? args[0]
                : Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "DJ", "Assets", "_Game", "Audio", "Resources", "TestLoops"));
            Directory.CreateDirectory(folder);

            var chords = new[]
            {
                Chord.AMinor, Chord.AMinor, Chord.FMajor, Chord.GMajor,
                Chord.AMinor, Chord.FMajor, Chord.GMajor, Chord.AMinor
            };

            double[] neonLow = new double[Length];
            double[] neonPerc = new double[Length];
            double[] neonMusic = new double[Length];
            double[] afterLow = new double[Length];
            double[] afterPerc = new double[Length];
            double[] afterMusic = new double[Length];
            double[] reactorLow = new double[Length];
            double[] reactorPerc = new double[Length];
            double[] reactorMusic = new double[Length];
            double[] cityLow = new double[Length];
            double[] cityPerc = new double[Length];
            double[] cityMusic = new double[Length];
            TrackDispatch.Render("neon_pulse", neonLow, neonPerc, neonMusic);
            TrackDispatch.Render("afterhours", afterLow, afterPerc, afterMusic);
            TrackDispatch.Render("bass_reactor", reactorLow, reactorPerc, reactorMusic);
            TrackDispatch.Render("city_static", cityLow, cityPerc, cityMusic);

            Write(folder, "neon_pulse_low", neonLow);
            Write(folder, "neon_pulse_perc", neonPerc);
            Write(folder, "neon_pulse_music", neonMusic);
            Write(folder, "bass_reactor_low", reactorLow);
            Write(folder, "bass_reactor_perc", reactorPerc);
            Write(folder, "bass_reactor_music", reactorMusic);
            Write(folder, "afterhours_low", afterLow);
            Write(folder, "afterhours_perc", afterPerc);
            Write(folder, "afterhours_music", afterMusic);
            Write(folder, "city_static_low", cityLow);
            Write(folder, "city_static_perc", cityPerc);
            Write(folder, "city_static_music", cityMusic);

            double[] neon = Mix(neonLow, neonPerc, neonMusic, "neon_pulse_low", "neon_pulse_perc", "neon_pulse_music");
            double[] reactor = Mix(reactorLow, reactorPerc, reactorMusic, "bass_reactor_low", "bass_reactor_perc", "bass_reactor_music");
            double[] after = Mix(afterLow, afterPerc, afterMusic, "afterhours_low", "afterhours_perc", "afterhours_music");
            double[] city = Mix(cityLow, cityPerc, cityMusic, "city_static_low", "city_static_perc", "city_static_music");
            Console.WriteLine("pair neon+after " + PairPeak(neon, after).ToString("0.00"));
            Console.WriteLine("pair neon+reactor " + PairPeak(neon, reactor).ToString("0.00"));
            Console.WriteLine("pair neon+city " + PairPeak(neon, city).ToString("0.00"));
            Console.WriteLine("pair after+reactor " + PairPeak(after, reactor).ToString("0.00"));
            Console.WriteLine("pair after+city " + PairPeak(after, city).ToString("0.00"));
            Console.WriteLine("pair reactor+city " + PairPeak(reactor, city).ToString("0.00"));
            DeleteRetired(folder);
            WriteCampaign(folder, chords);
            Console.WriteLine(folder);
            return 0;
        }

        private static void DeleteRetired(string folder)
        {
            string[] retired = { "a_low", "a_perc", "a_music", "b_low", "b_perc", "b_music" };
            for (int i = 0; i < retired.Length; i++)
            {
                File.Delete(Path.Combine(folder, retired[i] + ".wav"));
                File.Delete(Path.Combine(folder, retired[i] + ".wav.meta"));
            }
        }

        private static double[] Mix(double[] low, double[] perc, double[] music, string lowName, string percName, string musicName)
        {
            double lowScale = Scale(low, lowName);
            double percScale = Scale(perc, percName);
            double musicScale = Scale(music, musicName);
            var mixed = new double[Length];
            for (int i = 0; i < Length; i++)
                mixed[i] = low[i] * lowScale + perc[i] * percScale + music[i] * musicScale;
            return mixed;
        }

        private static double PairPeak(double[] a, double[] b)
        {
            const double gain = 0.7071d;
            double peak = 0d;
            for (int i = 0; i < Length; i++)
                peak = Math.Max(peak, Math.Abs((a[i] + b[i]) * gain));
            return peak;
        }

        private static double[] ReactorLow(Chord[] chords)
        {
            var buffer = new double[Length];
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int beat = 0; beat < 4; beat++)
                    AddKick(buffer, At(bar, beat * 4, false), 46d, 1d);

                Chord chord = chords[bar];
                AddBass(buffer, At(bar, 0, false), chord.Root, 0.62d, true);
                AddBass(buffer, At(bar, 7, true), chord.Fifth, 0.5d, false);
                AddBass(buffer, At(bar, 11, true), chord.Third, 0.46d, false);
                if (bar >= 6)
                    AddBass(buffer, At(bar, 4, false), chord.Root + 12, 0.4d, true);
            }

            return buffer;
        }

        private static double[] ReactorPerc()
        {
            var buffer = new double[Length];
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int step = 0; step < StepsPerBar; step++)
                    AddHat(buffer, At(bar, step, false), step % 2 == 0 ? 0.42d : 0.78d, false, 8000 + bar * 16 + step);

                AddClap(buffer, At(bar, 4, false), 0.95d, 9000 + bar);
                AddClap(buffer, At(bar, 12, false), 0.88d, 9100 + bar);
                if (bar == Bars - 1)
                {
                    AddHat(buffer, At(bar, 10, false), 0.7d, true, 9200);
                    AddHat(buffer, At(bar, 14, false), 0.66d, true, 9201);
                }
            }

            return buffer;
        }

        private static double[] ReactorMusic(Chord[] chords)
        {
            var buffer = new double[Length];
            for (int bar = 0; bar < Bars; bar++)
            {
                Chord chord = chords[bar];
                double lift = bar >= 4 ? 0.16d : 0d;
                AddPluck(buffer, AtEighth(bar, 2), Hz(chord.Root + 12), 0.2d, 0.55d + lift, 0.2d);
                AddPluck(buffer, AtEighth(bar, 6), Hz(chord.Fifth + 12), 0.16d, 0.42d + lift, 0.4d);
                if (bar >= 6)
                    AddPluck(buffer, AtEighth(bar, 0), Hz(chord.Third + 24), 0.14d, 0.36d, 0.8d);
            }

            return buffer;
        }

        private static double[] CityLow(Chord[] chords)
        {
            var buffer = new double[Length];
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int beat = 0; beat < 4; beat++)
                    AddKick(buffer, At(bar, beat * 4, false), 56d, 0.94d);

                Chord chord = chords[bar];
                AddBass(buffer, At(bar, 5, true), chord.Root, 0.48d, false);
                AddBass(buffer, At(bar, 9, true), chord.Fifth, 0.44d, false);
                if (bar < Bars - 1)
                    AddBass(buffer, At(bar, 14, true), chord.Third, 0.36d, false);
            }

            return buffer;
        }

        private static double[] CityPerc()
        {
            var buffer = new double[Length];
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int step = 2; step < StepsPerBar; step += 4)
                    AddHat(buffer, At(bar, step, false), 0.7d, false, 11000 + bar * 16 + step);

                AddClap(buffer, At(bar, 4, false), 0.72d, 12000 + bar);
                AddHat(buffer, At(bar, 7, false), 0.28d, false, 13000 + bar);
                AddHat(buffer, At(bar, 15, false), 0.24d, false, 14000 + bar);
            }

            return buffer;
        }

        private static readonly int[] CityRiff =
        {
            64, 0, 67, 0, 72, 0, 67, 0,
            69, 0, 67, 0, 64, 0, 60, 0,
            64, 0, 67, 0, 72, 0, 76, 0,
            74, 0, 72, 0, 67, 0, 64, 0,
            64, 0, 67, 0, 72, 0, 67, 0,
            69, 0, 65, 0, 64, 0, 60, 0,
            76, 0, 72, 0, 69, 0, 67, 0,
            64, 0, 60, 0, 57, 0, 64, 0
        };

        private static double[] CityMusic()
        {
            var buffer = new double[Length];
            for (int i = 0; i < CityRiff.Length; i++)
            {
                int midi = CityRiff[i];
                if (midi == 0)
                    continue;
                int bar = i / 8;
                int eighth = i % 8;
                double velocity = bar == 6 ? 0.78d : 0.62d;
                AddLead(buffer, AtEighth(bar, eighth), midi, 0.18d, velocity);
            }

            return buffer;
        }

        private static double Scale(double[] raw, string name)
        {
            double peak = 0d;
            for (int i = 0; i < raw.Length; i++)
                peak = Math.Max(peak, Math.Abs(raw[i]));
            return peak <= 0d ? 0d : Target(name) / peak;
        }

        private static double[] Low(Chord[] chords, bool deckA)
        {
            var buffer = new double[Length];
            double body = deckA ? 52d : 44d;
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int beat = 0; beat < 4; beat++)
                    AddKick(buffer, At(bar, beat * 4, false), body, deckA ? 1d : 0.92d);
            }

            for (int bar = 0; bar < Bars; bar++)
            {
                Chord chord = chords[bar];
                if (deckA)
                {
                    AddBass(buffer, At(bar, 0, true), chord.Root, 0.55d, true);
                    AddBass(buffer, At(bar, 3, true), chord.Root, 0.42d, false);
                    AddBass(buffer, At(bar, 6, true), chord.Third, 0.40d, false);
                    AddBass(buffer, At(bar, 8, true), chord.Fifth, 0.48d, true);
                    AddBass(buffer, At(bar, 13, true), chord.Below, 0.34d, false);
                }
                else
                {
                    AddBass(buffer, At(bar, 2, true), chord.Fifth, 0.40d, false);
                    AddBass(buffer, At(bar, 4, true), chord.Root, 0.46d, true);
                    AddBass(buffer, At(bar, 10, true), chord.Third, 0.38d, false);
                    AddBass(buffer, At(bar, 12, true), chord.Fifth, 0.44d, true);
                }
            }

            return buffer;
        }

        private static double[] Percussion(bool deckA)
        {
            var buffer = new double[Length];
            for (int bar = 0; bar < Bars; bar++)
            {
                if (deckA)
                {
                    for (int step = 0; step < StepsPerBar; step += 2)
                    {
                        bool offbeat = step == 2 || step == 6 || step == 10 || step == 14;
                        AddHat(buffer, At(bar, step, false), offbeat ? 0.85d : 0.38d, false, 1000 + step);
                    }

                    AddClap(buffer, At(bar, 4, false), 0.9d, 2000 + bar);
                    AddClap(buffer, At(bar, 12, false), 0.82d, 3000 + bar);
                    int openStep = bar == Bars - 1 ? 10 : 14;
                    if (bar % 2 == 1 || bar == Bars - 1)
                        AddHat(buffer, At(bar, openStep, false), 0.7d, true, 4000 + bar);
                }
                else
                {
                    for (int step = 1; step < StepsPerBar; step += 4)
                        AddHat(buffer, At(bar, step, true), 0.55d, false, 5000 + bar * 16 + step);
                }
            }

            return buffer;
        }

        private static double[] Music(Chord[] chords, bool deckA)
        {
            var buffer = new double[Length];
            if (deckA)
            {
                for (int i = 0; i < Hook.Length; i++)
                {
                    int midi = Hook[i];
                    if (midi == 0)
                        continue;
                    int bar = i / 8;
                    int eighth = i % 8;
                    bool cadences = eighth == 6;
                    double velocity = 0.72d;
                    if (bar == 3 || bar == 6)
                        velocity = 0.9d;
                    if (eighth == 3)
                        velocity *= 0.82d;
                    AddLead(buffer, AtEighth(bar, eighth), midi, cadences ? 0.42d : 0.22d, velocity);
                }
            }
            else
            {
                for (int bar = 0; bar < Bars; bar++)
                {
                    Chord chord = chords[bar];
                    AddPad(buffer, At(bar, 0, false), chord.Root + 12, 0.16d);
                    AddPad(buffer, At(bar, 0, false), chord.Fifth, 0.12d);
                }

                AddCounter(buffer, AtEighth(1, 0), 72, 1.35d, 0.62d);
                AddCounter(buffer, AtEighth(3, 0), 67, 1.35d, 0.58d);
                AddCounter(buffer, AtEighth(5, 0), 72, 1.35d, 0.64d);
                AddCounter(buffer, AtEighth(7, 0), 69, 1.15d, 0.6d);
            }

            return buffer;
        }

        private static readonly int[] Hook =
        {
            64, 0, 60, 64, 0, 0, 62, 0,
            64, 0, 67, 69, 0, 0, 67, 0,
            69, 0, 72, 69, 0, 0, 67, 0,
            71, 0, 74, 71, 0, 0, 67, 0,
            64, 0, 60, 64, 0, 0, 69, 0,
            72, 0, 69, 65, 0, 0, 69, 0,
            74, 0, 71, 67, 0, 0, 62, 0,
            64, 0, 60, 64, 0, 0, 57, 0
        };

        private static int At(int bar, int stepInBar, bool swing)
        {
            int sample = (bar * StepsPerBar + stepInBar) * Sixteenth;
            if (swing && (stepInBar % 2) == 1)
                sample += (int)(0.016d * SampleRate);
            return sample;
        }

        private static int AtEighth(int bar, int eighth)
        {
            return (bar * 8 + eighth) * Eighth;
        }

        private static void AddKick(double[] buffer, int start, double bodyHz, double velocity)
        {
            int count = (int)(0.22d * SampleRate);
            double phase = 0d;
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                double hz = bodyHz + 150d * Math.Exp(-t * 32d);
                phase += 2d * Math.PI * hz / SampleRate;
                double body = Math.Sin(phase) * Math.Exp(-t * 9.5d);
                double noise = Hash(i + 17) - previous;
                previous = Hash(i + 17);
                double click = noise * Math.Exp(-t * 220d);
                if (t < 0.0012d)
                    click *= t / 0.0012d;
                buffer[i] += (body + click * 0.45d) * velocity;
            }
        }

        private static void AddBass(double[] buffer, int start, int midi, double velocity, bool besideKick)
        {
            double freq = Hz(midi);
            int count = (int)(0.18d * SampleRate);
            double lp = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                double slide = freq * Math.Pow(2d, Math.Exp(-t * 70d) / 12d);
                double cycle = slide * t;
                double saw = 0d;
                for (int h = 1; h <= 5; h++)
                    saw += Math.Sin(2d * Math.PI * cycle * h) / h;
                double cutoff = 240d + 500d * Math.Exp(-t * 14d);
                lp += Alpha(cutoff) * (saw * 0.55d - lp);
                double sub = Math.Sin(2d * Math.PI * freq * 0.5d * t);
                double env = Math.Exp(-t * 7.5d);
                if (t < 0.01d)
                    env *= t / 0.01d;
                if (besideKick && t < 0.03d)
                    env *= t / 0.03d;
                double tone = Math.Tanh((lp * 0.85d + sub * 0.7d) * 1.35d);
                buffer[i] += tone * env * Release(t, 0.18d, 0.02d) * velocity;
            }
        }

        private static void AddLead(double[] buffer, int start, int midi, double duration, double velocity)
        {
            AddPluck(buffer, start, Hz(midi), duration, velocity, 1d);
            AddPluck(buffer, start, Hz(midi) * Cents(8d), duration, velocity * 0.28d, 0.15d);
            AddPluck(buffer, start, Hz(midi) * 0.5d, duration, velocity * 0.16d, 0.4d);
        }

        private static void AddPluck(double[] buffer, int start, double freq, double duration, double velocity, double salt)
        {
            int count = (int)(duration * SampleRate);
            double lp = 0d;
            double phase = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                phase += 2d * Math.PI * freq / SampleRate;
                double wave = Math.Sin(phase) + 0.45d * Math.Sin(phase * 2d) + 0.18d * Math.Sin(phase * 3d);
                double cutoff = 700d + 2400d * Math.Exp(-t * 7d);
                lp += Alpha(cutoff) * (wave - lp);
                double env = Math.Exp(-t * 3.6d);
                if (t < 0.006d)
                    env *= t / 0.006d;
                buffer[i] += lp * env * Release(t, duration, 0.012d) * velocity * (0.55d + 0.02d * salt);
            }
        }

        private static void AddCounter(double[] buffer, int start, int midi, double duration, double velocity)
        {
            int count = (int)(duration * SampleRate);
            double phase = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                double wobble = 1d + 0.004d * Math.Sin(2d * Math.PI * 4.5d * t);
                phase += 2d * Math.PI * Hz(midi) * wobble / SampleRate;
                double wave = Math.Sin(phase) + 0.22d * Math.Sin(phase * 2d);
                double attack = t < 0.04d ? t / 0.04d : 1d;
                buffer[i] += wave * attack * Release(t, duration, 0.14d) * velocity * 0.55d;
            }
        }

        private static void AddPad(double[] buffer, int start, int midi, double velocity)
        {
            const double duration = 1.72d;
            int count = (int)(duration * SampleRate);
            double phase = 0d;
            double detune = phase;
            double freq = Hz(midi);
            double freqB = freq * Cents(7d);
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                phase += 2d * Math.PI * freq / SampleRate;
                detune += 2d * Math.PI * freqB / SampleRate;
                double wave = Math.Sin(phase) + 0.65d * Math.Sin(detune);
                double attack = t < 0.06d ? t / 0.06d : 1d;
                buffer[i] += wave * attack * Release(t, duration, 0.28d) * velocity * 0.35d;
            }
        }

        private static void AddHat(double[] buffer, int start, double velocity, bool open, int salt)
        {
            double duration = open ? 0.18d : 0.035d;
            int count = (int)(duration * SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                double noise = Hash(i + salt);
                double high = noise - previous;
                previous = noise;
                double env = open ? Math.Exp(-t * 12d) : Math.Exp(-t * 70d);
                if (t < 0.001d)
                    env *= t / 0.001d;
                buffer[i] += Math.Tanh(high * 1.7d) * env * Release(t, duration, open ? 0.02d : 0.004d) * velocity;
            }
        }

        private static void AddClap(double[] buffer, int start, double velocity, int salt)
        {
            const double duration = 0.16d;
            int count = (int)(duration * SampleRate);
            double previous = 0d;
            for (int n = 0; n < count; n++)
            {
                int i = start + n;
                if (i >= buffer.Length)
                    break;
                double t = n / (double)SampleRate;
                double noise = Hash(i + salt);
                double high = noise - previous;
                previous = noise;
                double bursts = Burst(t, 0d, 0.008d) + 0.75d * Burst(t, 0.011d, 0.008d) + 0.55d * Burst(t, 0.023d, 0.03d);
                double tone = Math.Sin(2d * Math.PI * 210d * t) * Math.Exp(-t * 45d);
                double hit = Math.Tanh((high * bursts + tone * 0.35d) * 1.5d);
                buffer[i] += hit * Release(t, duration, 0.012d) * velocity;
            }
        }

        private static double Burst(double t, double at, double width)
        {
            double local = t - at;
            if (local < 0d || local > width)
                return 0d;
            return Math.Exp(-local * 28d);
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
            double cutoff = Math.Max(40d, Math.Min(8000d, cutoffHz));
            double rc = 1d / (2d * Math.PI * cutoff);
            double dt = 1d / SampleRate;
            return dt / (rc + dt);
        }

        private static double Hz(int midi)
        {
            return 440d * Math.Pow(2d, (midi - 69) / 12d);
        }

        private static double Cents(double cents)
        {
            return Math.Pow(2d, cents / 1200d);
        }

        private struct Chord
        {
            public Chord(int root, int third, int fifth, int below)
            {
                Root = root;
                Third = third;
                Fifth = fifth;
                Below = below;
            }

            public int Root { get; }
            public int Third { get; }
            public int Fifth { get; }
            public int Below { get; }

            public static Chord AMinor => new Chord(45, 48, 52, 43);
            public static Chord FMajor => new Chord(41, 45, 48, 40);
            public static Chord GMajor => new Chord(43, 47, 50, 41);
        }

        private static bool Write(string folder, string name, double[] raw)
        {
            FadeTail(raw);
            string path = Path.Combine(folder, name + ".wav");
            var pcm = new short[Length];
            double peak = 0d;
            for (int i = 0; i < Length; i++)
                peak = Math.Max(peak, Math.Abs(raw[i]));
            if (peak <= 0d || peak > 32d || double.IsNaN(peak) || double.IsInfinity(peak))
                throw new InvalidOperationException(name + " raw peak is not usable: " + peak.ToString("0.000"));

            double scale = Target(name) / peak;
            double tail = 0d;
            double scaledPeak = 0d;
            int tailStart = Length - SampleRate / 80;
            for (int i = 0; i < Length; i++)
            {
                double value = Math.Max(-1d, Math.Min(1d, raw[i] * scale));
                scaledPeak = Math.Max(scaledPeak, Math.Abs(value));
                if (i >= tailStart)
                    tail = Math.Max(tail, Math.Abs(value));
                pcm[i] = (short)Math.Round(value * 32767d);
            }

            if (pcm[Length - 1] != 0)
                throw new InvalidOperationException(name + " does not fade to zero");
            if (tail > 0.02d)
                throw new InvalidOperationException(name + " tail is hot: " + tail.ToString("0.0000"));
            if (scaledPeak > 1d || scaledPeak < 0.05d)
                throw new InvalidOperationException(name + " scaled peak " + scaledPeak.ToString("0.000"));

            _wroteCount++;
            _rawPeakMin = Math.Min(_rawPeakMin, peak);
            _rawPeakMax = Math.Max(_rawPeakMax, peak);
            _scaledPeakMin = Math.Min(_scaledPeakMin, scaledPeak);
            _scaledPeakMax = Math.Max(_scaledPeakMax, scaledPeak);
            _tailMax = Math.Max(_tailMax, tail);

            double sum = 0d;
            for (int bar = 0; bar < Bars; bar++)
            {
                double energy = 0d;
                int from = bar * SampleRate * 2;
                for (int i = from; i < from + SampleRate * 2; i++)
                    energy += raw[i] * raw[i];
                double rms = Math.Sqrt(energy / (SampleRate * 2));
                sum += rms;
                if (name.EndsWith("music", StringComparison.Ordinal))
                    Console.Write(rms.ToString("0.000") + (bar == Bars - 1 ? "\n" : " "));
            }

            Console.WriteLine(
                name + " peak " + peak.ToString("0.00") +
                " mean-bar " + (sum / Bars).ToString("0.000") +
                " tail " + tail.ToString("0.0000"));

            using (var stream = File.Create(path))
            using (var writer = new BinaryWriter(stream))
            {
                int dataBytes = pcm.Length * 2;
                writer.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
                writer.Write(36 + dataBytes);
                writer.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
                writer.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)1);
                writer.Write(SampleRate);
                writer.Write(SampleRate * 2);
                writer.Write((short)2);
                writer.Write((short)16);
                writer.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
                writer.Write(dataBytes);
                for (int i = 0; i < pcm.Length; i++)
                    writer.Write(pcm[i]);
            }

            return true;
        }

        private static int TailSamples()
        {
            return (int)(0.012d * SampleRate);
        }

        private static bool Fits(int start, double duration)
        {
            if (start < 0)
                return false;
            int end = start + (int)(duration * SampleRate);
            return end <= Length - TailSamples();
        }

        private static void FadeTail(double[] buffer)
        {
            int count = TailSamples();
            for (int n = 0; n < count; n++)
            {
                double gain = 1d - (n + 1) / (double)count;
                buffer[buffer.Length - count + n] *= gain;
            }
        }

        private static double Target(string name)
        {
            if (name.EndsWith("_low", StringComparison.Ordinal))
                return 0.50d;
            if (name.EndsWith("_perc", StringComparison.Ordinal))
            {
                if (name.StartsWith("neon", StringComparison.Ordinal))
                    return 0.18d;
                if (name.StartsWith("after", StringComparison.Ordinal))
                    return 0.12d;
                if (name.StartsWith("bass", StringComparison.Ordinal))
                    return 0.16d;
                return 0.14d;
            }

            if (name.StartsWith("neon", StringComparison.Ordinal))
                return 0.40d;
            if (name.StartsWith("after", StringComparison.Ordinal))
                return 0.28d;
            if (name.StartsWith("bass", StringComparison.Ordinal))
                return 0.30d;
            return 0.28d;
        }

        // Synthetic test loops for campaign playback, not licensed music and not a release OST.
        private static readonly string[] CampaignStems =
        {
            "dusk_pad", "velvet_room", "strobe_grid", "yellow_siren",
            "pier_salt", "tide_click", "throne_hum", "crown_loop",
            "prism_pane", "choir_dust", "ledger_tick", "favor_bass",
            "sky_cable", "wind_hat", "festival_grid", "city_choir",
            "lounge_key", "district_hat", "siren_sub", "pier_roll",
            "chrome_wake", "nave_pad", "market_click", "roof_hum",
            "exchange_acid", "relay_dub", "velvet_hush", "hardline_yellow",
            "salt_chrome", "prism_choir", "night_ledger", "skywire",
            "static_crown", "festival_anthem"
        };

        private static readonly int[] LeadIntervals = { 0, 2, 3, 5, 7, 8, 10, 12 };

        private static readonly int[][] KickPatterns =
        {
            new[] { 0, 8 },
            new[] { 0, 10 },
            new[] { 0, 8, 12 },
            new[] { 0, 4, 8, 12 },
            new[] { 0, 3, 8, 11 },
            new[] { 0, 4, 8, 10, 12 },
            new[] { 0, 2, 4, 8, 10, 12 },
            new[] { 0, 3, 6, 8, 11 }
        };

        private static readonly int[][] BassSteps =
        {
            new[] { 0, 7, 12 },
            new[] { 0, 8, 11 },
            new[] { 3, 8, 12 },
            new[] { 0, 6, 10 }
        };

        private static readonly Regex GuidRegex = new Regex(
            "guid:\\s*([0-9a-fA-F]{32})",
            RegexOptions.Compiled);

        private struct CampaignRecipe
        {
            public string Stem;
            public int Band;
            public int Kick;
            public int Hats;
            public int Octave;
            public int Interval;
            public int BassShape;
            public int Salt;
        }

        private static void WriteCampaign(string folder, Chord[] chords)
        {
            var recipes = new List<CampaignRecipe>(CampaignStems.Length);
            var seen = new HashSet<string>();
            var bandCounts = new int[3];
            for (int i = 0; i < CampaignStems.Length; i++)
            {
                string stem = CampaignStems[i];
                if (stem == "neon_pulse" || stem == "bass_reactor" || stem == "afterhours" || stem == "city_static")
                    throw new InvalidOperationException("MVP stem is protected: " + stem);

                int band = BandOf(stem);
                int index = bandCounts[band];
                bandCounts[band]++;
                CampaignRecipe recipe = MakeRecipe(stem, band, index);
                string key = recipe.Kick + ":" + recipe.Hats + ":" + recipe.Octave + ":" + recipe.Interval;
                if (!seen.Add(key))
                    throw new InvalidOperationException("recipe collision " + stem);
                recipes.Add(recipe);
            }

            if (recipes.Count != 34 || bandCounts[0] != 10 || bandCounts[1] != 19 || bandCounts[2] != 5)
                throw new InvalidOperationException("campaign recipe table drifted");

            string template = LoadMetaTemplate(folder);
            HashSet<string> guids = LoadGuids(folder);
            int written = 0;
            int metas = 0;
            for (int i = 0; i < recipes.Count; i++)
            {
                CampaignRecipe recipe = recipes[i];
                Console.WriteLine(recipe.Stem + " " + BandName(recipe.Band));

                var low = new double[Length];
                var perc = new double[Length];
                var music = new double[Length];
                TrackDispatch.Render(recipe.Stem, low, perc, music);
                EmitStem(folder, recipe.Stem + "_low", low, template, guids, ref written, ref metas);
                EmitStem(folder, recipe.Stem + "_perc", perc, template, guids, ref written, ref metas);
                EmitStem(folder, recipe.Stem + "_music", music, template, guids, ref written, ref metas);
            }

            for (int i = 0; i < recipes.Count; i++)
            {
                if (!CampaignComplete(folder, recipes[i].Stem))
                    throw new InvalidOperationException("missing stems for " + recipes[i].Stem);
            }

            string[] club =
            {
                "warehouse_line", "smoke_column", "press_line", "torque_drum",
                "voltage_alley", "mutabor_pulse", "foundry_drop", "afterglow_key",
                "signal_carrier", "signal_breach", "carbon_copy", "cold_cathode",
                "night_tram", "freight_lift", "return_duct", "ratchet_works",
                "phase_fault", "mirror_shunt", "amber_bloom", "void_static",
                "glass_rain", "redline_siren"
            };
            for (int i = 0; i < club.Length; i++)
            {
                Console.WriteLine(club[i] + " club");
                var low = new double[Length];
                var perc = new double[Length];
                var music = new double[Length];
                TrackDispatch.Render(club[i], low, perc, music);
                EmitStem(folder, club[i] + "_low", low, template, guids, ref written, ref metas);
                EmitStem(folder, club[i] + "_perc", perc, template, guids, ref written, ref metas);
                EmitStem(folder, club[i] + "_music", music, template, guids, ref written, ref metas);
                if (!CampaignComplete(folder, club[i]))
                    throw new InvalidOperationException("missing stems for " + club[i]);
            }

            VerifyHeader(folder, "dusk_pad_low.wav", 14000);
            VerifyHeader(folder, "strobe_grid_low.wav", 14000);
            VerifyHeader(folder, "festival_anthem_low.wav", 14000);
            VerifyMeta(folder, "dusk_pad_low");
            VerifyUniqueGuids(folder);

            int wavs = Directory.GetFiles(folder, "*.wav").Length;
            if (written == 0)
            {
                Console.WriteLine("campaign wavs written 0");
            }
            else
            {
                Console.WriteLine(
                    "campaign wavs written " + written +
                    " metas " + metas +
                    " raw-peak " + _rawPeakMin.ToString("0.00", CultureInfo.InvariantCulture) +
                    ".." + _rawPeakMax.ToString("0.00", CultureInfo.InvariantCulture) +
                    " scaled-peak " + _scaledPeakMin.ToString("0.00", CultureInfo.InvariantCulture) +
                    ".." + _scaledPeakMax.ToString("0.00", CultureInfo.InvariantCulture) +
                    " max-tail " + _tailMax.ToString("0.0000", CultureInfo.InvariantCulture));
            }

            Console.WriteLine("testloops wav count " + wavs);
        }

        private static CampaignRecipe MakeRecipe(string stem, int band, int index)
        {
            CampaignRecipe recipe = new CampaignRecipe();
            recipe.Stem = stem;
            recipe.Band = band;
            if (band == 0)
                recipe.Kick = index % 3;
            else if (band == 1)
                recipe.Kick = 3 + (index % 3);
            else
                recipe.Kick = 6 + (index % 2);
            recipe.Hats = (index * 2 + band) % 7;
            recipe.Octave = ((index + band) % 3) - 1;
            recipe.Interval = LeadIntervals[(index * 3 + band * 2 + 1) % LeadIntervals.Length];
            recipe.BassShape = (index + band) % BassSteps.Length;
            recipe.Salt = StemSalt(stem);
            return recipe;
        }

        private static int BandOf(string stem)
        {
            switch (stem)
            {
                case "dusk_pad":
                case "velvet_room":
                case "pier_salt":
                case "choir_dust":
                case "city_choir":
                case "lounge_key":
                case "nave_pad":
                case "roof_hum":
                case "velvet_hush":
                case "prism_choir":
                    return 0;
                case "throne_hum":
                case "crown_loop":
                case "festival_grid":
                case "static_crown":
                case "festival_anthem":
                    return 2;
                default:
                    return 1;
            }
        }

        private static string BandName(int band)
        {
            if (band == 0)
                return "soft";
            if (band == 2)
                return "boss";
            return "hard";
        }

        private static bool CampaignComplete(string folder, string stem)
        {
            return File.Exists(Path.Combine(folder, stem + "_low.wav"))
                && File.Exists(Path.Combine(folder, stem + "_perc.wav"))
                && File.Exists(Path.Combine(folder, stem + "_music.wav"));
        }

        private static void RenderLow(double[] buffer, Chord[] chords, CampaignRecipe recipe)
        {
            double body = recipe.Band == 0 ? 46d : recipe.Band == 2 ? 40d : 54d;
            body += (recipe.Kick % 3) * 3d;
            double kickVel = recipe.Band == 0 ? 0.8d : recipe.Band == 2 ? 1d : 0.92d;
            double bassVel = recipe.Band == 0 ? 0.58d : 0.5d;
            int[] kicks = KickPatterns[recipe.Kick];
            int[] steps = BassSteps[recipe.BassShape];
            for (int bar = 0; bar < Bars; bar++)
            {
                for (int k = 0; k < kicks.Length; k++)
                {
                    int at = At(bar, kicks[k], false);
                    if (Fits(at, 0.22d))
                        AddKick(buffer, at, body, kickVel);
                }

                Chord chord = chords[bar];
                for (int n = 0; n < steps.Length; n++)
                {
                    int step = steps[n];
                    int degree = n == 1 ? 2 : n == 2 ? 1 : 0;
                    int midi = ChordTone(chord, degree) + (12 * recipe.Octave);
                    if (midi < 24)
                        midi += 12;
                    if (midi > 72)
                        midi -= 12;
                    int at = At(bar, step, true);
                    if (Fits(at, 0.18d))
                        AddBass(buffer, at, midi, bassVel, IsKick(recipe.Kick, step));
                }
            }
        }

        private static void RenderPerc(double[] buffer, CampaignRecipe recipe)
        {
            double hatVel = recipe.Band == 0 ? 0.34d : recipe.Band == 2 ? 0.8d : 0.62d;
            double clap = recipe.Band == 0 ? 0.42d : recipe.Band == 2 ? 0.92d : 0.75d;
            for (int bar = 0; bar < Bars; bar++)
            {
                int backbeat = At(bar, 4, false);
                int backbeatLate = At(bar, 12, false);
                if (Fits(backbeat, 0.16d))
                    AddClap(buffer, backbeat, clap, recipe.Salt + bar);
                if (Fits(backbeatLate, 0.16d))
                    AddClap(buffer, backbeatLate, clap * 0.9d, recipe.Salt + 40 + bar);
                if (recipe.Band == 2 && (bar % 2) == 1 && bar < Bars - 1)
                {
                    int fill = At(bar, 14, false);
                    if (Fits(fill, 0.16d))
                        AddClap(buffer, fill, clap * 0.55d, recipe.Salt + 80 + bar);
                }

                for (int step = 0; step < StepsPerBar; step++)
                {
                    bool accent = recipe.Band > 0 && (bar % 2) == 1 && step == 14;
                    if (!accent && !HatHit(recipe.Hats, step))
                        continue;
                    bool open = accent;
                    double duration = open ? 0.18d : 0.035d;
                    int at = At(bar, step, false);
                    if (!Fits(at, duration))
                        continue;
                    double vel = open ? hatVel * 0.85d : step % 2 == 0 ? hatVel * 0.72d : hatVel;
                    AddHat(buffer, at, vel, open, recipe.Salt + bar * 32 + step);
                }
            }
        }

        private static void RenderMusic(double[] buffer, Chord[] chords, CampaignRecipe recipe)
        {
            double vel = recipe.Band == 0 ? 0.46d : recipe.Band == 2 ? 0.74d : 0.6d;
            for (int bar = 0; bar < Bars; bar++)
            {
                Chord chord = chords[bar];
                int root = SnapScale(chord.Root + 24);
                int leap = SnapScale(root + recipe.Interval);
                if (leap == root && recipe.Interval != 0)
                    leap = SnapScale(root + recipe.Interval + 1);
                int fifth = SnapScale(chord.Fifth + 24);
                int third = SnapScale(chord.Third + 24);
                PlaceLead(buffer, recipe.Band, AtEighth(bar, 0), root, vel);
                PlaceLead(buffer, recipe.Band, AtEighth(bar, 2), leap, vel * 0.92d);
                if (recipe.Band > 0)
                {
                    PlaceLead(buffer, recipe.Band, AtEighth(bar, 4), fifth, vel * 0.84d);
                    PlaceLead(buffer, recipe.Band, AtEighth(bar, 6), third, vel * 0.78d);
                }
                else if ((bar % 2) == 1)
                {
                    PlaceLead(buffer, recipe.Band, AtEighth(bar, 4), third, vel * 0.7d);
                }
            }
        }

        private static void PlaceLead(double[] buffer, int band, int start, int midi, double velocity)
        {
            const double duration = 0.16d;
            if (!Fits(start, duration))
                return;
            if (band == 0)
                AddPluck(buffer, start, Hz(midi), duration, velocity, 0.35d);
            else
                AddLead(buffer, start, midi, duration, velocity);
        }

        private static int ChordTone(Chord chord, int degree)
        {
            if (degree == 1)
                return chord.Third;
            if (degree == 2)
                return chord.Fifth;
            return chord.Root;
        }

        private static bool IsKick(int pattern, int step)
        {
            int[] steps = KickPatterns[pattern];
            for (int i = 0; i < steps.Length; i++)
            {
                if (steps[i] == step)
                    return true;
            }

            return false;
        }

        private static bool HatHit(int mode, int step)
        {
            switch (mode)
            {
                case 0:
                    return step == 2 || step == 10;
                case 1:
                    return step == 2 || step == 6 || step == 10 || step == 14;
                case 2:
                    return step % 4 == 0;
                case 3:
                    return step % 2 == 0;
                case 4:
                    return true;
                case 5:
                    return step % 3 == 0;
                default:
                    return (step % 2) == 1;
            }
        }

        private static int SnapScale(int midi)
        {
            int pc = midi % 12;
            if (pc < 0)
                pc += 12;
            int delta = 0;
            for (int step = 0; step <= 6; step++)
            {
                int up = pc + step;
                if (up >= 12)
                    up -= 12;
                if (IsDiatonic(up))
                {
                    delta = step;
                    break;
                }

                int down = pc - step;
                if (down < 0)
                    down += 12;
                if (IsDiatonic(down))
                {
                    delta = -step;
                    break;
                }
            }

            int snapped = midi + delta;
            while (snapped < 64)
                snapped += 12;
            while (snapped > 84)
                snapped -= 12;
            int snappedPc = snapped % 12;
            if (snappedPc < 0)
                snappedPc += 12;
            if (!IsDiatonic(snappedPc))
                throw new InvalidOperationException("lead left A minor");
            return snapped;
        }

        private static bool IsDiatonic(int pc)
        {
            return pc == 0 || pc == 2 || pc == 4 || pc == 5 || pc == 7 || pc == 9 || pc == 11;
        }

        private static int StemSalt(string stem)
        {
            int salt = 17;
            for (int i = 0; i < stem.Length; i++)
                salt = unchecked(salt * 31 + stem[i]);
            if (salt < 0)
                salt = -salt;
            return 1000 + (salt % 20000);
        }

        private static void EmitStem(
            string folder,
            string name,
            double[] buffer,
            string template,
            HashSet<string> guids,
            ref int written,
            ref int metas)
        {
            if (Write(folder, name, buffer))
                written++;
            if (EmitMeta(folder, name, template, guids))
                metas++;
        }

        private static void EnsureCampaignMetas(
            string folder,
            string stem,
            string template,
            HashSet<string> guids,
            ref int metas)
        {
            if (EmitMeta(folder, stem + "_low", template, guids))
                metas++;
            if (EmitMeta(folder, stem + "_perc", template, guids))
                metas++;
            if (EmitMeta(folder, stem + "_music", template, guids))
                metas++;
        }

        private static string LoadMetaTemplate(string folder)
        {
            string path = Path.Combine(folder, "neon_pulse_low.wav.meta");
            if (!File.Exists(path))
                throw new InvalidOperationException("Missing AudioImporter template " + path);
            string template = File.ReadAllText(path);
            if (template.IndexOf("forceToMono: 1", StringComparison.Ordinal) < 0
                || template.IndexOf("loadType: 0", StringComparison.Ordinal) < 0
                || template.IndexOf("compressionFormat: 0", StringComparison.Ordinal) < 0
                || template.IndexOf("preloadAudioData: 1", StringComparison.Ordinal) < 0
                || !GuidRegex.IsMatch(template))
                throw new InvalidOperationException("AudioImporter template drifted");
            return template;
        }

        private static HashSet<string> LoadGuids(string folder)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string assets = Path.GetFullPath(Path.Combine(folder, "..", "..", "..", ".."));
            string root = Directory.Exists(assets) ? assets : folder;
            foreach (string meta in Directory.EnumerateFiles(root, "*.meta", SearchOption.AllDirectories))
            {
                string text;
                try
                {
                    text = File.ReadAllText(meta);
                }
                catch (IOException)
                {
                    continue;
                }

                Match match = GuidRegex.Match(text);
                if (match.Success)
                    used.Add(match.Groups[1].Value);
            }

            return used;
        }

        private static bool EmitMeta(string folder, string name, string template, HashSet<string> guids)
        {
            string metaPath = Path.Combine(folder, name + ".wav.meta");
            if (File.Exists(metaPath))
                return false;
            if (!File.Exists(Path.Combine(folder, name + ".wav")))
                throw new InvalidOperationException("meta without wav " + name);

            string guid = FreshGuid(guids, name);
            Match match = GuidRegex.Match(template);
            string body = template.Substring(0, match.Index)
                + "guid: " + guid
                + template.Substring(match.Index + match.Length);
            if (body.IndexOf("guid: " + guid, StringComparison.Ordinal) < 0
                || body.IndexOf("forceToMono: 1", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("meta copy failed for " + name);
            File.WriteAllText(metaPath, body, new UTF8Encoding(false));
            return true;
        }

        private static string FreshGuid(HashSet<string> used, string name)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes("deckrogue-test-loop:" + name));
                for (int salt = 0; salt < 16; salt++)
                {
                    string guid = ToGuid(hash);
                    if (!used.Contains(guid))
                    {
                        used.Add(guid);
                        return guid;
                    }

                    hash = sha.ComputeHash(Encoding.UTF8.GetBytes("deckrogue-test-loop:" + name + ":" + salt.ToString(CultureInfo.InvariantCulture)));
                }
            }

            throw new InvalidOperationException("guid collision for " + name);
        }

        private static string ToGuid(byte[] hash)
        {
            var chars = new char[32];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < 16; i++)
            {
                chars[i * 2] = hex[hash[i] >> 4];
                chars[i * 2 + 1] = hex[hash[i] & 15];
            }

            return new string(chars);
        }

        private static void VerifyHeader(string folder, string file, int minPeak)
        {
            byte[] bytes = File.ReadAllBytes(Path.Combine(folder, file));
            using (var reader = new BinaryReader(new MemoryStream(bytes)))
            {
                string riff = new string(reader.ReadChars(4));
                reader.ReadInt32();
                string wave = new string(reader.ReadChars(4));
                reader.ReadChars(4);
                int fmtSize = reader.ReadInt32();
                short format = reader.ReadInt16();
                short channels = reader.ReadInt16();
                int rate = reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt16();
                short bits = reader.ReadInt16();
                if (fmtSize > 16)
                    reader.ReadBytes(fmtSize - 16);
                string dataId = new string(reader.ReadChars(4));
                int dataSize = reader.ReadInt32();
                double seconds = dataSize / (double)(rate * channels * (bits / 8));
                int peak = 0;
                int samples = dataSize / 2;
                for (int i = 0; i < samples; i++)
                {
                    short sample = reader.ReadInt16();
                    int abs = sample == short.MinValue ? 32768 : Math.Abs(sample);
                    if (abs > peak)
                        peak = abs;
                }

                Console.WriteLine(
                    file + " " + riff + "/" + wave +
                    " format " + format +
                    " ch " + channels +
                    " rate " + rate +
                    " bits " + bits +
                    " " + dataId +
                    " seconds " + seconds.ToString("0.000", CultureInfo.InvariantCulture) +
                    " peak " + peak);

                if (riff != "RIFF" || wave != "WAVE" || dataId != "data" || format != 1 || channels != 1 || rate != SampleRate || bits != 16)
                    throw new InvalidOperationException("header gate failed for " + file);
                if (Math.Abs(seconds - 16d) > 0.0000001d || bytes.Length != 44 + dataSize)
                    throw new InvalidOperationException("duration gate failed for " + file);
                if (bytes[bytes.Length - 1] != 0 || bytes[bytes.Length - 2] != 0)
                    throw new InvalidOperationException("seam gate failed for " + file);
                if (peak < minPeak)
                    throw new InvalidOperationException("peak gate failed for " + file);
            }
        }

        private static void VerifyMeta(string folder, string name)
        {
            string text = File.ReadAllText(Path.Combine(folder, name + ".wav.meta"));
            Match match = GuidRegex.Match(text);
            if (!match.Success || match.Groups[1].Value.Length != 32)
                throw new InvalidOperationException("meta guid failed for " + name);
            if (text.IndexOf("forceToMono: 1", StringComparison.Ordinal) < 0
                || text.IndexOf("loadType: 0", StringComparison.Ordinal) < 0
                || text.IndexOf("compressionFormat: 0", StringComparison.Ordinal) < 0
                || text.IndexOf("preloadAudioData: 1", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("meta importer failed for " + name);
        }

        private static void VerifyUniqueGuids(string folder)
        {
            var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string metaPath in Directory.EnumerateFiles(folder, "*.wav.meta"))
            {
                Match match = GuidRegex.Match(File.ReadAllText(metaPath));
                if (!match.Success || match.Groups[1].Value.Length != 32)
                    throw new InvalidOperationException("missing guid " + metaPath);
                string guid = match.Groups[1].Value;
                string prior;
                if (seen.TryGetValue(guid, out prior))
                    throw new InvalidOperationException("duplicate guid " + guid + " in " + prior);
                seen.Add(guid, metaPath);
            }

            Console.WriteLine("testloops guids " + seen.Count);
        }

        private static double Hash(int index)
        {
            uint x = (uint)index * 747796405u + 2891336453u;
            uint w = ((x >> (int)((x >> 28) + 4)) ^ x) * 277803737u;
            w = (w >> 22) ^ w;
            return (w / (double)uint.MaxValue) * 2d - 1d;
        }
    }
}
