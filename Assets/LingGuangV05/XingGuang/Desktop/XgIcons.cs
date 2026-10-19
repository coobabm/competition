using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>
    /// The game's drawn icon set: Resources/LingGuangV05/Icons/&lt;group&gt;/&lt;id&gt;.png, made by Tools/Icons (one script per
    /// group, all on iconkit.py so they share one glossy look). Ids may contain dots ("ph.xor"); the file swaps them for
    /// underscores. A missing file returns null, so every caller keeps its old drawn shape as the fallback.
    /// </summary>
    public static class XgIcons
    {
        const string Root = "LingGuangV05/Icons/";
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static string FileName(string id) => id == null ? null : id.Replace('.', '_').Replace('/', '_').Replace(' ', '_');

        public static Sprite Get(string group, string id)
        {
            if (string.IsNullOrEmpty(group) || string.IsNullOrEmpty(id)) return null;
            string key = group + "/" + FileName(id);
            if (cache.TryGetValue(key, out var sprite)) return sprite;
            var texture = group=="nav"||group=="hub"?OriginalLineIcon(group,id):null;
            if(texture==null)texture = Resources.Load<Texture2D>(Root + key);
            sprite = texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            if (sprite != null) sprite.name = "icon_" + group + "_" + FileName(id);
            cache[key] = sprite;
            return sprite;
        }

        // Original game artwork: navigation and board-menu symbols share one rounded stroke style.
        // Rasterised once and cached with the other icons; no external art or emoji font dependency.
        static Texture2D OriginalLineIcon(string group, string id)
        {
            Vector4[] lines;Vector3[] rings;
            switch(group+"/"+id)
            {
                case "nav/teach":
                    lines=new[]{new Vector4(10,15,10,46),new Vector4(10,46,31,51),new Vector4(31,51,54,46),new Vector4(54,46,54,15),new Vector4(54,15,32,20),new Vector4(32,20,10,15),new Vector4(32,20,32,50),new Vector4(17,36,25,38),new Vector4(39,38,47,36)};
                    rings=new Vector3[0];break;
                case "nav/train":
                    lines=new[]{new Vector4(11,51,11,13),new Vector4(11,13,54,13),new Vector4(18,22,28,25),new Vector4(28,25,36,36),new Vector4(36,36,45,33),new Vector4(45,33,53,49)};
                    rings=new[]{new Vector3(53,49,3)};break;
                case "nav/tree":
                    lines=new[]{new Vector4(32,45,18,22),new Vector4(32,45,47,22),new Vector4(18,22,47,22)};
                    rings=new[]{new Vector3(32,48,7),new Vector3(16,18,7),new Vector3(49,18,7)};break;
                case "nav/home":
                    lines=new[]{new Vector4(23,37,23,33),new Vector4(41,37,41,33),new Vector4(24,23,32,20),new Vector4(32,20,40,23)};
                    rings=new[]{new Vector3(32,32,23)};break;
                case "hub/ludo":
                    lines=new[]{new Vector4(32,8,32,56),new Vector4(10,24,32,38),new Vector4(32,38,54,24),new Vector4(22,10,32,16),new Vector4(32,16,42,10)};
                    rings=new Vector3[0];break;
                case "hub/checkers_cn":
                    lines=new[]{new Vector4(32,56,10,18),new Vector4(10,18,54,18),new Vector4(54,18,32,56),new Vector4(32,8,10,46),new Vector4(10,46,54,46),new Vector4(54,46,32,8)};
                    rings=new Vector3[0];break;
                default:return null;
            }
            const int size=64;var colors=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var point=new Vector2(x+.5f,y+.5f);float distance=100;
                foreach(var line in lines)
                {
                    var a=new Vector2(line.x,line.y);var b=new Vector2(line.z,line.w);var d=b-a;
                    float t=Mathf.Clamp01(Vector2.Dot(point-a,d)/Mathf.Max(.001f,d.sqrMagnitude));
                    distance=Mathf.Min(distance,Vector2.Distance(point,a+t*d));
                }
                foreach(var circle in rings)distance=Mathf.Min(distance,Mathf.Abs(Vector2.Distance(point,new Vector2(circle.x,circle.y))-circle.z));
                float alpha=Mathf.Clamp01(2.2f-distance);
                colors[y*size+x]=new Color(.62f+.3f*y/size,.91f,.88f,alpha);
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Original icon "+group+"/"+id,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels(colors);texture.Apply();return texture;
        }

        public static bool Has(string group, string id) => Get(group, id) != null;

        /// <summary>Adds a preserve-aspect Image child centred at (x, y) from the parent's top-left, or returns null when there is no icon.</summary>
        public static Image Place(Transform parent, string group, string id, float x, float y, float size, bool raycast = false)
        {
            var sprite = Get(group, id);
            if (sprite == null || parent == null) return null;
            var go = new GameObject("Icon " + id, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(size, size);
            var image = go.GetComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Puts the icon into an existing Image (keeping its rect), returning false when there is no icon.</summary>
        public static bool Apply(Image image, string group, string id)
        {
            var sprite = Get(group, id);
            if (sprite == null || image == null) return false;
            image.sprite = sprite; image.color = Color.white; image.preserveAspect = true;
            return true;
        }
    }
}
