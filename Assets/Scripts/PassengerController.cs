using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BusJam
{
    /// <summary>Hanh khach 3D (model Mixamo + Animator Ch_Anim: Idle / Running / Sitting).</summary>
    public class PassengerController : MonoBehaviour
    {
        public static float StandHeight = 0.9f;
        public static bool ShowSeated = true;       // true: ngoi tren GridPartPrefab (animation Sitting); false: chay vao xe roi bien mat
        public static float SeatedScale = 0.5f;     // ti le thu nho khi ngoi tren xe
        public static float SeatDrop = 0.25f;       // ha nhan vat xuong (ti le chieu cao)
        public static float SeatYaw = 0f;           // huong ngoi so voi huong xe (0 = quay ve dau xe)
        public static Vector3 SeatOffset = Vector3.zero;   // lech them (toa do local cua xe)
        static readonly int IdleH = Animator.StringToHash("Idle");
        static readonly int RunH = Animator.StringToHash("Running");
        static readonly int SitH = Animator.StringToHash("Sitting");

        [HideInInspector] public GameObject modelPrefab;
        [HideInInspector] public float yawOffset;
        public GameColor ColorID { get; private set; }

        Transform _fit;
        Animator _anim;
        readonly List<Renderer> _renderers = new List<Renderer>();

        public void EnsureVisual()
        {
            if (_fit != null) return;
            _fit = new GameObject("Fit").transform; _fit.SetParent(transform, false);

            if (modelPrefab != null)
            {
                var m = Instantiate(modelPrefab, _fit);
                m.transform.localPosition = Vector3.zero;
                foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
                m.transform.rotation = Quaternion.Euler(0, yawOffset, 0) * m.transform.rotation;

                var b = Visuals.GetBounds(m);
                Vector3 c0 = transform.position;
                m.transform.position -= new Vector3(b.center.x - c0.x, b.min.y - c0.y, b.center.z - c0.z);
                float s = StandHeight / Mathf.Max(0.01f, b.size.y);
                _fit.localScale = Vector3.one * s;

                _anim = m.GetComponentInChildren<Animator>();
                if (_anim != null) { _anim.applyRootMotion = false; _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate; }
                _renderers.AddRange(m.GetComponentsInChildren<Renderer>());
            }
            else
            {
                _renderers.Add(Visuals.Prim(_fit, "Body", new Vector3(0, 0.4f, 0), new Vector3(0.4f, 0.4f, 0.4f), Color.white, PrimitiveType.Capsule));
            }
        }

        public void Init(GameColor c, Vector3 pos)
        {
            EnsureVisual();
            transform.DOKill();
            ColorID = c;
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0, 180f, 0);     // quay mat ve phia ben xe (-Z)
            transform.localScale = Vector3.one;
            Color col = Palette.Get(c);
            foreach (var r in _renderers) Visuals.SetColor(r, col);
            Play(IdleH, 0f);
        }

        public void ResetForPool() { transform.DOKill(); }

        void Play(int hash, float fade)
        {
            if (_anim == null || !_anim.isActiveAndEnabled) return;
            if (fade <= 0f) _anim.Play(hash, 0, 0f); else _anim.CrossFadeInFixedTime(hash, fade);
        }

        public void PopIn(float delay)
        {
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(delay);
        }

        public void MoveTo(Vector3 pos, float t)
        {
            transform.DOKill();
            if ((transform.position - pos).sqrMagnitude < 0.0004f) return;
            Play(RunH, 0.08f);
            transform.DOMove(pos, t).SetEase(Ease.Linear).OnComplete(() => Play(IdleH, 0.1f));
        }

        /// <summary>Chay toi xe (Running) roi nhay len GridPartPrefab va ngoi (Sitting)</summary>
        public Sequence BoardBus(BusController bus, int seatIndex)
        {
            transform.DOKill();
            Vector3 seatPos = bus.SeatWorldPos(seatIndex);
            Vector3 ground = new Vector3(seatPos.x, 0f, seatPos.z);
            float dur = Mathf.Clamp(Vector3.Distance(transform.position, ground) / 6f, 0.25f, 0.8f);

            AudioManager.I.Play(Sfx.Step);
            Play(RunH, 0.08f);

            var s = DOTween.Sequence();
            s.Append(transform.DOLookAt(ground, 0.08f, AxisConstraint.Y));
            s.Append(transform.DOMove(ground, dur).SetEase(Ease.Linear));
            if (ShowSeated)
            {
                Vector3 sit = seatPos + bus.transform.rotation * SeatOffset + Vector3.down * (StandHeight * SeatedScale * SeatDrop);
                s.Append(transform.DOJump(sit, 0.25f, 1, 0.25f));
                s.Join(transform.DOScale(SeatedScale, 0.25f));
                s.Join(transform.DORotateQuaternion(bus.transform.rotation * Quaternion.Euler(0, SeatYaw, 0), 0.2f));
                s.AppendCallback(() =>
                {
                    transform.SetParent(bus.transform, true);   // di cung xe khi xe roi ben
                    Play(SitH, 0.1f);
                });
            }
            else s.Append(transform.DOScale(0f, 0.15f));
            s.OnComplete(() =>
            {
                bus.Riders.Add(this);
                bus.MarkBoarded(seatIndex);
                bus.BayBounce();
                AudioManager.I.Play(Sfx.Board);
            });
            return s;
        }
    }
}
