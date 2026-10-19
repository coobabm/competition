#if UNITY_EDITOR
using System;
using HongmengOS.Aero2010;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.OS;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LingGuangV05.Editor
{
    public static partial class ChapterOneInstaller
    {
        // Explicit additive scene migration, never run automatically while loading the game.
        // Data Import unlocks this folder and installation together, with no second charge.
        [MenuItem("Tools/LingGuang/Install Virtual Files Window")]
        public static void InstallFilesWindow()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait until compilation finishes and leave Play Mode.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath||scene.isDirty)throw new InvalidOperationException("Open and save Main before installing the files window.");
            var runtime=FindInScene<ChapterOneRuntime>(scene);
            var lcd=FindInScene<AeroLcdSourceBridge>(scene);
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if(runtime==null||lcd==null||lcd.sourceCanvas==null||font==null)throw new InvalidOperationException("Main desktop/font missing.");
            if(runtime.GetComponent<XgFilesWindow>()!=null)throw new InvalidOperationException("Files window already installed; no duplicate was created.");
            var template=FindWindow(lcd.sourceCanvas,"Notepad");
            if(template==null||template.taskbarButton==null||template.windowContainer==null||template.windowDragger==null)
                throw new InvalidOperationException("Native Notepad window template missing.");
            var desktop=template.transform.parent.parent;
            var desktopList=desktop.Find("Desktop List");
            var layout=template.taskbarButton.GetComponentInParent<AeroTaskbarLayout>();
            if(desktopList==null||desktopList.GetComponent<ItemDragContainer>()==null||layout==null)
                throw new InvalidOperationException("Native desktop/taskbar layout missing.");
            var spec=new AppSpec(XgFilesWindow.AppId,"AI Files",null,"");
            var window=CloneWindow(template,lcd,font,spec,out var content,out var close,out var minimize,out var maximize);
            CloneTaskbar(template.taskbarButton,window,font,layout,spec);
            var shortcut=CreateDesktopShortcut(desktopList,font,spec);EnsureDesktopDrag(shortcut.transform,desktopList);
            var view=runtime.gameObject.AddComponent<XgFilesWindow>();
            view.window=window;view.client=content;view.font=font;view.sourceCanvas=lcd.sourceCanvas;
            view.shortcutRoot=shortcut.gameObject;shortcut.gameObject.SetActive(false);
            view.titleLabel=window.windowDragger.transform.Find("ChapterOneTitle").GetComponent<TMP_Text>();
            view.desktopLabel=shortcut.transform.Find("ChapterOneDesktopLabel").GetComponent<TMP_Text>();
            view.titleLabel.text=view.desktopLabel.text="资料";
            var status=window.windowContainer.Find("Aero Status Bar/Status");if(status!=null)status.gameObject.SetActive(false);
            UnityEventTools.AddPersistentListener(shortcut.onClick,view.Show);
            UnityEventTools.AddPersistentListener(close.onClick,window.CloseWindow);
            UnityEventTools.AddPersistentListener(minimize.onClick,window.MinimizeWindow);
            UnityEventTools.AddPersistentListener(maximize.onClick,window.FullscreenWindow);
            // Runtime Awake explicitly RegisterTree includes this inactive factory-created root.
            SetLayer(window.transform,5);SetLayer(shortcut.transform,5);
            EditorUtility.SetDirty(runtime);EditorUtility.SetDirty(layout);
            EditorSceneManager.MarkSceneDirty(scene);
            // Deliberately leave the scene reviewable, not auto-saved or run.
        }
    }
}
#endif
