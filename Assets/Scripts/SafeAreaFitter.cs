using UnityEngine;

namespace BusJam
{
    /// <summary>
    /// Tu dong can chinh RectTransform theo Screen.safeArea
    /// Bao ve cac nut chuc nang, HUD khong bi tai tho, notch, vien duoi che khuat.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [ExecuteAlways]
    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform _rectTransform;
        Rect _lastSafeArea = new Rect(0, 0, 0, 0);
        Vector2Int _lastScreenSize = new Vector2Int(0, 0);
        ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        void Start()
        {
            ApplySafeArea();
        }

        void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastScreenSize.x != Screen.width ||
                _lastScreenSize.y != Screen.height ||
                _lastOrientation != Screen.orientation)
            {
                ApplySafeArea();
            }
        }

        public void ApplySafeArea()
        {
            if (_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Rect safeArea = Screen.safeArea;
            Vector2 minAnchor = safeArea.position;
            Vector2 maxAnchor = minAnchor + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            _rectTransform.anchorMin = minAnchor;
            _rectTransform.anchorMax = maxAnchor;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            _lastSafeArea = safeArea;
            _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;
        }
    }
}
