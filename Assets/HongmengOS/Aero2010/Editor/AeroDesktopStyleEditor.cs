using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Michsky.DreamOS;
using Object=UnityEngine.Object;
namespace HongmengOS.Aero2010.Editor
{
 public static class AeroDesktopStyleEditor
 {
  public const string ScenePath="Assets/DreamOS - Complete OS UI/Scenes/2010desktop.unity";
  const string Base="Assets/HongmengOS/Aero2010/";
  static TMP_FontAsset font;static Dictionary<string,Sprite> icons;static Material glass;
  static readonly Color Ink=new Color32(25,38,51,255),Paper=new Color32(245,249,252,255);
  public static void Apply()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.path!=ScenePath||EditorApplication.isPlaying)throw new InvalidOperationException("2010desktop edit scene required");
   var monitor=GameObject.Find("HongmengOS Monitor").transform;
   if(monitor.Find("Aero2010 Applied")!=null)return;
   var os=monitor.Find("Screen Viewport/hongmengos");
   if(PrefabUtility.IsPartOfPrefabInstance(os.gameObject))PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(os.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
   icons=AeroIconBuilder.BuildAssets();Directory.CreateDirectory(Base+"Theme");AssetDatabase.Refresh();
   glass=AssetDatabase.LoadAssetAtPath<Material>(Base+"Rendering/Aero Glass.mat");if(glass==null){glass=new Material(Shader.Find("HongmengOS/Aero2010/Glass"));AssetDatabase.CreateAsset(glass,Base+"Rendering/Aero Glass.mat");}
   var theme=Copy(os.GetComponentInChildren<AudioManager>(true).UIManagerAsset,Base+"Theme/Aero 2010 UI.asset");
   theme.enableUIBlur=false;theme.enableDynamicUpdate=false;theme.selectedTheme=UIManager.SelectedTheme.Default;
   theme.primaryColorDark=Ink;theme.secondaryColorDark=new Color32(91,112,130,255);theme.windowBGColorDark=Paper;theme.backgroundColorDark=new Color32(227,239,247,255);theme.taskBarColorDark=new Color(.08f,.21f,.38f,.82f);
   theme.highlightedColorDark=theme.highlightedColorCustom=new Color32(48,131,195,255);theme.highlightedColorSecondaryDark=theme.highlightedColorSecondaryCustom=Color.white;
   theme.systemFontThin=theme.systemFontLight=theme.systemFontRegular=theme.systemFontSemiBold=theme.systemFontBold=font;
   theme.enableKeystrokes=true;theme.enableMouseKeystroke=true;theme.enableKeyboardKeystroke=false;
   var apps=Copy(os.GetComponentInChildren<AppElement>(true).appLibrary,Base+"Theme/Aero 2010 Apps.asset");apps.alwaysUpdate=false;apps.optimizeUpdates=true;
   foreach(var app in apps.apps)if(icons.TryGetValue(app.appTitle,out var icon)){app.appIconBig=app.appIconMedium=app.appIconSmall=icon;app.appIconPreview=icon.texture;app.gradientLeft=app.gradientRight=Color.white;}
   foreach(var c in os.GetComponentsInChildren<MonoBehaviour>(true)){
    if(c==null)continue;var so=new SerializedObject(c);var p=so.GetIterator();bool dirty=false;
    while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference){if(p.objectReferenceValue is UIManager){p.objectReferenceValue=theme;dirty=true;}else if(p.objectReferenceValue is AppLibrary){p.objectReferenceValue=apps;dirty=true;}}
    if(dirty)so.ApplyModifiedPropertiesWithoutUndo();
   }
   foreach(var e in os.GetComponentsInChildren<UIManagerElement>(true)){e.themeManagerAsset=theme;e.keepAlphaValue=true;e.UpdateElement();}
   foreach(var t in os.GetComponentsInChildren<TMP_Text>(true)){t.font=font;t.fontStyle&=~(FontStyles.LowerCase|FontStyles.UpperCase);if(t.fontSize>24&&t.fontSize<60)t.fontSize=Mathf.Max(20,t.fontSize*.8f);}
   foreach(var a in os.GetComponentsInChildren<AppElement>(true)){
    a.appLibrary=apps;a.useGradient=false;var g=a.GetComponent<UIGradient>();if(g!=null)Object.DestroyImmediate(g);
    if(a.elementType==AppElement.ElementType.Gradient)a.elementType=AppElement.ElementType.None;
    if(a.elementType==AppElement.ElementType.Icon&&icons.TryGetValue(a.appID,out var icon)){var img=a.GetComponent<Image>();if(img!=null){img.sprite=icon;Fixed(img,Color.white);}}
   }
   foreach(var b in os.GetComponentsInChildren<ButtonManager>(true)){b.useRipple=false;b.useHoverEffect=false;b.useSounds=false;b.fadingMultiplier=14;b.textSize=Mathf.Clamp(b.textSize,16,22);if(b.hoverEffect!=null)b.hoverEffect.gameObject.SetActive(false);if(b.rippleParent!=null)b.rippleParent.SetActive(false);}
   foreach(var i in os.GetComponentsInChildren<Image>(true))if(i.name=="Background Blur"||i.name=="Blur")i.gameObject.SetActive(false);
   var desktop=os.Find("Desktop");Desktop(desktop);
   foreach(var w in desktop.Find("Apps & Windows").GetComponentsInChildren<WindowManager>(true))Window(w);
   AeroShellBuilder.Apply(desktop,icons,font);
   AeroSystemPageBuilder.Apply(desktop.Find("Apps & Windows/Settings/Container/Content/Main Content/About"),font,icons["Settings"]);
   var user=os.GetComponentInChildren<UserManager>(true);if(user!=null){user.disableUserCreating=true;user.disableLockScreen=true;user.systemPassword="";}
   EditorUtility.SetDirty(theme);EditorUtility.SetDirty(apps);new GameObject("Aero2010 Applied").transform.SetParent(monitor,false);AssetDatabase.SaveAssetIfDirty(theme);AssetDatabase.SaveAssetIfDirty(apps);AssetDatabase.SaveAssetIfDirty(glass);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  static T Copy<T>(T source,string path)where T:Object{var old=AssetDatabase.LoadAssetAtPath<T>(path);if(old!=null)return old;if(source==null)throw new Exception("Missing source "+typeof(T));var copy=Object.Instantiate(source);copy.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(copy,path);return copy;}
  static void Desktop(Transform d)
  {
   var wallpaper=d.Find("Wallpaper");var wo=wallpaper.GetComponent<WallpaperObject>();if(wo!=null)Object.DestroyImmediate(wo);var fit=wallpaper.GetComponent<AspectRatioFitter>();if(fit!=null)Object.DestroyImmediate(fit);
   var bg=wallpaper.GetComponent<Image>();bg.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(AeroIconBuilder.WallpaperPath);bg.color=Color.white;bg.type=UnityEngine.UI.Image.Type.Simple;Stretch((RectTransform)wallpaper);
   d.Find("Widgets").gameObject.SetActive(false);
   var grid=d.Find("Desktop List").GetComponent<GridLayoutGroup>();grid.cellSize=new Vector2(112,112);grid.spacing=new Vector2(14,9);grid.padding=new RectOffset(25,10,24,10);grid.constraint=GridLayoutGroup.Constraint.FixedRowCount;grid.constraintCount=8;grid.startAxis=GridLayoutGroup.Axis.Vertical;
   foreach(Transform item in grid.transform){var icon=(RectTransform)item.Find("Icon Background/Icon");icon.sizeDelta=new Vector2(56,56);var sh=item.Find("Icon Background/Shadow");if(sh!=null)sh.gameObject.SetActive(false);
    var title=item.Find("Title").GetComponent<TMP_Text>();FixedText(title,Color.white);title.fontSize=18;title.alignment=TextAlignmentOptions.Center;title.enableAutoSizing=false;title.fontStyle=FontStyles.Normal;title.rectTransform.sizeDelta=new Vector2(118,40);
    var shadow=title.gameObject.GetComponent<Shadow>()??title.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.9f);shadow.effectDistance=new Vector2(1,-1);
    Fixed(item.Find("Normal/Background").GetComponent<Image>(),Color.clear);Fixed(item.Find("Highlighted/Background").GetComponent<Image>(),new Color(.68f,.88f,1,.23f));
   }
   var version=Text("Aero Edition Mark",d,"hongmengos\n2010 • Aero Desktop",22,new Color(1,1,1,.7f));version.alignment=TextAlignmentOptions.BottomRight;version.rectTransform.anchorMin=version.rectTransform.anchorMax=new Vector2(1,0);version.rectTransform.pivot=new Vector2(1,0);version.rectTransform.anchoredPosition=new Vector2(-28,72);version.rectTransform.sizeDelta=new Vector2(300,70);
   version.transform.SetSiblingIndex(wallpaper.GetSiblingIndex()+1);
  }
  static void Window(WindowManager w)
  {
   w.useBackgroundBlur=false;var so=new SerializedObject(w);so.FindProperty("waitForAnimationCompletion").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();Stretch((RectTransform)w.transform);
   var container=w.transform.Find("Container");if(container==null)return;
   var bg=container.Find("Background")?.GetComponent<Image>();if(bg!=null){Fixed(bg,new Color(.36f,.64f,.84f,.88f));bg.material=glass;bg.sprite=null;}
   var shadow=container.Find("Shadow")?.GetComponent<Image>();if(shadow!=null)shadow.color=new Color(0,.02f,.08f,.45f);
   var content=container.Find("Content") as RectTransform;if(content!=null){content.offsetMin=new Vector2(7,28);content.offsetMax=new Vector2(-7,-39);var fill=content.GetComponent<Image>()??content.gameObject.AddComponent<Image>();Fixed(fill,Paper);fill.raycastTarget=false;}
   var dragger=container.Find("Dragger") as RectTransform;
   if(dragger!=null){dragger.anchorMin=new Vector2(0,1);dragger.anchorMax=Vector2.one;dragger.pivot=new Vector2(.5f,1);dragger.offsetMin=new Vector2(7,-38);dragger.offsetMax=new Vector2(-7,-3);var bar=dragger.GetComponent<Image>()??dragger.gameObject.AddComponent<Image>();Fixed(bar,new Color(.60f,.80f,.95f,.24f));bar.material=glass;bar.raycastTarget=true;
    var title=Text("Aero Window Title",dragger,w.name=="Settings"?"Control Panel":w.name=="Web Browser"?"Internet Explorer — hongmengos":w.name,20,Ink);Stretch(title.rectTransform,new Vector2(35,0),new Vector2(-140,0));
    if(icons.TryGetValue(w.name,out var icon)){var im=Image("Aero Title Icon",dragger,Color.white);im.sprite=icon;im.rectTransform.anchorMin=im.rectTransform.anchorMax=new Vector2(0,.5f);im.rectTransform.anchoredPosition=new Vector2(16,0);im.rectTransform.sizeDelta=new Vector2(24,24);}
    var buttons=content?.Find("Window Buttons") as RectTransform;
    if(buttons==null)buttons=content?.Find("Top Nav Buttons/Window Buttons") as RectTransform;
    if(buttons!=null){buttons.SetParent(dragger,false);buttons.anchorMin=buttons.anchorMax=new Vector2(1,1);buttons.pivot=new Vector2(1,1);buttons.anchoredPosition=new Vector2(0,1);buttons.sizeDelta=new Vector2(127,27);var h=buttons.GetComponent<HorizontalLayoutGroup>();if(h!=null){h.spacing=1;h.padding=new RectOffset();h.childControlWidth=true;h.childForceExpandWidth=true;h.childControlHeight=true;h.childForceExpandHeight=true;}
     foreach(Transform b in buttons){bool close=b.name.ToLowerInvariant().Contains("close")||b.name=="Exit";foreach(var img in b.GetComponentsInChildren<Image>(true))if(img.name=="Icon")Fixed(img,close?Color.white:Ink);var face=Image("Aero Caption Button",b,close?new Color(.72f,.17f,.12f,1):new Color(.67f,.84f,.97f,.8f));Stretch(face.rectTransform);face.material=glass;face.transform.SetAsFirstSibling();b.gameObject.AddComponent<AeroCaptionFeedback>().face=face;Border(b,new Color(1,1,1,.4f));}
    }dragger.SetAsLastSibling();
   }
   var edge=Border(container,new Color(.83f,.94f,1,.85f));edge.transform.SetAsLastSibling();
   var status=Image("Aero Status Bar",container,new Color32(230,239,247,255));Rect(status.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(7,7),new Vector2(-7,27));var st=Text("Status",status.transform,"Ready",14,new Color32(84,107,125,255));Stretch(st.rectTransform,new Vector2(8,0),new Vector2(-8,0));
   if(w.name=="Settings"){var about=container.Find("Content/Main Content/About");if(about!=null)foreach(var t in about.GetComponentsInChildren<TMP_Text>(true)){if(t.text=="hongmengos")t.text="hongmengos 2010";if(t.text=="3.0.0")t.text="Aero Edition · LCD 1080p";}}
  }
  public static GameObject Border(Transform p,Color c){var r=new GameObject("Aero Fine Border",typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(p,false);Stretch(r);var top=Image("Top",r,c);Rect(top.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(0,-1),Vector2.zero);var bottom=Image("Bottom",r,c);Rect(bottom.rectTransform,Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,1));var left=Image("Left",r,c);Rect(left.rectTransform,Vector2.zero,new Vector2(0,1),Vector2.zero,new Vector2(1,0));var right=Image("Right",r,c);Rect(right.rectTransform,new Vector2(1,0),Vector2.one,new Vector2(-1,0),Vector2.zero);return r.gameObject;}
  static Image Image(string name,Transform p,Color c){var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(p,false);var i=g.GetComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
  static TMP_Text Text(string name,Transform p,string str,int size,Color c){var g=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));g.transform.SetParent(p,false);var t=g.GetComponent<TextMeshProUGUI>();t.font=font;t.text=str;t.fontSize=size;t.color=c;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
  static void Fixed(Image i,Color c){var e=i.GetComponent<UIManagerElement>();if(e!=null)Object.DestroyImmediate(e);i.color=c;}
  static void FixedText(TMP_Text t,Color c){var e=t.GetComponent<UIManagerElement>();if(e!=null)Object.DestroyImmediate(e);t.color=c;}
  public static void Stretch(RectTransform r){Rect(r,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);}
  static void Stretch(RectTransform r,Vector2 min,Vector2 max){Rect(r,Vector2.zero,Vector2.one,min,max);}
  static void Rect(RectTransform r,Vector2 amin,Vector2 amax,Vector2 min,Vector2 max){r.anchorMin=amin;r.anchorMax=amax;r.offsetMin=min;r.offsetMax=max;}
 }
}
