using UnityEngine;
using UnityEngine.UI;

namespace HongmengOS.Retro
{
    /// <summary>Eight small quads, rebuilt only when geometry or state changes.</summary>
    [AddComponentMenu("HongmengOS/Retro Bevel")]
    public sealed class RetroBevelGraphic : MaskableGraphic
    {
        [SerializeField] private bool inset;
        [SerializeField, Min(1)] private float edge = 2;
        public bool Inset { get => inset; set { if (inset == value) return; inset = value; SetVerticesDirty(); } }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            if (r.width < edge * 4 || r.height < edge * 4) return;
            Color32 light = inset ? new Color32(90,90,90,255) : new Color32(255,255,255,255);
            Color32 dark = inset ? new Color32(255,255,255,255) : new Color32(40,40,40,255);
            Color32 innerLight = inset ? new Color32(40,40,40,255) : new Color32(220,220,220,255);
            Color32 innerDark = inset ? new Color32(220,220,220,255) : new Color32(128,128,128,255);
            Ring(vh,r,edge,light,dark);
            r.xMin += edge; r.xMax -= edge; r.yMin += edge; r.yMax -= edge;
            Ring(vh,r,edge,innerLight,innerDark);
        }
        private static void Ring(VertexHelper v,Rect r,float e,Color32 a,Color32 b)
        {
            Quad(v,new Rect(r.xMin,r.yMax-e,r.width,e),a);
            Quad(v,new Rect(r.xMin,r.yMin,e,r.height-e),a);
            Quad(v,new Rect(r.xMax-e,r.yMin,e,r.height-e),b);
            Quad(v,new Rect(r.xMin+e,r.yMin,r.width-e,e),b);
        }
        private static void Quad(VertexHelper v,Rect r,Color32 c)
        {
            int i=v.currentVertCount;
            v.AddVert(new Vector3(r.xMin,r.yMin),c,Vector2.zero); v.AddVert(new Vector3(r.xMin,r.yMax),c,Vector2.zero);
            v.AddVert(new Vector3(r.xMax,r.yMax),c,Vector2.zero); v.AddVert(new Vector3(r.xMax,r.yMin),c,Vector2.zero);
            v.AddTriangle(i,i+1,i+2);v.AddTriangle(i,i+2,i+3);
        }
    }
}
