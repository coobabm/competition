#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Actual production canvases rendered offscreen. No native Player, model, font edits, or real save directory.
// Use the parent's unique-company/product wrapper: BuildSettings reads process-isolated preferences.
public static class ArchiveDesignPreview
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    public static void CaptureAndExit(){int code=0;try{Capture();}catch(Exception e){Debug.LogException(e);code=1;}EditorApplication.Exit(code);}
    public static void Capture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("EditMode only");
        var room=Resources.Load<Texture2D>("LingGuangV05/Branding/ArchiveRoomFallback");
        if(room==null||room.width<384||room.height<216)throw new InvalidOperationException("Archive room artwork missing or not imported. Preview cannot validate blank fallback textures.");
        string output=Path.GetFullPath(Path.Combine("LocalVerification","ArchiveDesign",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
        Directory.CreateDirectory(output);var scene=EditorSceneManager.NewPreviewScene();
        string previous=ChapterOneRuntime.VerificationSaveDirectory;
        var thumbnail=ChapterOneRuntime.ArchiveThumbnailProvider;
        GameObject world=null,host=null,saveHost=null;
        try {
            ChapterOneRuntime.VerificationSaveDirectory=Path.Combine(output,"Save");ChapterOneRuntime.ArchiveThumbnailProvider=null;
            using(GameText.Bind(()=>GameText.Chinese)) {
                world=new GameObject("Archive preview world");SceneManager.MoveGameObjectToScene(world,scene);
                var camera=world.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.orthographic=true;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.04f,.04f);
                var bg=new GameObject("Approved room artwork",typeof(RectTransform),typeof(Canvas));bg.transform.SetParent(world.transform,false);
                var background=bg.AddComponent<RawImage>();background.texture=room;background.raycastTarget=false;
                bg.GetComponent<Canvas>().sortingOrder=-10;
                saveHost=new GameObject("Isolated archive fixture");saveHost.SetActive(false);SceneManager.MoveGameObjectToScene(saveHost,scene);
                var runtime=saveHost.AddComponent<ChapterOneRuntime>();typeof(ChapterOneRuntime).GetField("SaveDirectoryOverride",Flags).SetValue(runtime,Path.Combine(output,"Save"));runtime.EnsureInitialized();
                var profile=PrologueProfile.Read("嘴硬心软");profile.name="青禾";profile.self="我";profile.callMe="你";
                if(!runtime.CompletePrologue(profile))throw new Exception(runtime.SaveStatus);
                foreach(int slot in new[]{2,3,4,5,6}){runtime.Sim.S.labState="{\"version\":1,\"stage\":"+(slot-1)+"}";if(!runtime.SaveSlot(slot))throw new Exception(runtime.SaveStatus);runtime.RenameManualArchive(slot,"训练记录 "+(slot-1));}
                host=new GameObject("Actual title HUD");SceneManager.MoveGameObjectToScene(host,scene);var hud=host.AddComponent<CaseMenuHud>();
                hud.Build(null,_=>{},_=>{},_=>{},()=>{},()=>{});hud.ConfigureTitleActions(()=>runtime,()=>{},()=>{});hud.SetChromeAlpha(1);
                Render("01-title",camera,world,host,saveHost,output);
                hud.OpenArchives();Render("02-title-saves",camera,world,host,saveHost,output);hud.CloseArchives();
                hud.ShowSettings(.7f,false,false);Render("03-settings",camera,world,host,saveHost,output);hud.HideSettings();
                hud.OpenEndingArchives();Render("04-endings",camera,world,host,saveHost,output);hud.CloseArchives();
                host.SetActive(false);saveHost.SetActive(true);var panel=saveHost.AddComponent<SaveSlotsPanel>();panel.Open();
                Render("05-aero-saves",camera,world,host,saveHost,output);
                typeof(SaveSlotsPanel).GetMethod("Close",Flags).Invoke(panel,null);
                File.WriteAllText(Path.Combine(output,"scope.txt"),"Actual CaseMenuHud and SaveSlotsPanel canvases at 1920x1080, rendered over approved static room art. Synthetic isolated save fixtures; not a 3D boot or Player flow test. No real player files/prefs or font assets changed. Ending cards intentionally locked: fixture has no completion receipt.");
            }
        } finally {
            if(host!=null)UnityEngine.Object.DestroyImmediate(host);if(saveHost!=null)UnityEngine.Object.DestroyImmediate(saveHost);if(world!=null)UnityEngine.Object.DestroyImmediate(world);
            ChapterOneRuntime.VerificationSaveDirectory=previous;ChapterOneRuntime.ArchiveThumbnailProvider=thumbnail;EditorSceneManager.ClosePreviewScene(scene);
        }
        Debug.Log("[ArchiveDesignPreview] "+output);
    }
    static void Render(string name,Camera camera,GameObject world,GameObject host,GameObject saveHost,string output)
    {
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32){antiAliasing=4};Texture2D pixels=null;var previous=RenderTexture.active;
        try {
            target.Create();camera.targetTexture=target;camera.aspect=16f/9;
            foreach(var root in new[]{world,host,saveHost}) {
                foreach(var image in root.GetComponentsInChildren<ArchiveThumbnailView>())if(image.GetComponent<RawImage>().texture==null)throw new InvalidOperationException("Archive thumbnail fallback texture missing: "+image.name);
                foreach(var canvas in root.GetComponentsInChildren<Canvas>(true)) {var scaler=canvas.GetComponent<CanvasScaler>();if(scaler!=null)scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.scaleFactor=root==saveHost?Mathf.Sqrt((1920/1366f)*(1080/900f)):1;}
                foreach(var child in root.GetComponentsInChildren<Transform>(true))child.gameObject.layer=30;
                foreach(var caret in root.GetComponentsInChildren<CaseMenuPageCaret>(true))caret.Refresh();
                foreach(var label in root.GetComponentsInChildren<TMP_Text>(true))label.ForceMeshUpdate(true,true);
            }
            Canvas.ForceUpdateCanvases();camera.Render();
            pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
            File.WriteAllBytes(Path.Combine(output,name+".png"),pixels.EncodeToPNG());
        } finally {RenderTexture.active=previous;camera.targetTexture=null;if(pixels!=null)UnityEngine.Object.DestroyImmediate(pixels);target.Release();UnityEngine.Object.DestroyImmediate(target);}
    }
}
#endif
