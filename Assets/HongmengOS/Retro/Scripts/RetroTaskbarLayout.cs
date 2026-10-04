using UnityEngine;
using Michsky.DreamOS;
namespace HongmengOS.Retro
{
    /// <summary>Preserves DreamOS animation/state logic; only owns classic task button geometry.
    /// O(n) checks for n task buttons, no per-frame managed allocations.</summary>
    [DefaultExecutionOrder(1000)]
    public sealed class RetroTaskbarLayout : MonoBehaviour
    {
        [System.Serializable] public struct Entry
        {
            public TaskbarButton button;
            public RectTransform rect;
            public RectTransform icon;
            public RetroBevelGraphic bevel;
            public TMPro.TMP_Text label;
            [System.NonSerialized] public CanvasGroup windowGroup;
        }
        public Entry[] entries;
        private RectTransform area;
        private void Awake()
        {
            area=transform as RectTransform;
            if(entries==null)return;
            for(int i=0;i<entries.Length;i++) if(entries[i].button!=null&&entries[i].button.windowManager!=null)
                entries[i].windowGroup=entries[i].button.windowManager.GetComponent<CanvasGroup>();
        }
        private void LateUpdate()
        {
            if(area==null||entries==null)return;
            int count=0; WindowManager focused=null;int top=-1;
            for(int i=0;i<entries.Length;i++) {
                var e=entries[i];var w=e.button==null?null:e.button.windowManager;
                if(w!=null&&w.isOn&&w.gameObject.activeInHierarchy&&e.windowGroup!=null&&e.windowGroup.alpha>.5f&&w.transform.GetSiblingIndex()>top)
                { focused=w;top=w.transform.GetSiblingIndex(); }
            }
            for(int i=0;i<entries.Length;i++)if(Visible(entries[i]))count++;
            float width=Mathf.Clamp((area.rect.width-Mathf.Max(0,count-1)*5)/Mathf.Max(1,count),28,164);
            float x=0;
            for(int i=0;i<entries.Length;i++)
            {
                var e=entries[i];if(e.rect==null)continue;
                bool show=Visible(e);float w=show?width:0;
                var size=new Vector2(w,38);if(e.rect.sizeDelta!=size)e.rect.sizeDelta=size;
                var pos=new Vector2(x,0);if(e.rect.anchoredPosition!=pos)e.rect.anchoredPosition=pos;
                if(show)x+=width+5;
                if(e.icon!=null){var p=new Vector2(w<70?w/2:21,0);if(e.icon.anchoredPosition!=p)e.icon.anchoredPosition=p;}
                if(e.label!=null&&e.label.enabled!=(w>=70))e.label.enabled=w>=70;
                if(e.bevel!=null&&e.button!=null&&e.button.windowManager!=null)
                    e.bevel.Inset=e.button.windowManager==focused;
            }
        }
        private static bool Visible(Entry e) => e.button!=null&&(!e.button.OpenWindowsOnly||(e.button.windowManager!=null&&e.button.windowManager.isOn));
    }
}
