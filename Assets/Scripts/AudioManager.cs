using System;
using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    public enum Sfx { Click, Honk, Engine, Step, Bump, Win, Lose, Error, Board, CoinChime }

    [Serializable]
    public class SfxOverride
    {
        public Sfx id;
        public AudioClip clip;
    }

    /// <summary>
    /// Master Audio Controller:
    /// - Phan tach ro rang kenh _sfxSource (nhieu source trong pool) va _bgmSource.
    /// - Tu dong dong bo theo co IsSoundOn va IsMusicOn tu SettingsManager.
    /// - Tu tao synth audio cho toan bo SFX va nhac nen BGM neu khong gan file ngoai.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        public List<SfxOverride> overrides = new List<SfxOverride>();
        public AudioClip bgmClip;
        [Range(0, 1)] public float masterVolume = 0.8f;
        [Range(0, 1)] public float bgmVolume = 0.4f;

        const int SR = 44100;
        const float TwoPi = Mathf.PI * 2f;

        AudioSource[] _sfxSources;
        int _nextSfxIndex;
        AudioClip[] _clips;
        AudioSource _bgmSource;

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            _sfxSources = new AudioSource[8];
            for (int i = 0; i < _sfxSources.Length; i++)
            {
                _sfxSources[i] = gameObject.AddComponent<AudioSource>();
                _sfxSources[i].playOnAwake = false;
            }

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = bgmVolume * masterVolume;

            _clips = new AudioClip[Enum.GetValues(typeof(Sfx)).Length];
            BuildSynthSfx();

            foreach (var o in overrides)
            {
                if (o.clip != null) _clips[(int)o.id] = o.clip;
            }

            if (bgmClip != null)
            {
                _bgmSource.clip = bgmClip;
            }
            else
            {
                _bgmSource.clip = BuildSynthBgm();
            }
        }

        void Start()
        {
            if (SettingsManager.I != null)
            {
                SettingsManager.I.OnSoundChanged += HandleSoundChanged;
                SettingsManager.I.OnMusicChanged += HandleMusicChanged;
                ApplyMusicState(SettingsManager.I.IsMusicOn);
            }
            else
            {
                ApplyMusicState(true);
            }
        }

        void OnDestroy()
        {
            if (SettingsManager.I != null)
            {
                SettingsManager.I.OnSoundChanged -= HandleSoundChanged;
                SettingsManager.I.OnMusicChanged -= HandleMusicChanged;
            }
        }

        void HandleSoundChanged(bool isOn)
        {
            if (!isOn)
            {
                for (int i = 0; i < _sfxSources.Length; i++)
                {
                    _sfxSources[i].Stop();
                }
            }
        }

        void HandleMusicChanged(bool isOn)
        {
            ApplyMusicState(isOn);
        }

        void ApplyMusicState(bool isOn)
        {
            if (_bgmSource == null) return;
            if (isOn)
            {
                if (!_bgmSource.isPlaying && _bgmSource.clip != null)
                {
                    _bgmSource.volume = bgmVolume * masterVolume;
                    _bgmSource.Play();
                }
            }
            else
            {
                _bgmSource.Pause();
            }
        }

        public void Play(Sfx id, float vol = 1f, float pitchJitter = 0.05f)
        {
            if (SettingsManager.I != null && !SettingsManager.I.IsSoundOn) return;

            var c = _clips[(int)id];
            if (c == null) return;

            var s = _sfxSources[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxSources.Length;
            s.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            s.PlayOneShot(c, vol * masterVolume);
        }

        public void PlayBtnClick()
        {
            Play(Sfx.Click, 0.9f, 0.02f);
        }

        public void PlayCoinChime(float pitchOffset = 0f)
        {
            if (SettingsManager.I != null && !SettingsManager.I.IsSoundOn) return;

            var c = _clips[(int)Sfx.CoinChime];
            if (c == null) return;

            var s = _sfxSources[_nextSfxIndex];
            _nextSfxIndex = (_nextSfxIndex + 1) % _sfxSources.Length;
            s.pitch = 1f + pitchOffset + UnityEngine.Random.Range(-0.02f, 0.02f);
            s.PlayOneShot(c, 0.85f * masterVolume);
        }

        // ---------- synth algorithms ----------
        static AudioClip Gen(string n, float dur, Func<float, float> f)
        {
            int len = (int)(SR * dur);
            var d = new float[len];
            for (int i = 0; i < len; i++) d[i] = Mathf.Clamp(f((float)i / SR), -1f, 1f);
            var c = AudioClip.Create(n, len, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }

        static float Phase(float f0, float f1, float dur, float t) => TwoPi * (f0 * t + (f1 - f0) * t * t / (2f * dur));
        static float Decay(float t, float d) { float x = Mathf.Clamp01(1f - t / d); return x * x; }
        static float AD(float t, float d) => Mathf.Min(1f, t * 80f) * Mathf.Min(1f, (d - t) * 30f);
        static float Sq(float ph) => Mathf.Sin(ph) >= 0 ? 1f : -1f;

        void BuildSynthSfx()
        {
            _clips[(int)Sfx.Click] = Gen("click", 0.06f, t => Mathf.Sin(Phase(900, 500, 0.06f, t)) * Decay(t, 0.06f) * 0.5f);
            _clips[(int)Sfx.Honk] = Gen("honk", 0.35f, t => (Sq(TwoPi * 392f * t) + Sq(TwoPi * 494f * t)) * 0.12f * AD(t, 0.35f));
            _clips[(int)Sfx.Engine] = Gen("engine", 0.6f, t => (Mathf.Repeat(Phase(70, 220, 0.6f, t) / TwoPi, 1f) * 2f - 1f) * 0.22f * AD(t, 0.6f));
            _clips[(int)Sfx.Step] = Gen("step", 0.05f, t => (UnityEngine.Random.value * 2f - 1f) * 0.5f * Decay(t, 0.05f) + Mathf.Sin(Phase(300, 150, 0.05f, t)) * 0.3f * Decay(t, 0.05f));
            _clips[(int)Sfx.Bump] = Gen("bump", 0.18f, t => Mathf.Sin(Phase(150, 50, 0.18f, t)) * Decay(t, 0.18f) * 0.8f);
            _clips[(int)Sfx.Board] = Gen("board", 0.08f, t => Mathf.Sin(Phase(500, 900, 0.08f, t)) * Decay(t, 0.08f) * 0.4f);
            _clips[(int)Sfx.Error] = Gen("error", 0.18f, t => Sq(TwoPi * 160f * t) * 0.18f * AD(t, 0.18f));
            _clips[(int)Sfx.Lose] = Gen("lose", 0.7f, t => (Mathf.Repeat(Phase(330, 90, 0.7f, t) / TwoPi, 1f) * 2f - 1f) * 0.28f * Mathf.Sqrt(Mathf.Clamp01(1f - t / 0.7f)));

            float[] winNotes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            _clips[(int)Sfx.Win] = Gen("win", 0.7f, t =>
            {
                int i = Mathf.Min(3, (int)(t / 0.12f));
                float lt = t - i * 0.12f;
                float d = i == 3 ? 0.34f : 0.14f;
                return Mathf.Sin(TwoPi * winNotes[i] * lt) * Decay(lt, d) * 0.4f;
            });

            // CoinChime: tieng leng keng tinh te nhu chuong bac ASMR
            _clips[(int)Sfx.CoinChime] = Gen("coin_chime", 0.28f, t =>
            {
                float baseFreq = 1760f; // A6
                float harmonic = 3520f; // A7
                float bell = Mathf.Sin(TwoPi * baseFreq * t) * 0.5f + Mathf.Sin(TwoPi * harmonic * t) * 0.25f;
                return bell * Decay(t, 0.28f);
            });
        }

        AudioClip BuildSynthBgm()
        {
            // Tao mot doan loop nhac nen nhe nhang, de thuong (4 hop am C - G - Am - F)
            float dur = 6.4f;
            int len = (int)(SR * dur);
            var d = new float[len];
            float[] melody = { 523.25f, 659.25f, 783.99f, 659.25f, 587.33f, 783.99f, 880.00f, 783.99f, 440.00f, 523.25f, 659.25f, 523.25f, 698.46f, 659.25f, 587.33f, 523.25f };
            float noteDur = dur / melody.Length;

            for (int i = 0; i < len; i++)
            {
                float t = (float)i / SR;
                int noteIdx = Mathf.Clamp((int)(t / noteDur), 0, melody.Length - 1);
                float noteT = t - noteIdx * noteDur;
                float f = melody[noteIdx];

                // Sine wave kem soft decay moi not
                float sample = Mathf.Sin(TwoPi * f * noteT) * 0.08f * Decay(noteT, noteDur);
                // Bass note
                float bassF = (noteIdx / 4 == 0) ? 130.81f : (noteIdx / 4 == 1) ? 98.00f : (noteIdx / 4 == 2) ? 110.00f : 87.31f;
                sample += Mathf.Sin(TwoPi * bassF * t) * 0.06f;

                d[i] = Mathf.Clamp(sample, -1f, 1f);
            }

            var c = AudioClip.Create("synth_bgm", len, 1, SR, false);
            c.SetData(d, 0);
            return c;
        }
    }
}
