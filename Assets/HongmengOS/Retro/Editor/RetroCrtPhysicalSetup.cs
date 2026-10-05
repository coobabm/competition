using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

namespace HongmengOS.Retro.Editor
{
    /// <summary>Scene-local conversion. Does not edit project layers or shared DreamOS assets.</summary>
    public static class RetroCrtPhysicalSetup
    {
        const float UiScale=4.35f/1024f;
        public static void Apply()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!=RetroDesktopStyleEditor.ScenePath)throw new InvalidOperationException("Copied CRT scene in Edit mode required");
            var monitor=GameObject.Find("HongmengOS Monitor").GetComponent<RectTransform>();
            if(monitor.GetComponent<Canvas>().renderMode==RenderMode.WorldSpace&&GameObject.Find("HongmengOS CRT 3D Model")!=null)return;
            var model=RetroDesktopMonitorModelBuilder.Build();
            var c=monitor.GetComponent<RetroCrtController>();
            var main=GameObject.Find("Main Camera").GetComponent<Camera>();
            foreach(var layout in monitor.GetComponentsInChildren<CrtAspectLayout>(true))UnityEngine.Object.DestroyImmediate(layout);
            foreach(Transform child in monitor)if(child!=c.display.transform)child.gameObject.SetActive(false);
            var canvas=monitor.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=main;canvas.sortingOrder=0;canvas.overrideSorting=false;
            var scaler=monitor.GetComponent<CanvasScaler>();if(scaler!=null){scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=1;scaler.dynamicPixelsPerUnit=1;}
            monitor.anchorMin=monitor.anchorMax=new Vector2(.5f,.5f);monitor.pivot=new Vector2(.5f,.5f);monitor.sizeDelta=new Vector2(1280,1080);
            monitor.position=new Vector3(0,0,-.135f);monitor.rotation=Quaternion.identity;monitor.localScale=Vector3.one*UiScale;
            var glass=c.display.rectTransform;Center(glass,new Vector2(0,.12f/UiScale),new Vector2(1024,768));glass.localRotation=Quaternion.identity;glass.localScale=Vector3.one;
            var mat=c.display.material;mat.SetFloat("_Curvature",.025f);mat.SetFloat("_Reflection",.15f);mat.SetFloat("_Chromatic",.18f);mat.SetFloat("_Focus",.84f);mat.SetFloat("_Bloom",.10f);mat.SetFloat("_Halation",.045f);mat.SetFloat("_Vignette",.23f);mat.SetFloat("_ScanLines",320);mat.SetFloat("_Aspect",4f/3);mat.SetFloat("_Power",1);mat.SetFloat("_Flash",0);mat.SetFloat("_Collapse",1);EditorUtility.SetDirty(mat);
            c.inputSurface.curvature=.025f;c.inputSurface.flipY=false;c.initialPower=true;c.focus=.84f;c.brightness=.5f;c.focusDial=null;c.brightnessDial=null;
            var hardware=new GameObject("CRT Front Controls",typeof(RectTransform)).GetComponent<RectTransform>();hardware.SetParent(monitor,false);Center(hardware,Vector2.zero,monitor.sizeDelta);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var power=Hit("CRT Power Button",hardware,new Vector2(1.93f,-1.94f),new Vector2(.48f,.21f));
            UnityEventTools.AddPersistentListener(power.onClick,c.TogglePower);
            Key(hardware,c,"Focus minus",new Vector2(-.50f,-1.94f),RetroCrtPictureButton.Control.Focus,-.05f);
            Key(hardware,c,"Focus plus",new Vector2(-.10f,-1.94f),RetroCrtPictureButton.Control.Focus,.05f);
            Key(hardware,c,"Brightness minus",new Vector2(.52f,-1.94f),RetroCrtPictureButton.Control.Brightness,-.05f);
            Key(hardware,c,"Brightness plus",new Vector2(.92f,-1.94f),RetroCrtPictureButton.Control.Brightness,.05f);
            LinkKey(power.gameObject,model.transform.Find("Power Button Mount/Keycap"));
            LinkKey(hardware.Find("Focus minus").gameObject,model.transform.Find("Focus Control Mount/Focus Minus Mount/Keycap"));
            LinkKey(hardware.Find("Focus plus").gameObject,model.transform.Find("Focus Control Mount/Focus Plus Mount/Keycap"));
            LinkKey(hardware.Find("Brightness minus").gameObject,model.transform.Find("Brightness Control Mount/Brightness Minus Mount/Keycap"));
            LinkKey(hardware.Find("Brightness plus").gameObject,model.transform.Find("Brightness Control Mount/Brightness Plus Mount/Keycap"));
            var lamp=new GameObject("CRT Power Indicator",typeof(RectTransform),typeof(Image)).GetComponent<Image>();lamp.transform.SetParent(hardware,false);Center(lamp.rectTransform,new Vector2(2.32f,-1.94f)/UiScale,new Vector2(.055f,.055f)/UiScale);lamp.color=c.lampOnColor;lamp.raycastTarget=false;c.powerLamp=lamp;
            lamp.enabled=false;
            var lens=model.transform.Find("Power Indicator Mount/Power Indicator Lens");
            if(lens!=null){var visual=lens.gameObject.AddComponent<RetroCrtLampVisual>();visual.controller=c;visual.lamp=lens.GetComponent<Renderer>();}
            foreach(var t in monitor.GetComponentsInChildren<Transform>(true))t.gameObject.layer=0;
            main.cullingMask=1;main.orthographic=false;main.fieldOfView=34;main.nearClipPlane=.1f;main.farClipPlane=60;main.targetTexture=null;main.depth=0;main.clearFlags=CameraClearFlags.SolidColor;main.backgroundColor=new Color(.035f,.045f,.053f);main.allowHDR=true;main.allowMSAA=true;
            main.transform.position=new Vector3(3.2f,1.0f,-11.9f);main.transform.LookAt(new Vector3(0,-.40f,.55f));
            var additional=main.GetComponent<UniversalAdditionalCameraData>()??main.gameObject.AddComponent<UniversalAdditionalCameraData>();additional.renderPostProcessing=false;additional.renderShadows=true;additional.antialiasing=AntialiasingMode.None;
            c.sourceCamera.cullingMask=1<<5;
            var lightRoot=new GameObject("CRT Studio Lighting");
            Light("Softbox Key",lightRoot.transform,new Vector3(35,-30,0),1.2f,new Color(1,.965f,.91f),true);
            Light("Cool Fill",lightRoot.transform,new Vector3(30,135,0),.5f,new Color(.78f,.86f,1),false);
            RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.43f,.46f,.50f);RenderSettings.ambientIntensity=1;
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="CRT Desk Surface";floor.transform.position=new Vector3(0,-2.99f,1);floor.transform.localScale=new Vector3(32,.20f,28);UnityEngine.Object.DestroyImmediate(floor.GetComponent<Collider>());
            var fm=new Material(Shader.Find("Universal Render Pipeline/Lit"));fm.name="CRT Desk Charcoal";fm.SetColor("_BaseColor",new Color(.085f,.095f,.10f));fm.SetFloat("_Smoothness",.24f);
            string path="Assets/HongmengOS/Retro/CRT/CRT Desk.mat";var old=AssetDatabase.LoadAssetAtPath<Material>(path);if(old==null){AssetDatabase.CreateAsset(fm,path);old=fm;}else{EditorUtility.CopySerialized(fm,old);UnityEngine.Object.DestroyImmediate(fm);}floor.GetComponent<Renderer>().sharedMaterial=old;
            FinalizeGeometry();
            Canvas.ForceUpdateCanvases();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        public static void FinalizeGeometry()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.path!=RetroDesktopStyleEditor.ScenePath)throw new InvalidOperationException("Copied CRT scene in Edit mode required");
            var monitor=GameObject.Find("HongmengOS Monitor").transform;
            monitor.position=new Vector3(0,0,.028f);
            var hardware=monitor.Find("CRT Front Controls");
            if(hardware!=null){var p=hardware.localPosition;p.z=(-.135f-.028f)/UiScale;hardware.localPosition=p;}
            var source=GameObject.Find("hongmengos Desktop Source");foreach(var t in source.GetComponentsInChildren<Transform>(true))t.gameObject.layer=5;
            // Default gameplay state: straight-on and as large as the fascia and
            // physical controls allow. The stand is intentionally below frame.
            var cam=Camera.main;cam.transform.position=new Vector3(0,.04f,-8f);cam.transform.rotation=Quaternion.identity;cam.fieldOfView=32;
            var bed=GameObject.Find("CRT Tube Backing");if(bed==null){bed=GameObject.CreatePrimitive(PrimitiveType.Cube);bed.name="CRT Tube Backing";UnityEngine.Object.DestroyImmediate(bed.GetComponent<Collider>());}
            bed.transform.SetParent(GameObject.Find("HongmengOS CRT 3D Model").transform,false);bed.transform.localPosition=new Vector3(0,.12f,.16f);bed.transform.localScale=new Vector3(4.50f,3.40f,.10f);
            const string path="Assets/HongmengOS/Retro/CRT/CRT Tube Backing.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetColor("_BaseColor",new Color(.008f,.012f,.010f));mat.SetFloat("_Smoothness",.55f);EditorUtility.SetDirty(mat);bed.GetComponent<Renderer>().sharedMaterial=mat;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        static void Center(RectTransform r,Vector2 p,Vector2 s){r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=p;r.sizeDelta=s;r.localPosition=new Vector3(r.localPosition.x,r.localPosition.y,0);}
        static Button Hit(string name,RectTransform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            var image=go.GetComponent<Image>();image.color=new Color(1,1,1,.001f);Center(image.rectTransform,position/UiScale,size/UiScale);
            var b=go.GetComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.ColorTint;var colors=b.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.8f,.92f,1,.06f);colors.pressedColor=new Color(.2f,.2f,.2f,.22f);b.colors=colors;
            return b;
        }
        static void Key(RectTransform parent,RetroCrtController c,string name,Vector2 pos,RetroCrtPictureButton.Control parameter,float step)
        {
            var b=Hit(name,parent,pos,new Vector2(.30f,.18f));var p=b.gameObject.AddComponent<RetroCrtPictureButton>();p.controller=c;p.parameter=parameter;p.step=step;UnityEventTools.AddPersistentListener(b.onClick,p.Adjust);
        }
        static void LinkKey(GameObject hit,Transform cap){if(cap!=null)hit.AddComponent<RetroCrtKeyTravel>().keyCap=cap;}
        static void Light(string name,Transform parent,Vector3 rotation,float intensity,Color color,bool shadows)
        {
            var go=new GameObject(name,typeof(UnityEngine.Light));go.transform.SetParent(parent);go.transform.rotation=Quaternion.Euler(rotation);var l=go.GetComponent<UnityEngine.Light>();l.type=LightType.Directional;l.intensity=intensity;l.color=color;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowBias=.02f;
        }
    }
}
