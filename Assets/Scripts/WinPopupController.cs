using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    /// <summary>
    /// WinPopupController (PopupCanvas - Sorting Order: 20).
    /// Hien thi chuc mung vuot qua man choi kem tien thuong va nut Claim/Next Level.
    /// </summary>
    public class WinPopupController : MonoBehaviour
    {
        [Header("Containers")]
        [SerializeField] CanvasGroup _backdropCanvasGroup;
        [SerializeField] RectTransform _panelContent;

        [Header("UI Elements")]
        [SerializeField] TMP_Text _titleText;
        [SerializeField] TMP_Text _rewardText;
        [SerializeField] Button _btnClaim;

        Action _onClaimCallback;
        bool _isAnimating;

        public RectTransform ClaimButtonTransform => _btnClaim != null ? _btnClaim.GetComponent<RectTransform>() : _panelContent;

        void Awake()
        {
            if (_btnClaim != null)
            {
                _btnClaim.onClick.AddListener(OnClaimClicked);
            }
        }

        public void Open(int rewardCoins, Action onClaim)
        {
            if (_isAnimating) return;
            _isAnimating = true;

            _onClaimCallback = onClaim;
            if (_rewardText != null) _rewardText.text = "+" + rewardCoins;

            gameObject.SetActive(true);

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

        void OnClaimClicked()
        {
            if (_isAnimating) return;
            _isAnimating = true;

            if (AudioManager.I != null) AudioManager.I.PlayBtnClick();
            if (HapticManager.I != null) HapticManager.I.LightImpact();

            Close(() =>
            {
                _onClaimCallback?.Invoke();
            });
        }

        public void Close(Action onClosed = null)
        {
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
                        onClosed?.Invoke();
                    });
            }
            else
            {
                _isAnimating = false;
                gameObject.SetActive(false);
                onClosed?.Invoke();
            }
        }
    }
}
