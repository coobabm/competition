#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Desktop.XingGuang.MiniGames;
using LingGuangV05.XingGuang;
using UnityEditor;
using UnityEngine;

/// <summary>Checks final merged question arrays and actual sampling APIs without building UI or touching saves.</summary>
public static class QuestionExpansionChecks
{
    const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    [Serializable] sealed class Report
    {
        public string status = "running", error = "";
        public List<string> checks = new List<string>(), pools = new List<string>(), risks = new List<string>();
        public List<string> uncovered = new List<string> { "No rendered UI, player answers, answer settlement or actual keyboard/mouse interaction.", "Calendar validation checks date syntax and authored gates; it cannot establish historical truth of every authored sentence.", "Static constructors retain their normal process-wide initialization. No global Recent lists are modified by this fixture." };
    }
    static void Need(Report r, bool ok, string message) { if (!ok) throw new InvalidOperationException(message); r.checks.Add(message); }
    static Type Mini(string name) => typeof(XgMiniContext).Assembly.GetType("LingGuangV05.Desktop.XingGuang.MiniGames." + name, true);
    static T Value<T>(Type type, string field) => (T)(type.GetField(field, Static) ?? throw new MissingFieldException(type.Name, field)).GetValue(null);
    static int[] CompareDates(int total)
    {
        var bank = typeof(XgSim).Assembly.GetType("LingGuangV05.XingGuang.XgQuestionBank", true);
        var added = Value<int[]>(bank, "CompareSince");
        if (added.Length > total) throw new InvalidOperationException("Compare dates exceed combined questions");
        var dates = new int[total]; Array.Copy(added, 0, dates, total - added.Length, added.Length); return dates;
    }
    static bool Date(int date) => date == 0 || DateTime.TryParseExact(date.ToString(CultureInfo.InvariantCulture), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    static bool Text(string s) => !string.IsNullOrWhiteSpace(s);
    static void Unique(Report r, IEnumerable<string> values, string name)
    {
        var list = values.ToArray();
        Need(r, list.All(Text) && list.Distinct(StringComparer.Ordinal).Count() == list.Length, name + ": nonempty unique question keys");
    }
    static void Pool(Report r, string name, int count, int minimum, int[] since = null, int opening = 20160601)
    {
        Need(r, count >= minimum, name + ": expanded total at least " + minimum);
        int open = since == null ? count : since.Count(d => d == 0 || d <= opening);
        Need(r, since == null || since.Length == count && since.All(Date), name + ": parallel dates are valid Gregorian dates");
        Need(r, open >= 20, name + ": at least 20 eligible on " + opening);
        r.pools.Add(name + ": total=" + count + ", open=" + open + ", date=" + opening);
    }
    static string Output()
    {
        var args = Environment.GetCommandLineArgs(); string requested = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--verification-save-dir" && i + 1 < args.Length) requested = args[++i];
            else if (args[i].StartsWith("--verification-save-dir=", StringComparison.Ordinal)) requested = args[i].Substring("--verification-save-dir=".Length);
        }
        if (!Text(requested)) throw new ArgumentException("Supply external --verification-save-dir.");
        string root = Path.GetFullPath(requested), project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (root.Equals(project, StringComparison.OrdinalIgnoreCase) || root.StartsWith(project + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Output must be outside project.");
        return Path.Combine(root, "QuestionExpansion-" + Guid.NewGuid().ToString("N"));
    }
    [MenuItem("Tools/Local Verification/Question expansion")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Run in EditMode.");
        string output = Output(); Directory.CreateDirectory(output); var r = new Report();
        try
        {
            var bank = typeof(XgSim).Assembly.GetType("LingGuangV05.XingGuang.XgQuestionBank", true);
            RuntimeHelpers.RunClassConstructor(bank.TypeHandle);
            foreach (var name in new[] { "XgSentenceLesson", "XgOddPack", "XgTranslationLesson", "XgBetterAnswerLesson", "XgPackEnglish", "XgUndercoverGame", "XgPackPoem", "XgIdiomPack" })
            { RuntimeHelpers.RunClassConstructor(Mini(name).TypeHandle); r.checks.Add(name + ": static constructor initialized"); }
            CheckMiniBanks(r); CheckPublicBanks(r, bank); CheckPicker(r);
            r.status = "passed";
        }
        catch (Exception ex) { r.status = "failed"; r.error = ex.ToString(); throw; }
        finally { File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(r, true)); Debug.Log("QUESTION_EXPANSION " + r.status + " " + output); }
    }
    static void CheckMiniBanks(Report r)
    {
        var sentence = Mini("XgSentenceLesson"); var zh = Value<string[][]>(sentence, "Zh"); var en = Value<string[][]>(sentence, "En"); var bad = Value<int[]>(sentence, "Bad");
        var fix = Value<string[]>(sentence, "Fix"); var fixEn = Value<string[]>(sentence, "FixEn");
        Pool(r, "Sentence", zh.Length, 100, Value<int[]>(sentence, "Since"), 20160901);
        Need(r, new[] { en.Length, bad.Length, fix.Length, fixEn.Length }.All(n => n == zh.Length), "Sentence parallel array lengths");
        for (int i = 0; i < zh.Length; i++) Need(r, zh[i].All(Text) && en[i].All(Text) && bad[i] >= -1 && bad[i] < Math.Min(zh[i].Length, en[i].Length) && Text(fix[i]) && Text(fixEn[i]), "Sentence bilingual correction index " + i);
        Unique(r, zh.Select(row => string.Concat(row)), "Sentence");
        var odd = Mini("XgOddPack"); zh = Value<string[][]>(odd, "Zh"); en = Value<string[][]>(odd, "En");
        Pool(r, "Odd", zh.Length, 100, Value<int[]>(odd, "Since"), 20160701);
        Need(r, en.Length == zh.Length && zh.All(row => row.Length == 4 && row.All(Text)) && en.All(row => row.Length == 4 && row.All(Text)), "Odd bilingual four-option arrays");
        Unique(r, zh.Select(row => string.Join("|", row)), "Odd");
        var translation = Mini("XgTranslationLesson"); var sources = Value<string[]>(translation, "Sources"); var candidates = Value<string[]>(translation, "Candidates");
        var grades = Value<int[]>(translation, "Grades"); var wrong = Value<int[]>(translation, "WrongWords");
        Pool(r, "Translation", sources.Length, 100, opening: 20161001); Unique(r, sources, "Translation");
        Need(r, candidates.Length == sources.Length && grades.Length == sources.Length && wrong.Length == sources.Length, "Translation parallel arrays");
        for (int i = 0; i < sources.Length; i++) Need(r, Text(candidates[i]) && grades[i] >= 0 && grades[i] <= 2 && (grades[i] == 1 ? wrong[i] >= 0 && wrong[i] < candidates[i].Split(' ').Length : wrong[i] == -1), "Translation grading/word index " + i);
        var compare = Mini("XgBetterAnswerLesson"); var questions = Value<string[]>(compare, "Q"); Pool(r, "Compare", questions.Length, 100, CompareDates(questions.Length), 20161101); Unique(r, questions, "Compare");
        foreach (var field in new[] { "QEn", "Good", "GoodEn", "Bad", "BadEn" }) { var values = Value<string[]>(compare, field); Need(r, values.Length == questions.Length && values.All(Text), "Compare bilingual field " + field); }
        var english = Mini("XgPackEnglish"); zh = Value<string[][]>(english, "Zh"); var spans = Value<string[][]>(english, "Spans"); var errors = Value<int[][]>(english, "ErrorSpans");
        Pool(r, "Chinglish", zh.Length, 100, opening: 20161001); Unique(r, zh.Select(row => row[0]), "Chinglish");
        Need(r, spans.Length == zh.Length && errors.Length == zh.Length, "Chinglish span arrays");
        for (int i = 0; i < zh.Length; i++) Need(r, zh[i].Length == 6 && zh[i].All(Text) && spans[i].All(Text) && errors[i].Length > 0 && errors[i].All(j => j >= 0 && j < spans[i].Length), "Chinglish answer and span indices " + i);
        foreach (var field in new[] { "ContextEn", "ReasonZh", "ReasonEn" }) { var values = Value<string[]>(english, field); Need(r, values.Length == zh.Length && values.All(Text), "Chinglish context field " + field); }
        var spy = Mini("XgUndercoverGame"); zh = Value<string[][]>(spy, "Pairs"); en = Value<string[][]>(spy, "PairsEn"); Pool(r, "Undercover", zh.Length, 100, opening: 20160901);
        Need(r, en.Length == zh.Length && zh.All(row => row.Length == 7 && row.All(Text)) && en.All(row => row.Length == 7 && row.All(Text)), "Undercover bilingual seven-field arrays");
        Unique(r, zh.Select(row => row[0] + "|" + row[1]), "Undercover");
        foreach (var field in new[] { "Five", "Seven" })
        { var lines = Value<string[]>(Mini("XgPackPoem"), field); Pool(r, field, lines.Length, 100, opening: 20160801); Unique(r, lines, field); Need(r, lines.All(line => line.Length == (field == "Five" ? 5 : 7)), field + ": correct character lengths"); }
        var idioms = Value<string[]>(Mini("XgIdiomPack"), "Words"); Pool(r, "Idioms", idioms.Length, 100, opening: 20160801); Unique(r, idioms, "Idioms");
        Need(r, idioms.All(w => w.Length == 4 && idioms.Any(other => other != w && other[0] == w[3])), "Every idiom has a distinct legal successor");
    }
    static void CheckPublicBanks(Report r, Type bank)
    {
        var lines = XgTeachingDanmu.Lines; Pool(r, "Danmu", lines.Length, 100, lines.Select(x => x.since).ToArray(), 20160701); Unique(r, lines.Select(x => x.text), "Danmu");
        Need(r, lines.All(x => Text(x.textEn) && x.tone >= 0 && x.tone <= 2), "Danmu bilingual text and tone labels");
        foreach (int day in new[] { 20160101, 20160701, 20160808, 20161009, 20161010, 20161202 })
        {
            var random = new System.Random(711 + day);
            for (int i = 0; i < 300; i++) { var picked = XgTeachingDanmu.Pick(day, random); if (picked == null || picked.since > day) throw new InvalidOperationException("Danmu future question at " + day); }
            r.checks.Add("Danmu 300 dated draws at " + day);
        }
        var quotes = XgTeachingPoetry.Quotes; Pool(r, "Poem quotes", quotes.Length, 100, opening: 20160801); Unique(r, quotes.Select(q => q.line), "Poem quotes");
        Need(r, quotes.All(q => Text(q.lineEn) && Text(q.source) && Text(q.sourceEn) && Text(q.originalLineEn) && q.original == (q.line == q.originalLine)), "Poem truth labels and bilingual attribution");
        foreach (string desk in new[] { "spam", "headline", "review" })
        {
            int opening = desk == "spam" ? 20160601 : desk == "headline" ? 20160701 : 20160901;
            var phrases = XgMemes.Bank(desk); Pool(r, desk, phrases.Length, 100, phrases.Select(p => p.since).ToArray(), opening); Unique(r, phrases.Select(p => p.text), desk);
            var additions = Value<XgPhrase[]>(bank, desk == "spam" ? "Spam" : desk == "headline" ? "Headline" : "Review");
            Need(r, additions.All(p => Text(p.textEn) && Text(p.whyEn) && Text(p.why) && p.level >= 1 && p.level <= 3), desk + ": added questions and explanations are bilingual");
            foreach (int day in new[] { opening, 20161231 })
            {
                var random = new System.Random(day); var seen = new HashSet<string>();
                for (int i = 0; i < 3000; i++) { var p = XgMemes.Pick(desk, random, 3, day, "", false); if (p == null || p.since > day || p.level > 3 || p.family) throw new InvalidOperationException(desk + " invalid draw at " + day); seen.Add(p.text); }
                Need(r, seen.Count >= 20, desk + ": sampler reaches 20 unique legal questions at " + day);
            }
        }
    }
    static void CheckPicker(Report r)
    {
        var sim = new XgSim(); sim.Today = 20160701;
        var fixture = new GameObject("Inactive question sampler fixture", typeof(RectTransform));
        fixture.SetActive(false);
        XgMiniContext context = null; XgMiniGame instance = null;
        try
        {
        // XgSocialSurface's constructor reads Ui.dark even when no question is rendered.
        // A real inactive root and XgUi satisfy that contract; no Build, runtime, or scene bootstrap is invoked.
        var ui = new XgUi(null, null, false);
        context = new XgMiniContext((RectTransform)fixture.transform, ui, sim, null, "odd_one", () => false, () => .5, _ => { }, (_, __) => { });
        instance = (XgMiniGame)Activator.CreateInstance(Mini("XgOddPack"), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { context }, null);
        var method = Mini("XgOddPack").BaseType.GetMethod("PickFresh", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null) throw new MissingMethodException("PickFresh");
        Func<int[], List<int>, int> pick = (dates, recent) => (int)method.Invoke(instance, new object[] { dates, recent });
        var history = new List<int>(); int[] since = { 0, 20160701, 20160801, 0, 0, 0 };
        for (int i = 0; i < 200; i++)
        { var before = history.ToArray(); int chosen = pick(since, history); if (chosen < 0 || chosen >= since.Length || since[chosen] > sim.Today || before.Contains(chosen) || history.Count > 2) throw new InvalidOperationException("Fresh picker date/history boundary"); }
        r.checks.Add("Fresh picker 200 draws respect date, recent exclusion and bounded history");
        Need(r, pick(new[] { 0 }, new List<int> { 0 }) == 0, "Single eligible item falls back without crashing");
        Need(r, pick(new[] { 0, 0 }, new List<int> { 0, 1 }) >= 0, "Fully exhausted recent window falls back to eligible pool");
        int[] compareDates = CompareDates(Value<string[]>(Mini("XgBetterAnswerLesson"), "Q").Length);
        foreach (int day in new[] { 20160601, 20160901, 20161101, 20161231 }.Concat(compareDates.Where(d => d > 0)).Distinct())
        {
            sim.Today = day; var recent = new List<int>();
            int eligible = compareDates.Count(d => d == 0 || d <= day);
            Need(r, eligible > 0, "Combined compare pool has candidates at " + day);
            for (int i = 0; i < 300; i++)
            {
                var previous = recent.ToArray(); int chosen = pick(compareDates, recent);
                if (chosen < 0 || chosen >= compareDates.Length || compareDates[chosen] > day || (eligible > previous.Distinct().Count() && previous.Contains(chosen)))
                    throw new InvalidOperationException("Combined compare PickFresh date/history violation at " + day);
            }
            r.checks.Add("Actual PickFresh: 300 combined compare draws respect date/history at " + day);
        }
        sim.Today = 20160701;
        foreach (var dates in new[] { new int[0], new[] { 20170101 } })
        {
            try { int result = pick(dates, new List<int>()); r.risks.Add("No-eligible PickFresh returned " + result + "; callers must handle this sentinel."); }
            catch (TargetInvocationException ex) { r.risks.Add("No-eligible PickFresh throws " + ex.InnerException.GetType().Name + " (empty/future-only synthetic pool); shipped opening pools validated separately."); }
        }
        }
        finally
        {
            instance?.Dispose();
            context?.Close();
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }
}
#endif
