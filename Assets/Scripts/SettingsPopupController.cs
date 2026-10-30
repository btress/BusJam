using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    /// <summary>
    /// SettingsPopupController (PopupCanvas - Sorting Order: 20).
    /// Doc lap xu ly toggle Sound, Music, Haptic va luu tru vao PlayerPrefs.
    /// </summary>
    public class SettingsPopupController : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] CanvasGroup _backdropCanvasGroup;
        [SerializeField] RectTransform _panelContent;

        [Header("Buttons")]
        [SerializeField] Button _btnClose;
        [SerializeField] Button _btnSound;
        [SerializeField] Button _btnMusic;
        [SerializeField] Button _btnHaptic;

        [Header("Sound Toggle UI")]
        [SerializeField] Image _soundTrack;
        [SerializeField] RectTransform _soundKnob;
        [SerializeField] TMP_Text _soundStateText;

        [Header("Music Toggle UI")]
        [SerializeField] Image _musicTrack;
        [SerializeField] RectTransform _musicKnob;
        [SerializeField] TMP_Text _musicStateText;

        [Header("Haptic Toggle UI")]
        [SerializeField] Image _hapticTrack;
        [SerializeField] RectTransform _hapticKnob;
        [SerializeField] TMP_Text _hapticStateText;

        static readonly Color ColorOn = new Color(0.22f, 0.78f, 0.38f, 1f);
        static readonly Color ColorOff = new Color(0.35f, 0.38f, 0.48f, 1f);
        const float KnobOffset = 30f;

        bool _isAnimating;

        void Awake()
        {
            if (_btnClose != null) _btnClose.onClick.AddListener(Close);
            if (_btnSound != null) _btnSound.onClick.AddListener(OnSoundToggle);
            if (_btnMusic != null) _btnMusic.onClick.AddListener(OnMusicToggle);
            if (_btnHaptic != null) _btnHaptic.onClick.AddListener(OnHapticToggle);
        }

        void OnSoundToggle()
        {
            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (HapticManager.I != null) HapticManager.I.LightImpact();
            if (SettingsManager.I != null)
            {
                SettingsManager.I.ToggleSound();
                UpdateUI(true);
            }
        }

        void OnMusicToggle()
        {
            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (HapticManager.I != null) HapticManager.I.LightImpact();
            if (SettingsManager.I != null)
            {
                SettingsManager.I.ToggleMusic();
                UpdateUI(true);
            }
        }

        void OnHapticToggle()
        {
            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (SettingsManager.I != null)
            {
                SettingsManager.I.ToggleHaptic();
                UpdateUI(true);
            }
            if (HapticManager.I != null) HapticManager.I.LightImpact();
        }

        public void Open()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            gameObject.SetActive(true);
            UpdateUI(false);

            if (_backdropCanvasGroup != null)
            {
                _backdropCanvasGroup.DOKill();
                _backdropCanvasGroup.alpha = 0f;
                _backdropCanvasGroup.DOFade(0.75f, 0.25f).SetLink(gameObject);
            }

            if (_panelContent != null)
            {
                _panelContent.DOKill();
                _panelContent.localScale = Vector3.one * 0.5f;
                _panelContent.DOScale(1f, 0.35f).SetEase(Ease.OutBack)
                    .SetLink(gameObject)
                    .OnComplete(() => _isAnimating = false);
            }
            else
            {
                _isAnimating = false;
            }
        }

        public void Close()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (HapticManager.I != null) HapticManager.I.LightImpact();

            if (_backdropCanvasGroup != null)
            {
                _backdropCanvasGroup.DOKill();
                _backdropCanvasGroup.DOFade(0f, 0.2f).SetLink(gameObject);
            }

            if (_panelContent != null)
            {
                _panelContent.DOKill();
                _panelContent.DOScale(0f, 0.2f).SetEase(Ease.InBack)
                    .SetLink(gameObject)
                    .OnComplete(() =>
                    {
                        _isAnimating = false;
                        gameObject.SetActive(false);
                    });
            }
            else
            {
                _isAnimating = false;
                gameObject.SetActive(false);
            }
        }

        void UpdateUI(bool animate)
        {
            if (SettingsManager.I == null) return;
            ApplyToggleVisual(_soundTrack, _soundKnob, _soundStateText, SettingsManager.I.IsSoundOn, animate);
            ApplyToggleVisual(_musicTrack, _musicKnob, _musicStateText, SettingsManager.I.IsMusicOn, animate);
            ApplyToggleVisual(_hapticTrack, _hapticKnob, _hapticStateText, SettingsManager.I.IsHapticOn, animate);
        }

        void ApplyToggleVisual(Image track, RectTransform knob, TMP_Text text, bool isOn, bool animate)
        {
            Color targetCol = isOn ? ColorOn : ColorOff;
            float targetX = isOn ? KnobOffset : -KnobOffset;

            if (text != null) text.text = isOn ? "ON" : "OFF";

            if (track != null)
            {
                if (animate)
                    track.DOColor(targetCol, 0.2f).SetLink(gameObject);
                else
                    track.color = targetCol;
            }

            if (knob != null)
            {
                Vector2 targetPos = new Vector2(targetX, knob.anchoredPosition.y);
                if (animate)
                    knob.DOAnchorPos(targetPos, 0.2f).SetEase(Ease.OutQuad).SetLink(gameObject);
                else
                    knob.anchoredPosition = targetPos;
            }
        }
    }
}
