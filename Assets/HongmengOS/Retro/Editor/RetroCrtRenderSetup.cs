using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using LingGuangV05.Desktop.OS;
using TMPro;
namespace HongmengOS.Retro.Editor
{
    public static class RetroCrtRenderSetup
    {
        [MenuItem("HongmengOS/Enable optical CRT in copied scene")]
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!=RetroDesktopStyleEditor.ScenePath)throw new InvalidOperationException("Copied scene in Edit mode required.");
            if(GameObject.Find("CRT Source Camera")!=null)throw new InvalidOperationException("CRT rendering already configured.");
            var monitor=GameObject.Find("HongmengOS Monitor").transform as RectTransform;
            var viewport=monitor.Find("Screen Viewport") as RectTransform;
            var os=viewport.Find("hongmengos") as RectTransform;
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            RetroCrtBuilder.Build(monitor,viewport,font);
            var display=monitor.Find("Retro CRT Glass").GetComponent<RawImage>();
            Directory.CreateDirectory("Assets/HongmengOS/Retro/Rendering");AssetDatabase.Refresh();
            var target=new RenderTexture(1024,768,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){name="HM-98 Raster 1024x768",antiAliasing=1,useMipMap=false,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            AssetDatabase.CreateAsset(target,"Assets/HongmengOS/Retro/Rendering/HM-98 Raster.renderTexture");
            var cameraObject=new GameObject("CRT Source Camera",typeof(Camera),typeof(UniversalAdditionalCameraData));
            var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=5;camera.nearClipPlane=.1f;camera.farClipPlane=100;camera.transform.position=new Vector3(0,0,-20);camera.depth=-10;camera.cullingMask=1<<5;camera.targetTexture=target;camera.allowHDR=false;camera.allowMSAA=false;
            var additional=cameraObject.GetComponent<UniversalAdditionalCameraData>();additional.renderPostProcessing=false;additional.renderShadows=false;
            os.SetParent(null,false);os.name="hongmengos Desktop Source";
            var canvas=os.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            var scaler=os.GetComponent<CanvasScaler>();scaler.enabled=true;scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,960);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;scaler.matchWidthOrHeight=.5f;
            foreach(var childCanvas in os.GetComponentsInChildren<Canvas>(true))childCanvas.worldCamera=camera;
            display.texture=target;display.uvRect=new Rect(0,0,1,1);display.raycastTarget=true;
            var input=display.gameObject.AddComponent<RetroCrtInputSurface>();input.displayRect=display.rectTransform;input.sourceCamera=camera;input.curvature=.10f;
            input.sourceRaycasters=os.GetComponentsInChildren<GraphicRaycaster>(true);foreach(var r in input.sourceRaycasters)r.enabled=false;
            var controller=monitor.gameObject.AddComponent<RetroCrtController>();controller.display=display;controller.sourceCamera=camera;controller.inputSurface=input;controller.desktopAudio=os.GetComponentInChildren<AudioManager>(true);controller.desktopAudio.enabled=false;controller.focus=.42f;controller.brightness=.5f;controller.flashStrength=.9f;
            var shell=monitor.Find("Retro CRT Casing");
            controller.powerLamp=shell.Find("Power lamp green lens").GetComponent<Graphic>();
            var switchObject=shell.Find("Power switch key").gameObject;var switchGraphic=switchObject.GetComponent<Graphic>();switchGraphic.raycastTarget=true;
            var power=switchObject.AddComponent<Button>();power.targetGraphic=switchGraphic;var colors=power.colors;colors.highlightedColor=new Color(1.06f,1.06f,1.06f,1);colors.pressedColor=new Color(.72f,.72f,.72f,1);colors.fadeDuration=.03f;power.colors=colors;
            UnityEventTools.AddPersistentListener(power.onClick,controller.TogglePower);
            controller.focusDial=Knob(shell.Find("FOCUS Knurled Dial") as RectTransform,controller,RetroCrtKnob.Control.Focus,.42f);
            controller.brightnessDial=Knob(shell.Find("BRIGHTNESS Knurled Dial") as RectTransform,controller,RetroCrtKnob.Control.Brightness,.5f);
            var bridge=monitor.gameObject.AddComponent<RetroCrtSourceBridge>();bridge.sourceCanvas=canvas;bridge.sourceCamera=camera;bridge.inputSurface=input;bridge.controller=controller;bridge.theme=controller.desktopAudio.UIManagerAsset;bridge.contextMenu=os.GetComponentInChildren<ContextMenuManager>(true);
            if(bridge.contextMenu!=null){bridge.contextMenu.cameraSource=ContextMenuManager.CameraSource.Custom;bridge.contextMenu.enableBlur=false;var so=new SerializedObject(bridge.contextMenu);so.FindProperty("targetCanvas").objectReferenceValue=canvas;so.FindProperty("targetCamera").objectReferenceValue=camera;so.ApplyModifiedPropertiesWithoutUndo();}
            var main=GameObject.Find("Main Camera")?.GetComponent<Camera>();if(main!=null){main.cullingMask=0;main.clearFlags=CameraClearFlags.SolidColor;main.backgroundColor=Color.black;}
            Canvas.ForceUpdateCanvases();
            var desktop=os.Find("Desktop");
            foreach(var w in desktop.Find("Apps & Windows").GetComponentsInChildren<WindowManager>(true)){
                var r=(RectTransform)w.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
                var wc=w.windowContainer;wc.anchorMin=Vector2.zero;wc.anchorMax=Vector2.one;
                float width=w.name=="Calculator"?450:Mathf.Min(1100,((RectTransform)w.transform.parent).rect.width-90);
                float height=w.name=="Calculator"?590:Mathf.Min(720,((RectTransform)w.transform.parent).rect.height-100);
                float pw=((RectTransform)w.transform.parent).rect.width,ph=((RectTransform)w.transform.parent).rect.height;
                wc.offsetMin=new Vector2((pw-width)/2,(ph-height)/2);wc.offsetMax=-wc.offsetMin;
            }
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        private static RectTransform Knob(RectTransform parent,RetroCrtController controller,RetroCrtKnob.Control parameter,float value)
        {
            var hit=parent.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,0);hit.raycastTarget=true;
            var holder=new GameObject("Rotating Pointer",typeof(RectTransform)).GetComponent<RectTransform>();holder.SetParent(parent,false);holder.anchorMin=holder.anchorMax=new Vector2(.5f,.5f);holder.sizeDelta=Vector2.zero;
            var pointer=parent.Find("Ivory dial pointer") as RectTransform;pointer.SetParent(holder,false);pointer.anchorMin=pointer.anchorMax=new Vector2(.5f,.5f);pointer.anchoredPosition=new Vector2(0,31);pointer.localRotation=Quaternion.identity;
            var knob=parent.gameObject.AddComponent<RetroCrtKnob>();knob.controller=controller;knob.parameter=parameter;knob.dial=holder;knob.normalizedValue=value;
            holder.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(130,-130,value));return holder;
        }
    }
}
