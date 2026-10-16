using System;
using UnityEngine;

namespace BusJam
{
    public enum FxType { Smoke, Spark, Confetti }

    public class PooledParticle : MonoBehaviour
    {
        public ParticleSystem ps;
        public FxType type;
        public Action<PooledParticle> onStopped;
        void OnParticleSystemStopped() { onStopped?.Invoke(this); }
    }

    /// <summary>Pool VFX (khoi/bui, tia lua, phao giay). Cac particle tu tra ve pool khi ket thuc (stopAction = Callback).</summary>
    public class ParticlePool : MonoBehaviour
    {
        ComponentPool<PooledParticle>[] _pools;

        public void Init()
        {
            _pools = new[] { Create(FxType.Smoke, 24), Create(FxType.Spark, 12), Create(FxType.Confetti, 6) };
        }

        ComponentPool<PooledParticle> Create(FxType t, int n) => new ComponentPool<PooledParticle>(transform, () => Build(t), n);

        public void Play(FxType t, Vector3 pos, Color? tint = null)
        {
            var p = _pools[(int)t].Get();
            p.transform.position = pos;
            if (tint.HasValue) { var m = p.ps.main; m.startColor = tint.Value; }
            p.ps.Clear();
            p.ps.Play();
        }

        public void ReleaseAll() { foreach (var p in _pools) p.ReleaseAll(); }

        PooledParticle Build(FxType type)
        {
            var go = new GameObject("Fx_" + type);
            go.SetActive(false); // tranh tu Play truoc khi cau hinh xong
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var pp = go.AddComponent<PooledParticle>();
            pp.ps = ps; pp.type = type;
            pp.onStopped = p => _pools[(int)p.type].Release(p);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Callback;

            var em = ps.emission;
            em.rateOverTime = 0;
            var shape = ps.shape;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sortingOrder = 50;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);   // Circle nam tren mat phang XZ; Cone phun len +Y

            switch (type)
            {
                case FxType.Smoke:
                    main.duration = 0.5f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.85f, 0.85f, 0.7f), new Color(0.55f, 0.55f, 0.55f, 0.5f));
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)5) });
                    shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.12f;
                    var fade = ps.colorOverLifetime; fade.enabled = true; fade.color = FadeGradient();
                    var grow = ps.sizeOverLifetime; grow.enabled = true;
                    grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.7f));
                    r.sharedMaterial = Visuals.ParticleMaterial(true);
                    break;

                case FxType.Spark:
                    main.duration = 0.3f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.4f), new Color(1f, 0.55f, 0.1f));
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10) });
                    shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.05f;
                    var sf = ps.colorOverLifetime; sf.enabled = true; sf.color = FadeGradient();
                    r.sharedMaterial = Visuals.ParticleMaterial(true);
                    break;

                default: // Confetti
                    main.duration = 2.2f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.2f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 11f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    main.gravityModifier = 0.9f;
                    var grad = new Gradient();
                    var keys = new GradientColorKey[Palette.ColorCount];
                    for (int i = 0; i < keys.Length; i++) keys[i] = new GradientColorKey(Palette.All[i], i / (float)(keys.Length - 1));
                    grad.SetKeys(keys, new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, 1) });
                    var mg = new ParticleSystem.MinMaxGradient(grad);
                    mg.mode = ParticleSystemGradientMode.RandomColor;
                    main.startColor = mg;
                    em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)70) });
                    shape.shapeType = ParticleSystemShapeType.Cone; shape.angle = 35f; shape.radius = 0.3f;
                    var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
                    var cf = ps.colorOverLifetime; cf.enabled = true; cf.color = FadeGradient(0.7f);
                    r.sharedMaterial = Visuals.ParticleMaterial(false);
                    break;
            }
            return pp;
        }

        static Gradient FadeGradient(float fadeStart = 0f)
        {
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(1, fadeStart), new GradientAlphaKey(0, 1) });
            return g;
        }
    }
}
