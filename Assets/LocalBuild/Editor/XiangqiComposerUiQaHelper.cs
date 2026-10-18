#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Games;
using LingGuangV05.Desktop.XingGuang;
using Michsky.DreamOS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Actual canvas controls inside root's isolated Main PlayMode fixture; no own bootstrap/model execution.</summary>
public static class XiangqiComposerUiQaHelper
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Flags).GetValue(obj);
    static void Check(bool value,string label){if(!value)throw new InvalidOperationException(label);Debug.Log("PASS XiangqiComposerUiQa: "+label);}
    static void Click(GameHubView hub,string name)
    {
        var button=hub.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);
        Check(button!=null&&button.IsInteractable(),"Visible enabled control: "+name);var rect=(RectTransform)button.transform;
        FilesWindowUiChecks.Click((RectTransform)button.transform); // Production LCD mapping, top-hit guard and real down/up.
    }
    public static IEnumerator CheckMain(GameHubView hub,XingGuangController ctl,Action switchToUnpurchasedSave)
    {
        Check(switchToUnpurchasedSave!=null,"Root supplied actual isolated save-rebind callback");
        var window=hub.GetComponentInParent<WindowManager>(true);window.OpenWindow();window.FocusToWindow();
        float ready=Time.unscaledTime+1.5f;while(Time.unscaledTime<ready)yield return null;
        var sim=ctl.Sim;sim.S.unlocked.Remove("board.logic4");yield return null;
        var nav=Field<Dictionary<string,Image>>(hub,"navFills")["board.xiangqi"];
        Check(!nav.gameObject.activeSelf,"Unpurchased Lv4 hides the restricted Xiangqi entry");
        var stale=Field<Button>(hub,"xiangqiNewButton");stale.onClick.Invoke();
        Check(Field<XiangqiRookComposer>(hub,"xiangqiComposer")==null,"Stale callback cannot bypass gate (guard-only check)");
        sim.S.stage=5;sim.S.unlocked.Add("board.logic4");yield return null;
        Click(hub,"Nav board.xiangqi");yield return null;
        double samples=sim.Labels("chess");int records=sim.S.boardRounds.Count,files=sim.S.dataFiles.Count;
        Click(hub,"Compose Xiangqi puzzle");
        float deadline=Time.unscaledTime+16;while(Time.unscaledTime<deadline&&Field<XiangqiRookEndgame>(hub,"xiangqiBoard")==null)yield return null;
        var board=Field<XiangqiRookEndgame>(hub,"xiangqiBoard");Check(board!=null,"Verified position appears through native UI");var initial=board.Position();
        var move=Field<XiangqiRookComposer>(hub,"xiangqiComposer").Solutions[0];
        Click(hub,"Xiangqi square "+move.From);Click(hub,"Xiangqi square "+move.To);yield return null;
        Check(Field<XiangqiRookEndgame>(hub,"xiangqiBoard").Checkmate,"Actual board buttons deliver verified legal checkmate");
        Click(hub,"Reset Xiangqi puzzle");yield return null;
        Check(initial.SequenceEqual(Field<XiangqiRookEndgame>(hub,"xiangqiBoard").Position()),"Reset restores same position");
        Click(hub,"Nav home");yield return null;Click(hub,"Nav board.xiangqi");yield return null;
        Check(initial.SequenceEqual(Field<XiangqiRookEndgame>(hub,"xiangqiBoard").Position()),"Reopen retains current position");
        Check(sim.Labels("chess")==samples&&sim.S.boardRounds.Count==records&&sim.S.dataFiles.Count==files,"No sample/record/file reward from composition or practice");
        Click(hub,"Compose Xiangqi puzzle");switchToUnpurchasedSave();Check(!ReferenceEquals(sim,ctl.Sim),"Real save rebind occurred");stale.onClick.Invoke();yield return null;
        Check(!nav.gameObject.activeSelf&&Field<XiangqiRookComposer>(hub,"xiangqiComposer")==null&&Field<XiangqiRookEndgame>(hub,"xiangqiBoard")==null,"Save switch cancels pending generator, clears former board and closes entry");
        Check(Field<string>(hub,"showing")=="home","Unpurchased new save returns home");
    }
}
#endif
