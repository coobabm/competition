#if UNITY_EDITOR
using LingGuangV05.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;

// Called by root's existing isolated real-training fixture. Does not create paid nodes or fake epochs/music.
public static class DailyRhythmActualUiChecks
{
    const BindingFlags F=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    public static readonly List<string> Checks=new List<string>();
    static void Need(bool pass,string message){if(!pass)throw new InvalidOperationException("Daily rhythm actual UI: "+message);Checks.Add(message);}
    static T Get<T>(object value,string field)=>(T)value.GetType().GetField(field,F).GetValue(value);
    public static IEnumerator Frames(XingGuangController ctl,Action<bool> language,Action<RectTransform> actualLCDclick,Action<string> capture)
    {
        Checks.Clear();Need(ctl!=null&&ctl.Sim!=null&&ctl.View!=null,"real initialized lab supplied");
        Need(ctl.enabled&&!ctl.runtime.TestMode,"real training controller runs, no test-economy stub");
        Need(ctl.Sim.DailyRhythmFeedback,"Rhythm must already be bought through the root fixture's real purchase");
        Need(!ctl.View.Juice.Reduced,"normal-motion branch selected; reduced-effects branch is a separate check");
        bool previous=GameText.IsEnglish;
        try
        {
            for(int i=0;i<2;i++)
            {
                bool english=i==1;language(english);ctl.Sim.English=english;ctl.View.Open("train");yield return null;yield return null;
                Need(!ctl.Sim.AnyEpochActive,"no competing active epoch before actual training click");
                var page=ctl.View.Train;var button=Get<XgBtn>(page,"train");Need(button.button.IsInteractable(),"real Train button enabled");
                int before=ctl.Sim.Run(ctl.Sim.SelectedTrack).epoch;
                foreach(var frame in LcdActualUiProbe.WaitForClick(button.rt,"daily rhythm actual Train"))yield return frame;
                actualLCDclick(button.rt);
                double until=EditorApplication.timeSinceStartup+20;
                while(ctl.Sim.Run(ctl.Sim.SelectedTrack).epoch==before)
                {Need(EditorApplication.timeSinceStartup<until,"actual clicked epoch completes within bound");yield return null;}
                Need(ctl.Sim.Run(ctl.Sim.SelectedTrack).epoch==before+1,"actual training completes exactly one round before any delayed visual cue");
                until=EditorApplication.timeSinceStartup+1.5;
                while(Get<bool>(page,"rhythmPending")){Need(EditorApplication.timeSinceStartup<until,"completion cue resolves within bounded real time");yield return null;}
                string marker=english?"Round complete":"本轮完成";bool found=false,accent=false;
                foreach(var label in ctl.View.Juice.GetComponentsInChildren<TMP_Text>())
                {
                    if(!label.gameObject.activeInHierarchy||!label.text.Contains(marker))continue;
                    found=true;accent=label.text.Contains(english?"music accent":"随音乐重音");
                }
                Need(found,"visible completion text reports an actual accent or explicit normal fallback");
                Checks.Add(accent?"Actual source energy accent branch observed; no BPM estimate asserted.":"Ordinary fallback observed; this run does not claim a detected music accent.");
                capture("daily-rhythm-"+(english?"en":"zh")+(accent?"-accent":"-fallback")+".png");
                double settle=EditorApplication.timeSinceStartup+1.1;while(EditorApplication.timeSinceStartup<settle)yield return null;
                yield return DailyFeedbackActualUiChecks.DismissAssessment(ctl,actualLCDclick);
            }
        }
        finally{language(previous);ctl.Sim.English=previous;}
    }
}
#endif
