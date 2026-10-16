using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    /// <summary>Xe buyt 3D (model Buss_export). Dau xe = +Z local. Tu dong can chinh model: xoay theo truc dai, can giua, scale theo chieu dai xe.</summary>
    public class BusController : MonoBehaviour
    {
        public const int Capacity = 3;
        public static float Speed = 16f;
        public static string TintKeywords = "";
        public static bool TintBody = true;
        public enum BusState { Parked, Moving, AtBay, Leaving }

        [HideInInspector] public GameObject modelPrefab;
        [HideInInspector] public float yawOffset;
        [HideInInspector] public Transform visual;
        [HideInInspector] public Transform[] seats;

        public GameColor ColorID { get; private set; }
        public Direction Dir { get; private set; }
        public int Length { get; private set; }
        public Vector2Int Tail { get; private set; }
        public Vector2Int Head => Tail + Dir.ToVec() * (Length - 1);

        [HideInInspector] public BusState State;
        [HideInInspector] public int Boarded;
        [HideInInspector] public int BayIndex = -1;
        public readonly List<PassengerController> Riders = new List<PassengerController>();

        BoxCollider _col;
        Transform _fit;
        Renderer _stripe;
        Renderer[] _marks;
        float _len = 1f, _wid = 0.8f, _hei = 0.5f;      // kich thuoc model sau khi xoay/can giua (chua scale)
        float _curHei = 0.5f;
        readonly List<(Renderer r, int i)> _tint = new List<(Renderer, int)>();
        Vector3 _home, _lastSmoke;
        float _smokeAcc;
        Sequence _shake;

        bool _customSeats;

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public void EnsureVisual()
        {
            if (_col == null) { _col = GetComponent<BoxCollider>(); if (_col == null) _col = gameObject.AddComponent<BoxCollider>(); }
            if (visual != null) return;

            visual = new GameObject("Visual").transform; visual.SetParent(transform, false);
            _fit = new GameObject("Fit").transform; _fit.SetParent(visual, false);
            seats = new Transform[Capacity];
            _marks = new Renderer[Capacity];

            if (modelPrefab != null)
            {
                var m = Instantiate(modelPrefab, _fit);
                m.transform.localPosition = Vector3.zero;
                foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);

                // Cau truc BusPrefab: Visual / FrontPoint / SeatPoints(Seat_i/GridPartPrefab) / DirectionArrow
                Transform visNode = FindDeep(m.transform, "Visual");
                Transform front = FindDeep(m.transform, "FrontPoint");
                Transform arrow = FindDeep(m.transform, "DirectionArrow");
                Transform seatRoot = FindDeep(m.transform, "SeatPoints");
                if (arrow != null) arrow.gameObject.SetActive(false);
                GameObject boundsRoot = visNode != null ? visNode.gameObject : m;

                var b = Visuals.GetBounds(boundsRoot);
                float yaw;
                if (front != null && new Vector2(front.position.x - b.center.x, front.position.z - b.center.z).magnitude > 0.01f)
                    yaw = Vector3.SignedAngle(new Vector3(front.position.x - b.center.x, 0, front.position.z - b.center.z), Vector3.forward, Vector3.up);   // FrontPoint -> +Z
                else
                    yaw = b.size.x > b.size.z ? 90f : 0f;
                m.transform.rotation = Quaternion.Euler(0, yaw + yawOffset, 0) * m.transform.rotation;
                b = Visuals.GetBounds(boundsRoot);
                Vector3 c0 = transform.position;
                m.transform.position -= new Vector3(b.center.x - c0.x, b.min.y - c0.y, b.center.z - c0.z);
                _len = Mathf.Max(0.01f, b.size.z); _wid = Mathf.Max(0.01f, b.size.x); _hei = Mathf.Max(0.01f, b.size.y);

                // ghe: dung GridPartPrefab trong model lam cho ngoi
                if (seatRoot != null)
                {
                    int found = 0;
                    for (int i = 0; i < Capacity; i++)
                    {
                        var seat = FindDeep(seatRoot, "Seat_" + i);
                        if (seat == null) break;
                        var part = FindDeep(seat, "GridPartPrefab");
                        seats[i] = part != null ? part : seat;
                        found++;
                    }
                    _customSeats = found == Capacity;
                }

                _tint.Clear();
                if (TintBody)
                    foreach (var r in m.GetComponentsInChildren<Renderer>())
                    {
                        if (seatRoot != null && r.transform.IsChildOf(seatRoot)) continue;
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++) if (Tintable(mats[i])) _tint.Add((r, i));
                    }
            }
            else
            {
                Visuals.Prim(_fit, "Body", new Vector3(0, 0.2f, 0), new Vector3(0.8f, 0.4f, 1f), Color.white);
                _len = 1f; _wid = 0.8f; _hei = 0.4f;
            }

            _stripe = Visuals.Prim(visual, "ColorStripe", Vector3.zero, Vector3.one, Color.white);
            for (int i = 0; i < Capacity; i++)
            {
                _marks[i] = Visuals.Prim(visual, "SeatMark" + i, Vector3.zero, new Vector3(0.22f, 0.01f, 0.22f), Color.white, PrimitiveType.Cylinder);
                if (_customSeats) _marks[i].gameObject.SetActive(false);
                else { var st = new GameObject("Seat" + i).transform; st.SetParent(transform, false); seats[i] = st; }
            }
        }

        static bool Tintable(Material m)
        {
            if (m == null) return false;
            string n = m.name.ToLowerInvariant();
            if (!string.IsNullOrEmpty(TintKeywords))
                foreach (var k in TintKeywords.Split(','))
                { var kk = k.Trim().ToLowerInvariant(); if (kk.Length > 0 && n.Contains(kk)) return true; }
            Color c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
            Color.RGBToHSV(c, out _, out float s, out float v);
            return s > 0.35f && v > 0.3f && m.mainTexture == null;     // vat lieu mau don sac (vd: than xe xanh la)
        }

        public void Init(BusData d, GridManager grid, float popDelay)
        {
            EnsureVisual();
            ResetForPool();
            Tail = d.tail; Dir = d.direction; Length = d.length; ColorID = d.colorID;
            State = BusState.Parked; Boarded = 0; BayIndex = -1; Riders.Clear();

            _home = (grid.CellCenter(Tail) + grid.CellCenter(Head)) * 0.5f;
            transform.position = _home;
            transform.rotation = Quaternion.Euler(0, Dir.ToAngle(), 0);
            visual.localScale = Vector3.one;

            // scale theo chieu dai xe (2 hoac 3 o)
            float target = Length * 0.95f;
            float s = Mathf.Min(target / _len, 0.9f / _wid);
            float sz = target / (_len * s);
            _fit.localScale = new Vector3(s, s, s * sz);
            float len = _len * s * sz, wid = _wid * s;
            _curHei = _hei * s;

            Color col = Palette.Get(ColorID);
            Visuals.SetColor(_stripe, col);
            _stripe.transform.localPosition = new Vector3(0, 0, 0);
            _stripe.transform.localScale = new Vector3(wid * 0.35f, 0.05f, len * 0.6f);
            foreach (var (r, i) in _tint) Visuals.SetColor(r, col, i);

            float spacing = Mathf.Max(0.3f, (len - 0.7f) * 0.5f);
            for (int i = 0; i < Capacity; i++)
            {
                if (!_customSeats) seats[i].localPosition = new Vector3(0, _curHei * 0.4f, (i - 1) * spacing);
                _marks[i].transform.localPosition = new Vector3(0, _curHei + 0.06f, (i - 1) * spacing);
                Visuals.SetColor(_marks[i], Color.white);
            }

            _col.center = new Vector3(0, Mathf.Max(_curHei, 0.4f) * 0.5f, 0);
            _col.size = new Vector3(Mathf.Max(wid, 0.8f), Mathf.Max(_curHei, 0.4f), len);
            _col.enabled = true;

            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetDelay(popDelay);
        }

        public void ResetForPool()
        {
            transform.DOKill();
            if (visual != null) visual.DOKill();
            _shake = null;
            if (_col != null) _col.enabled = false;
        }

        public Vector3 SeatWorldPos(int i) => seats[i].position;
        public void MarkBoarded(int i) { if (_marks != null && i >= 0 && i < Capacity) Visuals.SetColor(_marks[i], new Color(0.12f, 0.12f, 0.12f)); }

        Vector3 FwdVec() => new Vector3(Dir.ToVec().x, 0f, Dir.ToVec().y);

        // ---------- Blocked feedback ----------
        public void PlayBlocked(BusController blocker)
        {
            if (_shake != null && _shake.IsActive()) return;
            transform.DOKill();
            transform.position = _home;
            Vector3 dir = FwdVec();

            _shake = DOTween.Sequence();
            _shake.Append(transform.DOMove(_home + dir * 0.2f, 0.07f).SetEase(Ease.OutQuad));
            _shake.AppendCallback(() =>
            {
                GameManager.I.Fx.Play(FxType.Spark, transform.position + dir * (Length * 0.5f + 0.1f) + Vector3.up * 0.4f);
                AudioManager.I.Play(Sfx.Bump);
                GameManager.I.Shake.Light();
                if (blocker != null) blocker.Punch(dir);
            });
            _shake.Append(transform.DOMove(_home, 0.2f).SetEase(Ease.OutBounce));
            _shake.Join(visual.DOShakePosition(0.2f, 0.06f, 25));
            _shake.OnKill(() => { if (State == BusState.Parked) transform.position = _home; });
        }

        public void Punch(Vector3 dir)
        {
            if (visual == null) return;
            visual.DOKill(true);
            visual.DOPunchPosition(dir * 0.12f, 0.2f, 8, 0.5f);
        }

        // ---------- Squash & stretch ----------
        public void Squash()
        {
            visual.DOKill();
            visual.localScale = Vector3.one;
            var s = DOTween.Sequence();
            s.Append(visual.DOScale(new Vector3(1.12f, 0.85f, 0.9f), 0.08f));
            s.Append(visual.DOScale(Vector3.one, 0.22f).SetEase(Ease.OutElastic));
        }

        public void BayBounce()
        {
            visual.DOKill(true);
            visual.DOPunchScale(new Vector3(0.06f, -0.08f, 0.04f), 0.2f, 6, 1f);
        }

        void EmitSmoke()
        {
            var p = transform.position;
            _smokeAcc += Vector3.Distance(p, _lastSmoke);
            _lastSmoke = p;
            if (_smokeAcc > 0.7f)
            {
                _smokeAcc = 0;
                GameManager.I.Fx.Play(FxType.Smoke, p - transform.forward * (Length * 0.5f) + Vector3.up * 0.25f);
            }
        }

        // ---------- Parking lot -> bay ----------
        List<Vector3> BuildPath(GridManager g, Vector3 bay, float laneZ)
        {
            var pts = new List<Vector3>(5);
            Vector3 p = transform.position;
            float half = Length * 0.5f + 0.1f;
            switch (Dir)
            {
                case Direction.Up:
                    pts.Add(new Vector3(p.x, 0, g.Top + half));
                    pts.Add(new Vector3(bay.x, 0, g.Top + half));
                    break;
                case Direction.Right:
                    pts.Add(new Vector3(g.Right + half, 0, p.z));
                    pts.Add(new Vector3(g.Right + half, 0, laneZ));
                    pts.Add(new Vector3(bay.x, 0, laneZ));
                    break;
                case Direction.Left:
                    pts.Add(new Vector3(g.Left - half, 0, p.z));
                    pts.Add(new Vector3(g.Left - half, 0, laneZ));
                    pts.Add(new Vector3(bay.x, 0, laneZ));
                    break;
                default: // Down
                    float sideX = p.x >= (g.Left + g.Right) * 0.5f ? g.Right + 1f : g.Left - 1f;
                    pts.Add(new Vector3(p.x, 0, g.Bottom - half));
                    pts.Add(new Vector3(sideX, 0, g.Bottom - half));
                    pts.Add(new Vector3(sideX, 0, laneZ));
                    pts.Add(new Vector3(bay.x, 0, laneZ));
                    break;
            }
            pts.Add(bay);
            return pts;
        }

        public void LaunchToBay(GridManager g, Vector3 bay, float laneZ, Action onArrived)
        {
            transform.DOKill();
            if (_col != null) _col.enabled = false;
            State = BusState.Moving;

            var pts = BuildPath(g, bay, laneZ);
            float len = 0; Vector3 prev = transform.position;
            for (int i = 0; i < pts.Count; i++) { len += Vector3.Distance(prev, pts[i]); prev = pts[i]; }
            float dur = Mathf.Max(0.35f, len / Speed);

            Squash();
            AudioManager.I.Play(Sfx.Engine);
            GameManager.I.Fx.Play(FxType.Smoke, transform.position - transform.forward * (Length * 0.5f) + Vector3.up * 0.25f);
            _lastSmoke = transform.position; _smokeAcc = 0;

            var seq = DOTween.Sequence();
            seq.Append(transform.DOPath(pts.ToArray(), dur, PathType.Linear).SetEase(Ease.InSine).OnUpdate(EmitSmoke));
            seq.Insert(Mathf.Max(0f, dur - 0.25f), transform.DORotateQuaternion(Quaternion.identity, 0.25f));
            seq.OnComplete(() =>
            {
                State = BusState.AtBay;
                transform.position = bay;
                transform.rotation = Quaternion.identity;     // dau xe huong +Z (ve phia hang khach)
                BayBounce();
                onArrived?.Invoke();
            });
        }

        // ---------- Bay -> leave (full) ----------
        public void DepartFromBay(float laneZ, Action onDone)
        {
            transform.DOKill();
            State = BusState.Leaving;
            Vector3 p = transform.position;
            float sign = p.x >= 0 ? 1f : -1f;
            float exitX = sign * 13f;
            var pts = new[] { new Vector3(p.x, 0, laneZ), new Vector3(exitX, 0, laneZ) };
            float len = Mathf.Abs(p.z - laneZ) + Mathf.Abs(exitX - p.x);
            float dur = len / (Speed * 0.9f);

            Squash();
            AudioManager.I.Play(Sfx.Honk);
            AudioManager.I.Play(Sfx.Engine);
            GameManager.I.Shake.Strong();
            GameManager.I.Fx.Play(FxType.Smoke, p - transform.forward * (Length * 0.5f) + Vector3.up * 0.25f);
            _lastSmoke = p; _smokeAcc = 0;

            var seq = DOTween.Sequence();
            seq.Append(transform.DOPath(pts, dur, PathType.Linear).SetEase(Ease.InQuad).OnUpdate(EmitSmoke));
            seq.Insert(0f, transform.DORotateQuaternion(Quaternion.Euler(0, sign > 0 ? 90f : 270f, 0), 0.25f));
            seq.OnComplete(() => onDone?.Invoke());
        }
    }
}
