#if UNITY_EDITOR
using System;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

public static class LifeCadenceEditorChecks
{
    [MenuItem("Tools/Local Verification/Life event cadence")]
    public static void Run()
    {
        var data = Resources.Load<TextAsset>("LingGuangV05/Life/life_events_100");
        if (data == null) throw new InvalidOperationException("Missing production life-event resource.");
        int checks = LifeCadenceChecks.Run(data.text, state => JsonUtility.FromJson<XgState>(JsonUtility.ToJson(state)));
        Debug.Log("LIFE_CADENCE_EDITOR_PASS: " + checks + " real-simulation/JsonUtility checks. No real saves or Main scene.");
        LifeHostSettingsChecks.Run();
    }
}
#endif
