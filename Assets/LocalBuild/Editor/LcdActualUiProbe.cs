#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HongmengOS.Aero2010;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Observes the same mapped production LCD raycast as FilesWindowUiChecks.Click.
// Never changes a canvas/raycaster, changes sibling order, closes an overlay or invokes a button.
public static class LcdActualUiProbe
{
    const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static string Path(Transform target)
    {
        var names=new List<string>();for(var t=target;t!=null;t=t.parent)names.Add(t.name+"["+t.GetSiblingIndex()+"]");names.Reverse();return string.Join("/",names);
    }
    public static bool CanClick(RectTransform target,out string detail)
    {
        var log=new StringBuilder();bool ready=false;
        log.Append("frame=").Append(Time.frameCount).Append(" screen=").Append(Screen.width).Append('x').Append(Screen.height);
        try
        {
            if(target==null)throw new InvalidOperationException("Target missing");
            Canvas.ForceUpdateCanvases();
            var bridge=(AeroLcdSourceBridge)typeof(FilesWindowUiChecks).GetMethod("Bridge",Static).Invoke(null,null);
            var surface=bridge.inputSurface;var camera=bridge.sourceCamera;
            log.Append(" target=").Append(Path(target)).Append(" sourceOwned=").Append(target.IsChildOf(bridge.sourceCanvas.transform));
            log.Append(" bridgeCount=").Append(UnityEngine.Object.FindObjectsByType<AeroLcdSourceBridge>(FindObjectsSortMode.None).Length);
            log.Append(" input=").Append(surface.inputEnabled).Append('/').Append(surface.isActiveAndEnabled);
            log.Append(" sourceCamera=").Append(camera.name).Append(" enabled=").Append(camera.enabled).Append(" depth=").Append(camera.depth).Append(" pixels=").Append(camera.pixelWidth).Append('x').Append(camera.pixelHeight);
            log.Append(" sourcePixel=").Append(camera.WorldToScreenPoint(target.TransformPoint(target.rect.center)));
            log.Append(" targetRect=").Append(target.rect).Append(" scale=").Append(target.lossyScale);
            foreach(var g in target.GetComponents<Graphic>())
                log.Append(" graphic=").Append(g.GetType().Name).Append(" depth=").Append(g.depth).Append(" raycast=").Append(g.raycastTarget).Append(" culled=").Append(g.canvasRenderer.cull).Append(" active=").Append(g.isActiveAndEnabled);
            foreach(var c in target.GetComponentsInParent<Canvas>(true))
                log.Append(" canvas=").Append(Path(c.transform)).Append(" enabled=").Append(c.enabled).Append(" mode=").Append(c.renderMode).Append(" override=").Append(c.overrideSorting).Append(" order=").Append(c.sortingOrder).Append(" renderOrder=").Append(c.renderOrder);
            var p=(PointerEventData)typeof(FilesWindowUiChecks).GetMethod("Pointer",Static).Invoke(null,new object[]{target});
            log.Append(" physicalPixel=").Append(p.position);
            surface.OnPointerEnter(p);
            var hover=(PointerEventData)typeof(AeroLcdInputSurface).GetField("hoverData",Instance).GetValue(surface);
            var hit=hover?.pointerCurrentRaycast.gameObject;var handler=hit==null?null:ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit);
            log.Append(" hit=").Append(hit==null?"none":Path(hit.transform)).Append(" clickHandler=").Append(handler==null?"none":Path(handler.transform));
            log.Append(" contextManagerOpen=").Append(bridge.contextMenu!=null&&bridge.contextMenu.isOn);
            var results=(List<RaycastResult>)typeof(AeroLcdInputSurface).GetField("results",Instance).GetValue(surface);
            var compare=typeof(AeroLcdInputSurface).GetMethod("CompareRaycasts",Static);
            var sorted=results.ToList();sorted.Sort((a,b)=>(int)compare.Invoke(null,new object[]{a,b}));
            foreach(var r in sorted.Take(8))
                log.Append(" | hitCandidate=").Append(r.gameObject==null?"none":Path(r.gameObject.transform)).Append(" depth=").Append(r.depth).Append(" sorting=").Append(r.sortingLayer).Append('/').Append(r.sortingOrder).Append(" distance=").Append(r.distance).Append(" raycaster=").Append(r.module==null?"none":Path(r.module.transform));
            foreach(var r in surface.sourceRaycasters??Array.Empty<GraphicRaycaster>())
                if(r!=null)log.Append(" | sourceRaycaster=").Append(Path(r.transform)).Append(" active=").Append(r.gameObject.activeInHierarchy).Append(" enabled=").Append(r.enabled).Append(" eventCamera=").Append(r.eventCamera==null?"none":r.eventCamera.name).Append(" sortPriority=").Append(r.sortOrderPriority).Append(" renderPriority=").Append(r.renderOrderPriority);
            ready=surface.inputEnabled&&surface.isActiveAndEnabled&&target.gameObject.activeInHierarchy&&handler==target.gameObject;
        }
        catch(Exception error){log.Append(" probeError=").Append(error is TargetInvocationException&&error.InnerException!=null?error.InnerException.ToString():error.ToString());}
        detail=log.ToString();return ready;
    }
    public static IEnumerable<object> WaitForClick(RectTransform target,string label,double seconds=2)
    {
        double deadline=EditorApplication.timeSinceStartup+seconds;int frames=0;string detail;
        bool ready=CanClick(target,out detail);Debug.Log("LCD click probe initial ["+label+"]: "+detail);
        while(!ready)
        {
            if(EditorApplication.timeSinceStartup>=deadline)throw new InvalidOperationException("LCD actual click did not become ready ["+label+"]: "+detail);
            yield return null;frames++;ready=CanClick(target,out detail);
        }
        if(frames>0)Debug.Log("LCD click probe ready after "+frames+" natural frame(s) ["+label+"]: "+detail);
    }
}
#endif
