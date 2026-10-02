using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuang.Game
{
    /// <summary>Original paper/ink HUD artwork; no assets extracted from The Binding of Isaac.</summary>
    public static class IsaacHudSkin
    {
        public static readonly Color Paper = new Color(.83f, .76f, .61f);
        public static readonly Color PaperLight = new Color(.94f, .87f, .72f);
        public static readonly Color Ink = new Color(.16f, .13f, .12f);
        public static readonly Color Chalk = new Color(.94f, .91f, .83f);
        public static readonly Color Muted = new Color(.68f, .65f, .59f);
        public static readonly Color Red = new Color(.59f, .24f, .22f);
        public static readonly Color Gold = new Color(.91f, .71f, .31f);
        static Sprite paper;
        static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        public static Sprite PaperSprite => paper != null ? paper : paper = CreatePaper();

        static Sprite CreatePaper()
        {
            const int w = 128, h = 96;
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float left = 3 + Mathf.Floor(1.6f * Mathf.Sin(y * .61f) + 1.4f * Mathf.Sin(y * .19f));
                float right = 4 + Mathf.Floor(1.4f * Mathf.Sin(y * .47f + 2));
                float bottom = 3 + Mathf.Floor(1.8f * Mathf.Sin(x * .39f));
                float top = 4 + Mathf.Floor(1.5f * Mathf.Sin(x * .57f + 3));
                float edge = Mathf.Min(Mathf.Min(x - left, w - 1 - x - right), Mathf.Min(y - bottom, h - 1 - y - top));
                if (edge < 0) continue;
                uint hash = unchecked((uint)(x * 374761393 + y * 668265263));
                float grain = (hash % 101) / 100f;
                float v = edge < 2 ? .14f : edge < 3 ? .30f : .98f + grain * .02f;
                // Sparse graphite flecks, not a busy tiled texture under text.
                if (edge > 6 && hash % 181 == 0) v *= .80f;
                pixels[y * w + x] = new Color(v, v, v, 1);
            }
            return SpriteFrom(pixels, w, h, "LG torn paper", new Vector4(12, 12, 12, 12));
        }

        static Sprite SpriteFrom(Color[] pixels, int w, int h, string name, Vector4 border)
        {
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
            { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            texture.SetPixels(pixels); texture.Apply(false, true);
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, border);
            sprite.name = name; sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }

        public static void ApplyPaper(Image image, Color color)
        {
            image.sprite = PaperSprite; image.type = Image.Type.Sliced; image.color = color;
        }

        public static void Outline(TextMeshProUGUI text)
        {
            text.fontStyle |= FontStyles.Bold;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.015f, .012f, .015f, .95f);
            shadow.effectDistance = new Vector2(2, -2);
            shadow.useGraphicAlpha = true;
        }

        public static string ItemGlyph(string id)
        {
            switch (id)
            {
                case "headphones": case "earphones": return "headphones";
                case "computer": return "computer";
                case "bicycle": return "bike";
                case "oldphoto": case "old_photo": case "photo": return "photo";
                case "homework": case "diary": case "loveletter": case "love_letter": return "book";
                default: return "item";
            }
        }

        public static Sprite Icon(string kind)
        {
            if (icons.TryGetValue(kind, out var sprite) && sprite != null) return sprite;
            const int size = 40;
            var p = new Color[size * size];
            void Dot(float cx, float cy, float r, Color c)
            {
                for (int y = Mathf.Max(0, Mathf.FloorToInt(cy-r)); y <= Mathf.Min(size-1, Mathf.CeilToInt(cy+r)); y++)
                    for (int x = Mathf.Max(0, Mathf.FloorToInt(cx-r)); x <= Mathf.Min(size-1, Mathf.CeilToInt(cx+r)); x++)
                        if ((x-cx)*(x-cx)+(y-cy)*(y-cy) <= r*r) p[y*size+x]=c;
            }
            void Stroke(float x1, float y1, float x2, float y2, Color c, float width = 2)
            {
                int steps = Mathf.CeilToInt(Vector2.Distance(new Vector2(x1,y1),new Vector2(x2,y2))*2);
                for (int i=0;i<=steps;i++) { float t=steps>0?i/(float)steps:0; Dot(Mathf.Lerp(x1,x2,t),Mathf.Lerp(y1,y2,t),width,c); }
            }
            void Line(float x1,float y1,float x2,float y2)
            { Stroke(x1,y1,x2,y2,Ink,3); Stroke(x1,y1,x2,y2,Chalk,1.4f); }
            void Ring(float cx,float cy,float r,Color color)
            { Dot(cx,cy,r+2,Ink); Dot(cx,cy,r,color); Dot(cx,cy,r-2,Ink); }
            switch(kind)
            {
                case "coin": Dot(20,20,14,Ink);Dot(20,20,11,Gold);Stroke(18,13,21,28,Ink,1.5f);Stroke(13,27,16,30,PaperLight,1);break;
                case "charge":
                    Line(23,34,10,18);Line(10,18,22,19);Line(22,19,16,5);Line(16,5,30,24);Line(30,24,19,23);break;
                case "move":
                    Line(8,11,30,11);Line(30,11,23,18);Line(30,11,23,5);Dot(12,28,6,Ink);Dot(12,28,3,Chalk);Line(18,31,30,28);break;
                case "eye":
                    Line(4,20,14,29);Line(14,29,26,29);Line(26,29,36,20);Line(36,20,25,11);Line(25,11,14,11);Line(14,11,4,20);Dot(20,20,6,Ink);Dot(20,20,3,Chalk);break;
                case "headphones":
                    Line(8,12,8,25);Line(8,25,14,32);Line(14,32,25,32);Line(25,32,32,25);Line(32,25,32,12);Stroke(10,11,10,20,Ink,5);Stroke(10,11,10,20,Chalk,2);Stroke(30,11,30,20,Ink,5);Stroke(30,11,30,20,Chalk,2);break;
                case "computer":
                    Line(6,14,6,32);Line(6,32,34,31);Line(34,31,34,14);Line(34,14,6,14);Line(20,14,20,7);Line(12,6,28,6);break;
                case "bike":
                    Ring(10,12,6,Chalk);Ring(30,12,6,Chalk);Line(10,12,17,24);Line(17,24,26,12);Line(26,12,10,12);Line(30,12,26,29);Line(26,29,31,30);Line(13,26,20,26);break;
                case "pill":
                    Line(11,9,7,16);Line(7,16,22,33);Line(22,33,30,33);Line(30,33,34,26);Line(34,26,18,8);Line(18,8,11,9);Line(14,24,27,17);break;
                case "photo":
                    Line(6,6,5,33);Line(5,33,34,34);Line(34,34,33,6);Line(33,6,6,6);Line(10,12,18,23);Line(18,23,27,12);Dot(26,27,3,Chalk);break;
                case "book":
                    Line(7,7,7,32);Line(7,32,30,34);Line(30,34,32,7);Line(32,7,7,7);Line(12,9,12,30);Line(18,25,25,25);break;
                default:
                    Line(20,34,32,21);Line(32,21,21,6);Line(21,6,7,20);Line(7,20,20,34);Line(13,20,26,20);Line(20,12,20,28);break;
            }
            sprite=SpriteFrom(p,size,size,"LG doodle "+kind,Vector4.zero);icons[kind]=sprite;return sprite;
        }
    }
}
