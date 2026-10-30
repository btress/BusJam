using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace BusJam
{
    /// <summary>
    /// CoinFlyController (VFXCanvas - Sorting Order: 30).
    /// Quan ly Object Pool cac icon coin UI va hoat anh Fly-to-Target khi thang man.
    /// </summary>
    public class CoinFlyController : MonoBehaviour
    {
        [Header("Coin UI Setup")]
        [SerializeField] GameObject _coinPrefab;
        [SerializeField] Transform _poolRoot;
        [SerializeField] Sprite _coinSprite;

        ObjectPool<RectTransform> _pool;

        void Awake()
        {
            if (_poolRoot == null) _poolRoot = transform;

            _pool = new ObjectPool<RectTransform>(
                CreateCoin,
                coin =>
                {
                    coin.gameObject.SetActive(true);
                    coin.localScale = Vector3.one;
                },
                coin =>
                {
                    coin.DOKill();
                    coin.gameObject.SetActive(false);
                    coin.SetParent(_poolRoot, false);
                },
                coin =>
                {
                    if (coin != null) Destroy(coin.gameObject);
                },
                true, 16, 32
            );

            // Prewarm 12 coins
            var prewarmList = new List<RectTransform>(12);
            for (int i = 0; i < 12; i++) prewarmList.Add(_pool.Get());
            foreach (var c in prewarmList) _pool.Release(c);
        }

        RectTransform CreateCoin()
        {
            if (_coinPrefab != null)
            {
                var go = Instantiate(_coinPrefab, _poolRoot);
                return go.GetComponent<RectTransform>();
            }

            var coinGo = new GameObject("CoinUI", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            coinGo.transform.SetParent(_poolRoot, false);
            var rt = coinGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(70, 70);
            var img = coinGo.GetComponent<Image>();
            if (_coinSprite != null) img.sprite = _coinSprite;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>
        /// Hoat anh Fly-to-Target:
        /// 1. Spawn 8-10 icon tien tu Pool.
        /// 2. No bung nhe xung quanh vi tri xuat phat.
        /// 3. Tung dong tien bay ve phia targetHUDIcon.
        /// 4. Chiem dich: HUD nảy, SFX chuong keu, tang tien chinh xac, thu hoi coin ve Pool.
        /// </summary>
        public void PlayFlyEffect(Vector3 startScreenOrWorldPos, RectTransform targetHUDIcon, int totalRewardCoins, Action onEffectComplete)
        {
            if (targetHUDIcon == null || UIManager.I == null)
            {
                if (UIManager.I != null) UIManager.I.AddCoins(totalRewardCoins);
                onEffectComplete?.Invoke();
                return;
            }

            int initialCoins = UIManager.I.TotalCoins;
            int finalCoins = initialCoins + totalRewardCoins;

            // Luu ngay finalCoins vao PlayerPrefs de khong bao gio bi mat tien
            UIManager.I.SaveTotalCoins(finalCoins);

            int coinCount = UnityEngine.Random.Range(8, 11);
            int arrivedCount = 0;
            Vector3 targetWorldPos = targetHUDIcon.position;

            for (int i = 0; i < coinCount; i++)
            {
                int index = i;
                float delay = index * 0.06f;

                var coin = _pool.Get();
                coin.SetParent(transform, true);
                coin.position = startScreenOrWorldPos;
                coin.localScale = Vector3.zero;

                // 1. Nổ bung nhẹ ra xung quanh với góc ngẫu nhiên
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float burstRadius = UnityEngine.Random.Range(70f, 130f);
                Vector3 burstOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * burstRadius;
                Vector3 burstPos = startScreenOrWorldPos + burstOffset;

                var seq = DOTween.Sequence();
                seq.SetLink(gameObject);
                seq.SetDelay(delay);

                // Pop in and explode
                seq.Append(coin.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetLink(coin.gameObject));
                seq.Join(coin.DOMove(burstPos, 0.25f).SetEase(Ease.OutQuad).SetLink(coin.gameObject));

                // Small pause
                seq.AppendInterval(0.04f);

                // 2. Bay ve phia Icon Tien tren HUD (DOMove)
                float travelTime = UnityEngine.Random.Range(0.42f, 0.50f);
                seq.Append(coin.DOMove(targetWorldPos, travelTime).SetEase(Ease.InCubic).SetLink(coin.gameObject));
                seq.Join(coin.DOScale(0.7f, travelTime).SetLink(coin.gameObject));

                // 3. Khi cham dich
                seq.OnComplete(() =>
                {
                    arrivedCount++;
                    _pool.Release(coin);

                    // Am thanh leng keng kem pitch tang dan tao cam giac cao trao
                    float pitchOffset = (index / (float)coinCount) * 0.35f;
                    if (AudioManager.I != null) AudioManager.I.PlayCoinChime(pitchOffset);
                    if (HapticManager.I != null) HapticManager.I.LightImpact();

                    // Tinh toan tien hien thi tang dan theo ty le phan tram so xu da cham dich
                    int currentStepCoins = Mathf.RoundToInt(Mathf.Lerp(initialCoins, finalCoins, (float)arrivedCount / coinCount));
                    if (UIManager.I.HUD != null)
                    {
                        UIManager.I.HUD.SetCoins(currentStepCoins, true);
                        UIManager.I.HUD.PunchCoinIcon();
                    }

                    if (arrivedCount >= coinCount)
                    {
                        // Dam bao hien thi 100% dung tong so tien cuoi cung
                        if (UIManager.I.HUD != null)
                        {
                            UIManager.I.HUD.SetCoins(finalCoins, false);
                        }

                        // Tao khoang ngung 0.35s de nguoi choi nhin thay so 25 xu ro rang roi moi chuyen sang level tiep theo
                        DOVirtual.DelayedCall(0.35f, () => onEffectComplete?.Invoke()).SetLink(gameObject);
                    }
                });
            }
        }
    }
}
