using UnityEngine;

namespace BusJam
{
    /// <summary>Anh nen gan vao camera (xa 150 don vi, nam sau moi vat the). Phu kin man hinh (cover), can sat mep tren.</summary>
    public class BackgroundController : MonoBehaviour
    {
        const float _pixelsPerUnit = 100f;
        Camera _cam;
        Transform _quad;
        float _sw, _sh;
        int _lastW, _lastH;

        public void Init(Camera cam, Texture2D tex)
        {
            if (tex == null) return;
            _cam = cam;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), _pixelsPerUnit);
            var go = new GameObject("BackgroundImage");
            _quad = go.transform;
            _quad.SetParent(cam.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -1000;
            _sw = tex.width / _pixelsPerUnit; _sh = tex.height / _pixelsPerUnit;
            Fit();
        }

        void LateUpdate()
        {
            if (_quad != null && (Screen.width != _lastW || Screen.height != _lastH)) Fit();
        }

        void Fit()
        {
            _lastW = Screen.width; _lastH = Screen.height;
            float viewH = _cam.orthographicSize * 2f;
            float viewW = viewH * _cam.aspect;
            float s = Mathf.Max(viewW / _sw, viewH / _sh);              // cover
            float y = _sh * s > viewH ? viewH * 0.5f - _sh * s * 0.5f : 0f;   // anh cao hon man hinh -> can mep tren
            _quad.localScale = new Vector3(s, s, 1f);
            _quad.localPosition = new Vector3(0f, y, 150f);
        }
    }
}
