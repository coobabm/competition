#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Tieba;
using UnityEditor;
using UnityEngine;

/// <summary>
/// E2 patch-5 candidate check (not run). Pure logic: the sidebar reuse key must change for anything DrawSide shows,
/// including same-count lists whose names or order differ. No Play mode, no save, no PlayerPrefs, no timing.
/// </summary>
public static class TiebaSideKeyChecks
{
    static int checks;
    static void Need(bool ok, string why) { if (!ok) throw new Exception("Tieba side key: " + why); checks++; }

    static ForumBoard B(string id, string name, string en) => new ForumBoard { id = id, name = name, nameEn = en };
    static List<ForumBoard> Copy(List<ForumBoard> list)
    {
        var copy = new List<ForumBoard>();
        foreach (var b in list) copy.Add(B(b.id, b.name, b.nameEn));
        return copy;
    }

    [MenuItem("Tools/Local Verification/Tieba sidebar key (E2 candidate)")]
    public static void Run()
    {
        checks = 0;
        string path = Path.Combine(Application.dataPath, "LingGuangV05", "Resources", "LingGuangV05", "Forum", "forum.json");
        var shipped = ForumLibrary.Parse(File.ReadAllText(path)).Boards;
        Need(shipped.Count >= 2, "the shipped forum has at least two boards to reorder");

        var a = Copy(shipped);
        string baseKey = TiebaView.SideKey(false, "", a);
        Need(baseKey == TiebaView.SideKey(false, "", Copy(shipped)), "equal content in new objects → same key (rows may be reused)");
        Need(baseKey != TiebaView.SideKey(true, "", a), "language change → rebuild");
        Need(baseKey != TiebaView.SideKey(false, a[0].id, a), "highlighting a board → rebuild");
        Need(TiebaView.SideKey(false, a[0].id, a) != TiebaView.SideKey(false, a[1].id, a), "moving the highlight → rebuild");

        // Same count, different content: a count-only key would wrongly reuse these.
        var renamed = Copy(shipped); renamed[0].name += "（改）";
        Need(renamed.Count == a.Count && baseKey != TiebaView.SideKey(false, "", renamed), "same count, renamed board → rebuild");
        var renamedEn = Copy(shipped); renamedEn[0].nameEn += " (new)";
        Need(baseKey != TiebaView.SideKey(false, "", renamedEn), "same count, changed English name → rebuild");
        var reid = Copy(shipped); reid[0].id += "_x";
        Need(baseKey != TiebaView.SideKey(false, "", reid), "same count, changed id (click target/icon) → rebuild");
        var swapped = Copy(shipped); var first = swapped[0]; swapped[0] = swapped[1]; swapped[1] = first;
        Need(swapped.Count == a.Count && baseKey != TiebaView.SideKey(false, "", swapped), "same count, swapped order → rebuild");
        var replaced = Copy(shipped); replaced[replaced.Count - 1] = B("zz", "新吧", "New board");
        Need(baseKey != TiebaView.SideKey(false, "", replaced), "same count, one board replaced → rebuild");

        // Separator safety: field boundaries cannot be confused by shifting characters between id and name.
        var x = new List<ForumBoard> { B("ab", "c", "c") }; var y = new List<ForumBoard> { B("a", "bc", "c") };
        Need(TiebaView.SideKey(false, "", x) != TiebaView.SideKey(false, "", y), "id/name boundary is part of the key");
        Need(TiebaView.SideKey(false, "", new List<ForumBoard>()) != TiebaView.SideKey(false, "", new List<ForumBoard> { B("", "", "") }), "empty list differs from one empty board");
        Need(TiebaView.SideKey(false, null, null) == TiebaView.SideKey(false, "", null), "null highlight equals no highlight");

        Debug.Log("TIEBA_SIDE_KEY_PASS: " + checks + " checks (language, highlight, id/name/English name, order; not count-only). Logic only.");
    }
}
#endif
