#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using LingGuangV05.CaseMenu;
using LingGuangV05.Runtime;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Candidate for the serial Unity owner. No execution is claimed by this file.
public static class PlayerKnowledgeHudChecks
{
    static int count;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); count++; }

    [MenuItem("LingGuang/Verification/Player knowledge settings help")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use isolated EditMode verification");
        count = 0;
        var owner = new GameObject("Player knowledge help fixture");
        string language = "zh-CN";
        using (GameText.Bind(() => language))
        try
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            Check(font != null, "shipped bilingual font exists");
            var hud = owner.AddComponent<CaseMenuHud>();
            hud.Build(font, _ => { }, _ => { }, _ => { }, () => { }, () => { });
            var canvas = owner.GetComponentInChildren<Canvas>(true);
            // Deterministic logical-window geometry, not a physical GameView/input test.
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvas.transform;
            var refresh = typeof(CaseMenuHud).GetMethod("RefreshPlayerHelp", BindingFlags.Instance | BindingFlags.NonPublic);
            Button Button(string name) => owner.GetComponentsInChildren<Button>(true).Single(x => x.name == name);
            TMP_Text Text(string name) => owner.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == name);
            hud.OpenPlayerHelp(); Check(!hud.IsPlayerHelpOpen, "help cannot appear without settings");
            foreach (bool english in new[] { false, true })
            foreach (Vector2 size in new[] { new Vector2(1920,1080), new Vector2(1280,720), new Vector2(1024,768), new Vector2(640,480) })
            {
                language = english ? GameText.English : "zh-CN";
                canvasRect.sizeDelta = size;
                hud.ShowSettings(.8f, false, false);
                Button("PlayerHelp").onClick.Invoke();
                Canvas.ForceUpdateCanvases(); refresh.Invoke(hud, new object[] { true });
                Check(hud.IsSettingsOpen && hud.IsPlayerHelpOpen, "settings owns active help");
                Check(!hud.IsPlayerHelpGpuExpanded && !Text("GpuExplanation").gameObject.activeSelf, "GPU supplement is opt-in every opening");
                foreach (string name in new[] { "SixStagesExplanation", "TrainingWorkflowExplanation", "ContextAndTrainingExplanation" })
                    Check(!Text(name).gameObject.activeSelf, "extended reading is collapsed by default: " + name);
                Check(Text("Audience").text == PlayerKnowledgeCopy.Audience(english), "explicit localized player label");
                var card = (RectTransform)Text("Audience").transform.parent;
                Check(card.rect.width <= size.x - 60 && card.rect.height <= size.y - 60, "help fits available logical window");
                Check(!owner.GetComponentsInChildren<RectTransform>(true).Single(x => x.name == "SettingsCard").gameObject.activeSelf, "covered settings controls are unavailable");
                Button("GpuSupplement").onClick.Invoke();
                Check(hud.IsPlayerHelpGpuExpanded && Text("GpuExplanation").gameObject.activeSelf, "explicit GPU click reveals full supplement");
                foreach (string name in new[] { "SixStages", "TrainingWorkflow", "ContextAndTraining" }) Button(name).onClick.Invoke();
                foreach (string name in new[] { "Audience", "Context", "FormalExplanations", "GpuExplanation", "SixStagesExplanation", "TrainingWorkflowExplanation", "ContextAndTrainingExplanation" })
                {
                    var text = Text(name);
                    float preferred = text.GetPreferredValues(text.text, text.rectTransform.rect.width, Mathf.Infinity).y;
                    Check(text.rectTransform.rect.height + .5f >= preferred && text.overflowMode == TextOverflowModes.Overflow, name + " wraps without truncating paragraphs");
                }
                var scroll = owner.GetComponentsInChildren<ScrollRect>(true).Single();
                scroll.verticalNormalizedPosition = 0;
                Check(scroll.content.rect.height > scroll.viewport.rect.height, "long text is genuinely scrollable");
                Button("PlayerHelpBack").onClick.Invoke();
                Check(!hud.IsPlayerHelpOpen && hud.IsSettingsOpen, "back returns to settings");
                Button("PlayerHelp").onClick.Invoke();
                Check(!hud.IsPlayerHelpGpuExpanded, "reopening resets GPU expansion");
                Check(scroll.verticalNormalizedPosition > .99f, "reopening starts at first explanation");
                hud.HideSettings(); Check(!hud.IsPlayerHelpOpen && !hud.IsSettingsOpen, "settings close closes help");
            }
            Debug.Log("PLAYER_KNOWLEDGE_HUD_PASS " + count + ". Direct UI callbacks and logical-window geometry; not physical input, menu transition, or natural-play acceptance.");
        }
        finally { UnityEngine.Object.DestroyImmediate(owner); }
    }
}
#endif
