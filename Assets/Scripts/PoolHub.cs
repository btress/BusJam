using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace BusJam
{
    /// <summary>Wrapper quanh UnityEngine.Pool.ObjectPool: prewarm 1 lan luc khoi dong, khong Instantiate/Destroy khi choi.</summary>
    public class ComponentPool<T> where T : Component
    {
        readonly ObjectPool<T> _pool;
        readonly HashSet<T> _active = new HashSet<T>();
        readonly List<T> _tmp = new List<T>();
        readonly Transform _root;

        public ComponentPool(Transform root, Func<T> create, int prewarm, Action<T> onGet = null, Action<T> onRelease = null)
        {
            _root = root;
            _pool = new ObjectPool<T>(
                create,
                t => { t.gameObject.SetActive(true); onGet?.Invoke(t); },
                t => { onRelease?.Invoke(t); t.transform.SetParent(_root, false); t.gameObject.SetActive(false); },
                t => { if (t != null) UnityEngine.Object.Destroy(t.gameObject); },
                true, prewarm, 10000);

            var warm = new List<T>(prewarm);
            for (int i = 0; i < prewarm; i++) warm.Add(_pool.Get());
            foreach (var t in warm) _pool.Release(t);
        }

        public T Get() { var t = _pool.Get(); _active.Add(t); return t; }
        public void Release(T t) { if (t != null && _active.Remove(t)) _pool.Release(t); }
        public void ReleaseAll()
        {
            _tmp.Clear(); _tmp.AddRange(_active);
            for (int i = 0; i < _tmp.Count; i++) Release(_tmp[i]);
        }
    }

    /// <summary>Pool xe buyt (BusController) + pool hanh khach (PassengerController).</summary>
    public class PoolHub : MonoBehaviour
    {
        ComponentPool<BusController> _bus;
        ComponentPool<PassengerController> _pax;

        public void Init(GameObject busModel, GameObject paxModel, float busYaw, float paxYaw)
        {
            _bus = new ComponentPool<BusController>(transform, () =>
            {
                var b = new GameObject("Bus").AddComponent<BusController>();
                b.transform.SetParent(transform, false);
                b.modelPrefab = busModel; b.yawOffset = busYaw;
                b.EnsureVisual();           // Instantiate model chi xay ra luc prewarm
                return b;
            }, 24, null, b => b.ResetForPool());

            _pax = new ComponentPool<PassengerController>(transform, () =>
            {
                var p = new GameObject("Passenger").AddComponent<PassengerController>();
                p.transform.SetParent(transform, false);
                p.modelPrefab = paxModel; p.yawOffset = paxYaw;
                p.EnsureVisual();
                return p;
            }, 48, null, p => p.ResetForPool());
        }

        public BusController GetBus() => _bus.Get();
        public void ReleaseBus(BusController b) => _bus.Release(b);
        public PassengerController GetPassenger() => _pax.Get();
        public void ReleasePassenger(PassengerController p) => _pax.Release(p);

        public void ReleaseAll()
        {
            _pax.ReleaseAll();
            _bus.ReleaseAll();
        }
    }
}
