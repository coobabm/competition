using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using LingGuangV05.Desktop.OS;
namespace HongmengOS.Aero2010.Editor
{
 public static class AeroLcdSetup
 {
  const string Base="Assets/HongmengOS/Aero2010/";const float Scale=5.12f/1920;
  public static void Apply()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(scene.path!=AeroDesktopStyleEditor.ScenePath||EditorApplication.isPlaying)throw new InvalidOperationException("2010desktop edit mode required");
   if(GameObject.Find("LCD Source Camera")!=null)return;
   var monitor=GameObject.Find("HongmengOS Monitor").GetComponent<RectTransform>();
   var os=monitor.Find("Screen Viewport/hongmengos") as RectTransform;
   var main=GameObject.Find("Main Camera").GetComponent<Camera>();
   var model=AeroLcdModelBuilder.Build();
   Directory.CreateDirectory(Base+"Rendering");AssetDatabase.Refresh();
   const string rtpath=Base+"Rendering/LCD 1920x1080.renderTexture";
   var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtpath);
   if(rt==null){rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){name="LCD 1920x1080",antiAliasing=1,useMipMap=false,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};AssetDatabase.CreateAsset(rt,rtpath);}
   var cameraObject=new GameObject("LCD Source Camera",typeof(Camera),typeof(UniversalAdditionalCameraData));var source=cameraObject.GetComponent<Camera>();
   source.clearFlags=CameraClearFlags.SolidColor;source.backgroundColor=Color.black;source.orthographic=true;source.orthographicSize=5;source.nearClipPlane=.1f;source.farClipPlane=100;source.transform.position=new Vector3(0,0,-20);source.depth=-10;source.cullingMask=1<<5;source.targetTexture=rt;source.allowHDR=false;source.allowMSAA=false;cameraObject.GetComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;cameraObject.GetComponent<UniversalAdditionalCameraData>().renderShadows=false;
   os.SetParent(null,false);os.name="hongmengos 2010 Desktop Source";var canvas=os.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=source;canvas.planeDistance=10;
   var scaler=os.GetComponent<CanvasScaler>();scaler.enabled=true;scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
   foreach(var c in os.GetComponentsInChildren<Canvas>(true))c.worldCamera=source;
   foreach(Transform child in monitor)child.gameObject.SetActive(false);
   var displayCanvas=monitor.GetComponent<Canvas>();displayCanvas.renderMode=RenderMode.WorldSpace;displayCanvas.worldCamera=main;displayCanvas.sortingOrder=0;displayCanvas.overrideSorting=false;
   var ms=monitor.GetComponent<CanvasScaler>();if(ms!=null){ms.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;ms.scaleFactor=1;ms.dynamicPixelsPerUnit=1;}
   foreach(var component in monitor.GetComponents<MonoBehaviour>())if(!(component is GraphicRaycaster)&&!(component is CanvasScaler))UnityEngine.Object.DestroyImmediate(component);
   monitor.anchorMin=monitor.anchorMax=monitor.pivot=new Vector2(.5f,.5f);monitor.sizeDelta=new Vector2(2100,1260);monitor.position=new Vector3(0,0,.028f);monitor.rotation=Quaternion.identity;monitor.localScale=Vector3.one*Scale;
   var display=new GameObject("LCD Full HD Panel",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();display.transform.SetParent(monitor,false);Center(display.rectTransform,Vector2.zero,new Vector2(1920,1080));display.texture=rt;display.color=Color.white;display.raycastTarget=true;
   var input=display.gameObject.AddComponent<AeroLcdInputSurface>();input.displayRect=display.rectTransform;input.sourceCamera=source;input.sourceRaycasters=os.GetComponentsInChildren<GraphicRaycaster>(true);foreach(var r in input.sourceRaycasters)r.enabled=false;
   var c0=monitor.gameObject.AddComponent<AeroLcdController>();c0.display=display;c0.sourceCamera=source;c0.inputSurface=input;c0.desktopAudio=os.GetComponentInChildren<AudioManager>(true);c0.desktopAudio.enabled=false;c0.powerLamp=model.transform.Find("Power Button Mount/Keycap/Power LED").GetComponent<Renderer>();
   var controls=new GameObject("LCD Hardware Controls",typeof(RectTransform)).GetComponent<RectTransform>();controls.SetParent(monitor,false);Center(controls,Vector2.zero,monitor.sizeDelta);controls.localPosition=new Vector3(0,0,(-.10f-.028f)/Scale);
   var power=Button(controls,"LCD Power",new Vector2(2.65f,-1.51f),new Vector2(.15f,.13f),model.transform.Find("Power Button Mount/Keycap"));UnityEventTools.AddPersistentListener(power.onClick,c0.TogglePower);
   var b1=Button(controls,"LCD Standard Preset",new Vector2(2.65f,-.83f),new Vector2(.15f,.13f),model.transform.Find("OSD Button 1 Mount/Keycap"));UnityEventTools.AddPersistentListener(b1.onClick,c0.StandardPreset);
   var b2=Button(controls,"LCD Brightness Down",new Vector2(2.65f,-1f),new Vector2(.15f,.13f),model.transform.Find("OSD Button 2 Mount/Keycap"));UnityEventTools.AddPersistentListener(b2.onClick,c0.Dimmer);
   var b3=Button(controls,"LCD Brightness Up",new Vector2(2.65f,-1.17f),new Vector2(.15f,.13f),model.transform.Find("OSD Button 3 Mount/Keycap"));UnityEventTools.AddPersistentListener(b3.onClick,c0.Brighter);
   var b4=Button(controls,"LCD Menu",new Vector2(2.65f,-1.34f),new Vector2(.15f,.13f),model.transform.Find("OSD Button 4 Mount/Keycap"));UnityEventTools.AddPersistentListener(b4.onClick,c0.ShowOsd);
   var osd=new GameObject("LCD On Screen Display",typeof(RectTransform),typeof(Image)).GetComponent<Image>();osd.transform.SetParent(controls,false);Center(osd.rectTransform,new Vector2(470,-220),new Vector2(470,165));osd.color=new Color(.02f,.05f,.10f,.94f);osd.raycastTarget=false;AeroDesktopStyleEditor.Border(osd.transform,new Color(.38f,.55f,.7f));
   var label=new GameObject("LCD OSD Text",typeof(RectTransform),typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();label.transform.SetParent(osd.transform,false);Center(label.rectTransform,Vector2.zero,new Vector2(430,140));label.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");label.fontSize=20;label.color=Color.white;label.text="Brightness   92%\n\nStandard   •   1920 × 1080 / 60 Hz";label.raycastTarget=false;c0.osd=osd.gameObject;c0.osdLabel=label;osd.gameObject.SetActive(false);
   var bridge=monitor.gameObject.AddComponent<AeroLcdSourceBridge>();bridge.sourceCanvas=canvas;bridge.sourceCamera=source;bridge.inputSurface=input;bridge.controller=c0;bridge.theme=c0.desktopAudio.UIManagerAsset;bridge.contextMenu=os.GetComponentInChildren<ContextMenuManager>(true);
   if(bridge.contextMenu!=null){bridge.contextMenu.cameraSource=ContextMenuManager.CameraSource.Custom;bridge.contextMenu.enableBlur=false;var so=new SerializedObject(bridge.contextMenu);so.FindProperty("targetCanvas").objectReferenceValue=canvas;so.FindProperty("targetCamera").objectReferenceValue=source;so.ApplyModifiedPropertiesWithoutUndo();}
   var menu=os.GetComponentInChildren<AeroStartMenu>(true);if(menu!=null)UnityEventTools.AddPersistentListener(menu.shutdown,c0.PowerOff);
   foreach(var t in os.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;foreach(var t in monitor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;
   main.cullingMask=1;main.orthographic=true;main.orthographicSize=1.64f;main.nearClipPlane=.1f;main.farClipPlane=60;main.targetTexture=null;main.depth=0;main.clearFlags=CameraClearFlags.SolidColor;main.backgroundColor=new Color(.01f,.014f,.02f);main.allowHDR=false;main.allowMSAA=true;main.transform.position=new Vector3(0,0,-8);main.transform.rotation=Quaternion.identity;main.gameObject.AddComponent<AeroMonitorFraming>();
   var add=main.GetComponent<UniversalAdditionalCameraData>()??main.gameObject.AddComponent<UniversalAdditionalCameraData>();add.renderPostProcessing=false;add.renderShadows=true;add.antialiasing=AntialiasingMode.None;
   var lights=new GameObject("LCD Studio Lighting");Light(lights.transform,"Softbox",new Vector3(35,-25,0),1.3f,new Color(.92f,.96f,1));Light(lights.transform,"Side Fill",new Vector3(20,140,0),.55f,new Color(.72f,.84f,1));RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.48f,.52f);
   Canvas.ForceUpdateCanvases();
   foreach(var w in os.GetComponentsInChildren<WindowManager>(true)){var r=(RectTransform)w.transform;AeroDesktopStyleEditor.Stretch(r);var wc=w.windowContainer;wc.anchorMin=Vector2.zero;wc.anchorMax=Vector2.one;float width=w.name=="Calculator"?450:1280,height=w.name=="Calculator"?600:830;float pw=((RectTransform)w.transform.parent).rect.width,ph=((RectTransform)w.transform.parent).rect.height;wc.offsetMin=new Vector2((pw-width)/2,(ph-height)/2);wc.offsetMax=-wc.offsetMin;}
   AssetDatabase.SaveAssetIfDirty(rt);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  }
  static void Center(RectTransform r,Vector2 p,Vector2 s){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=p;r.sizeDelta=s;r.localPosition=new Vector3(r.localPosition.x,r.localPosition.y,0);}
  static Button Button(Transform p,string n,Vector2 pos,Vector2 size,Transform cap){var g=new GameObject(n,typeof(RectTransform),typeof(Image),typeof(Button),typeof(AeroLcdKeyTravel));g.transform.SetParent(p,false);var img=g.GetComponent<Image>();img.color=new Color(1,1,1,.001f);Center(img.rectTransform,pos/Scale,size/Scale);var b=g.GetComponent<Button>();b.targetGraphic=img;var colors=b.colors;colors.highlightedColor=new Color(.55f,.8f,1,.14f);colors.pressedColor=new Color(.1f,.1f,.1f,.15f);b.colors=colors;g.GetComponent<AeroLcdKeyTravel>().keyCap=cap;return b;}
  static void Light(Transform p,string n,Vector3 rotation,float power,Color color){var g=new GameObject(n,typeof(UnityEngine.Light));g.transform.SetParent(p,false);g.transform.rotation=Quaternion.Euler(rotation);var l=g.GetComponent<UnityEngine.Light>();l.type=LightType.Directional;l.intensity=power;l.color=color;l.shadows=LightShadows.None;}
 }
}
