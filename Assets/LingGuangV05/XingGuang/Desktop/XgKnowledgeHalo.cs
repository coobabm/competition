using UnityEngine;using UnityEngine.UI;
namespace LingGuangV05.Desktop.XingGuang
{
 public sealed class XgKnowledgeHalo:MaskableGraphic
 {
  protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var center=rectTransform.rect.center;const int segments=40;float radius=Mathf.Min(rectTransform.rect.width,rectTransform.rect.height)*.42f;var amber=new Color32(232,182,90,255);for(int i=0;i<segments;i++){float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;XgDraw.Seg(vh,center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,2.5f,amber);}}
 }
}
