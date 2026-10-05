using System;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using UnityEngine;

namespace LingGuangV05.EditorTools
{
    /// <summary>Small isolated presentation checks. Invoke Run() through Editor tooling; no player-save access.</summary>
    public static class XgTrainingPresentationQa
    {
        [Serializable] public sealed class Report
        {
            public int passed;
            public string error = "", lifecycleScope = "";
            public bool success, lifecycleVerified;
            public float scaleAfterDisable, scaleAfterHandler;
        }

        public static string Run()
        {
            var report = new Report();
            GameObject host = null;
            float scale = Time.timeScale;
            try
            {
                Check(XgTrainPage.UseAssessmentOverlay(true, 5), "Manual assessments retain the dismissible overlay"); report.passed++;
                Check(!XgTrainPage.UseAssessmentOverlay(false, 4), "Auto4 uses mini-card even for a new grade"); report.passed++;
                Check(XgTrainPage.UseAssessmentOverlay(false, 3), "Pre-auto4 assessment remains an overlay"); report.passed++;
                Check(Math.Abs(XgTrainPage.AutomaticCardSeconds - 3f) < .0001f, "Automatic card lasts three seconds"); report.passed++;
                Check(XgJuice.Sfx.VariantsPerEvent == 3, "Each sound has three real clips");
                foreach (XgJuice.Sfx.Id id in Enum.GetValues(typeof(XgJuice.Sfx.Id)))
                {
                    var a = XgJuice.Sfx.Clip(id, 0); var b = XgJuice.Sfx.Clip(id, 1); var c = XgJuice.Sfx.Clip(id, 2);
                    Check(a != null && b != null && c != null && a != b && b != c && a != c, "Distinct audio variants: " + id);
                    Check(a.samples != b.samples && b.samples != c.samples, "Distinct variant envelopes: " + id);
                }
                report.passed++;
                host = new GameObject("TrainingPresentationQa", typeof(RectTransform));
                var juice = host.AddComponent<XgJuice>();
                juice.Init((RectTransform)host.transform, (RectTransform)host.transform, null);
                juice.Reduced = true;
                juice.HitStop(200); juice.Shake(12);
                Check(Time.timeScale == scale, "Reduced FX never freezes time");
                var hitstop = typeof(XgJuice).GetField("hitstop", BindingFlags.Instance | BindingFlags.NonPublic);
                Check((float)hitstop.GetValue(juice) == 0, "Reduced hitstop remains empty"); report.passed++;
                juice.Reduced = false;
                Time.timeScale = .5f; juice.HitStop(60);
                host.SetActive(false);
                report.scaleAfterDisable = Time.timeScale;
                if (Application.isPlaying)
                {
                    report.lifecycleScope = "Play Mode: real SetActive(false) lifecycle";
                    Check(Math.Abs(Time.timeScale - .5f) < .0001f, "Disabling juice restores previous time scale, not literal one");
                    report.lifecycleVerified = true;
                }
                else
                {
                    // Ordinary MonoBehaviours need not receive play lifecycle events in Edit Mode.
                    // Check the handler, and explicitly leave the real lifecycle gate unverified.
                    report.lifecycleScope = "Edit Mode: direct restoration handler only; verify lifecycle in Play";
                    typeof(XgJuice).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(juice, null);
                    Check(Math.Abs(Time.timeScale - .5f) < .0001f, "The disable handler restores previous time scale, not literal one");
                }
                report.scaleAfterHandler = Time.timeScale; report.passed++;
                report.success = true;
            }
            catch (Exception e) { report.error = e.ToString(); }
            finally { if (host != null) UnityEngine.Object.DestroyImmediate(host); Time.timeScale = scale; }
            return JsonUtility.ToJson(report, true);
        }

        static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
