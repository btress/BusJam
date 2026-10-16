using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    public enum Sfx { Click, Honk, Engine, Step, Bump, Win, Lose, Error, Board }

    [Serializable] public class SfxOverride { public Sfx id; public AudioClip clip; }

    /// <summary>SFX manager. Neu khong gan AudioClip, tu sinh am thanh tong hop (synth) de prototype co tieng ngay.</summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }
        public List<SfxOverride> overrides = new List<SfxOverride>();
        [Range(0, 1)] public float masterVolume = 0.8f;

        const int SR = 44100;
        const float TwoPi = Mathf.PI * 2f;
        AudioSource[] _src; int _next; AudioClip[] _clips;

        void Awake()
        {
            I = this;
            _src = new AudioSource[8];
            for (int i = 0; i < _src.Length; i++) { _src[i] = gameObject.AddComponent<AudioSource>(); _src[i].playOnAwake = false; }
            _clips = new AudioClip[Enum.GetValues(typeof(Sfx)).Length];
            BuildSynth();
            foreach (var o in overrides) if (o.clip != null) _clips[(int)o.id] = o.clip;
        }

        public void Play(Sfx id, float vol = 1f, float pitchJitter = 0.05f)
        {
            var c = _clips[(int)id]; if (c == null) return;
            var s = _src[_next]; _next = (_next + 1) % _src.Length;
            s.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(c, vol * masterVolume);
        }

        // ---------- synth ----------
        static AudioClip Gen(string n, float dur, Func<float, float> f)
        {
            int len = (int)(SR * dur); var d = new float[len];
            for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(f((float)i / SR), -1f, 1f);
            var c = AudioClip.Create(n, len, 1, SR, false); c.SetData(d, 0); return c;
        }
        static float Phase(float f0, float f1, float dur, float t) => TwoPi * (f0 * t + (f1 - f0) * t * t / (2f * dur));
        static float Decay(float t, float d) { float x = Mathf.Clamp01(1f - t / d); return x * x; }
        static float AD(float t, float d) => Mathf.Min(1f, t * 80f) * Mathf.Min(1f, (d - t) * 30f);
        static float Sq(float ph) => Mathf.Sin(ph) >= 0 ? 1f : -1f;

        void BuildSynth()
        {
            _clips[(int)Sfx.Click] = Gen("click", 0.06f, t => Mathf.Sin(Phase(900, 500, 0.06f, t)) * Decay(t, 0.06f) * 0.5f);
            _clips[(int)Sfx.Honk] = Gen("honk", 0.35f, t => (Sq(TwoPi * 392f * t) + Sq(TwoPi * 494f * t)) * 0.12f * AD(t, 0.35f));
            _clips[(int)Sfx.Engine] = Gen("engine", 0.6f, t => (Mathf.Repeat(Phase(70, 220, 0.6f, t) / TwoPi, 1f) * 2f - 1f) * 0.22f * AD(t, 0.6f));
            _clips[(int)Sfx.Step] = Gen("step", 0.05f, t => (UnityEngine.Random.value * 2f - 1f) * 0.5f * Decay(t, 0.05f) + Mathf.Sin(Phase(300, 150, 0.05f, t)) * 0.3f * Decay(t, 0.05f));
            _clips[(int)Sfx.Bump] = Gen("bump", 0.18f, t => Mathf.Sin(Phase(150, 50, 0.18f, t)) * Decay(t, 0.18f) * 0.8f);
            _clips[(int)Sfx.Board] = Gen("board", 0.08f, t => Mathf.Sin(Phase(500, 900, 0.08f, t)) * Decay(t, 0.08f) * 0.4f);
            _clips[(int)Sfx.Error] = Gen("error", 0.18f, t => Sq(TwoPi * 160f * t) * 0.18f * AD(t, 0.18f));
            _clips[(int)Sfx.Lose] = Gen("lose", 0.7f, t => (Mathf.Repeat(Phase(330, 90, 0.7f, t) / TwoPi, 1f) * 2f - 1f) * 0.28f * Mathf.Sqrt(Mathf.Clamp01(1f - t / 0.7f)));
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            _clips[(int)Sfx.Win] = Gen("win", 0.7f, t =>
            {
                int i = Mathf.Min(3, (int)(t / 0.12f));
                float lt = t - i * 0.12f;
                float d = i == 3 ? 0.34f : 0.14f;
                return Mathf.Sin(TwoPi * notes[i] * lt) * Decay(lt, d) * 0.4f;
            });
        }
    }
}
