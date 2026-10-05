using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using LingGuangV05.XingGuang;

// Link the game source, not a second implementation of its questions or answers.
static class Export
{
    static readonly string[] Categories = {
        "算术", "数的性质", "比较", "数列", "条件推理", "三段论", "真假运算", "日历", "真话假话"
    };
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    static int Main(string[] args)
    {
        if (args.Length != 1) { Console.Error.WriteLine("Usage: Export <new-output.jsonl>"); return 2; }
        var lines = new List<string>();
        var counts = new Dictionary<string, int>();
        foreach (var c in Categories) { counts[c + "/True"] = 0; counts[c + "/False"] = 0; }
        var seen = new HashSet<string>();
        var random = new Random(20261003);
        int accepted = 0;
        for (int attempt = 0; attempt < 200000 && accepted < 180; attempt++)
        {
            int level = 1 + attempt % 5, seed = random.Next();
            var q = XgLogic.Generate(seed, level);
            string key = q.category + "/" + q.truth;
            if (counts[key] >= 10 || !seen.Add(q.text)) continue;
            counts[key]++;
            lines.Add(Row("balanced", accepted++, seed, q));
        }
        if (accepted != 180) throw new InvalidOperationException("Could not fill unique balanced buckets.");
        random = new Random(20261004);
        for (int index = 0; index < 500; index++)
        {
            int seed = random.Next();
            lines.Add(Row("natural", index, seed, XgLogic.Generate(seed, 1 + index % 5)));
        }
        // Refuse to overwrite another run's corpus. Build fully before creating the output.
        using (var stream = new FileStream(args[0], FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            foreach (var line in lines) writer.WriteLine(line);
        Console.WriteLine("Exported 180 unique balanced + 500 natural draws: " + Path.GetFullPath(args[0]));
        return 0;
    }

    static string Row(string suite, int index, int seed, XgLogicQuestion q) => JsonSerializer.Serialize(new {
        id = suite + "-" + index.ToString("D4"), suite, seed, q.level, q.category,
        category_en = q.categoryEn, question = q.text, question_en = q.textEn,
        q.truth, q.why, why_en = q.whyEn
    }, Json);
}
