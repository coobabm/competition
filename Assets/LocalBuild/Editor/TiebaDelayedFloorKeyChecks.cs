#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LingGuangV05.Core.Forum;
using LingGuangV05.Desktop.Tieba;
using UnityEditor;
using UnityEngine;

/// <summary>
/// E2 patch-3 candidate check (not run). Pure data/logic, no Play mode, no save, no PlayerPrefs.
/// Proves on the shipped forum.json that the delayed-floor key changes exactly when a time-dependent floor view changes,
/// and that the flag key follows every "requires" flag. It does not measure performance.
/// </summary>
public static class TiebaDelayedFloorKeyChecks
{
    static int checks;
    static void Need(bool ok, string why) { if (!ok) throw new Exception("Tieba delayed-floor key: " + why); checks++; }

    static string ForumJsonPath => Path.Combine(Application.dataPath, "LingGuangV05", "Resources", "LingGuangV05", "Forum", "forum.json");

    static ForumContext At(int stage, int month, int day, bool posted, double seconds, Func<string, bool> flag = null)
        => new ForumContext { stage = stage, today = new DateTime(2016, month, day), helpPosted = posted, secondsSinceHelp = seconds, flag = flag };

    /// <summary>Every floor's view, plus every thread's visibility, in library order: what the pages can show.</summary>
    static string Views(ForumLibrary lib, ForumContext c)
    {
        var sb = new StringBuilder();
        foreach (var t in lib.Threads)
        {
            sb.Append(ForumLibrary.Visible(t, c) ? 'V' : 'v');
            foreach (var f in t.floors) sb.Append((int)ForumLibrary.View(f, c));
            sb.Append('|');
        }
        return sb.ToString();
    }

    static int OldBucket(ForumContext c) => c.helpPosted ? (int)(c.secondsSinceHelp / 10) : -1;

    [MenuItem("Tools/Local Verification/Tieba delayed-floor key (E2 candidate)")]
    public static void Run()
    {
        checks = 0;
        string json = File.ReadAllText(ForumJsonPath);

        // 1. Correction of the old D2 report: the word appears four times, but only three are positive delays.
        Need(Regex.Matches(json, "afterSeconds").Count == 4, "forum.json mentions afterSeconds four times (one is the note text)");
        var numeric = Regex.Matches(json, "\"afterSeconds\"\\s*:\\s*([0-9.]+)");
        Need(numeric.Count == 3, "exactly three numeric afterSeconds fields");
        Need(Regex.IsMatch(json, "\"note\"\\s*:\\s*\"[^\"]*afterSeconds"), "the fourth mention is inside the top-level note string");

        var lib = ForumLibrary.Parse(json);
        var delays = new List<double>(); var owners = new HashSet<string>();
        foreach (var t in lib.Threads) foreach (var f in t.floors) if (f.afterSeconds > 0) { delays.Add(f.afterSeconds); owners.Add(t.id); }
        Need(delays.Count == 3 && delays[0] == 40 && delays[1] == 90 && delays[2] == 150, "parsed positive delays are 40/90/150 in order, got " + string.Join("/", delays));
        Need(owners.Count == 1 && owners.Contains("help_bios"), "all delayed floors belong to help_bios");

        // 2. Key table at the thresholds.
        Need(TiebaView.DelayedFloorKey(lib, At(1, 6, 5, false, 0)) == "000", "not posted: nothing arrived");
        Need(TiebaView.DelayedFloorKey(lib, At(1, 6, 5, false, 1000)) == "000", "not posted: elapsed time is ignored");
        var table = new[] { (0.0, "000"), (39.9, "000"), (40.0, "100"), (89.9, "100"), (90.0, "110"), (149.9, "110"), (150.0, "111"), (100000.0, "111") };
        foreach (var (s, expect) in table)
            Need(TiebaView.DelayedFloorKey(lib, At(1, 6, 5, true, s)) == expect, "posted, " + s + " s → " + expect);
        Need(TiebaView.DelayedFloorKey(null, At(1, 6, 5, true, 0)) == "" && TiebaView.DelayedFloorKey(lib, null) == "", "null inputs give an empty key");

        // 3. Semantics: with stage/date/flags fixed, the key changes exactly when some thread/floor view changes.
        bool sawOldBucketOnlyChange = false;
        foreach (var (stage, month, day) in new[] { (1, 6, 5), (3, 8, 20), (5, 10, 20), (6, 12, 31) })
            foreach (bool posted in new[] { false, true })
            {
                string prevKey = null, prevViews = null; int prevBucket = 0;
                for (double s = 0; s <= 400; s += .5)
                {
                    var c = At(stage, month, day, posted, s);
                    string key = TiebaView.DelayedFloorKey(lib, c), views = Views(lib, c); int bucket = OldBucket(c);
                    if (prevKey != null)
                    {
                        Need((key != prevKey) == (views != prevViews), "stage " + stage + " posted " + posted + " at " + s + " s: key change must equal view change");
                        if (bucket != prevBucket && views == prevViews) sawOldBucketOnlyChange = true;
                    }
                    prevKey = key; prevViews = views; prevBucket = bucket;
                }
            }
        // Source fact only: the old 10 s bucket changed while nothing visible changed. Not a frame-time claim.
        Need(sawOldBucketOnlyChange, "the old bucket changes without any view change (documents the removed redraw cause)");

        // 4. Flags: every distinct "requires" flag has its own bit, and each one does change some view.
        var all = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var t in lib.Threads) { if (t.requires.Length > 0) all.Add(t.requires); foreach (var f in t.floors) if (f.requires.Length > 0) all.Add(f.requires); }
        Need(all.Contains("qc.trap"), "qc.trap is one of the gate flags");
        Need(TiebaView.FlagGateKey(lib, At(6, 12, 31, false, 0)) == new string('0', all.Count), "no flag reader: all bits clear");
        Need(TiebaView.FlagGateKey(lib, At(6, 12, 31, false, 0, id => true)) == new string('1', all.Count), "every flag set: all bits set");
        foreach (var flag in all)
        {
            string name = flag;
            var on = At(6, 12, 31, true, 1000, id => true);
            var off = At(6, 12, 31, true, 1000, id => id != name);
            Need(TiebaView.FlagGateKey(lib, on) != TiebaView.FlagGateKey(lib, off), "flag " + name + " changes the flag key");
            Need(Views(lib, on) != Views(lib, off), "flag " + name + " changes at least one thread or floor view");
            Need(TiebaView.DelayedFloorKey(lib, on) == TiebaView.DelayedFloorKey(lib, off), "flag " + name + " does not disturb the delayed key");
        }

        Debug.Log("TIEBA_DELAYED_FLOOR_KEY_PASS: " + checks + " checks on shipped forum.json (three positive delays 40/90/150; the fourth mention is the note). Logic only; no UI, no timing.");
    }
}
#endif
