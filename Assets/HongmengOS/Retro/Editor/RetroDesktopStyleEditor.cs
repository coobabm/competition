using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using LingGuangV05.Desktop.Apps;
using LingGuangV05.Desktop.OS;
using Object=UnityEngine.Object;

namespace HongmengOS.Retro.Editor
{
    public static class RetroDesktopStyleEditor
    {
        public const string ScenePath="Assets/LingGuangV05/DesktopBridge/OSArt/Scenes/90desktop.unity";
        const string Base="Assets/HongmengOS/Retro/";
        static readonly Color Gray=new Color32(192,192,192,255), Navy=new Color32(0,0,128,255), Teal=new Color32(0,128,128,255);
        static TMP_FontAsset font;
        static Dictionary<string,Sprite> icons;

        [MenuItem("HongmengOS/Apply Classic 98 to copied scene")]
        public static void ApplyTheme()
        {
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path!=ScenePath||EditorApplication.isPlaying)throw new InvalidOperationException("Open the copied Retro scene in Edit mode.");
            var monitor=GameObject.Find("HongmengOS Monitor").transform as RectTransform;
            if(monitor.Find("Retro 98 Applied")!=null)throw new InvalidOperationException("Theme already applied; use targeted edits.");
            var viewport=(RectTransform)monitor.Find("Screen Viewport");
            var os=viewport.Find("hongmengos");
            if(PrefabUtility.IsPartOfPrefabInstance(os.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(os.gameObject),PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            icons=RetroIconBuilder.BuildAssets();
            Directory.CreateDirectory(Base+"Theme");AssetDatabase.Refresh();
            var theme=CopyAsset<UIManager>(os.GetComponentInChildren<AudioManager>(true).UIManagerAsset,Base+"Theme/Classic 98 UI.asset");
            theme.enableUIBlur=false;theme.enableDynamicUpdate=false;theme.selectedTheme=UIManager.SelectedTheme.Default;
            theme.primaryColorDark=Color.black;theme.secondaryColorDark=new Color32(80,80,80,255);
            theme.windowBGColorDark=Gray;theme.backgroundColorDark=Gray;theme.taskBarColorDark=Gray;
            theme.highlightedColorDark=Navy;theme.highlightedColorCustom=Navy;theme.highlightedColorSecondaryDark=Color.white;theme.highlightedColorSecondaryCustom=Color.white;
            theme.systemFontThin=theme.systemFontLight=theme.systemFontRegular=theme.systemFontSemiBold=theme.systemFontBold=font;
            theme.enableKeystrokes=true;theme.enableMouseKeystroke=true;theme.enableKeyboardKeystroke=false;
            var appSource=os.GetComponentInChildren<AppElement>(true).appLibrary;
            var library=CopyAsset<AppLibrary>(appSource,Base+"Theme/Classic 98 Apps.asset");
            library.alwaysUpdate=false;library.optimizeUpdates=true;
            foreach(var app in library.apps)if(icons.TryGetValue(app.appTitle,out var icon)) {
                app.appIconBig=app.appIconMedium=app.appIconSmall=icon;app.appIconPreview=icon.texture;app.gradientLeft=app.gradientRight=Color.white;
            }
            // Rebind all serialized theme references including private SettingsApp fields.
            foreach(var c in os.GetComponentsInChildren<MonoBehaviour>(true)) {
                if(c==null)continue;var so=new SerializedObject(c);var p=so.GetIterator();bool changed=false;
                while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference) {
                    if(p.objectReferenceValue is UIManager){p.objectReferenceValue=theme;changed=true;}
                    else if(p.objectReferenceValue is AppLibrary){p.objectReferenceValue=library;changed=true;}
                }
                if(changed)so.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach(var e in os.GetComponentsInChildren<UIManagerElement>(true)) {
                e.themeManagerAsset=theme;e.keepAlphaValue=true;e.useCustomFont=false;e.UpdateElement();
            }
            foreach(var t in os.GetComponentsInChildren<TMP_Text>(true)) {
                t.font=font;t.fontStyle&=~FontStyles.LowerCase;t.fontStyle&=~FontStyles.UpperCase;
                if(t.fontSize>16&&t.fontSize<42)t.fontSize=Mathf.Max(16,t.fontSize*.88f);
            }
            foreach(var a in os.GetComponentsInChildren<AppElement>(true)) {
                a.appLibrary=library;a.useGradient=false;
                var gradient=a.GetComponent<UIGradient>();if(gradient!=null)Object.DestroyImmediate(gradient);
                if(a.elementType==AppElement.ElementType.Gradient)a.elementType=AppElement.ElementType.None;
                if(a.elementType==AppElement.ElementType.Icon&&icons.TryGetValue(a.appID,out var icon)) {
                    var img=a.GetComponent<Image>();if(img!=null){img.sprite=icon;FixedColor(img,Color.white);}
                }
            }
            foreach(var i in os.GetComponentsInChildren<Image>(true)) {
                if(i.name=="Background Blur"||i.name=="Blur")i.gameObject.SetActive(false);
                if(i.sprite!=null&&i.sprite.name.StartsWith("Rounded")&&(i.name=="Background"||i.name=="Indicator")){i.sprite=null;i.type=UnityEngine.UI.Image.Type.Simple;}
                if(i.name=="Background" && i.color.a>.3f)FixedColor(i,Gray);
            }
            foreach(var b in os.GetComponentsInChildren<ButtonManager>(true)) {
                b.useRipple=false;b.useHoverEffect=false;b.fadingMultiplier=15;b.textSize=Mathf.Clamp(b.textSize,16,22);
                if(b.hoverEffect!=null)b.hoverEffect.gameObject.SetActive(false);
                if(b.rippleParent!=null)b.rippleParent.SetActive(false);
            }
            var desktop=os.Find("Desktop");
            Desktop(desktop);
            foreach(var w in desktop.Find("Apps & Windows").GetComponentsInChildren<WindowManager>(true))Window(w);
            Taskbar(desktop);
            StartMenu(desktop);
            var user=os.GetComponentInChildren<UserManager>(true);
            if(user!=null){user.disableUserCreating=true;user.disableLockScreen=true;user.systemPassword="";}
            EditorUtility.SetDirty(theme);EditorUtility.SetDirty(library);
            new GameObject("Retro 98 Applied",typeof(RectTransform)).transform.SetParent(monitor,false);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        }
        static T CopyAsset<T>(T source,string path) where T:Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);if(existing!=null)return existing;
            if(source==null)throw new InvalidOperationException("Missing source "+typeof(T).Name);
            var clone=Object.Instantiate(source);clone.name=Path.GetFileNameWithoutExtension(path);AssetDatabase.CreateAsset(clone,path);return clone;
        }
        static void Desktop(Transform desktop)
        {
            var wallpaper=desktop.Find("Wallpaper");
            Object.DestroyImmediate(wallpaper.GetComponent<WallpaperObject>());
            var fit=wallpaper.GetComponent<AspectRatioFitter>();if(fit!=null)Object.DestroyImmediate(fit);
            var img=wallpaper.GetComponent<Image>();img.sprite=null;img.color=Teal;Stretch((RectTransform)wallpaper);
            desktop.Find("Widgets").gameObject.SetActive(false);
            var grid=desktop.Find("Desktop List").GetComponent<GridLayoutGroup>();
            grid.cellSize=new Vector2(112,94);grid.spacing=new Vector2(20,12);grid.padding=new RectOffset(26,12,24,12);
            grid.constraint=GridLayoutGroup.Constraint.FixedRowCount;grid.constraintCount=7;grid.startAxis=GridLayoutGroup.Axis.Vertical;
            foreach(Transform item in grid.transform) {
                var icon=(RectTransform)item.Find("Icon Background/Icon");icon.sizeDelta=new Vector2(56,56);
                var shadow=item.Find("Icon Background/Shadow");if(shadow!=null)shadow.gameObject.SetActive(false);
                var title=item.Find("Title").GetComponent<TMP_Text>();FixedText(title,Color.white);title.fontSize=16;title.alignment=TextAlignmentOptions.Center;title.enableAutoSizing=false;
                var r=title.rectTransform;r.sizeDelta=new Vector2(r.sizeDelta.x,30);
                var normal=item.Find("Normal/Background").GetComponent<Image>();FixedColor(normal,new Color(0,0,0,0));
                var hover=item.Find("Highlighted/Background").GetComponent<Image>();FixedColor(hover,Navy);
            }
        }
        static void Window(WindowManager w)
        {
            w.useBackgroundBlur=false;
            var so=new SerializedObject(w);so.FindProperty("waitForAnimationCompletion").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
            var r=(RectTransform)w.transform;Stretch(r);
            var wc=w.windowContainer;
            float width=w.name=="Calculator"?450:Mathf.Min(1140,r.rect.width-90);
            float height=w.name=="Calculator"?560:Mathf.Min(620,r.rect.height-70);
            wc.anchorMin=Vector2.zero;wc.anchorMax=Vector2.one;
            wc.offsetMin=new Vector2((r.rect.width-width)/2+30,(r.rect.height-height)/2);
            wc.offsetMax=new Vector2(-(r.rect.width-width)/2+30,-(r.rect.height-height)/2);
            var container=w.transform.Find("Container");if(container==null)return;
            var bg=container.Find("Background")?.GetComponent<Image>();if(bg!=null){FixedColor(bg,Gray);bg.sprite=null;}
            var shadow=container.Find("Shadow")?.GetComponent<Image>();if(shadow!=null)shadow.color=new Color(0,0,0,.3f);
            var content=container.Find("Content") as RectTransform;
            if(content!=null){content.offsetMin=new Vector2(5,27);content.offsetMax=new Vector2(-5,-41);}
            var dragger=container.Find("Dragger") as RectTransform;
            if(dragger!=null) {
                dragger.anchorMin=new Vector2(0,1);dragger.anchorMax=Vector2.one;dragger.pivot=new Vector2(.5f,1);dragger.offsetMin=new Vector2(5,-37);dragger.offsetMax=new Vector2(-5,-5);
                var bar=dragger.GetComponent<Image>()??dragger.gameObject.AddComponent<Image>();FixedColor(bar,Navy);bar.raycastTarget=true;
                var title=Text("Retro Title",dragger,w.name=="Settings"?"Control Panel - hongmengos 98":w.name,19,Color.white);title.fontStyle=FontStyles.Bold;
                SetRect(title.rectTransform,new Vector2(0,0),new Vector2(1,1),new Vector2(37,0),new Vector2(-148,0));
                if(icons.TryGetValue(w.name,out var icon)){var im=Image("Retro Title Icon",dragger,Color.white);im.sprite=icon;Box(im.rectTransform,8,4,24,24);}
                var buttons=content?.Find("Window Buttons") as RectTransform;
                if(buttons!=null){buttons.SetParent(dragger,false);buttons.anchorMin=buttons.anchorMax=new Vector2(1,.5f);buttons.pivot=new Vector2(1,.5f);buttons.anchoredPosition=new Vector2(-3,0);buttons.sizeDelta=new Vector2(118,27);
                    var layout=buttons.GetComponent<HorizontalLayoutGroup>();layout.spacing=3;layout.padding=new RectOffset();layout.childControlWidth=true;layout.childForceExpandWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=true;
                    foreach(Transform b in buttons){var be=Bevel(b,false);var fill=Image("Retro Button Face",b,Gray);Stretch(fill.rectTransform);fill.transform.SetAsFirstSibling();be.transform.SetAsLastSibling();var fb=b.gameObject.AddComponent<RetroButtonFeedback>();fb.bevel=be;}
                }
                dragger.SetAsLastSibling();
            }
            var edge=Bevel(container,false);edge.transform.SetAsLastSibling();
            var status=Image("Retro Status",container,Gray);SetRect(status.rectTransform,Vector2.zero,new Vector2(1,0),new Vector2(5,5),new Vector2(-5,25));Bevel(status.transform,true);
            var st=Text("Status Text",status.transform,"Ready",13,Color.black);Stretch(st.rectTransform,new Vector2(8,0),new Vector2(-8,0));
            if(w.name=="Settings"){
                var about=container.Find("Content/Main Content/About");
                if(about!=null)foreach(var t in about.GetComponentsInChildren<TMP_Text>(true)){
                    if(t.text=="hongmengos") { t.text="hongmengos 98";t.fontSize=40;t.fontStyle=FontStyles.Bold;FixedText(t,Color.black); }
                    if(t.text=="3.0.0")t.text="Classic Edition";
                }
            }
        }
        static void Taskbar(Transform desktop)
        {
            var taskbar=desktop.Find("Taskbar");var tr=(RectTransform)taskbar;tr.sizeDelta=new Vector2(tr.sizeDelta.x,48);
            FixedColor(taskbar.Find("Background").GetComponent<Image>(),Gray);Bevel(taskbar,false);
            var start=taskbar.Find("Quick Center Button");
            start.GetComponent<Animator>().enabled=false;var animated=start.GetComponent<AnimatedIconHandler>();if(animated!=null)animated.enabled=false;
            foreach(Transform child in start)child.gameObject.SetActive(false);
            var sr=(RectTransform)start;sr.anchorMin=sr.anchorMax=new Vector2(0,.5f);sr.pivot=new Vector2(0,.5f);sr.anchoredPosition=new Vector2(5,0);sr.sizeDelta=new Vector2(112,38);
            var face=Image("Retro Start Face",start,Gray);Stretch(face.rectTransform);var bevel=Bevel(start,false);start.gameObject.AddComponent<RetroButtonFeedback>().bevel=bevel;
            var logo=Image("Retro Start Icon",start,Color.white);logo.sprite=icons["Settings"];Box(logo.rectTransform,8,5,28,28);
            var label=Text("Retro Start Text",start,"Start",22,Color.black);label.fontStyle=FontStyles.Bold;Stretch(label.rectTransform,new Vector2(42,0),new Vector2(-5,0));
            var clock=taskbar.Find("Date & Time");Bevel(clock,true);
            var list=(RectTransform)taskbar.Find("Shortcut List");SetRect(list,new Vector2(0,0),new Vector2(1,1),new Vector2(128,0),new Vector2(-292,0));
            list.GetComponent<HorizontalLayoutGroup>().enabled=false;
            var entries=new List<RetroTaskbarLayout.Entry>();
            foreach(Transform t in list) {
                var b=t.GetComponent<TaskbarButton>();if(b==null)continue;
                var r=(RectTransform)t;r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(0,.5f);
                var face2=Image("Retro Task Face",t,Gray);Stretch(face2.rectTransform,new Vector2(0,1),new Vector2(0,-1));face2.transform.SetAsFirstSibling();
                var be=Bevel(t,false);var icon=(RectTransform)t.Find("Icon");icon.anchorMin=icon.anchorMax=new Vector2(0,.5f);icon.pivot=new Vector2(.5f,.5f);icon.sizeDelta=new Vector2(28,28);icon.anchoredPosition=new Vector2(21,0);
                var text=Text("Retro Task Label",t,t.name,16,Color.black);Stretch(text.rectTransform,new Vector2(42,0),new Vector2(-7,0));text.overflowMode=TextOverflowModes.Ellipsis;text.textWrappingMode=TextWrappingModes.NoWrap;
                var sep=t.Find("Seperator");if(sep!=null)sep.gameObject.SetActive(false);
                entries.Add(new RetroTaskbarLayout.Entry{button=b,rect=r,icon=icon,bevel=be,label=text});
            }
            list.gameObject.AddComponent<RetroTaskbarLayout>().entries=entries.ToArray();
        }
        static void StartMenu(Transform desktop)
        {
            desktop.Find("Quick Center").gameObject.SetActive(false);
            var start=desktop.Find("Taskbar/Quick Center Button").GetComponent<ButtonManager>();
            start.onClick=new UnityEngine.Events.UnityEvent();start.onDoubleClick=new UnityEngine.Events.UnityEvent();start.useCustomContent=true;start.bypassUpdateOnEnable=true;start.checkForDoubleClick=false;
            var host=new GameObject("Retro Start Controller",typeof(RetroStartMenu));host.transform.SetParent(desktop,false);var control=host.GetComponent<RetroStartMenu>();
            var backdrop=Image("Retro Start Menu",desktop,new Color(0,0,0,0));Stretch(backdrop.rectTransform);backdrop.raycastTarget=true;
            control.menu=backdrop.gameObject;var close=backdrop.gameObject.AddComponent<Button>();close.transition=Selectable.Transition.None;UnityEventTools.AddPersistentListener(close.onClick,control.Close);
            var panel=Image("Programs",backdrop.transform,Gray);Box(panel.rectTransform,5,49,314,614);panel.raycastTarget=true;Bevel(panel.transform,false);
            var strip=Image("Brand Strip",panel.transform,new Color32(128,128,128,255));SetRect(strip.rectTransform,Vector2.zero,new Vector2(0,1),new Vector2(5,5),new Vector2(40,-5));
            var brand=Text("Brand",strip.transform,"hongmengos 98",22,Color.white);brand.fontStyle=FontStyles.Bold;brand.rectTransform.anchorMin=brand.rectTransform.anchorMax=new Vector2(.5f,.08f);brand.rectTransform.sizeDelta=new Vector2(310,30);brand.rectTransform.pivot=new Vector2(0,.5f);brand.rectTransform.localEulerAngles=new Vector3(0,0,90);
            var heading=Text("Heading",panel.transform,"Programs",18,Color.black);heading.fontStyle=FontStyles.Bold;Box(heading.rectTransform,56,566,246,38);
            int i=0;
            foreach(Transform item in desktop.Find("Desktop List")){
                var window=desktop.Find("Apps & Windows/"+item.name)?.GetComponent<WindowManager>();if(window==null)continue;
                var row=Image(item.name,panel.transform,Gray);Box(row.rectTransform,47,524-i*39,258,38);row.raycastTarget=true;
                var button=row.gameObject.AddComponent<Button>();var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.72f,.72f,.9f);colors.pressedColor=new Color(.56f,.56f,.72f);colors.fadeDuration=0;button.colors=colors;
                UnityEventTools.AddPersistentListener(button.onClick,window.OpenWindow);UnityEventTools.AddPersistentListener(button.onClick,control.Close);
                var icon=Image("Icon",row.transform,Color.white);icon.sprite=icons[item.name];Box(icon.rectTransform,8,5,28,28);
                var title=Text("Label",row.transform,item.name,17,Color.black);Stretch(title.rectTransform,new Vector2(47,0),new Vector2(-5,0));i++;
            }
            var foot=Text("Footer",panel.transform,"hongmengos  /  Classic Desktop",12,new Color32(80,80,80,255));Box(foot.rectTransform,56,9,242,20);
            UnityEventTools.AddPersistentListener(start.onClick,control.Toggle);backdrop.gameObject.SetActive(false);
        }
        public static void PolishDialogs()
        {
            if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath||EditorApplication.isPlaying)throw new InvalidOperationException("Copied scene Edit mode required.");
            font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            var desktop=GameObject.Find("HongmengOS Monitor/Screen Viewport/hongmengos/Desktop").transform;
            foreach(var b in desktop.GetComponentsInChildren<ButtonManager>(true)) {
                if(!b.transform.IsChildOf(desktop.Find("Apps & Windows"))||b.transform.Find("Retro Bevel")!=null)continue;
                var bg=b.transform.Find("Normal/Background")?.GetComponent<Image>();
                if(bg==null||bg.color.a<.3f)continue;
                FixedColor(bg,Gray);bg.sprite=null;var bevel=Bevel(b.transform,false);b.gameObject.AddComponent<RetroButtonFeedback>().bevel=bevel;
            }
            foreach(var input in desktop.GetComponentsInChildren<TMP_InputField>(true)) {
                var bg=input.GetComponent<Image>()??input.transform.Find("Background")?.GetComponent<Image>();
                if(bg!=null){FixedColor(bg,Color.white);bg.sprite=null;}
                if(input.transform.Find("Retro Bevel")==null)Bevel(input.transform,true);
            }
            var settings=desktop.Find("Apps & Windows/Settings").GetComponent<WindowManager>();
            foreach(var p in settings.GetComponentsInChildren<PanelButton>(true)) {
                if(p.transform.Find("Retro Bevel")==null)Bevel(p.transform,false);
                var so=new SerializedObject(p);var selected=so.FindProperty("selectCG").objectReferenceValue as CanvasGroup;
                if(selected!=null&&selected.transform.Find("Classic Selection")==null){var fill=Image("Classic Selection",selected.transform,Navy);Stretch(fill.rectTransform,new Vector2(4,4),new Vector2(-4,-4));fill.transform.SetAsFirstSibling();}
                var st=so.FindProperty("selectedTextObj").objectReferenceValue as TMP_Text;if(st!=null)FixedText(st,Color.white);
                var si=so.FindProperty("selectedImageObj").objectReferenceValue as Image;if(si!=null)FixedColor(si,Color.white);
            }
            var about=settings.transform.Find("Container/Content/Main Content/About");
            if(about.Find("Classic System Card")==null) {
                var card=Image("Classic System Card",about,new Color32(212,212,204,255));SetRect(card.rectTransform,Vector2.zero,Vector2.one,new Vector2(32,46),new Vector2(-32,-155));Bevel(card.transform,true);
                var icon=Image("Computer",card.transform,Color.white);icon.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Base+"Icons/Generated/Settings.png");
                // Icon names are sanitized by the generator; use the cloned library as authoritative source.
                var app=AssetDatabase.LoadAssetAtPath<AppLibrary>(Base+"Theme/Classic 98 Apps.asset");foreach(var item in app.apps)if(item.appTitle=="Settings")icon.sprite=item.appIconBig;
                icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,1);icon.rectTransform.pivot=new Vector2(0,1);icon.rectTransform.anchoredPosition=new Vector2(26,-30);icon.rectTransform.sizeDelta=new Vector2(112,112);
                var title=Text("System Heading",card.transform,"hongmengos 98",24,Color.black);title.fontStyle=FontStyles.Bold;SetRect(title.rectTransform,new Vector2(0,1),Vector2.one,new Vector2(165,-64),new Vector2(-20,-24));
                var info=Text("System Details",card.transform,"Classic Desktop Environment\n\nDisplay       HM-98 Colour CRT\nInterface    13 desktop applications\nAudio         Mouse-click feedback\n\nA 1990s-inspired desktop for your game.",17,Color.black);SetRect(info.rectTransform,Vector2.zero,Vector2.one,new Vector2(166,34),new Vector2(-22,-78));info.alignment=TextAlignmentOptions.TopLeft;
                var caption=Text("Computer Caption",card.transform,"Personal\nComputer",15,new Color32(60,60,60,255));SetRect(caption.rectTransform,new Vector2(0,1),new Vector2(0,1),new Vector2(25,-196),new Vector2(140,-149));caption.alignment=TextAlignmentOptions.Center;
                var ok=Image("Classic OK",about,Gray);ok.raycastTarget=true;SetRect(ok.rectTransform,new Vector2(1,0),new Vector2(1,0),new Vector2(-138,7),new Vector2(-32,37));var bevel=Bevel(ok.transform,false);ok.gameObject.AddComponent<RetroButtonFeedback>().bevel=bevel;var button=ok.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;UnityEventTools.AddPersistentListener(button.onClick,settings.CloseWindow);
                var label=Text("Label",ok.transform,"OK",17,Color.black);Stretch(label.rectTransform);label.alignment=TextAlignmentOptions.Center;
            }
            EditorSceneManager.MarkSceneDirty(desktop.gameObject.scene);EditorSceneManager.SaveScene(desktop.gameObject.scene);
        }
        static void FixedColor(Image i,Color color){var e=i.GetComponent<UIManagerElement>();if(e!=null)Object.DestroyImmediate(e);i.color=color;}
        static void FixedText(TMP_Text t,Color color){var e=t.GetComponent<UIManagerElement>();if(e!=null){e.useCustomColor=true;e.useCustomFont=true;}t.color=color;t.font=font;}
        public static void Stretch(RectTransform r){Stretch(r,Vector2.zero,Vector2.zero);}
        static void Stretch(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=min;r.offsetMax=max;}
        static void SetRect(RectTransform r,Vector2 a,Vector2 b,Vector2 min,Vector2 max){r.anchorMin=a;r.anchorMax=b;r.offsetMin=min;r.offsetMax=max;}
        static void Box(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);}
        static Image Image(string name,Transform p,Color c){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(p,false);var i=g.GetComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
        static TextMeshProUGUI Text(string name,Transform p,string s,float size,Color c){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));g.transform.SetParent(p,false);var t=g.GetComponent<TextMeshProUGUI>();t.font=font;t.text=s;t.fontSize=size;t.color=c;t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;}
        static RetroBevelGraphic Bevel(Transform p,bool inset){var g=new GameObject("Retro Bevel",typeof(RectTransform),typeof(CanvasRenderer),typeof(RetroBevelGraphic));g.transform.SetParent(p,false);Stretch((RectTransform)g.transform);var b=g.GetComponent<RetroBevelGraphic>();b.raycastTarget=false;b.Inset=inset;return b;}
    }
}
