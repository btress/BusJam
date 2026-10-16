using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    /// <summary>Ben don khach: 2-3 vi tri do (bays) nam giua bai do xe va hang khach.</summary>
    public class BayManager : MonoBehaviour
    {
        public const float Spacing = 2.2f;
        const int MaxBays = 3;

        readonly BusController[] _bus = new BusController[MaxBays];
        Renderer[] _marks;
        int _count;
        float _z;
        static readonly Color Idle = new Color(0.50f, 0.48f, 0.58f);
        static readonly Color Err = new Color(0.95f, 0.25f, 0.25f);

        public Vector3 QueueFront => new Vector3(0, 0, _z + 2.2f);

        void Awake()
        {
            _marks = new Renderer[MaxBays];
            for (int i = 0; i < MaxBays; i++)
            {
                _marks[i] = Visuals.Prim(transform, "Bay" + i, Vector3.zero, new Vector3(1.3f, 0.02f, 3.3f), Idle);
                _marks[i].gameObject.SetActive(false);
            }
        }

        public void Init(int count, float gridTop)
        {
            Clear();
            _count = count;
            _z = gridTop + 2.9f;
            for (int i = 0; i < MaxBays; i++)
            {
                _marks[i].gameObject.SetActive(i < count);
                Visuals.SetColor(_marks[i], Idle);
                if (i < count) _marks[i].transform.position = BayPos(i) + Vector3.up * 0.01f;
            }
        }

        public void Clear() { for (int i = 0; i < MaxBays; i++) _bus[i] = null; }

        public Vector3 BayPos(int i) => new Vector3((i - (_count - 1) * 0.5f) * Spacing, 0f, _z);

        public int FreeCount { get { int n = 0; for (int i = 0; i < _count; i++) if (_bus[i] == null) n++; return n; } }
        public int UsedCount => _count - FreeCount;

        public bool AllArrived
        {
            get
            {
                for (int i = 0; i < _count; i++)
                    if (_bus[i] != null && _bus[i].State != BusController.BusState.AtBay) return false;
                return true;
            }
        }

        public int Reserve(BusController b)
        {
            for (int i = 0; i < _count; i++)
                if (_bus[i] == null) { _bus[i] = b; b.BayIndex = i; return i; }
            return -1;
        }

        public void Free(BusController b)
        {
            if (b.BayIndex >= 0 && b.BayIndex < MaxBays && _bus[b.BayIndex] == b) _bus[b.BayIndex] = null;
            b.BayIndex = -1;
        }

        public BusController FindBoardable(GameColor c)
        {
            BusController best = null;
            for (int i = 0; i < _count; i++)
            {
                var b = _bus[i];
                if (b == null || b.State != BusController.BusState.AtBay || b.ColorID != c || b.Boarded >= BusController.Capacity) continue;
                if (best == null || b.Boarded > best.Boarded) best = b;
            }
            return best;
        }

        public void PlayFullError()
        {
            for (int i = 0; i < _count; i++)
            {
                var r = _marks[i];
                DOTween.Kill(r);
                DOTween.To(v => Visuals.SetColor(r, Color.Lerp(Idle, Err, v)), 0f, 1f, 0.12f)
                    .SetLoops(4, LoopType.Yoyo).SetTarget(r);
            }
        }
    }
}
