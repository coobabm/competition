using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace HongmengOS.Aero2010.Editor
{
 public static class AeroSystemPageBuilder
 {
  public static void Apply(Transform about,TMP_FontAsset font,Sprite icon)
  {
   if(about==null)return;var title=about.Find("Title");if(title!=null)title.gameObject.SetActive(false);var content=about.Find("Content");if(content==null||content.Find("Aero System Information")!=null)return;
   foreach(Transform child in content)child.gameObject.SetActive(false);
   var root=new GameObject("Aero System Information",typeof(RectTransform)).GetComponent<RectTransform>();root.SetParent(content,false);root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=new Vector2(24,0);root.offsetMax=new Vector2(-24,0);
   Text(root,font,"Heading","View basic information about your computer",27,new Color32(34,92,142,255),0,10,800,38);
   Section(root,font,"hongmengos edition",78);
   Text(root,font,"Edition","hongmengos 2010",30,new Color32(25,42,59,255),0,119,690,42);
   Text(root,font,"Edition details","Aero Desktop\nClassic PC collection · 2010 edition",19,new Color32(66,80,94,255),0,169,660,65);
   var im=new GameObject("LCD desktop emblem",typeof(RectTransform),typeof(Image)).GetComponent<Image>();im.transform.SetParent(root,false);im.sprite=icon;im.color=Color.white;im.preserveAspect=true;im.raycastTarget=false;im.rectTransform.anchorMin=im.rectTransform.anchorMax=new Vector2(1,1);im.rectTransform.pivot=new Vector2(1,1);im.rectTransform.anchoredPosition=new Vector2(-12,-115);im.rectTransform.sizeDelta=new Vector2(104,104);
   Section(root,font,"System",265);
   Row(root,font,"Desktop engine","DreamOS / Unity",310);
   Row(root,font,"Display","1920 × 1080 · 16:9 widescreen LCD",350);
   Row(root,font,"Appearance","Aero Glass · Full-color desktop icons",390);
   Row(root,font,"Input","Mouse and keyboard",430);
   Section(root,font,"Computer name and desktop settings",493);
   Row(root,font,"Computer name","HONGMENG-DESKTOP",538);
   Row(root,font,"Operating mode","Interactive game desktop",578);
   Text(root,font,"Simulation note","This is a simulated desktop for gameplay, not an installed operating system.",16,new Color32(95,110,125,255),0,654,900,44);
   foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=about.gameObject.layer;
  }
  static void Section(Transform p,TMP_FontAsset f,string text,float y){Text(p,f,text,text,21,new Color32(34,92,142,255),0,y,900,31);var i=new GameObject("Section divider",typeof(RectTransform),typeof(Image)).GetComponent<Image>();i.transform.SetParent(p,false);i.color=new Color32(190,211,226,255);i.raycastTarget=false;i.rectTransform.anchorMin=new Vector2(0,1);i.rectTransform.anchorMax=Vector2.one;i.rectTransform.pivot=new Vector2(.5f,1);i.rectTransform.offsetMin=new Vector2(0,-y-36);i.rectTransform.offsetMax=new Vector2(0,-y-35);}
  static void Row(Transform p,TMP_FontAsset f,string label,string value,float y){Text(p,f,label,label+":",18,new Color32(92,105,119,255),0,y,215,30);Text(p,f,label+" Value",value,18,new Color32(28,42,56,255),235,y,640,30);}
  static void Text(Transform p,TMP_FontAsset f,string name,string value,float size,Color color,float x,float y,float w,float h){var t=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();t.transform.SetParent(p,false);t.font=f;t.fontSize=size;t.color=color;t.text=value;t.alignment=TextAlignmentOptions.TopLeft;t.raycastTarget=false;t.rectTransform.anchorMin=t.rectTransform.anchorMax=new Vector2(0,1);t.rectTransform.pivot=new Vector2(0,1);t.rectTransform.anchoredPosition=new Vector2(x,-y);t.rectTransform.sizeDelta=new Vector2(w,h);}
 }
}
