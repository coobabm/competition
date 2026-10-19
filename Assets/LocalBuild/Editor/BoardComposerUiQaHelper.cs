#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LingGuangV05.Core.Games;
using LingGuangV05.Desktop.Games;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.OS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Run inside root's already-isolated Main PlayMode fixture. Does not load scenes, run models or touch user saves.</summary>
public static class BoardComposerUiQaHelper
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,Flags).GetValue(obj);
    static void Check(bool value,string message){if(!value)throw new InvalidOperationException(message);Debug.Log("PASS BoardComposerUiQa: "+message);}
    static void Click(GameHubView hub,string name)
    {
        var button=hub.GetComponentsInChildren<Button>(true).FirstOrDefault(x=>x.name==name&&x.gameObject.activeInHierarchy);
        Check(button!=null&&button.IsInteractable(),"Visible enabled control: "+name);
        FilesWindowUiChecks.Click((RectTransform)button.transform); // Production LCD mapping, top-hit guard and real down/up.
    }
    /// <param name="switchToUnpurchasedSave">Root's real runtime/controller save rebind. It must replace ctl.Sim with a fresh save without Lv4.</param>
    public static IEnumerator CheckMain(GameHubView hub,XingGuangController ctl,Action switchToUnpurchasedSave)
    {
        Check(switchToUnpurchasedSave!=null,"Actual save-switch callback supplied by isolated root fixture");
        var window=hub.GetComponentInParent<WindowManager>(true);window.OpenWindow();window.FocusToWindow();
        float readyAt=Time.unscaledTime+1.5f;while(Time.unscaledTime<readyAt)yield return null;
        var sim=ctl.Sim;sim.S.unlocked.Remove("board.logic4");yield return null;
        var nav=Field<Dictionary<string,Image>>(hub,"navFills")["board.composer"];
        Check(!nav.gameObject.activeSelf,"Unpurchased Lv4 has no composer entry");
        var staleButton=Field<Button>(hub,"composeButton");staleButton.onClick.Invoke();
        Check(Field<ChessComposer>(hub,"composer")==null,"Direct stale callback cannot bypass purchase gate (guard-only check)");
        sim.S.stage=5;sim.S.unlocked.Add("board.logic4");yield return null;
        Click(hub,"Nav board.composer");yield return null;
        double samples=sim.Labels("chess");int records=sim.S.boardRounds.Count,files=sim.S.dataFiles.Count;
        Click(hub,"Compose new puzzle");
        float deadline=Time.unscaledTime+12;while(Time.unscaledTime<deadline&&Field<ChessGame>(hub,"composedBoard")==null)yield return null;
        var board=Field<ChessGame>(hub,"composedBoard");Check(board!=null,"A verified puzzle appears through the actual purchased entry");
        string fen=board.ToFen();var generator=Field<ChessComposer>(hub,"composer");var move=generator.Solutions[0];
        Click(hub,"Compose square "+move.From);Click(hub,"Compose square "+move.To);yield return null;
        Check(Field<ChessGame>(hub,"composedBoard").Result==ChessGame.WhiteWins,"Actual board buttons produce legal checkmate");
        Click(hub,"Reset composed puzzle");yield return null;
        Check(Field<ChessGame>(hub,"composedBoard").ToFen()==fen,"Actual reset restores the same puzzle");
        Click(hub,"Nav home");yield return null;Click(hub,"Nav board.composer");yield return null;
        Check(Field<ChessGame>(hub,"composedBoard").ToFen()==fen,"Reopening retains the current verified puzzle");
        Check(sim.Labels("chess")==samples&&sim.S.boardRounds.Count==records&&sim.S.dataFiles.Count==files,"Composition/solve/reset creates no free samples or fictitious game records");
        Click(hub,"Compose new puzzle");switchToUnpurchasedSave();
        Check(!ReferenceEquals(sim,ctl.Sim),"Root callback really changed saves");
        // A click delivered in the same frame as rebinding must be refused before Update clears the old display.
        staleButton.onClick.Invoke();yield return null;
        Check(!nav.gameObject.activeSelf&&Field<ChessComposer>(hub,"composer")==null&&Field<ChessGame>(hub,"composedBoard")==null,"Save switch cancels generation, clears old board and closes purchased entry");
        Check(Field<string>(hub,"showing")=="home","New unpurchased save returns to Game Hub home");
    }
}
#endif
