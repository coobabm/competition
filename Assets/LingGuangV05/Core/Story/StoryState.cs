using System;
using System.Collections.Generic;

namespace LingGuangV05.Core.Story
{
    /// <summary>
    /// Durable story memory, saved inside the game save. Plain lists so JsonUtility can serialize it.
    /// This is the "facts written back" half of a rule-database narrative: beats read it and write it.
    /// </summary>
    [Serializable]
    public sealed class StoryState
    {
        /// <summary>Ids of once-only beats that finished playing.</summary>
        public List<string> fired = new List<string>();
        /// <summary>Free-form story facts (numbers). Unknown flags read as 0.</summary>
        public List<StoryFlag> flags = new List<StoryFlag>();
        /// <summary>"The first day of it": first card, first correction, first sentence...</summary>
        public List<StoryFirst> firsts = new List<StoryFirst>();
        /// <summary>Play counts and last play time per beat (and per group as "@group").</summary>
        public List<StoryPlay> plays = new List<StoryPlay>();
        /// <summary>Story clock in seconds; advances with real time times the story time scale.</summary>
        public double clock;
        /// <summary>Story clock time of the last narration line, used for pacing.</summary>
        public double lastNarration = -1e9;

        public void Repair()
        {
            if (fired == null) fired = new List<string>();
            if (flags == null) flags = new List<StoryFlag>();
            if (firsts == null) firsts = new List<StoryFirst>();
            if (plays == null) plays = new List<StoryPlay>();
            if (double.IsNaN(clock) || double.IsInfinity(clock) || clock < 0) clock = 0;
            if (double.IsNaN(lastNarration) || double.IsInfinity(lastNarration)) lastNarration = -1e9;
        }

        public bool HasFired(string id) { return fired.Contains(id); }
        public void MarkFired(string id) { if (!fired.Contains(id)) fired.Add(id); }
        public void Unfire(string id) { fired.Remove(id); }

        public double GetFlag(string key)
        {
            for (int i = 0; i < flags.Count; i++) if (flags[i].key == key) return flags[i].value;
            return 0;
        }

        public void SetFlag(string key, double value)
        {
            for (int i = 0; i < flags.Count; i++)
                if (flags[i].key == key) { flags[i].value = value; return; }
            flags.Add(new StoryFlag { key = key, value = value });
        }

        public StoryFirst GetFirst(string id)
        {
            for (int i = 0; i < firsts.Count; i++) if (firsts[i].id == id) return firsts[i];
            return null;
        }

        /// <summary>Writes only when the record is missing. Returns true when written.</summary>
        public bool RecordFirst(StoryFirst record)
        {
            if (record == null || string.IsNullOrEmpty(record.id) || GetFirst(record.id) != null) return false;
            firsts.Add(record);
            return true;
        }

        public StoryPlay GetPlay(string id)
        {
            for (int i = 0; i < plays.Count; i++) if (plays[i].id == id) return plays[i];
            return null;
        }

        public void NotePlay(string id)
        {
            var play = GetPlay(id);
            if (play == null) { play = new StoryPlay { id = id }; plays.Add(play); }
            play.count++;
            play.last = clock;
        }
    }

    [Serializable] public sealed class StoryFlag { public string key = ""; public double value; }

    [Serializable]
    public sealed class StoryFirst
    {
        public string id = "";
        public int day;
        public double gameSeconds;
        public string text = "";
        public double value;
    }

    [Serializable] public sealed class StoryPlay { public string id = ""; public int count; public double last; }
}
