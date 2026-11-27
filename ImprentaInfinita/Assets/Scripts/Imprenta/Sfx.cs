using System.Collections.Generic;
using UnityEngine;

namespace Imprenta
{
    /// <summary>
    /// Sonidos sintetizados en tiempo de carga para el prototipo gris (sin archivos).
    /// En la rebanada vertical se sustituyen por foley grabado (doc 4.16).
    /// </summary>
    public static class Sfx
    {
        const int SR = 44100;
        static readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
        static AudioSource[] pool2D;
        static AudioSource[] pool3D;
        static int next2D, next3D;
        public static float Volume = 0.8f;
        static System.Random rng = new System.Random(7);

        public static void Init(GameObject host)
        {
            pool2D = new AudioSource[10];
            for (int i = 0; i < pool2D.Length; i++) { pool2D[i] = host.AddComponent<AudioSource>(); pool2D[i].playOnAwake = false; }
            pool3D = new AudioSource[16];
            for (int i = 0; i < pool3D.Length; i++)
            {
                var go = new GameObject("Sfx3D_" + i);
                go.transform.SetParent(host.transform);
                var a = go.AddComponent<AudioSource>();
                a.playOnAwake = false; a.spatialBlend = 1f; a.minDistance = 1.5f; a.maxDistance = 40f; a.rolloffMode = AudioRolloffMode.Logarithmic;
                pool3D[i] = a;
            }
            Make("key", 6, Key);
            Make("keyBad", 3, i => Key(i, 0.75f));
            Make("bell", 1, Bell);
            Make("return", 1, Return);
            Make("fump", 3, Fump);
            Make("paper", 6, Paper);
            Make("coin", 2, Coin);
            Make("jam", 1, Jam);
            Make("buy", 1, Buy);
            Make("vacuum", 3, Vacuum);
            Make("deny", 1, Deny);
            Make("success", 1, Success);
            Make("siren", 1, Siren);
            Make("horn", 1, Horn);
            Make("stamp", 1, Stamp);
            Make("chime", 1, Chime);
            Make("printer", 3, Printer);
            Make("prestige", 1, Prestige);
        }

        delegate float[] Synth(int variant);

        static void Make(string name, int variants, Synth s)
        {
            var arr = new AudioClip[variants];
            for (int v = 0; v < variants; v++)
            {
                var data = s(v);
                var c = AudioClip.Create(name + v, data.Length, 1, SR, false);
                c.SetData(data, 0);
                arr[v] = c;
            }
            clips[name] = arr;
        }

        public static void Play(string name, float vol = 1f, float pitch = 1f)
        {
            if (pool2D == null || !clips.TryGetValue(name, out var arr)) return;
            var a = pool2D[next2D++ % pool2D.Length];
            a.pitch = pitch * (0.96f + 0.08f * (float)rng.NextDouble());
            a.PlayOneShot(arr[rng.Next(arr.Length)], vol * Volume);
        }

        public static void PlayAt(string name, Vector3 pos, float vol = 1f, float pitch = 1f)
        {
            if (pool3D == null || !clips.TryGetValue(name, out var arr)) return;
            var a = pool3D[next3D++ % pool3D.Length];
            a.transform.position = pos;
            a.pitch = pitch * (0.95f + 0.1f * (float)rng.NextDouble());
            a.PlayOneShot(arr[rng.Next(arr.Length)], vol * Volume);
        }

        /// <summary>Bucle continuo (motor de margarita, zumbido de la nave).</summary>
        public static AudioSource Loop(GameObject host, string kind, float vol, bool spatial)
        {
            float[] data = kind == "hum" ? Hum() : Motor();
            var c = AudioClip.Create(kind, data.Length, 1, SR, false);
            c.SetData(data, 0);
            var a = host.AddComponent<AudioSource>();
            a.clip = c; a.loop = true; a.volume = vol * Volume; a.spatialBlend = spatial ? 1 : 0; a.minDistance = 1.5f; a.maxDistance = 30f;
            a.Play();
            return a;
        }

        // ------------------------------------------------------------------ síntesis
        static float Rand() => (float)(rng.NextDouble() * 2 - 1);
        static float[] Buf(float seconds) => new float[(int)(SR * seconds)];

        static float[] Siren(int v)
        {
            var b = Buf(4.5f);
            float ph = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = 420 + 360 * (0.5f - 0.5f * Mathf.Cos(t * Mathf.PI * 0.9f));
                ph += 2 * Mathf.PI * f / SR;
                float env = Mathf.Clamp01(t * 3) * Mathf.Clamp01((4.5f - t) * 1.5f);
                b[i] = (Mathf.Sin(ph) * 0.6f + Mathf.Sin(ph * 2) * 0.25f + Mathf.Sign(Mathf.Sin(ph)) * 0.08f) * env * 0.5f;
            }
            return b;
        }

        static float[] Horn(int v)
        {
            var b = Buf(1.1f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                bool on = t < 0.35f || (t > 0.5f && t < 1.05f);
                float env = on ? 1 : 0;
                float s = Mathf.Sin(2 * Mathf.PI * 349 * t) + 0.7f * Mathf.Sin(2 * Mathf.PI * 440 * t);
                b[i] = Mathf.Clamp(s * 1.6f, -1, 1) * env * 0.35f;
            }
            return b;
        }

        static float[] Stamp(int v)
        {
            var b = Buf(0.5f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float clunk = Mathf.Sin(2 * Mathf.PI * 120 * t) * Mathf.Exp(-t * 30) * 0.8f;
                float click = t > 0.18f ? Rand() * Mathf.Exp(-(t - 0.18f) * 200) * 0.7f : 0;
                float bell = t > 0.2f ? Mathf.Sin(2 * Mathf.PI * 1760 * t) * Mathf.Exp(-(t - 0.2f) * 12) * 0.3f : 0;
                b[i] = (clunk + click + bell + Rand() * Mathf.Exp(-t * 80) * 0.4f) * 0.6f;
            }
            return b;
        }

        static float[] Chime(int v)
        {
            var b = Buf(1.2f);
            float[] notes = { 659, 784, 988 };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR, s = 0;
                for (int k = 0; k < 3; k++) { float tt = t - k * 0.15f; if (tt > 0) s += Mathf.Sin(2 * Mathf.PI * notes[k] * tt) * Mathf.Exp(-tt * 4); }
                b[i] = s * 0.25f;
            }
            return b;
        }

        static float[] Printer(int v)
        {
            var b = Buf(0.6f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float buzz = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * (900 + v * 120) * t)) * (Mathf.Sin(2 * Mathf.PI * 18 * t) > 0 ? 1 : 0.2f);
                b[i] = (buzz * 0.12f + Rand() * 0.05f) * Mathf.Clamp01((0.6f - t) * 8);
            }
            return b;
        }

        static float[] Prestige(int v)
        {
            var b = Buf(3f);
            float[] notes = { 523, 659, 784, 1047, 1319 };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR, s = 0;
                for (int k = 0; k < notes.Length; k++) { float tt = t - k * 0.18f; if (tt > 0) s += Mathf.Sin(2 * Mathf.PI * notes[k] * tt) * Mathf.Exp(-tt * 1.2f) * (1 + 0.3f * Mathf.Sin(2 * Mathf.PI * 5 * tt)); }
                b[i] = s * 0.15f;
            }
            return b;
        }

        static float[] Key(int v) => Key(v, 1f);
        static float[] Key(int v, float tone)
        {
            var b = Buf(0.11f);
            float lp = 0, f1 = (2600 + v * 230) * tone, f2 = 170 * tone;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float n = Rand();
                lp += (n - lp) * 0.35f;
                float click = (n - lp) * Mathf.Exp(-t * 160);
                float ring = Mathf.Sin(2 * Mathf.PI * f1 * t) * Mathf.Exp(-t * 90) * 0.25f;
                float thunk = Mathf.Sin(2 * Mathf.PI * f2 * t) * Mathf.Exp(-t * 55) * 0.6f;
                float slap = t > 0.012f ? Rand() * Mathf.Exp(-(t - 0.012f) * 260) * 0.5f : 0;
                b[i] = (click * 0.9f + ring + thunk + slap) * 0.55f;
            }
            return b;
        }

        static float[] Bell(int v)
        {
            var b = Buf(1.4f);
            float f = 2093;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                b[i] = (Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 3.2f)
                      + 0.45f * Mathf.Sin(2 * Mathf.PI * f * 2.76f * t) * Mathf.Exp(-t * 6f)
                      + 0.25f * Mathf.Sin(2 * Mathf.PI * f * 5.4f * t) * Mathf.Exp(-t * 11f)) * 0.35f;
            }
            return b;
        }

        static float[] Return(int v)
        {
            var b = Buf(0.75f);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float n = Rand(); lp += (n - lp) * 0.08f;
                float slide = lp * (t < 0.55f ? 0.6f : 0.6f * Mathf.Exp(-(t - 0.55f) * 30));
                float period = 0.028f + t * 0.01f;
                float ph = t % period;
                float ratchet = ph < 0.004f ? Rand() * (1 - ph / 0.004f) * 0.7f : 0;
                float stop = t > 0.56f ? Mathf.Sin(2 * Mathf.PI * 140 * t) * Mathf.Exp(-(t - 0.56f) * 40) : 0;
                b[i] = (slide + ratchet + stop * 0.8f) * 0.5f;
            }
            return b;
        }

        static float[] Fump(int v)
        {
            var b = Buf(0.55f);
            float lp = 0, lp2 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float env = t < 0.06f ? t / 0.06f : Mathf.Exp(-(t - 0.06f) * 9);
                float n = Rand(); lp += (n - lp) * 0.05f; lp2 += (lp - lp2) * 0.1f;
                float sweep = Mathf.Sin(2 * Mathf.PI * (110 - 70 * t) * t) * Mathf.Exp(-t * 7) * 0.5f;
                b[i] = (lp * 2.2f + lp2 * 2f + sweep) * env * (0.9f + v * 0.05f);
            }
            return b;
        }

        static float[] Paper(int v)
        {
            var b = Buf(0.22f + v * 0.03f);
            float hp = 0, prev = 0, env = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                if (i % 220 == 0) env = Mathf.Clamp01((float)rng.NextDouble() * 1.3f) * Mathf.Sin(Mathf.PI * t / (b.Length / (float)SR));
                float n = Rand();
                hp = 0.85f * (hp + n - prev); prev = n;
                b[i] = hp * env * 0.28f;
            }
            return b;
        }

        static float[] Coin(int v)
        {
            var b = Buf(0.18f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float c1 = t < 0.003f ? Rand() : 0;
                float c2 = (t > 0.05f && t < 0.054f) ? Rand() : 0;
                float tone = Mathf.Sin(2 * Mathf.PI * (1400 + v * 180) * t) * Mathf.Exp(-t * 30) * 0.3f;
                b[i] = (c1 + c2) * 0.5f + tone;
            }
            return b;
        }

        static float[] Jam(int v)
        {
            var b = Buf(0.5f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                b[i] = (Mathf.Sin(2 * Mathf.PI * 90 * t) * Mathf.Exp(-t * 12)
                      + Mathf.Sin(2 * Mathf.PI * 1830 * t) * Mathf.Exp(-t * 14) * 0.3f
                      + Mathf.Sin(2 * Mathf.PI * 2450 * t) * Mathf.Exp(-t * 18) * 0.25f
                      + Rand() * Mathf.Exp(-t * 50) * 0.4f) * 0.6f;
            }
            return b;
        }

        static float[] Buy(int v)
        {
            var b = Buf(0.6f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float a = Mathf.Sin(2 * Mathf.PI * 1318 * t) * Mathf.Exp(-t * 7);
                float c = t > 0.09f ? Mathf.Sin(2 * Mathf.PI * 1760 * t) * Mathf.Exp(-(t - 0.09f) * 6) : 0;
                float clk = t < 0.004f ? Rand() : 0;
                b[i] = (a * 0.35f + c * 0.35f + clk * 0.6f) * 0.7f;
            }
            return b;
        }

        static float[] Success(int v)
        {
            var b = Buf(1.4f);
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR, s = 0;
                for (int k = 0; k < notes.Length; k++)
                {
                    float st = k * 0.12f;
                    if (t > st) s += Mathf.Sin(2 * Mathf.PI * notes[k] * t) * Mathf.Exp(-(t - st) * 3f);
                }
                b[i] = s * 0.18f;
            }
            return b;
        }

        static float[] Vacuum(int v)
        {
            var b = Buf(0.16f);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float n = Rand(); lp += (n - lp) * 0.25f;
                float env = Mathf.Sin(Mathf.PI * t / 0.16f);
                b[i] = (lp * 0.6f + Mathf.Sin(2 * Mathf.PI * (300 + 900 * t) * t) * 0.15f) * env * 0.5f;
            }
            return b;
        }

        static float[] Deny(int v)
        {
            var b = Buf(0.22f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                b[i] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * 160 * t)) * Mathf.Exp(-t * 12) * 0.15f;
            }
            return b;
        }

        static float[] Hum()
        {
            var b = Buf(4f);
            float brown = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                brown = Mathf.Clamp(brown + Rand() * 0.02f, -1, 1) * 0.999f;
                // fundido cruzado para que el bucle no haga clic
                float edge = Mathf.Min(1, Mathf.Min(t, 4f - t) / 0.2f);
                b[i] = (brown * 0.5f + Mathf.Sin(2 * Mathf.PI * 50 * t) * 0.05f + Mathf.Sin(2 * Mathf.PI * 100 * t) * 0.03f) * edge;
            }
            return b;
        }

        static float[] Motor()
        {
            var b = Buf(1f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float saw = (t * 120f) % 1f * 2 - 1;
                float tick = (t * 9f) % 1f < 0.04f ? Rand() * 0.8f : 0;
                b[i] = (saw * 0.12f + Mathf.Sin(2 * Mathf.PI * 240 * t) * 0.08f + tick * 0.3f) * 0.6f;
            }
            return b;
        }
    }
}
