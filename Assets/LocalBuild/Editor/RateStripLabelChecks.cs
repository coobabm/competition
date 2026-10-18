#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEngine;

// Call from the isolated Main fixture with shared.lr purchased, train.curve open,
// and fewer than SamplesToTrain samples in every dataset of the selected track.
// setEnglish must use the fixture's existing GameText binding. Caller restores locale.
public static class RateStripLabelChecks
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static object Field(object obj, string name) => obj.GetType().GetField(name, Flags).GetValue(obj);
    static void Need(bool test, string reason) { if (!test) throw new InvalidOperationException("Rate strip: " + reason); }
    public static void Run(XgTrainPage page, XgSim sim, Action<bool> setEnglish)
    {
        Need(sim.Has("shared.lr") && sim.TrainPart("rate"), "fixture must own learning-rate node");
        Need(sim.TeachingTrainOpen, "fixture must have opened training");
        foreach (var dataset in XgCatalog.DatasetsFor(sim.SelectedTrack))
            Need(sim.Samples(dataset.id) < XgCatalog.SamplesToTrain, "fixture must genuinely lack training samples");
        Need(!sim.TrainingUnlocked(sim.SelectedTrack), "sample gate must be closed");
        var run = sim.Run(sim.SelectedTrack);
        bool active = run.epochActive;
        try
        {
            run.epochActive = false;
            var buttons = (XgBtn[])Field(page, "rateBtns");
            foreach (bool english in new[] { false, true })
            {
                setEnglish(english);
                page.Refresh();
                Need(((RectTransform)Field(page, "rateStrip")).gameObject.activeSelf, "owned rate strip should be shown");
                Need(buttons.Length == 6, "all six existing rate tiers preserved");
                foreach (var button in buttons)
                {
                    Need(!string.IsNullOrWhiteSpace(button.label.text), "owned tier label cannot be blank without samples");
                    Need(button.button.interactable, "owned rate can be selected before samples are ready");
                }
                string title = ((TMP_Text)Field(page, "rateTitle")).text;
                Need(english ? title.Contains("Rate") && !Regex.IsMatch(title, "[\\u4e00-\\u9fff]") : title.Contains("学习率"), "title must refresh in selected language");
            }
            run.epochActive = true;
            page.Refresh();
            foreach (var button in buttons) Need(!button.button.interactable, "all rate choices must lock during training");
            Debug.Log("RateStripLabelChecks PASS: six owned labels without samples; zh/en title refresh; active-round lock.");
        }
        finally { run.epochActive = active; page.Refresh(); }
    }
}
#endif
