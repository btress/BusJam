using System.Collections.Generic;
using UnityEngine;

namespace BusJam
{
    /// <summary>Hang doi hanh khach (List<PassengerController>). Chi hien toi da MaxVisible nguoi, nguoi tiep theo spawn khi co cho.</summary>
    public class PassengerQueue : MonoBehaviour
    {
        const int MaxVisible = 5;
        const float Spacing = 0.65f;

        readonly List<PassengerController> _visible = new List<PassengerController>();
        List<GameColor> _order;
        int _spawned, _consumed;
        Vector3 _front;

        public int Remaining => _order == null ? 0 : _order.Count - _consumed;
        Vector3 Slot(int i) => _front + Vector3.forward * (Spacing * i);

        public void Init(List<GameColor> order, Vector3 front)
        {
            Clear();
            _order = order; _front = front;
            int n = Mathf.Min(MaxVisible, order.Count);
            for (int i = 0; i < n; i++) Spawn(i, i * 0.05f);
        }

        public void Clear() { _visible.Clear(); _spawned = 0; _consumed = 0; }

        void Spawn(int slot, float delay)
        {
            var p = GameManager.I.Pools.GetPassenger();
            p.Init(_order[_spawned++], Slot(slot));
            p.PopIn(delay);
            _visible.Add(p);
        }

        public PassengerController Peek() => _visible.Count > 0 ? _visible[0] : null;

        public PassengerController Dequeue()
        {
            var p = _visible[0];
            _visible.RemoveAt(0);
            _consumed++;
            for (int i = 0; i < _visible.Count; i++) _visible[i].MoveTo(Slot(i), 0.25f);
            if (_spawned < _order.Count) Spawn(_visible.Count, 0.1f);
            return p;
        }
    }
}
