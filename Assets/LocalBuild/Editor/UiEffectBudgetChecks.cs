#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using LingGuangV05.Desktop;
using LingGuangV05.Desktop.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UiEffectBudgetChecks
{
    const string LowKey="LingGuang.Graphics.Low", FpsKey="LingGuang.Graphics.Fps";
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Tick(object target)=>target.GetType().GetMethod("Update",Private).Invoke(target,null);
    [MenuItem("Tools/Local Verification/UI effect budgets")]
    public static void Run()
    {
        bool hadLow=PlayerPrefs.HasKey(LowKey),hadFps=PlayerPrefs.HasKey(FpsKey);
        int savedLow=PlayerPrefs.GetInt(LowKey),savedFps=PlayerPrefs.GetInt(FpsKey);
        var root=new GameObject("Effect budget verification",typeof(RectTransform),typeof(Canvas));
        try
        {
            GamePerformance.SetLow(false);GamePerformance.SetSixty(true);
            Require(Application.targetFrameRate==60&&QualitySettings.vSyncCount==0,"60fps setting");
            GamePerformance.SetSixty(false);Require(Application.targetFrameRate==30,"30fps setting");
            var rt=(RectTransform)root.transform;rt.sizeDelta=new Vector2(800,600);
            var juice=root.AddComponent<XgJuice>();juice.Init(rt,rt,TMP_Settings.defaultFontAsset);juice.Muted=true;
            for(int i=0;i<100;i++)juice.Float(Vector2.zero,"reward "+i,Color.white);
            Require(juice.ActiveFloaters==8,"Only eight latest reward floaters remain");
            Require(root.GetComponentsInChildren<TMP_Text>(true).Length==8,"Floater objects are reused rather than leaked");
            for(int i=0;i<20;i++){juice.Burst(Vector2.zero,80,Color.white);juice.Trail(Vector2.zero,Vector2.one,Color.white);juice.Crumble(Vector2.zero,80,Color.white);}
            Require(juice.ActiveParticles==XgJuice.ParticleLimit,"All particle emitters share the budget");
            GamePerformance.SetLow(true);Tick(juice);
            Require(juice.ActiveParticles==0,"Switching low clears already-running particles");
            juice.Burst(Vector2.zero,20,Color.white);juice.Trail(Vector2.zero,Vector2.one,Color.white);juice.TransferCoins(Vector2.zero,Vector2.one);juice.Crumble(Vector2.zero,10,Color.white);
            Require(juice.ActiveParticles==0,"Low mode suppresses every particle entry point");
            Require(juice.ActiveFloaters==8,"Low mode preserves readable answer rewards");
            juice.Visible=()=>false;Tick(juice);Require(juice.ActiveFloaters==0,"Hidden application clears presentation");
            var chartObject=new GameObject("Chart",typeof(RectTransform),typeof(CanvasRenderer));chartObject.transform.SetParent(root.transform,false);
            ((RectTransform)chartObject.transform).sizeDelta=new Vector2(400,200);
            var chart=chartObject.AddComponent<XgChartGraphic>();
            var train=new List<float>{.2f,.3f};var val=new List<float>{.1f,.25f};
            chart.SetData(train,val,.25f);Tick(chart);Canvas.ForceUpdateCanvases();
            int built=chart.MeshBuilds;Require(built>0,"Real chart geometry was built");
            for(int i=0;i<40;i++){chart.SetData(train,val,.25f);Tick(chart);Canvas.ForceUpdateCanvases();}
            Require(chart.MeshBuilds==built,"Unchanged history never dirties geometry");
            train.Add(.4f);val.Add(.35f);chart.SetData(train,val,.35f);Tick(chart);Canvas.ForceUpdateCanvases();
            Require(chart.MeshBuilds==built,"Low-mode redraw waits for next budget interval");
            typeof(XgChartGraphic).GetField("nextDraw",Private).SetValue(chart,-1f);Tick(chart);Canvas.ForceUpdateCanvases();
            Require(chart.MeshBuilds==built+1,"Pending changed history is drawn once, not lost");
            float next=(float)typeof(XgChartGraphic).GetField("nextDraw",Private).GetValue(chart);
            Require(next>=Time.unscaledTime+.099f,"Low-mode curve schedules a 100ms interval");
            GamePerformance.SetLow(false);train.Add(.5f);val.Add(.45f);chart.SetData(train,val,.45f);Canvas.ForceUpdateCanvases();
            Require(chart.MeshBuilds==built+2,"High mode redraws changed history immediately");
            Debug.Log("UI_EFFECT_BUDGET_PASS: real pool limits, low-mode suppression, hidden cleanup, unchanged curve and low-rate scheduling. Timing gate is tested directly, not a claim of measured FPS.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            if(hadLow)PlayerPrefs.SetInt(LowKey,savedLow);else PlayerPrefs.DeleteKey(LowKey);
            if(hadFps)PlayerPrefs.SetInt(FpsKey,savedFps);else PlayerPrefs.DeleteKey(FpsKey);
            GamePerformance.Apply();PlayerPrefs.Save();
        }
    }
}
#endif
