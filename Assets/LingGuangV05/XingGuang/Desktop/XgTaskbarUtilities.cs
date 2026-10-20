using UnityEngine;
using UnityEngine.UI;
using HongmengOS.Aero2010;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Compact utility icons share the real shell taskbar, independently of app windows.</summary>
    public sealed class XgTaskbarUtilities : MonoBehaviour
    {
        RectTransform bar, row;
        static readonly string[] Peers={"Aero Notification Area","Tray Apps 2016","Tray LingGuang"};
        readonly Vector3[] corners=new Vector3[4];
        RectTransform applicationList;
        Vector2 originalListMax;
        public static void Attach(RectTransform taskbar,RectTransform button)
        {
            var existing=taskbar.Find("AI Utility Tray");
            var layout=existing!=null?existing.GetComponent<XgTaskbarUtilities>():null;
            if(layout==null)
            {
                var go=new GameObject("AI Utility Tray",typeof(RectTransform));go.transform.SetParent(taskbar,false);
                layout=go.AddComponent<XgTaskbarUtilities>();layout.bar=taskbar;layout.row=(RectTransform)go.transform;
                layout.row.anchorMin=layout.row.anchorMax=new Vector2(1,.5f);layout.row.pivot=new Vector2(1,.5f);
                var group=go.AddComponent<HorizontalLayoutGroup>();group.spacing=4;group.childAlignment=TextAnchor.MiddleRight;
                group.childControlWidth=group.childControlHeight=true;group.childForceExpandWidth=group.childForceExpandHeight=false;
            }
            button.SetParent(layout.row,false);
            var size=button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>();
            size.minWidth=size.preferredWidth=32;size.minHeight=size.preferredHeight=32;size.flexibleWidth=size.flexibleHeight=0;
            // Stable order regardless of which purchased utility becomes available first.
            button.SetSiblingIndex(button.name=="AI Brain Taskbar"?layout.row.childCount-1:0);
            layout.Refresh();
        }
        void LateUpdate()=>Refresh();
        void Refresh()
        {
            if(bar==null||row==null)return;
            float boundary=bar.rect.xMax;bool found=false;
            foreach(string name in Peers)
            {
                var peer=bar.Find(name) as RectTransform;if(peer==null)continue;
                peer.GetWorldCorners(corners);boundary=Mathf.Min(boundary,bar.InverseTransformPoint(corners[0]).x);found=true;
            }
            if(!found)boundary=bar.rect.xMax-242;
            int count=0;for(int i=0;i<row.childCount;i++)if(row.GetChild(i).gameObject.activeSelf)count++;
            var size=new Vector2(count*32+Mathf.Max(0,count-1)*4,40);
            var position=new Vector2(boundary-bar.rect.xMax-6,0);
            if(row.sizeDelta!=size)row.sizeDelta=size;
            if(row.anchoredPosition!=position)row.anchoredPosition=position;
            ReserveApplicationSpace(count,boundary-6-size.x-6);
        }
        void ReserveApplicationSpace(int iconCount,float utilityLeft)
        {
            if(applicationList==null)
            {
                var layout=bar.GetComponentInChildren<AeroTaskbarLayout>(true);
                if(layout==null)return;
                applicationList=layout.transform as RectTransform;
                if(applicationList==null)return;
                originalListMax=applicationList.offsetMax;
            }
            var parent=applicationList.parent as RectTransform;if(parent==null)return;
            // Convert the tray's actual left edge to the application's anchor space.
            // The shell then compresses its own running buttons into the remaining width.
            float edge=parent.InverseTransformPoint(bar.TransformPoint(new Vector3(utilityLeft,0,0))).x;
            float anchor=parent.rect.xMin+parent.rect.width*applicationList.anchorMax.x;
            var max=originalListMax;
            if(iconCount>0)max.x=Mathf.Min(max.x,edge-anchor);
            if(applicationList.offsetMax!=max)applicationList.offsetMax=max;
        }
        void OnDestroy(){if(applicationList!=null)applicationList.offsetMax=originalListMax;}
    }
}
