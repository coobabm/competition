#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
public static class TrainingSealChecks
{
    static readonly string[] Parts={"holdout","rate","checkpoint","monitor","weights"};
    static readonly string[] Nodes={"train.holdout","shared.lr","train.checkpoint","train.monitor","train.weights"};
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    [Serializable]sealed class Report {public string scope="Isolated in-memory component/real unlock gates. No disk, purchases, actual LCD routing or training-page integration claimed.",status="passed",error="";public List<string> checks=new List<string>();}
    [MenuItem("Tools/Local Verification/Training seal components")]
    public static void Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Run in EditMode");
        var report=new Report();GameObject root=null,events=null;
        Action<bool,string> check=(ok,label)=>{if(!ok)throw new Exception(label);report.checks.Add(label);};
        try
        {
            root=new GameObject("Isolated seal fixtures",typeof(RectTransform));((RectTransform)root.transform).sizeDelta=new Vector2(340,96);
            if(EventSystem.current==null)events=new GameObject("Isolated seal events",typeof(EventSystem));
            var sim=new XgSim();sim.S.teachingFlow=true;var kit=new XgUi(null,null,true);
            for(int i=0;i<Parts.Length;i++)
            {
                string part=Parts[i],nodeId=Nodes[i];var node=XgCatalog.Node(nodeId);check(node!=null,"real node exists "+nodeId);sim.S.unlocked.Remove(nodeId);int actions=0;
                var seal=XgTrainingSeal.Create((RectTransform)root.transform,kit,nodeId,()=>!sim.TrainPart(part),()=>"Locked · "+node.nameEn,()=>"Open this training tool",()=>"¥"+sim.NodeCost(node).ToString("0"),()=>false,()=>actions++);
                check(seal.gameObject.activeSelf&&seal.Control.button.IsInteractable(),"locked module remains explainable "+nodeId);
                check(seal.PriceLabel.text=="¥"+sim.NodeCost(node).ToString("0"),"price uses existing model formula "+nodeId);
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};ExecuteEvents.Execute(seal.gameObject,pointer,ExecuteEvents.pointerClickHandler);
                check(actions==1&&!sim.Has(nodeId),"click navigates without granting node "+nodeId);
                sim.S.unlocked.Add(nodeId);seal.RefreshState();check(!seal.gameObject.activeSelf,"owned module hides overlay "+nodeId);
                sim.S.unlocked.Remove(nodeId);sim.S.teachingFlow=false;seal.RefreshState();check(!seal.gameObject.activeSelf,"legacy visible tool never relocked "+nodeId);
                sim.S.teachingFlow=true;seal.RefreshState();check(seal.gameObject.activeSelf,"rebind restores actual lock "+nodeId);
                UnityEngine.Object.DestroyImmediate(seal.gameObject);
            }
            check(typeof(XgTechTreePage).GetMethod("FocusNodeDetails")!=null,"explicit details navigation entry exists");
        }
        catch(Exception e){report.status="failed";report.error=e.ToString();}
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);if(events!=null)UnityEngine.Object.DestroyImmediate(events);
            string path=Path.GetFullPath(Path.Combine("LocalVerification","TrainingSealComponents",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")));Directory.CreateDirectory(path);File.WriteAllText(Path.Combine(path,"report.json"),JsonUtility.ToJson(report,true));Debug.Log("TrainingSealChecks "+report.status+": "+path);
        }
        if(report.status!="passed")throw new Exception(report.error);
    }
    // Run after the isolated fixture has rendered at the target window size. This
    // inspects current state only; the caller supplies new/mixed/owned/legacy fixtures.
    public static void AssertLayout(XingGuangView view)
    {
        if(!Application.isPlaying||view==null||view.Tab!="train")throw new InvalidOperationException("Requires visible isolated training page");
        var page=view.Train;
        Func<string,RectTransform> rect=name=>(RectTransform)typeof(XgTrainPage).GetField(name,Flags).GetValue(page);
        var basis=rect("left");var train=(XgBtn)typeof(XgTrainPage).GetField("train",Flags).GetValue(page);
        var curve=(UnityEngine.UI.Graphic)typeof(XgTrainPage).GetField("chart",Flags).GetValue(page);
        var seals=(XgTrainingSeal[])typeof(XgTrainPage).GetField("moduleSeals",Flags).GetValue(page);
        if(seals.Length!=5)throw new Exception("Expected five module-local seals");
        foreach(var seal in seals)
        {
            int i=Array.IndexOf(Nodes,seal.NodeId);
            if(i<0||seal.gameObject.activeSelf==view.Sim.TrainPart(Parts[i]))throw new Exception("Wrong lock state: "+seal.NodeId);
            if(!seal.gameObject.activeSelf)continue;
            var bounds=Bounds(seal.Control.rt,basis);
            if(bounds.height<XgTrainingSeal.MinimumHeight-1||bounds.width<160)throw new Exception("Undersized module: "+seal.NodeId);
            foreach(var target in new[]{train.rt,rect("sheetArea"),curve.rectTransform})
            {
                Rect sealVisible=VisibleBounds(seal.Control.rt,basis),targetVisible=VisibleBounds(target,basis);
                Rect overlap=Intersection(sealVisible,targetVisible);
                if(overlap.width>.1f&&overlap.height>.1f)throw new Exception("Seal covers paid base control: "+seal.NodeId+" / "+target.name+" visible intersection="+overlap+" seal="+sealVisible+" target="+targetVisible);
            }
            if(seal.NameLabel.text.Length==0||seal.DescriptionLabel.text.Length==0||seal.PriceLabel.text.Length==0)throw new Exception("Empty module copy: "+seal.NodeId);
        }
        if(rect("sealViewport").rect.height<140||curve.rectTransform.rect.height<80)throw new Exception("Training viewport/curve became unusable");
        if(train.rt.parent!=rect("trainRow"))throw new Exception("Training button must remain outside scrolling content");
    }
    // Scroll content can geometrically extend below the fixed controls while being
    // clipped out. Only actual visible pixels overlap. Do not whitelist any module.
    static Rect VisibleBounds(RectTransform target,RectTransform basis)
    {
        if(!target.gameObject.activeInHierarchy)return Rect.zero;
        Rect visible=Bounds(target,basis);
        for(Transform t=target;t!=null;t=t.parent)
        {
            if(t.TryGetComponent<UnityEngine.UI.RectMask2D>(out var clip)&&clip.isActiveAndEnabled)
            {
                Rect local=clip.rectTransform.rect;Vector4 pad=clip.padding;
                local=Rect.MinMaxRect(local.xMin+pad.x,local.yMin+pad.y,local.xMax-pad.z,local.yMax-pad.w);
                visible=Intersection(visible,Bounds(clip.rectTransform,basis,local));
            }
            // Current training hierarchy uses rectangular masks only. Including
            // stencil-mask bounds also handles the containing desktop viewport.
            if(t.TryGetComponent<UnityEngine.UI.Mask>(out var mask)&&mask.isActiveAndEnabled)
                visible=Intersection(visible,Bounds((RectTransform)t,basis));
        }
        return visible;
    }
    static Rect Intersection(Rect a,Rect b)
    {
        float x=Mathf.Max(a.xMin,b.xMin),y=Mathf.Max(a.yMin,b.yMin);
        float right=Mathf.Min(a.xMax,b.xMax),top=Mathf.Min(a.yMax,b.yMax);
        return right>x&&top>y?Rect.MinMaxRect(x,y,right,top):Rect.zero;
    }
    static Rect Bounds(RectTransform target,RectTransform basis)
        =>Bounds(target,basis,target.rect);
    static Rect Bounds(RectTransform target,RectTransform basis,Rect local)
    {
        var corners=new[]{target.TransformPoint(new Vector3(local.xMin,local.yMin)),target.TransformPoint(new Vector3(local.xMin,local.yMax)),target.TransformPoint(new Vector3(local.xMax,local.yMax)),target.TransformPoint(new Vector3(local.xMax,local.yMin))};
        Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=new Vector2(float.NegativeInfinity,float.NegativeInfinity);
        foreach(var corner in corners){Vector2 p=basis.InverseTransformPoint(corner);min=Vector2.Min(min,p);max=Vector2.Max(max,p);}
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    // Optional PlayMode continuation for an already isolated visible .exe fixture.
    // Caller owns scene/save isolation and checks actual screen-pointer routing separately.
    public static void AssertDetails(XingGuangView view)
    {
        if(!Application.isPlaying||view==null)throw new InvalidOperationException("Requires isolated PlayMode view");
        string old=view.Tab;view.ShowTab("tree");
        try
        {
            foreach(string id in Nodes)
            {
                if(!view.Tree.FocusNodeDetails(id))throw new Exception("Cannot focus details: "+id);
                var shown=typeof(XgTechTreePage).GetField("detailShown",Flags).GetValue(view.Tree);
                var tooltip=(RectTransform)typeof(XgTechTreePage).GetField("tooltip",Flags).GetValue(view.Tree);
                var covers=shown?.GetType().GetMethod("Covers",Flags);
                if(shown==null||covers==null||!(bool)covers.Invoke(shown,new object[]{id})||!tooltip.gameObject.activeInHierarchy)throw new Exception("Wrong or hidden detail card: "+id);
            }
        }
        finally{view.ShowTab(old);}
    }
}
#endif
