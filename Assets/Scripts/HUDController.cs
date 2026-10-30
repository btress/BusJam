using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    /// <summary>
    /// HUDController (HUDCanvas - Sorting Order: 10).
    /// Quan ly hien thi Level, Info khach con lai, bo dem Coin/Sao va cac nut Settings / Restart.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] TMP_Text _levelText;
        [SerializeField] TMP_Text _infoText;
        [SerializeField] TMP_Text _coinText;
        [SerializeField] RectTransform _coinIcon;
        [SerializeField] Button _btnSettings;
        [SerializeField] Button _btnRestart;

        int _displayedCoins;
        int _targetCoins;
        Tween _coinCountTween;

        public RectTransform CoinIconTarget => _coinIcon;

        void Awake()
        {
            if (_btnSettings != null)
            {
                _btnSettings.onClick.AddListener(OnSettingsClicked);
            }
            if (_btnRestart != null)
            {
                _btnRestart.onClick.AddListener(OnRestartClicked);
            }
        }

        void OnSettingsClicked()
        {
            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (HapticManager.I != null) HapticManager.I.LightImpact();
            if (UIManager.I != null) UIManager.I.ShowSettings();
        }

        void OnRestartClicked()
        {
            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (HapticManager.I != null) HapticManager.I.LightImpact();
            if (GameManager.I != null) GameManager.I.RestartLevel();
        }

        public void SetLevel(int level)
        {
            if (_levelText != null) _levelText.text = "LEVEL " + level;
        }

        public void SetInfo(string info)
        {
            if (_infoText != null) _infoText.text = info;
        }

        public void InitCoins(int amount)
        {
            _coinCountTween?.Kill();
            _displayedCoins = amount;
            _targetCoins = amount;
            if (_coinText != null) _coinText.text = amount.ToString();
        }

        public void SetCoins(int targetAmount, bool animated = true)
        {
            _targetCoins = targetAmount;
            if (!animated)
            {
                _coinCountTween?.Kill();
                _displayedCoins = _targetCoins;
                if (_coinText != null) _coinText.text = _displayedCoins.ToString();
                return;
            }

            _coinCountTween?.Kill();
            int from = _displayedCoins;
            _coinCountTween = DOTween.To(() => from, x =>
            {
                from = x;
                _displayedCoins = x;
                if (_coinText != null) _coinText.text = x.ToString();
            }, _targetCoins, 0.25f).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        public void AddCoins(int addedAmount, bool animated = true)
        {
            SetCoins(_targetCoins + addedAmount, animated);
        }

        public void PunchCoinIcon()
        {
            if (_coinIcon == null) return;
            _coinIcon.DOKill();
            _coinIcon.localScale = Vector3.one;
            _coinIcon.DOPunchScale(Vector3.one * 0.35f, 0.22f, 8, 0.5f).SetLink(gameObject);
        }
    }
}
