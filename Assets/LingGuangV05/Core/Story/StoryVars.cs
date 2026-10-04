using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Story
{
    /// <summary>Read access to game facts by name. No reflection: implementations use a hand-written table.</summary>
    public interface IStoryVars
    {
        /// <summary>False means the name is unknown (a content error), not "zero".</summary>
        bool TryGet(string name, out double value);
        /// <summary>Every name TryGet accepts. Used by content lint.</summary>
        IEnumerable<string> KnownNames { get; }
    }

    /// <summary>Optional write access for the "set" step. Implementations whitelist writable names.</summary>
    public interface IStoryVarsWriter
    {
        bool TrySet(string name, double value);
    }

    public sealed class StoryUnknownVariableException : Exception
    {
        public StoryUnknownVariableException(string name) : base("Unknown story variable \"" + name + "\".") { Name = name; }
        public string Name { get; private set; }
    }

    /// <summary>
    /// Combines game facts with story memory:
    /// flag.X (story flags, default 0), fired.X (1 when beat X finished), first.X (1 when recorded),
    /// plays.X (play count of beat X), ext.X (externals injected by the host, default 0).
    /// Everything else goes to the game table and must be known.
    /// </summary>
    public sealed class StoryContext : IStoryVars
    {
        private readonly Dictionary<string, double> externals = new Dictionary<string, double>(StringComparer.Ordinal);
        public StoryContext(IStoryVars game, StoryState state)
        {
            Game = game;
            State = state;
        }
        public IStoryVars Game { get; private set; }
        public StoryState State { get; private set; }

        public void SetExternal(string name, double value) { externals[name] = value; }

        public bool TryGet(string name, out double value)
        {
            value = 0;
            if (string.IsNullOrEmpty(name)) return false;
            if (name.StartsWith("flag.", StringComparison.Ordinal)) { value = State.GetFlag(name.Substring(5)); return true; }
            if (name.StartsWith("fired.", StringComparison.Ordinal)) { value = State.HasFired(name.Substring(6)) ? 1 : 0; return true; }
            if (name.StartsWith("first.", StringComparison.Ordinal)) { value = State.GetFirst(name.Substring(6)) != null ? 1 : 0; return true; }
            if (name.StartsWith("plays.", StringComparison.Ordinal))
            {
                var play = State.GetPlay(name.Substring(6));
                value = play == null ? 0 : play.count;
                return true;
            }
            if (name.StartsWith("ext.", StringComparison.Ordinal)) { externals.TryGetValue(name.Substring(4), out value); return true; }
            if (name == "story.clock") { value = State.clock; return true; }
            return Game != null && Game.TryGet(name, out value);
        }

        public double Get(string name)
        {
            double value;
            if (!TryGet(name, out value)) throw new StoryUnknownVariableException(name);
            return value;
        }

        public IEnumerable<string> KnownNames
        {
            get
            {
                yield return "story.clock";
                if (Game != null) foreach (var n in Game.KnownNames) yield return n;
            }
        }

        public static bool IsDynamicName(string name)
        {
            return name.StartsWith("flag.", StringComparison.Ordinal) || name.StartsWith("fired.", StringComparison.Ordinal) ||
                   name.StartsWith("first.", StringComparison.Ordinal) || name.StartsWith("plays.", StringComparison.Ordinal) ||
                   name.StartsWith("ext.", StringComparison.Ordinal);
        }
    }
}
