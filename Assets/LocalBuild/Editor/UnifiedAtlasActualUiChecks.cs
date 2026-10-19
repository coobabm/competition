#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

// Callable from the root's isolated actual-LCD runner. This helper never creates a real save or changes rights.
// Fixture must have all six abilities, the viewer, collected cards and unlocked structures; disable its
// simulation controller and use TestMode so full JSON comparisons measure atlas effects, not normal time.
public static partial class UnifiedAtlasActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static readonly List<string> Checks=new List<string>();
    public static int Assertions {get;private set;}
    static T Get<T>(object value,string field)=>(T)value.GetType().GetField(field,F).GetValue(value);
    static void Need(bool value,string text){Assertions++;if(!value)throw new InvalidOperationException("Unified atlas: "+text);}
    static IEnumerator Tap(RectTransform target,string label,Action<RectTransform> click)
    {foreach(var frame in LcdActualUiProbe.WaitForClick(target,label))yield return frame;click(target);yield return null;yield return null;}
    static void TextInside(TMP_Text text,string label)
    {
        text.ForceMeshUpdate();Need(!text.isTextOverflowing,label+" is not truncated");
        Need(text.rectTransform.rect.width>0 && text.rectTransform.rect.height>0,label+" has positive bounds");
    }
    public static IEnumerator Run(XingGuangController ctl,Action<bool> language,Action<RectTransform> actualLcdClick,Action<string> capture)
    {
        Checks.Clear();Assertions=0;
        Need(ctl!=null && ctl.View!=null && ctl.runtime.TestMode && !ctl.enabled,"requires isolated paused TestMode fixture");
        Need(!ctl.Sim.AnyEpochActive && !ctl.Sim.StagePresentationHeld,"fixture has no active training or pending story");
        for(int stage=1;stage<=6;stage++)Need(XgUnifiedAtlasCatalog.CanReplay(ctl.Sim,stage),"fixture owns replay rights for stage "+stage);
        var view=ctl.View;bool oldEnglish=ctl.Sim.English,oldReduced=ctl.Sim.S.reduceFx;
        try
        {
            for(int locale=0;locale<2;locale++)
            {
                bool en=locale==1;language(en);ctl.Sim.English=en;
                view.Open("tree");yield return null;yield return null;
                var tool=Get<List<XgBtn>>(view.Tree,"toolbar")[6];
                yield return Tap(tool.rt,"Tech atlas entry",actualLcdClick);
                Need(view.AtlasShowing,"Tech entry opens unified atlas");
                var atlas=Get<XgUnifiedAtlas>(view,"atlas");
                var idleEntries=Get<List<XgAtlasEntry>>(atlas,"entries");
                float idleUntil=Time.unscaledTime+.6f;while(Time.unscaledTime<idleUntil)yield return null;
                Need(ReferenceEquals(idleEntries,Get<List<XgAtlasEntry>>(atlas,"entries")),"unchanged idle collection refresh reuses its entry projection");
                for(int motion=0;motion<2;motion++)
                {
                    ctl.Sim.S.reduceFx=motion==1;
                    // Real category/entry/replay controls, never direct calls to demonstration internals.
                    yield return Tap(Get<XgBtn[]>(atlas,"tabs")[0].rt,"Ability demonstrations",actualLcdClick);
                    for(int stage=1;stage<=6;stage++)
                    {
                        string saved=JsonUtility.ToJson(ctl.Sim.S);
                        yield return Tap(Get<XgBtn[]>(atlas,"rows")[stage-1].rt,"Stage "+stage,actualLcdClick);
                        Need(atlas.SelectedId=="ab."+stage && atlas.ReplayVisible,"selected learned ability is visibly replaying");
                        yield return Tap(Get<XgBtn>(atlas,"action").rt,"Replay stage "+stage,actualLcdClick);
                        Need(JsonUtility.ToJson(ctl.Sim.S)==saved,"replay changes no saved field");
                        TextInside(Get<TMP_Text>(atlas,"detailTitle"),"ability title");
                        if(stage>=4)
                        {
                            var demo=Get<XgStageSequenceDemo>(atlas,"sequence");
                            if(motion==1)Need(demo.ActiveToken==5,"reduced-motion sequence reveals static final frame");
                            foreach(string field in new[]{"title","explanation","answer"})TextInside(Get<TMP_Text>(demo,field),field);
                        }
                        capture("atlas-"+(en?"en":"zh")+"-stage"+stage+(motion==1?"-reduced":"-normal")+".png");
                    }
                }
                for(int category=1;category<3;category++)
                {
                    string saved=JsonUtility.ToJson(ctl.Sim.S);
                    yield return Tap(Get<XgBtn[]>(atlas,"tabs")[category].rt,"Collection category "+category,actualLcdClick);
                    Need(!atlas.ReplayVisible,"changing category stops the ability demonstration");
                    Need(JsonUtility.ToJson(ctl.Sim.S)==saved,"collection browsing is read-only");
                    capture("atlas-"+(en?"en":"zh")+"-category"+category+".png");
                }
                yield return Tap(Get<XgBtn>(atlas,"close").rt,"Close atlas",actualLcdClick);
                Need(!view.AtlasShowing,"close stops overlay");
                view.Open("home");yield return null;yield return null;
                yield return Tap(Get<XgBtn>(view,"profileMore").rt,"Profile More",actualLcdClick);
                yield return Tap(Get<XgBtn>(view,"profileAtlas").rt,"Profile atlas entry",actualLcdClick);
                Need(view.AtlasShowing && ReferenceEquals(atlas,Get<XgUnifiedAtlas>(view,"atlas")),"both entry points open the same collection instance");
                view.Open("train");yield return null;yield return null;
                Need(!view.AtlasShowing && !atlas.ReplayVisible,"changing page disposes replay presentation");
            }
            Checks.Add("Actual LCD entry/category/ability/replay/close buttons; 24 ability views, 4 collection views, both languages and reduced effects; full saved-state comparisons.");
        }
        finally
        {
            var atlas=Get<XgUnifiedAtlas>(view,"atlas");if(atlas!=null)atlas.Close();
            ctl.Sim.S.reduceFx=oldReduced;language(oldEnglish);ctl.Sim.English=oldEnglish;
        }
    }
}
#endif
