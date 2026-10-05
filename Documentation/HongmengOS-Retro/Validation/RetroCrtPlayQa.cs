using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HongmengOS.Retro;
using Michsky.DreamOS;
public sealed class RetroCrtPlayQa : MonoBehaviour
{
    [Serializable] public class Check{public string name;public bool passed;public string detail;}
    [Serializable] public class Report{public List<Check> checks=new List<Check>();public bool complete;public string exception;}
    public Report report=new Report();
    public string evidence="/Users/kk/competition/Evidence/HongmengOS-Retro";
    RetroCrtController control; RetroCrtAudioProbe probe; bool running;
    public void Record(string name,bool ok,string detail=""){report.checks.Add(new Check{name=name,passed=ok,detail=detail});Save();}
    void Save(){File.WriteAllText(Path.Combine(evidence,"crt-physical-playmode-qa.json"),JsonUtility.ToJson(report,true));}
    public static Vector2 DisplayPoint(RectTransform target,Camera camera=null){if(camera==null){var c=target.GetComponentInParent<Canvas>();if(c!=null&&c.renderMode!=RenderMode.ScreenSpaceOverlay)camera=c.worldCamera;}return RectTransformUtility.WorldToScreenPoint(camera,target.TransformPoint(target.rect.center));}
    public static Vector2 SourcePoint(RectTransform target,RetroCrtInputSurface surface)
    {
        var p=DisplayPoint(target,surface.sourceCamera);var rt=surface.sourceCamera.targetTexture;
        Vector2 uv=new Vector2(p.x/rt.width,p.y/rt.height);if(surface.flipY)uv.y=1-uv.y;
        Vector2 t=uv*2-Vector2.one,q=t;
        for(int i=0;i<16;i++)q=new Vector2(t.x/(1+surface.curvature*q.y*q.y),t.y/(1+surface.curvature*q.x*q.x));
        uv=q*.5f+Vector2.one*.5f;var r=surface.displayRect.rect;var c=surface.displayRect.GetComponentInParent<Canvas>();
        return RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,surface.displayRect.TransformPoint(new Vector3(r.xMin+uv.x*r.width,r.yMin+uv.y*r.height,0)));
    }
    public static PointerEventData Data(Vector2 p){var d=new PointerEventData(EventSystem.current){position=p,button=PointerEventData.InputButton.Left,pointerId=-1,eligibleForClick=true};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(d,hits);if(hits.Count>0){d.pointerCurrentRaycast=hits[0];d.pointerPressRaycast=hits[0];}return d;}
    public static string Click(Vector2 p){var d=Data(p);var hit=d.pointerCurrentRaycast.gameObject;if(hit==null)return "NO HIT";ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerDownHandler);ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerUpHandler);ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerClickHandler);return hit.name;}
    public static string ClickSource(RectTransform target,RetroCrtInputSurface surface){return Click(SourcePoint(target,surface));}
    public void RunHardware(){if(running||report.complete)return;running=true;StartCoroutine(Guard(Hardware()));}
    IEnumerator Guard(IEnumerator work){while(true){object v;try{if(!work.MoveNext())break;v=work.Current;}catch(Exception e){report.exception=e.ToString();Record("QA completed without exception",false,e.Message);running=false;yield break;}yield return v;}running=false;}
    RectTransform Source(string path){return (RectTransform)GameObject.Find("hongmengos Desktop Source").transform.Find("Desktop/"+path);}
    void SourceClick(string path){ClickSource(Source(path),control.inputSurface);}
    void HardwareClick(string name){Click(DisplayPoint((RectTransform)GameObject.Find("HongmengOS Monitor/CRT Front Controls/"+name).transform));}
    IEnumerator Hardware()
    {
        control=FindFirstObjectByType<RetroCrtController>();probe=control.hardwareAudio.GetComponent<RetroCrtAudioProbe>()??control.hardwareAudio.gameObject.AddComponent<RetroCrtAudioProbe>();
        var shape=control.display.rectTransform.rect;Record("4:3 aperture",Mathf.Abs(shape.width/shape.height-4f/3)<.0001f,shape.ToString());
        var rt=control.sourceCamera.targetTexture;Record("Low resolution CRT source",rt.width==1024&&rt.height==768&&rt.filterMode==FilterMode.Bilinear,rt.width+"x"+rt.height);
        Record("Mild curvature matches input",control.inputSurface.curvature<=.03f&&Mathf.Approximately(control.inputSurface.curvature,control.display.material.GetFloat("_Curvature")));
        Record("Isolated render layers",control.display.gameObject.layer==0&&control.sourceCamera.cullingMask==(1<<5)&&Camera.main.cullingMask==1);
        float f=control.Focus;HardwareClick("Focus plus");yield return new WaitForSecondsRealtime(.1f);Record("Focus plus key",control.Focus>f,control.Focus.ToString());HardwareClick("Focus minus");Record("Focus minus key",Mathf.Approximately(control.Focus,f));
        f=control.Brightness;HardwareClick("Brightness plus");Record("Brightness plus key",control.Brightness>f);HardwareClick("Brightness minus");Record("Brightness minus key",Mathf.Approximately(control.Brightness,f));
        yield return new WaitForSecondsRealtime(.25f);Record("Mechanical DSP output",probe.peak>.0001f&&probe.samples>0,"peak="+probe.peak+",samples="+probe.samples);probe.peak=0;probe.samples=0;
        HardwareClick("CRT Power Button");Record("Power button flash",control.IsTransitioning&&control.display.material.GetFloat("_Flash")>.5f);Record("Transition input blocked",!control.inputSurface.inputEnabled);
        HardwareClick("CRT Power Button");HardwareClick("CRT Power Button");Record("Repeated toggle debounce",control.IsPowered&&control.IsTransitioning);
        ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"crt-physical-flash.png"));yield return new WaitForSecondsRealtime(.7f);
        Record("Off stable",!control.IsPowered&&!control.IsTransitioning&&!control.sourceCamera.enabled&&!control.inputSurface.inputEnabled);Record("Power audio DSP output",probe.peak>.0001f&&probe.samples>0,"peak="+probe.peak+",samples="+probe.samples);
        ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"crt-physical-off.png"));
        var menu=FindFirstObjectByType<RetroStartMenu>();menu.Close();SourceClick("Taskbar/Quick Center Button");yield return null;Record("Desktop blocked when off",!menu.menu.activeSelf);
        f=control.Brightness;HardwareClick("Brightness plus");Record("Physical keys work when off",control.Brightness>f);HardwareClick("Brightness minus");
        HardwareClick("CRT Power Button");Record("Power on flash",control.IsTransitioning&&control.display.material.GetFloat("_Flash")>.5f);yield return new WaitForSecondsRealtime(.8f);
        Record("On stable",control.IsPowered&&!control.IsTransitioning&&control.sourceCamera.enabled&&control.inputSurface.inputEnabled);
        SourceClick("Taskbar/Quick Center Button");yield return new WaitForSecondsRealtime(.2f);Record("Start menu mapped click",menu.menu.activeSelf);
        SourceClick("Retro Start Menu/Programs/Settings");yield return new WaitForSecondsRealtime(1.1f);
        var settings=Source("Apps & Windows/Settings").GetComponent<WindowManager>();Record("Settings first open stays open",settings.isOn&&settings.gameObject.activeInHierarchy);
        if(!settings.isOn){settings.OpenWindow();yield return new WaitForSecondsRealtime(1.1f);}
        SourceClick("Apps & Windows/Settings/Container/Content/Navigation Panel/Scroll Rect/Categories/About");yield return new WaitForSecondsRealtime(.7f);
        var about=Source("Apps & Windows/Settings/Container/Content/Main Content/About");var cg=about.GetComponent<CanvasGroup>();Record("About mapped click",about.gameObject.activeInHierarchy&&(cg==null||cg.alpha>.9f));
        bool hidden=true;foreach(string n in new[]{"Support Channels","Discord","Twitter","YouTube"}){var s=about.Find("Content/List Parent/List/"+n);if(s!=null&&s.gameObject.activeInHierarchy)hidden=false;}Record("Removed social links stay hidden",hidden);
        ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"crt-physical-about.png"));
        SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Fullscreen");yield return new WaitForSecondsRealtime(.8f);Record("Maximize mapped click",settings.isFullscreen);
        SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Fullscreen");yield return new WaitForSecondsRealtime(.8f);Record("Restore mapped click",!settings.isFullscreen);
        var dragger=Source("Apps & Windows/Settings/Container/Dragger");var before=settings.windowContainer.anchoredPosition;var dp=SourcePoint(dragger,control.inputSurface);var d=Data(dp);var hit=d.pointerCurrentRaycast.gameObject;
        ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerDownHandler);d.delta=new Vector2(55,25);d.position+=d.delta;ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.beginDragHandler);ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.dragHandler);d.position+=d.delta;ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.dragHandler);ExecuteEvents.ExecuteHierarchy(hit,d,ExecuteEvents.pointerUpHandler);yield return null;
        Record("Mapped window drag",(settings.windowContainer.anchoredPosition-before).sqrMagnitude>4,settings.windowContainer.anchoredPosition.ToString());
        SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Minimize");yield return new WaitForSecondsRealtime(.9f);Record("Minimize mapped click",settings.GetComponent<CanvasGroup>().alpha<.1f);
        SourceClick("Taskbar/Shortcut List/Settings");yield return new WaitForSecondsRealtime(.9f);Record("Taskbar restore mapped click",settings.GetComponent<CanvasGroup>().alpha>.9f);
        SourceClick("Apps & Windows/Settings/Container/Dragger/Window Buttons/Exit");yield return new WaitForSecondsRealtime(.9f);Record("Close mapped click",!settings.isOn);
        SourceClick("Desktop List/Notepad");yield return new WaitForSecondsRealtime(.08f);SourceClick("Desktop List/Notepad");yield return new WaitForSecondsRealtime(1.1f);var note=Source("Apps & Windows/Notepad").GetComponent<WindowManager>();Record("Desktop icon double click",note.isOn&&note.gameObject.activeInHierarchy);
        if(note.isOn)note.CloseWindow();yield return new WaitForSecondsRealtime(1);
        ScreenCapture.CaptureScreenshot(Path.Combine(evidence,"crt-physical-final.png"));report.complete=true;Save();
    }
}
