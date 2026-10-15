using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using LingGuangV05.XingGuang;
using LingGuangV05.Desktop.XingGuang;

class Program
{
    static int checks;
    static readonly JsonSerializerOptions Json = new JsonSerializerOptions { IncludeFields = true, IgnoreReadOnlyProperties = true };
    static readonly string[] Qualitative = { "position", "caption", "multihead", "layernorm", "residual", "label.trapmemory" };
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); checks++; }
    static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    static int SentenceCount(string text) => Regex.Split(text, @"(?<=[。！？])|(?<=[.!?])\s*(?=[A-Z])").Count(s => !string.IsNullOrWhiteSpace(s));
    static void Inspect(XgTechEntry entry, XgSim sim)
    {
        string before = JsonSerializer.Serialize(sim.S, Json);
        string name = XgTechCardCopy.Name(entry, sim), summary = XgTechCardCopy.Summary(entry, sim), effect = XgTechCardCopy.Effect(entry, sim), science = XgTechCardCopy.Science(entry, sim);
        string label = entry.id + (sim.English ? " EN" : " ZH");
        Check(!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(summary) && !string.IsNullOrWhiteSpace(effect) && !string.IsNullOrWhiteSpace(science), label + " has all copy layers");
        Check(sim.English || summary.Length <= 12, label + " summary <=12 characters");
        if (XgTechCardCopy.Hidden(entry, sim))
        {
            Check(name == (XgSim.IsAtlas(entry.First) ? sim.English ? "???" : "？？？" : sim.NodeName(entry.First)), label + " reveals no name");
            Check(effect == (sim.English ? "Effect not discovered yet" : "效果尚未发现"), label + " reveals no effect");
            Check(!Regex.IsMatch(name + effect, "[0-9]"), label + " reveals no year or multiplier");
        }
        else
        {
            int count = SentenceCount(science);
            Check(count >= 3 && count <= 5, label + " science needs 3-5 sentences, was " + count + ": " + science);
            Check(Regex.IsMatch(effect, "[0-9]") || Qualitative.Contains(entry.id) || entry.First.kind == XgNodeKind.Phenomenon, label + " needs an actual numeric effect or a documented nonnumeric rule");
            var node = XgTechTree.Next(entry, sim) ?? entry.members.Last();
            foreach (var discount in XgSim.DiscountsOf(node.id))
            {
                if (discount.paramsFactor < 1) Check(effect.Contains("×" + N(discount.paramsFactor)), label + " uses actual parameter factor");
                if (discount.samplesFactor < 1) Check(effect.Contains("×" + N(discount.samplesFactor)), label + " uses actual sample factor");
            }
            if (node.kind == XgNodeKind.Arch)
            {
                var arch = XgCatalog.Arch(node.target);
                Check(arch.year <= 0 || name.EndsWith(XgCatalog.ShownYear(arch.year)), label + " year comes from archive");
            }
            else Check(!Regex.IsMatch(name, @"\b(18|19|20)\d{2}\b"), label + " no invented year");
            if (node.kind == XgNodeKind.Depth) Check(effect.EndsWith(node.value.ToString()), label + " current depth level");
            if (node.kind == XgNodeKind.Width) Check(effect.EndsWith(XgCatalog.Widths[node.value].ToString()), label + " current width level");
            if (node.kind == XgNodeKind.Auto) Check(effect.Contains(N(XgSim.AutoInterval(node.value))), label + " current automation interval");
        }
        Check(JsonSerializer.Serialize(sim.S, Json) == before, label + " display changes no simulation fields");
    }

    static int Main()
    {
        try
        {
            foreach (bool english in new[] { false, true })
            {
                var sim = new XgSim { English = english }; sim.S.stage = 6;
                foreach (var entry in XgTechTree.Entries) if (entry.First.kind != XgNodeKind.Ability) Inspect(entry, sim);
                sim.S.autoLabelRevealed = true;
                foreach (var phenomenon in XgPhenomena.All) sim.S.phenomena.seen.Add(phenomenon.id);
                foreach (var entry in XgTechTree.Entries)
                {
                    if (entry.First.kind == XgNodeKind.Ability) continue;
                    Inspect(entry, sim);
                    if (!entry.IsChain) continue;
                    foreach (var node in entry.members) { if (!sim.S.unlocked.Contains(node.id)) sim.S.unlocked.Add(node.id); Inspect(entry, sim); }
                }
                foreach (int level in new[] { 0, 2, XgCatalog.RaiseMax })
                {
                    sim.S.payRaise = level;
                    var entry = XgTechTree.EntryOf("label.raise"); Inspect(entry, sim);
                    Check(XgTechCardCopy.Effect(entry, sim).Contains("×" + N(XgCatalog.RaiseMultipliers[level])), "Pay card uses actual current level");
                }
                var lstm = XgTechTree.EntryOf("lstm");
                Check(!XgTechCardCopy.Effect(lstm, sim).Contains("80") && !XgTechCardCopy.Science(lstm, sim).Contains("8字"), "No invented LSTM word-count cutoff");
            }
            Console.WriteLine("PASS " + checks + " assertions; all cards, both languages, mystery/atlas states and chain levels");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
