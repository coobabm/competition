#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using LingGuangV05.Desktop.Story;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

public static class CrowdNoticeChecks
{
    static void Need(bool ok, string message) { if (!ok) throw new Exception(message); }
    public static void Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit Mode only");
        var instance = typeof(XingGuangController).GetProperty("Instance");
        var simProperty = typeof(XingGuangController).GetProperty("Sim");
        var previous = XingGuangController.Instance;
        var fixture = new GameObject("Inactive crowd notice fixture"); fixture.SetActive(false);
        try
        {
            var controller = fixture.AddComponent<XingGuangController>();
            var tray = fixture.AddComponent<TrayPopups>();
            instance.SetValue(null, controller);
            var sim = new XgSim(); simProperty.SetValue(controller, sim);
            var queue = (ICollection)typeof(TrayPopups).GetField("queue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tray);
            for (int stage = 1; stage <= 2; stage++)
            {
                sim.S.stage = stage;
                tray.Enqueue("摆渡众包", "early story", 3, story: true);
                tray.EnqueueNotification("Bodu Crowdsourcing", "early notification", 3, null, () => true, null);
                Need(queue.Count == 0, "Stage " + stage + " must not enqueue either kind of popup");
            }
            sim.S.stage = 3;
            tray.Enqueue("摆渡众包", "eligible story", 3, story: true);
            bool callerValid = true;
            tray.EnqueueNotification("Bodu Crowd", "eligible notification", 3, null, () => callerValid, null);
            Need(queue.Count == 2, "Stage three must enqueue both paths");
            foreach (var notice in queue)
            {
                var valid = (Func<bool>)notice.GetType().GetField("valid").GetValue(notice);
                Need(valid != null && valid(), "Queued popup needs a live guard");
                sim.S.stage = 2; Need(!valid(), "Queued popup bypassed stage gate"); sim.S.stage = 3;
                var other = new XgSim(); other.S.stage = 3; simProperty.SetValue(controller, other);
                Need(!valid(), "Queued popup crossed saves"); simProperty.SetValue(controller, sim);
                if ((string)notice.GetType().GetField("text").GetValue(notice) == "eligible notification")
                { callerValid = false; Need(!valid(), "Existing validity callback must be retained"); callerValid = true; }
            }
            Debug.Log("CROWD_NOTICE_PASS: real inactive tray queue, both entry paths, stages 1/2/3, queued stage/save recheck and existing validity.");
        }
        finally { UnityEngine.Object.DestroyImmediate(fixture); instance.SetValue(null, previous); }
    }
}
#endif
