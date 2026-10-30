using UnityEngine;

namespace BusJam
{
    /// <summary>
    /// Dieu khien rung Haptic tren thiet bi di dong ket hop phan hoi rung camera.
    /// Kiem tra co IsHapticOn truoc khi kich hoat bat ky rung dong nao.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager I { get; private set; }

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
        }

        public void LightImpact()
        {
            if (SettingsManager.I != null && !SettingsManager.I.IsHapticOn) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            if (CameraShake.I != null) CameraShake.I.Light();
        }

        public void Success()
        {
            if (SettingsManager.I != null && !SettingsManager.I.IsHapticOn) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            if (CameraShake.I != null) CameraShake.I.Strong();
        }

        public void Warning()
        {
            if (SettingsManager.I != null && !SettingsManager.I.IsHapticOn) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            if (CameraShake.I != null) CameraShake.I.Light();
        }
    }
}
