using System;
using UnityEngine;

namespace BusJam
{
    /// <summary>
    /// Quan ly cai dat am thanh, nhac nen va rung (Haptic).
    /// Luu tru va dong bo trang thai qua PlayerPrefs.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager I { get; private set; }

        const string KeySound = "BusJam_Sound";
        const string KeyMusic = "BusJam_Music";
        const string KeyHaptic = "BusJam_Haptic";

        public event Action<bool> OnSoundChanged;
        public event Action<bool> OnMusicChanged;
        public event Action<bool> OnHapticChanged;

        bool _isSoundOn = true;
        bool _isMusicOn = true;
        bool _isHapticOn = true;

        public bool IsSoundOn => _isSoundOn;
        public bool IsMusicOn => _isMusicOn;
        public bool IsHapticOn => _isHapticOn;

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
            LoadSettings();
        }

        void LoadSettings()
        {
            _isSoundOn = PlayerPrefs.GetInt(KeySound, 1) == 1;
            _isMusicOn = PlayerPrefs.GetInt(KeyMusic, 1) == 1;
            _isHapticOn = PlayerPrefs.GetInt(KeyHaptic, 1) == 1;
        }

        public void SetSound(bool value)
        {
            if (_isSoundOn == value) return;
            _isSoundOn = value;
            PlayerPrefs.SetInt(KeySound, value ? 1 : 0);
            PlayerPrefs.Save();
            OnSoundChanged?.Invoke(_isSoundOn);
        }

        public void SetMusic(bool value)
        {
            if (_isMusicOn == value) return;
            _isMusicOn = value;
            PlayerPrefs.SetInt(KeyMusic, value ? 1 : 0);
            PlayerPrefs.Save();
            OnMusicChanged?.Invoke(_isMusicOn);
        }

        public void SetHaptic(bool value)
        {
            if (_isHapticOn == value) return;
            _isHapticOn = value;
            PlayerPrefs.SetInt(KeyHaptic, value ? 1 : 0);
            PlayerPrefs.Save();
            OnHapticChanged?.Invoke(_isHapticOn);
        }

        public void ToggleSound() => SetSound(!_isSoundOn);
        public void ToggleMusic() => SetMusic(!_isMusicOn);
        public void ToggleHaptic() => SetHaptic(!_isHapticOn);
    }
}
