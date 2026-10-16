using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    public class CameraShake : MonoBehaviour
    {
        public bool vibrateOnBlocked = true;   // Handheld.Vibrate tren mobile
        Transform _cam; Vector3 _home;

        public void Init(Transform cam) { _cam = cam; _home = cam.position; }

        public void Light()
        {
            Shake(0.12f, 0.15f);
#if UNITY_ANDROID || UNITY_IOS
            if (vibrateOnBlocked) Handheld.Vibrate();
#endif
        }

        public void Strong() => Shake(0.3f, 0.25f);

        void Shake(float strength, float dur)
        {
            _cam.DOKill();
            _cam.position = _home;
            _cam.DOShakePosition(dur, new Vector3(strength, strength, 0f), 22, 90f, false, true)
                .OnKill(() => _cam.position = _home);
        }
    }
}
