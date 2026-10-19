using System;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace LingGuangV05.Desktop.XingGuang.MiniGames
{
    /// <summary>One drag owns a pixel snapshot, never board cells, coordinates, a file key or a solution.</summary>
    public sealed class XgBoardPictureDrag : MonoBehaviour,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        XgSim owner;XgState state;byte[] pixels;
        public static XgSim Current=>XingGuangController.Instance?.Sim;
        public static bool Visible(Component source)
        {
            if(source==null||!source.gameObject.activeInHierarchy)return false;
            var window=source.GetComponentInParent<WindowManager>();
            if(window!=null&&!window.isOn)return false;
            foreach(var group in source.GetComponentsInParent<CanvasGroup>())
                if(group.alpha<.01f||!group.interactable||!group.blocksRaycasts)return false;
            return true;
        }
        public void OnBeginDrag(PointerEventData e)
        {
            Clear();if(e==null||e.button!=PointerEventData.InputButton.Left||!Visible(this))return;
            var sim=Current;if(sim==null)return;
            var image=GetComponent<Image>();var raw=GetComponent<RawImage>();
            var texture=image!=null?image.sprite?.texture:raw?.texture as Texture2D;
            if(texture==null||!texture.isReadable||texture.width!=240||texture.height!=240)return;
            if(image!=null&&(image.sprite.rect!=new Rect(0,0,240,240)))return;
            if(raw!=null&&raw.uvRect!=new Rect(0,0,1,1)&&raw.uvRect!=new Rect(0,1,1,-1))return;
            var colors=texture.GetPixels32();pixels=new byte[240*240*3];
            // Unity texture rows run bottom-up; the teaching RawImage deliberately flips its UV vertically.
            bool inverted=raw!=null&&raw.uvRect.height<0;
            for(int y=0;y<240;y++)for(int x=0;x<240;x++)
            {var c=colors[(inverted?y:239-y)*240+x];int p=(y*240+x)*3;pixels[p]=c.r;pixels[p+1]=c.g;pixels[p+2]=c.b;}
            owner=sim;state=sim.S;
        }
        public bool TryTake(XgSim target,out byte[] rgb)
        {
            rgb=null;
            if(pixels==null||!Visible(this)||target==null||!ReferenceEquals(owner,target)||!ReferenceEquals(owner,Current)||!ReferenceEquals(state,target.S)||target.BoardLogicLevel<2){Clear();return false;}
            rgb=pixels;Clear();return true;
        }
        void Update(){if(pixels!=null&&(!ReferenceEquals(owner,Current)||!ReferenceEquals(state,owner?.S)||!Visible(this)))Clear();}
        void Clear(){pixels=null;owner=null;state=null;}
        void OnDisable()=>Clear();
        public void OnDrag(PointerEventData e){}
        public void OnEndDrag(PointerEventData e)=>Clear();
    }
    public sealed class XgBoardPictureDrop : MonoBehaviour,IDropHandler
    {
        public XgSim Owner;public Func<bool> CanAccept;public Action<byte[]> Accept;
        public void OnDrop(PointerEventData e)
        {
            if(e==null||e.button!=PointerEventData.InputButton.Left||e.pointerDrag==null||!XgBoardPictureDrag.Visible(this)||Owner==null||Owner.BoardLogicLevel<2||!ReferenceEquals(Owner,XgBoardPictureDrag.Current)||CanAccept!=null&&!CanAccept())return;
            var source=e.pointerDrag.GetComponent<XgBoardPictureDrag>();
            if(source!=null&&source.TryTake(Owner,out var rgb))Accept?.Invoke(rgb);
        }
    }
}
