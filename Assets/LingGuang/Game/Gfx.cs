using System.Collections.Generic;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Procedural textures, materials and renderer helpers (placeholder art, HDR glow).</summary>
    public static class Gfx
    {
        static Material additive, alpha, neuron, neuronIcon;
        static Sprite disc, ring, thinRing, hex, hexOutline, softDot, square, diamond;
        static Texture2D lineTex;
        static readonly MaterialPropertyBlock mpb = new MaterialPropertyBlock();
        static readonly int ColorId = Shader.PropertyToID("_Color");

        public static Material Neuron
        {
            get
            {
                if (neuron == null)
                {
                    neuron = new Material(Alpha) { name = "LG_CoolNeuron" };
                    neuron.SetFloat("_Monochrome", 1f);
                }
                return neuron;
            }
        }

        public static Material NeuronIcon
        {
            get
            {
                if (neuronIcon == null)
                {
                    neuronIcon = new Material(Neuron) { name = "LG_PixelNeuronIcon" };
                    neuronIcon.SetFloat("_PixelGrid", 32f);
                }
                return neuronIcon;
            }
        }

        public static Material Additive
        {
            get
            {
                if (additive == null)
                {
                    additive = new Material(Shader.Find("LingGuang/Glow")) { name = "LG_Additive" };
                    additive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    additive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                }
                return additive;
            }
        }

        public static Material Alpha
        {
            get
            {
                if (alpha == null)
                {
                    alpha = new Material(Shader.Find("LingGuang/Glow")) { name = "LG_Alpha" };
                    alpha.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    alpha.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                return alpha;
            }
        }

        public static Sprite Disc => disc ??= MakeSprite(128, (x, y) => Mathf.Clamp01((1f - Mathf.Sqrt(x * x + y * y)) * 6f), "disc");
        public static Sprite SoftDot => softDot ??= MakeSprite(128, (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f); }, "softdot");
        public static Sprite Ring => ring ??= MakeSprite(256, (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) * 18f); }, "ring");
        public static Sprite ThinRing => thinRing ??= MakeSprite(256, (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) * 85f); }, "thin-ripple");
        public static Sprite Square => square ??= MakeSprite(8, (x, y) => 1f, "square");
        public static Sprite Diamond => diamond ??= MakeSprite(16, (x, y) => Mathf.Abs(x) + Mathf.Abs(y) <= 1f ? 1f : 0f, "pixel-diamond");
        public static Sprite Hex => hex ??= MakeSprite(256, (x, y) => Mathf.Clamp01((0.95f - HexDist(x, y)) * 40f), "hex");
        public static Sprite HexOutline => hexOutline ??= MakeSprite(256, (x, y) => { float d = HexDist(x, y); return Mathf.Clamp01(1f - Mathf.Abs(d - 0.92f) * 30f); }, "hexoutline");

        public static Texture2D LineTex
        {
            get
            {
                if (lineTex != null) return lineTex;
                lineTex = new Texture2D(4, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "line" };
                for (int y = 0; y < 64; y++)
                {
                    float v = 1f - Mathf.Abs((y + 0.5f) / 64f * 2f - 1f);
                    float a = Mathf.Pow(v, 1.6f);
                    for (int x = 0; x < 4; x++) lineTex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
                lineTex.Apply();
                return lineTex;
            }
        }

        // pointy-top hexagon distance (1 at the corners)
        static float HexDist(float x, float y)
        {
            x = Mathf.Abs(x); y = Mathf.Abs(y);
            // pointy top: vertices at top/bottom
            return Mathf.Max(x, x * 0.5f + y * 0.8660254f) / 0.8660254f;
        }

        static Sprite MakeSprite(int size, System.Func<float, float, float> alphaFn, string name)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = name };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float fx = (x + 0.5f) / size * 2f - 1f, fy = (y + 0.5f) / size * 2f - 1f;
                    byte a = (byte)(Mathf.Clamp01(alphaFn(fx, fy)) * 255);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static SpriteRenderer MakeSprite(string name, Transform parent, Sprite sprite, Material mat, int order, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sharedMaterial = mat;
            sr.sortingOrder = order;
            go.transform.localScale = Vector3.one * size;
            return sr;
        }

        public static void SetColor(Renderer r, Color c)
        {
            r.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, c);
            if (r is SpriteRenderer sr && sr.sprite != null) mpb.SetTexture("_MainTex", sr.sprite.texture);
            r.SetPropertyBlock(mpb);
        }

        public static LineRenderer MakeLine(string name, Transform parent, Material mat, int order, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = mat;
            lr.useWorldSpace = true;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.textureMode = LineTextureMode.Stretch;
            lr.sortingOrder = order;
            lr.alignment = LineAlignment.TransformZ;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            var mpb2 = new MaterialPropertyBlock();
            mpb2.SetTexture("_MainTex", LineTex);
            lr.SetPropertyBlock(mpb2);
            return lr;
        }

        public static void SetLineColor(LineRenderer lr, Color c)
        {
            lr.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, c);
            mpb.SetTexture("_MainTex", LineTex);
            lr.SetPropertyBlock(mpb);
        }

        public static Color HDR(Color c, float intensity)
        {
            return new Color(c.r * intensity, c.g * intensity, c.b * intensity, c.a);
        }
    }

    /// <summary>Tiny generic object pool.</summary>
    public sealed class Pool<T> where T : Component
    {
        readonly System.Func<T> make;
        readonly Stack<T> free = new Stack<T>();
        public readonly List<T> active = new List<T>();
        public Pool(System.Func<T> make) { this.make = make; }
        public T Get()
        {
            var t = free.Count > 0 ? free.Pop() : make();
            t.gameObject.SetActive(true);
            active.Add(t);
            return t;
        }
        public void Release(T t)
        {
            t.gameObject.SetActive(false);
            active.Remove(t);
            free.Push(t);
        }
        public void ReleaseAll()
        {
            for (int i = active.Count - 1; i >= 0; i--) Release(active[i]);
        }
    }
}
