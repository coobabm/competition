using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HongmengOS.Aero2010;
using Michsky.DreamOS;
public sealed class Aero2010PlayQa : MonoBehaviour
{
 [Serializable] public class Check{public string name;public bool passed;public string detail;}
 [Serializable] public class Report{public List<Check> checks=new List<Check>();public bool complete;public string exception;}
 public Report report=new Report();
 public string evidence="/Users/kk/competition/Evidence/HongmengOS-Aero2010";
 AeroLcdController control;bool running;readonly List<string> runtimeErrors=new List<string>();
 void OnEnable(){Application.logMessageReceived+=CaptureLog;}
 void OnDisable(){Application.logMessageReceived-=CaptureLog;}
 void CaptureLog(string message,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)runtimeErrors.Add(message);}
 void Record(string n,bool ok,string d=""){report.checks.Add(new Check{name=n,passed=ok,detail=d});Save();}
 void Save(){File.WriteAllText(Path.Combine(evidence,"aero-editor-playmode-qa.json"),JsonUtility.ToJson(report,true));}
 public static Vector2 DisplayPoint(RectTransform target,Camera camera=null){if(camera==null){var c=target.GetComponentInParent<Canvas>();if(c!=null&&c.renderMode!=RenderMode.ScreenSpaceOverlay)camera=c.worldCamera;}return RectTransformUtility.WorldToScreenPoint(camera,target.TransformPoint(target.rect.center));}
 public static Vector2 SourcePoint(RectTransform target,AeroLcdInputSurface surface)
 {
  var p=DisplayPoint(target,surface.sourceCamera);var rt=surface.sourceCamera.targetTexture;var uv=new Vector2(p.x/rt.width,p.y/rt.height);var r=surface.displayRect.rect;var c=surface.displayRect.GetComponentInParent<Canvas>();return RectTransformUtility.WorldToScreenPoint(c.worldCamera,surface.displayRect.TransformPoint(new Vector3(r.xMin+uv.x*r.width,r.yMin+uv.y*r.height,0)));
 }
 public static PointerEventData Data(Vector2 p){var d=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left,pointerId=-1,eligibleForClick=true};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);if(hits.Count>0){d.pointerCurrentRaycast=hits[0];d.pointerPressRaycast=hits[0];}return d;}
 public static string Click(Vector2 p){var d=Data(p);var hit=d.pointerCurrentRaycast.gameObject;if(hit==null)return "NO HIT";ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerDownHandler);ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerUpHandler);ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerClickHandler);return hit.name;}
 public static string ClickSource(RectTransform target,AeroLcdInputSurface surface){return Click(SourcePoint(target,surface));}
 RectTransform Source(string path){return (RectTransform)GameObject.Find("hongmengos 2010 Desktop Source").transform.Find("Desktop/"+path);}
 void SourceClick(string path){ClickSource(Source(path),control.inputSurface);}
 void HardwareClick(string name){Click(DisplayPoint((RectTransform)GameObject.Find("HongmengOS Monitor/LCD Hardware Controls/"+name).transform));}
 void IconChecks()
 {
  var l=FindFirstObjectByType<AeroLicensedIconLoader>();
  Record("Oxygen all 13 runtime icons",l!=null&&l.IsReady&&l.LoadedIconCount==13&&l.Errors.Count==0,l==null?"loader missing":"loaded="+l.LoadedIconCount+"; errors="+l.Errors.Count);
  if(l==null)return;
  bool copies=l.RuntimeLibrary!=l.originalLibrary;
  for(int i=0;i<l.originalLibrary.apps.Count;i++)copies&=!object.ReferenceEquals(l.RuntimeLibrary.apps[i],l.originalLibrary.apps[i]);
  Record("Oxygen deep-cloned library",copies);
  Record("Original icon library remains fallback",l.originalLibrary.apps.TrueForAll(a=>a.appIconBig!=null&&!a.appIconBig.name.StartsWith("Oxygen")));
  int correct=0;foreach(var b in l.bindings)if(b.image!=null&&b.image.sprite!=null&&b.image.sprite.name=="Oxygen runtime icon — "+b.iconKey)correct++;
  Record("All image bindings survive app interaction",correct==l.bindings.Length,"correct="+correct+"/"+l.bindings.Length);
  Record("AppElements use cloned runtime library",l.GetComponentsInChildren<AppElement>(true).All(e=>e.appLibrary==l.RuntimeLibrary));
  bool stable=true;int managed=0;
  foreach(var b in l.GetComponentsInChildren<ButtonManager>(true))if(!b.useCustomContent)foreach(var image in new[]{b.normalImageObj,b.highlightImageObj,b.pressedImageObj,b.disabledImageObj})if(image!=null&&image.sprite!=null&&image.sprite.name.StartsWith("Oxygen runtime icon")){managed++;stable&=b.buttonIcon==image.sprite;}
  Record("No ButtonManager field overwrites Oxygen image bindings",stable,"managed image states="+managed+"; app buttons use AppElement/direct Image bindings");
  l.enabled=false;l.enabled=true;Record("Loader enable cycle retains ready icons",l.IsReady&&l.LoadedIconCount==13&&l.Errors.Count==0);
  var validate=typeof(AeroLicensedIconLoader).GetMethod("ValidatePngHeader",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  var valid=File.ReadAllBytes(Path.Combine(Application.streamingAssetsPath,"HongmengOS-Aero2010/Oxygen/Settings.png"));
  bool accepted=false;try{validate.Invoke(null,new object[]{valid});accepted=true;}catch{}
  Record("PNG validator accepts distributed artwork",accepted);
  var bad=(byte[])valid.Clone();bad[0]=0;bool rejected=false;try{validate.Invoke(null,new object[]{bad});}catch(System.Reflection.TargetInvocationException e){rejected=e.InnerException is InvalidDataException;}
  Record("PNG validator rejects non-PNG bytes",rejected);
  var huge=(byte[])valid.Clone();huge[16]=0x7f;huge[17]=0xff;huge[18]=0xff;huge[19]=0xff;rejected=false;try{validate.Invoke(null,new object[]{huge});}catch(System.Reflection.TargetInvocationException e){rejected=e.InnerException is InvalidDataException;}
  Record("PNG validator rejects oversized dimensions before decode",rejected);
 }
 public void Run(){if(running||report.complete)return;running=true;StartCoroutine(Guard(Tests()));}
 IEnumerator Guard(IEnumerator work){while(true){object v;try{if(!work.MoveNext())break;v=work.Current;}catch(Exception e){report.exception=e.ToString();Record("QA without exception",false,e.Message);running=false;yield break;}yield return v;}running=false;}
 IEnumerator Tests()
 {
  control=FindFirstObjectByType<AeroLcdController>();var surface=control.inputSurface;var menu=FindFirstObjectByType<AeroStartMenu>();var layout=FindFirstObjectByType<AeroTaskbarLayout>();
  var rt=control.sourceCamera.targetTexture;Record("Full HD 16:9 flat panel",rt.width==1920&&rt.height==1080&&Mathf.Abs(control.display.rectTransform.rect.width/control.display.rectTransform.rect.height-16f/9)<.00001f);
  Record("Front-on view",Quaternion.Angle(Camera.main.transform.rotation,Quaternion.identity)<.01f&&Mathf.Abs(Camera.main.transform.position.x)<.0001f);
  Record("Layer isolation",control.display.gameObject.layer==0&&control.sourceCamera.cullingMask==(1<<5)&&Camera.main.cullingMask==1);
  Record("Only open apps on taskbar initially",layout.entries.All(x=>x.window.isOn?x.visibility.alpha>0:x.visibility.alpha==0));
  var corner=surface.displayRect.TransformPoint(new Vector3(surface.displayRect.rect.xMin+1,surface.displayRect.rect.yMin+1,0));Vector2 mapped;Record("LCD corner is clickable (no CRT mask)",surface.TryMap(RectTransformUtility.WorldToScreenPoint(Camera.main,corner),out mapped),mapped.ToString());
  control.enabled=false;control.enabled=true;Record("Controller re-enable restores input",control.CanInteract&&surface.inputEnabled);
  var desktopProbe=control.desktopAudio.audioSource.gameObject.AddComponent<AeroAudioProbe>();control.PlayDesktopClick();yield return new WaitForSecondsRealtime(.2f);Record("Desktop click audio samples",desktopProbe.peak>.0001f&&desktopProbe.samples>0,"peak="+desktopProbe.peak);
  var probe=control.GetComponent<AudioSource>().gameObject.AddComponent<AeroAudioProbe>();float before=control.brightness;HardwareClick("LCD Brightness Down");Record("Physical brightness down",control.brightness<before);HardwareClick("LCD Brightness Up");Record("Physical brightness up",Mathf.Abs(control.brightness-before)<.001f);HardwareClick("LCD Menu");Record("LCD OSD",control.osd.activeSelf);HardwareClick("LCD Standard Preset");Record("Standard preset",Mathf.Approximately(control.brightness,.92f));
  yield return new WaitForSecondsRealtime(.2f);Record("Mechanical audio samples",probe.peak>.0001f&&probe.samples>0,"peak="+probe.peak+", samples="+probe.samples);
  HardwareClick("LCD Power");Record("Power transition blocks pointer",control.IsTransitioning&&!surface.inputEnabled);HardwareClick("LCD Power");yield return new WaitForSecondsRealtime(.35f);Record("Power off and debounced",!control.IsPowered&&!control.sourceCamera.enabled&&!surface.inputEnabled&&control.display.color.r==0);
  menu.Close();SourceClick("Taskbar/Quick Center Button");yield return null;Record("No desktop activation while off",!menu.menu.activeSelf);ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"aero-lcd-off.png"));
  HardwareClick("LCD Power");yield return new WaitForSecondsRealtime(.35f);Record("Power restored",control.IsPowered&&surface.inputEnabled&&control.sourceCamera.enabled);
  SourceClick("Taskbar/Quick Center Button");yield return new WaitForSecondsRealtime(.15f);Record("Start orb mapped click",menu.menu.activeSelf);ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"aero-start-menu.png"));
  var scroll=Data(SourcePoint(menu.programScroll.viewport,surface));scroll.scrollDelta=new Vector2(0,-3);ExecuteEvents.ExecuteHierarchy(scroll.pointerCurrentRaycast.gameObject,scroll,ExecuteEvents.scrollHandler);yield return new WaitForSecondsRealtime(.2f);Record("Mapped Start menu scroll",menu.programScroll.verticalNormalizedPosition<.99f,menu.programScroll.verticalNormalizedPosition.ToString());menu.programScroll.verticalNormalizedPosition=1;
  ClickSource(menu.search.GetComponent<RectTransform>(),surface);yield return null;Record("Search receives text focus",menu.search.isFocused);menu.search.text="settings";yield return null;Record("Search filters programs",menu.programs.Count(x=>x.row.activeSelf)==1);
  var settingsRow=menu.programs.First(x=>x.window.name=="Settings");ClickSource((RectTransform)settingsRow.row.transform,surface);yield return new WaitForSecondsRealtime(1.1f);var settings=settingsRow.window;Record("Settings first mapped open",settings.isOn&&settings.gameObject.activeInHierarchy);
  Record("Taskbar shows opened Settings",layout.entries.First(x=>x.window==settings).visibility.alpha>.9f);
  SourceClick("Apps & Windows/Settings/Container/Content/Navigation Panel/Scroll Rect/Categories/About");yield return new WaitForSecondsRealtime(.7f);var about=Source("Apps & Windows/Settings/Container/Content/Main Content/About");var cg=about.GetComponent<CanvasGroup>();Record("About page",about.gameObject.activeInHierarchy&&(cg==null||cg.alpha>.9f));
  bool hidden=true;foreach(string n in new[]{"Support Channels","Discord","Twitter","YouTube"}){var s=about.Find("Content/List Parent/List/"+n);if(s!=null&&s.gameObject.activeInHierarchy)hidden=false;}Record("Social/support cleanup inherited",hidden);ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"aero-control-panel.png"));
  SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Fullscreen");yield return new WaitForSecondsRealtime(.7f);Record("Maximize via mapped caption",settings.isFullscreen);SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Fullscreen");yield return new WaitForSecondsRealtime(.7f);Record("Restore via mapped caption",!settings.isFullscreen);
  var drag=Source("Apps & Windows/Settings/Container/Dragger");var initial=settings.windowContainer.anchoredPosition;var data=Data(SourcePoint(drag,surface));var hit=data.pointerCurrentRaycast.gameObject;ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerDownHandler);data.delta=new Vector2(45,20);data.position+=data.delta;ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.beginDragHandler);ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.dragHandler);data.position+=data.delta;ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.dragHandler);ExecuteEvents.ExecuteHierarchy(hit,data,ExecuteEvents.pointerUpHandler);yield return null;Record("Mapped window drag",(settings.windowContainer.anchoredPosition-initial).sqrMagnitude>4);
  SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Minimize");yield return new WaitForSecondsRealtime(.8f);Record("Minimize",settings.GetComponent<CanvasGroup>().alpha<.1f);SourceClick("Taskbar/Shortcut List/Settings");yield return new WaitForSecondsRealtime(.8f);Record("Taskbar restores window",settings.GetComponent<CanvasGroup>().alpha>.9f);
  menu.ShowDesktop();yield return new WaitForSecondsRealtime(.8f);Record("Show desktop minimizes",settings.GetComponent<CanvasGroup>().alpha<.1f);menu.ShowDesktop();yield return new WaitForSecondsRealtime(.8f);Record("Show desktop restores",settings.GetComponent<CanvasGroup>().alpha>.9f);
  SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Exit");yield return new WaitForSecondsRealtime(.8f);Record("Close removes taskbar item",!settings.isOn&&layout.entries.First(x=>x.window==settings).visibility.alpha==0);
  SourceClick("Desktop List/Notepad");yield return new WaitForSecondsRealtime(.08f);SourceClick("Desktop List/Notepad");yield return new WaitForSecondsRealtime(1.1f);var note=Source("Apps & Windows/Notepad").GetComponent<WindowManager>();Record("Desktop double click",note.isOn&&note.gameObject.activeInHierarchy);if(note.isOn)note.CloseWindow();yield return new WaitForSecondsRealtime(.9f);
  int missing=0;foreach(var go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))missing+=UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);Record("No missing scripts",missing==0,missing.ToString());
  IconChecks();
  Record("No runtime Error or Exception during regression",runtimeErrors.Count==0,string.Join(" | ",runtimeErrors));ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"aero-desktop-final.png"));report.complete=true;Save();
 }
}
