#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.Core;
using LingGuangV05.Runtime;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.YY;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

// Exercises the actual no-scene route twice without Main, player saves, or a visible overlay.
public static class LoveQuestionFallbackChecks
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Set(object target,string name,object value) => target.GetType().GetField(name,Private).SetValue(target,value);
    static void Call(object target,string name,params object[] args) => target.GetType().GetMethod(name,Private).Invoke(target,args);
    static void Need(bool ok,string reason) { if(!ok)throw new Exception(reason); }
    public static void Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit Mode only");
        var previous=YYChatHub.Instance;
        var fixture=new GameObject("Inactive love fallback fixture");fixture.SetActive(false);
        try
        {
            var runtime=fixture.AddComponent<ChapterOneRuntime>();
            runtime.SetSimulationForTests(new ChapterOneSim());
            var hub=fixture.AddComponent<YYChatHub>();hub.runtime=runtime;
            var state=new YYState();state.conversations.Add(new YYConversation{id=YYChatHub.GirlfriendId});
            typeof(YYChatHub).GetProperty("S").SetValue(hub,state);
            typeof(YYChatHub).GetProperty("Instance").SetValue(null,hub);
            var scene=fixture.AddComponent<LoveQuestionCutscene>();Set(scene,"runtime",runtime);
            var sim=new XgSim();sim.S.stage=5;sim.S.loveConsent=1;sim.LoveDataReady=true;
            Call(scene,"Bind",sim);
            // A previous successful scene left posted=true. Both later requests must still settle.
            Set(scene,"posted",true);
            for(int attempt=0;attempt<2;attempt++)
            {
                sim.LoveToday=10+attempt*XgSim.LoveCooldownDays;
                sim.LoveReply();Need(sim.LoveVerdictPending,"Expected a new pending verdict");
                int lines=sim.S.chat.Count;
                Call(scene,"PostWithoutScene");
                Need(!sim.LoveVerdictPending,"Fallback left the verdict stuck pending");
                Need(sim.S.loveVerdicts==attempt+1,"Fallback did not post exactly one new verdict");
                Need(sim.S.chat.Count==lines+2,"Fallback must append conclusion and limit to chat");
                Need(!LoveQuestionCutscene.Playing,"Fallback must not block the desktop");
            }
            // If data disappears, cancellation must also release pending without creating an overlay.
            sim.LoveToday+=XgSim.LoveCooldownDays;sim.LoveReply();
            typeof(YYChatHub).GetProperty("Instance").SetValue(null,null);
            Call(scene,"PostWithoutScene");
            Need(!sim.LoveVerdictPending&&!LoveQuestionCutscene.Playing,"Missing data left fallback blocking");
            Debug.Log("LOVE_FALLBACK_PASS: repeated real fallback posts, chat output, pending release and missing-data cancellation; no player saves.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fixture);
            typeof(YYChatHub).GetProperty("Instance").SetValue(null,previous);
        }
    }
}
#endif
