using System;
using UnityEngine;

namespace BusJam
{
    /// <summary>
    /// UIManager (Dieu phoi dong/mo panel, khong can thiep vao logic tinh toan game).
    /// Quan ly cac sub-controllers theo layer:
    /// - HUDController (HUDCanvas - Order: 10)
    /// - WinPopupController, LosePopupController, SettingsPopupController (PopupCanvas - Order: 20)
    /// - CoinFlyController (VFXCanvas - Order: 30)
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }

        const string KeyCoins = "BusJam_Coins";

        [Header("Layer Controllers")]
        [SerializeField] HUDController _hud;
        [SerializeField] WinPopupController _winPopup;
        [SerializeField] LosePopupController _losePopup;
        [SerializeField] SettingsPopupController _settingsPopup;
        [SerializeField] CoinFlyController _coinFly;

        int _totalCoins;

        public int TotalCoins => _totalCoins;
        public HUDController HUD => _hud;
        public WinPopupController WinPopup => _winPopup;
        public LosePopupController LosePopup => _losePopup;
        public SettingsPopupController SettingsPopup => _settingsPopup;
        public CoinFlyController CoinFly => _coinFly;

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            _totalCoins = PlayerPrefs.GetInt(KeyCoins, 0);
            if (_hud != null) _hud.InitCoins(_totalCoins);
        }

        public void BindControllers(HUDController hud, WinPopupController win, LosePopupController lose, SettingsPopupController settings, CoinFlyController coinFly)
        {
            _hud = hud;
            _winPopup = win;
            _losePopup = lose;
            _settingsPopup = settings;
            _coinFly = coinFly;
            if (_hud != null) _hud.InitCoins(_totalCoins);
        }

        public void SetLevel(int n)
        {
            if (_hud != null) _hud.SetLevel(n);
        }

        public void SetInfo(string s)
        {
            if (_hud != null) _hud.SetInfo(s);
        }

        public void SyncCoinsDisplay()
        {
            _totalCoins = PlayerPrefs.GetInt(KeyCoins, _totalCoins);
            if (_hud != null) _hud.SetCoins(_totalCoins, false);
        }

        public void SaveTotalCoins(int newTotal)
        {
            _totalCoins = newTotal;
            PlayerPrefs.SetInt(KeyCoins, _totalCoins);
            PlayerPrefs.Save();
        }

        public void AddCoins(int amount)
        {
            _totalCoins += amount;
            PlayerPrefs.SetInt(KeyCoins, _totalCoins);
            PlayerPrefs.Save();
            if (_hud != null) _hud.AddCoins(amount, true);
        }

        public void ShowWin(int rewardCoins, Action onNextLevel)
        {
            HideAllPopups();
            if (_winPopup != null)
            {
                _winPopup.Open(rewardCoins, () =>
                {
                    // Khi bam Claim tren Win Popup: kich hoat hieu ung Fly-to-Target
                    Vector3 startPos = _winPopup.ClaimButtonTransform != null
                        ? _winPopup.ClaimButtonTransform.position
                        : new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);

                    if (_coinFly != null && _hud != null)
                    {
                        _coinFly.PlayFlyEffect(startPos, _hud.CoinIconTarget, rewardCoins, onNextLevel);
                    }
                    else
                    {
                        AddCoins(rewardCoins);
                        onNextLevel?.Invoke();
                    }
                });
            }
            else
            {
                AddCoins(rewardCoins);
                onNextLevel?.Invoke();
            }
        }

        public void ShowLose(Action onRetry)
        {
            HideAllPopups();
            if (_losePopup != null)
            {
                _losePopup.Open(onRetry);
            }
            else
            {
                onRetry?.Invoke();
            }
        }

        public void ShowSettings()
        {
            if (_settingsPopup != null)
            {
                _settingsPopup.Open();
            }
        }

        public void HideAllPopups()
        {
            if (_winPopup != null) _winPopup.gameObject.SetActive(false);
            if (_losePopup != null) _losePopup.gameObject.SetActive(false);
            if (_settingsPopup != null) _settingsPopup.gameObject.SetActive(false);
        }

        public void Hide()
        {
            HideAllPopups();
        }
    }
}
