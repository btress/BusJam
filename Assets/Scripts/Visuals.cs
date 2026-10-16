using UnityEngine;

namespace BusJam
{
    /// <summary>Helper hien thi 3D: primitive co mau (MaterialPropertyBlock, khong tao material moi) + vat lieu particle.</summary>
    public static class Visuals
    {
        static MaterialPropertyBlock _mpb;
        static Texture2D _circleTex;
        static Material _matSquare, _matCircle;

        /// <summary>Doi mau (ho tro ca Built-in _Color va URP _BaseColor). mat &lt; 0 = tat ca material.</summary>
        public static void SetColor(Renderer r, Color c, int mat = -1)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();
            _mpb.Clear();
            _mpb.SetColor("_Color", c);
            _mpb.SetColor("_BaseColor", c);
            if (mat < 0) r.SetPropertyBlock(_mpb); else r.SetPropertyBlock(_mpb, mat);
        }

        public static Renderer Prim(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, PrimitiveType t = PrimitiveType.Cube)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            SetColor(r, color);
            return r;
        }

        public static Bounds GetBounds(GameObject g)
        {
            Bounds b = new Bounds(); bool init = false;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer) continue;
                if (!init) { b = r.bounds; init = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        static Texture2D CircleTex
        {
            get
            {
                if (_circleTex == null)
                {
                    const int n = 64;
                    _circleTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                            _circleTex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((n / 2f - 1f - d) / 2f)));
                        }
                    _circleTex.Apply();
                }
                return _circleTex;
            }
        }

        public static Material ParticleMaterial(bool circle)
        {
            if (circle)
            {
                if (_matCircle == null) _matCircle = new Material(Shader.Find("Sprites/Default")) { mainTexture = CircleTex };
                return _matCircle;
            }
            if (_matSquare == null) _matSquare = new Material(Shader.Find("Sprites/Default"));
            return _matSquare;
        }
    }
}
