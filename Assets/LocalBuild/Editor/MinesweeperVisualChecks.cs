#if UNITY_EDITOR
using System;
using System.Reflection;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Isolated EditMode UI construction, no scene bootstrap, settlement or player persistence.
public static class MinesweeperVisualChecks
{
    static void Need(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    [MenuItem("Tools/Local Verification/Minesweeper grey cells and zero")]
    public static void Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode, "Run in Edit Mode");
        var fixture = new GameObject("Minesweeper visual fixture", typeof(RectTransform), typeof(Canvas));
        fixture.hideFlags = HideFlags.HideAndDontSave;
        fixture.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        var root = (RectTransform)fixture.transform; root.sizeDelta = new Vector2(900, 480);
        XgMiniContext context = null; XgMiniGame game = null;
        int zeroes = 0, boards = 0;
        try
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset");
            Need(font != null, "Shipped desktop font missing");
            var sim = new XgSim(); string before = JsonUtility.ToJson(sim.S);
            var type = typeof(XgMiniContext).Assembly.GetType("LingGuangV05.Desktop.XingGuang.MiniGames.XgMinePack", true);
            var cardField = type.BaseType.GetField("QuestionCard", BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (bool dark in new[] { false, true })
            {
                context = new XgMiniContext(root, new XgUi(font, null, dark), sim, null, "minesweeper_yn",
                    () => false, () => .5, _ => { }, (_, __) => { throw new Exception("Visual check must not settle"); });
                // Seed only this fixture's private context; production question generation is untouched.
                typeof(XgMiniContext).GetField("Random").SetValue(context, new System.Random(731));
                game = (XgMiniGame)Activator.CreateInstance(type, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { context }, null);
                for (int round = 0; round < 8; round++)
                {
                    game.Build(); Canvas.ForceUpdateCanvases(); boards++;
                    var card = (XgCard)cardField.GetValue(game);
                    Need(card != null && card.teachingImageKeys.Length == 25, "Real mine board missing");
                    for (int i = 0; i < 25; i++)
                    {
                        bool target = i == card.teachingImageKey;
                        var image = root.Find("Cell" + i).GetComponent<Image>();
                        var value = root.Find("Value" + i).GetComponent<TMP_Text>();
                        Color32 expected = target ? new Color32(255,213,84,255) : new Color32(214,218,224,255);
                        Need(((Color32)image.color).Equals(expected), "Cell background: " + i);
                        if (target) { Need(value.text == "?", "Target must stay hidden"); continue; }
                        if (card.teachingImageKeys[i] != 0) { Need(value.text == "●", "Known mine marker missing"); continue; }
                        int count = 0, x = i % 5, y = i / 5;
                        for (int row = Math.Max(0,y-1); row <= Math.Min(4,y+1); row++)
                        for (int col = Math.Max(0,x-1); col <= Math.Min(4,x+1); col++)
                            if (row != y || col != x) count += card.teachingImageKeys[row*5+col];
                        Need(value.text == count.ToString(), "Opened safe cell must show its count, including zero: " + i);
                        if (count == 0)
                        {
                            value.ForceMeshUpdate(true, true);
                            Need(value.color.a > .99f && value.textInfo.characterCount == 1 && value.textInfo.characterInfo[0].isVisible,
                                "Zero must have visible TMP geometry");
                            zeroes++;
                        }
                    }
                }
                game.Dispose(); game = null; context.Close(); context = null;
            }
            Need(zeroes > 0, "Fixture must exercise opened zero cells");
            Need(before == JsonUtility.ToJson(sim.S), "Visual construction changed simulation/progression state");
            Debug.Log("MINESWEEPER_VISUAL_PASS boards=" + boards + " zeroes=" + zeroes + ". Actual UI objects and TMP geometry; no PlayMode, screenshots or answer settlement.");
        }
        finally { game?.Dispose(); context?.Close(); UnityEngine.Object.DestroyImmediate(fixture); }
    }
}
#endif
