using System;
using System.Collections.Generic;
using System.Globalization;

namespace LingGuangV05.Core.Story
{
    /// <summary>One instruction inside a beat. Arguments stay as parsed JSON values.</summary>
    public sealed class StoryStep
    {
        private readonly Dictionary<string, object> args;
        public StoryStep(string op, Dictionary<string, object> arguments)
        {
            Op = op;
            args = arguments ?? new Dictionary<string, object>(StringComparer.Ordinal);
        }
        public string Op { get; private set; }
        public IEnumerable<string> ArgNames { get { return args.Keys; } }
        public bool Has(string key) { return args.ContainsKey(key); }

        public string Str(string key, string fallback = null)
        {
            object v;
            if (!args.TryGetValue(key, out v) || v == null) return fallback;
            if (v is string) return (string)v;
            if (v is double) return ((double)v).ToString(CultureInfo.InvariantCulture);
            if (v is bool) return (bool)v ? "1" : "0";
            return fallback;
        }

        public double Num(string key, double fallback = 0)
        {
            object v;
            if (!args.TryGetValue(key, out v) || v == null) return fallback;
            if (v is double) return (double)v;
            if (v is bool) return (bool)v ? 1 : 0;
            double parsed;
            if (v is string && double.TryParse((string)v, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)) return parsed;
            return fallback;
        }
    }

    /// <summary>
    /// A storylet: trigger + prerequisites + content (steps) + effects (steps too).
    /// Beats in the same group compete: the best match plays (priority, then specificity, then least played).
    /// Ungrouped beats all play when they match.
    /// </summary>
    public sealed class StoryBeat
    {
        public string Id;
        public string[] On = new string[0];
        public StoryCondition[] When = new StoryCondition[0];
        public bool Once = true;
        public int Priority;
        public string Group;
        public double Cooldown;
        public double GroupCooldown;
        public StoryStep[] Steps = new StoryStep[0];
        public int Order;
        public string Source;
        public int Specificity { get { return When.Length; } }
        public override string ToString() { return Id; }
    }

    /// <summary>All beats and lines, merged from any number of JSON files.</summary>
    public sealed class StoryLibrary
    {
        private readonly List<StoryBeat> beats = new List<StoryBeat>();
        private readonly Dictionary<string, StoryBeat> byId = new Dictionary<string, StoryBeat>(StringComparer.Ordinal);
        private readonly Dictionary<string, string[]> lines = new Dictionary<string, string[]>(StringComparer.Ordinal);

        public IReadOnlyList<StoryBeat> Beats { get { return beats; } }
        public IEnumerable<string> LineKeys { get { return lines.Keys; } }

        public static StoryLibrary FromJson(params string[] jsons)
        {
            var lib = new StoryLibrary();
            for (int i = 0; i < jsons.Length; i++) lib.Add(jsons[i], "file" + i);
            return lib;
        }

        public StoryBeat Get(string id)
        {
            StoryBeat beat;
            return id != null && byId.TryGetValue(id, out beat) ? beat : null;
        }

        public bool HasLine(string key) { return key != null && lines.ContainsKey(key); }

        /// <summary>Variants rotate by <paramref name="variant"/> so repeated barks do not repeat text.</summary>
        public bool TryGetLine(string key, int variant, out string text)
        {
            string[] values;
            if (key == null || !lines.TryGetValue(key, out values) || values.Length == 0) { text = null; return false; }
            text = values[((variant % values.Length) + values.Length) % values.Length];
            return true;
        }

        public void Add(string json, string sourceName)
        {
            object root;
            try { root = StoryJson.Parse(json); }
            catch (StoryFormatException e) { throw new StoryFormatException(sourceName + ": " + e.Message); }
            var obj = root as Dictionary<string, object>;
            if (obj == null) throw new StoryFormatException(sourceName + ": root must be an object.");
            foreach (var key in obj.Keys)
                if (key != "beats" && key != "lines" && key != "comment")
                    throw new StoryFormatException(sourceName + ": unknown top-level key \"" + key + "\".");
            object value;
            if (obj.TryGetValue("lines", out value)) AddLines(value, sourceName);
            if (obj.TryGetValue("beats", out value))
            {
                var list = value as List<object>;
                if (list == null) throw new StoryFormatException(sourceName + ": \"beats\" must be an array.");
                for (int i = 0; i < list.Count; i++) AddBeat(list[i] as Dictionary<string, object>, sourceName, i);
            }
        }

        private void AddLines(object value, string source)
        {
            var map = value as Dictionary<string, object>;
            if (map == null) throw new StoryFormatException(source + ": \"lines\" must be an object of key -> text.");
            foreach (var pair in map)
            {
                if (lines.ContainsKey(pair.Key)) throw new StoryFormatException(source + ": duplicate line key \"" + pair.Key + "\".");
                if (pair.Value is string) { lines[pair.Key] = new[] { (string)pair.Value }; continue; }
                var list = pair.Value as List<object>;
                if (list == null || list.Count == 0) throw new StoryFormatException(source + ": line \"" + pair.Key + "\" must be text or a non-empty array of text.");
                var texts = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    texts[i] = list[i] as string;
                    if (texts[i] == null) throw new StoryFormatException(source + ": line \"" + pair.Key + "\" has a non-text variant.");
                }
                lines[pair.Key] = texts;
            }
        }

        private static readonly HashSet<string> BeatKeys = new HashSet<string>(StringComparer.Ordinal)
        { "id", "on", "when", "once", "priority", "group", "cooldown", "groupCooldown", "steps", "comment" };

        private void AddBeat(Dictionary<string, object> obj, string source, int index)
        {
            string where = source + " beat #" + index;
            if (obj == null) throw new StoryFormatException(where + ": must be an object.");
            foreach (var key in obj.Keys)
                if (!BeatKeys.Contains(key)) throw new StoryFormatException(where + ": unknown key \"" + key + "\".");
            var beat = new StoryBeat { Source = source, Order = beats.Count };
            beat.Id = obj.ContainsKey("id") ? obj["id"] as string : null;
            if (string.IsNullOrEmpty(beat.Id)) throw new StoryFormatException(where + ": missing \"id\".");
            where = source + " beat \"" + beat.Id + "\"";
            if (byId.ContainsKey(beat.Id)) throw new StoryFormatException(where + ": duplicate id.");

            object v;
            if (!obj.TryGetValue("on", out v)) throw new StoryFormatException(where + ": missing \"on\".");
            beat.On = Strings(v, where, "on");
            if (beat.On.Length == 0) throw new StoryFormatException(where + ": \"on\" is empty.");
            if (obj.TryGetValue("when", out v))
            {
                string[] conds = Strings(v, where, "when");
                beat.When = new StoryCondition[conds.Length];
                for (int i = 0; i < conds.Length; i++)
                {
                    try { beat.When[i] = StoryCondition.Parse(conds[i]); }
                    catch (StoryFormatException e) { throw new StoryFormatException(where + ": " + e.Message); }
                }
            }
            if (obj.TryGetValue("once", out v)) { if (!(v is bool)) throw new StoryFormatException(where + ": \"once\" must be true/false."); beat.Once = (bool)v; }
            beat.Priority = (int)Number(obj, "priority", 0, where);
            beat.Cooldown = Number(obj, "cooldown", 0, where);
            beat.GroupCooldown = Number(obj, "groupCooldown", 0, where);
            if (obj.TryGetValue("group", out v)) { beat.Group = v as string; if (string.IsNullOrEmpty(beat.Group)) throw new StoryFormatException(where + ": \"group\" must be text."); }

            if (!obj.TryGetValue("steps", out v) || !(v is List<object>)) throw new StoryFormatException(where + ": \"steps\" must be an array.");
            var steps = (List<object>)v;
            beat.Steps = new StoryStep[steps.Count];
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i] as Dictionary<string, object>;
                object op;
                if (s == null || !s.TryGetValue("op", out op) || !(op is string)) throw new StoryFormatException(where + " step #" + i + ": needs \"op\".");
                var args = new Dictionary<string, object>(s, StringComparer.Ordinal);
                args.Remove("op");
                beat.Steps[i] = new StoryStep((string)op, args);
            }
            beats.Add(beat);
            byId[beat.Id] = beat;
        }

        private static double Number(Dictionary<string, object> obj, string key, double fallback, string where)
        {
            object v;
            if (!obj.TryGetValue(key, out v)) return fallback;
            if (!(v is double)) throw new StoryFormatException(where + ": \"" + key + "\" must be a number.");
            return (double)v;
        }

        private static string[] Strings(object v, string where, string key)
        {
            if (v is string) return new[] { (string)v };
            var list = v as List<object>;
            if (list == null) throw new StoryFormatException(where + ": \"" + key + "\" must be text or an array of text.");
            var result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                result[i] = list[i] as string;
                if (result[i] == null) throw new StoryFormatException(where + ": \"" + key + "\" must contain only text.");
            }
            return result;
        }
    }
}
