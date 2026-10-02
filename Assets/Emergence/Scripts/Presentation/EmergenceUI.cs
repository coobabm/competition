using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Emergence
{
    public sealed class HoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public Action entered, exited;
        public void OnPointerEnter(PointerEventData data) { entered?.Invoke(); }
        public void OnPointerExit(PointerEventData data) { exited?.Invoke(); }
    }

    public static class EmergenceUI
    {
        public static readonly Color Ink = Hex("08151D"), Panel = Hex("0D202A"), Line = Hex("23404A"),
            Foreground = Hex("E4ECE6"), Muted = Hex("8FA8AE"), Teal = Hex("78E2CB"), Gold = Hex("E5C589"),
            Violet = Hex("BDACED"), Red = Hex("ED948C");
        public static Font Font;
        static Sprite rounded;
        public static Color Hex(string s) { ColorUtility.TryParseHtmlString("#" + s, out var c); return c; }
        public static RectTransform Box(Transform parent, string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
        }
        public static RectTransform Fill(Transform parent, string name)
        {
            var r = Box(parent, name, 0, 0, 0, 0); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        public static Image Surface(Transform parent, string name, float x, float y, float w, float h, Color color, bool round = true)
        {
            var r = Box(parent, name, x, y, w, h); var i = r.gameObject.AddComponent<Image>(); i.color = color;
            i.raycastTarget = false; if (round) { i.sprite = Rounded; i.type = Image.Type.Sliced; } return i;
        }
        public static Text Label(Transform parent, string value, float x, float y, float w, float h, int size = 18, Color? color = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var r = Box(parent, "Text", x, y, w, h); var t = r.gameObject.AddComponent<Text>(); t.font = Font;
            t.text = value; t.fontSize = size; t.color = color ?? Foreground; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.12f; t.raycastTarget = false; t.supportRichText = true; return t;
        }
        public static Button Button(Transform parent, string name, string title, float x, float y, float w, float h, Action action, Color? tint = null, bool primary = false, int size = 18)
        {
            Color accent = tint ?? Teal;
            var bg = Surface(parent, name, x, y, w, h, primary ? accent : Hex("132C36")); bg.raycastTarget = true;
            var b = bg.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            var c = b.colors; c.normalColor = Color.white; c.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            c.pressedColor = new Color(.72f,.8f,.8f); c.selectedColor = Color.white; c.disabledColor = new Color(.5f,.55f,.55f,.5f); c.fadeDuration = .1f; b.colors = c;
            Label(bg.transform, title, 10, 0, w-20, h, size, primary ? Ink : accent, TextAnchor.MiddleCenter);
            b.onClick.AddListener(() => action?.Invoke()); return b;
        }
        public static void Clear(Transform t) { for (int i=t.childCount-1;i>=0;i--) { var go=t.GetChild(i).gameObject; go.SetActive(false); UnityEngine.Object.Destroy(go); } }
        public static Color NodeColor(NodeKind kind)
        {
            switch (kind) { case NodeKind.Sensitive:return Teal;case NodeKind.Core:return Gold;case NodeKind.Delay:return Violet;
                case NodeKind.Capacitor:return Hex("F0AC78");case NodeKind.Projector:return Hex("91C5F1");default:return Hex("B1CBD0"); }
        }
        public static string ShortKind(NodeKind kind) { switch(kind) { case NodeKind.Sensitive:return "敏";case NodeKind.Core:return "核";case NodeKind.Delay:return "迟";case NodeKind.Capacitor:return "蓄";case NodeKind.Projector:return "远";default:return "常"; } }
        public static string Number(double n) { if(n>=1e9)return (n/1e9).ToString("0.##")+"B";if(n>=1e6)return (n/1e6).ToString("0.##")+"M";return Math.Floor(n).ToString("N0"); }
        static Sprite Rounded
        {
            get
            {
                if(rounded!=null)return rounded;
                const int s=64; const float radius=10;
                var tex=new Texture2D(s,s,TextureFormat.RGBA32,false); tex.name="Emergence / rounded surface"; tex.hideFlags=HideFlags.HideAndDontSave;
                for(int y=0;y<s;y++)for(int x=0;x<s;x++)
                { float dx=Mathf.Max(Mathf.Abs(x-(s-1)*.5f)-(s*.5f-radius),0);float dy=Mathf.Max(Mathf.Abs(y-(s-1)*.5f)-(s*.5f-radius),0);float a=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy));tex.SetPixel(x,y,new Color(1,1,1,a)); }
                tex.Apply();rounded=Sprite.Create(tex,new Rect(0,0,s,s),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12)); return rounded;
            }
        }
    }
}
