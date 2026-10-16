using UnityEngine;
using UnityEngine.EventSystems;

namespace BusJam
{
    /// <summary>Cham xe (Physics.Raycast). Bi khoa khi GameManager.IsBusy; chan click xuyen UI bang IsPointerOverGameObject.</summary>
    public class InputHandler : MonoBehaviour
    {
        Camera _cam;

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || gm.IsBusy || !gm.CanPlay) return;
            if (!Input.GetMouseButtonDown(0)) return;   // touch dau tien duoc gia lap thanh chuot

            if (EventSystem.current != null)
            {
                bool overUI = Input.touchCount > 0
                    ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                    : EventSystem.current.IsPointerOverGameObject();
                if (overUI) return;
            }

            if (_cam == null) _cam = Camera.main;
            var ray = _cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 300f))
            {
                var bus = hit.collider.GetComponentInParent<BusController>();
                if (bus != null) gm.OnBusTapped(bus);
            }
        }
    }
}
