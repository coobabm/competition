using System;
using LingGuangV05.Core;
using LingGuangV05.Core.Era;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    /// <summary>The 2016 event table (Resources/LingGuangV05/Era/era_events.json), loaded once.</summary>
    public static class EraContent
    {
        public const string ResourcePath = "LingGuangV05/Era/era_events";
        static EraEvents events;

        public static EraEvents Events
        {
            get
            {
                if (events != null) return events;
                var asset = Resources.Load<TextAsset>(ResourcePath);
                try { events = EraEvents.Parse(asset != null ? asset.text : "{\"events\":[]}"); }
                catch (FormatException error)
                {
                    Debug.LogError("[Era] " + error.Message);
                    events = EraEvents.Parse("{\"events\":[]}");
                }
                return events;
            }
        }

        /// <summary>Takes the oldest undelivered push event of a channel for this save and marks it delivered.</summary>
        public static EraEvent TakeDue(GameState save, string channel)
        {
            if (save == null) return null;
            if (save.eraDelivered == null) save.eraDelivered = new System.Collections.Generic.List<string>();
            foreach (var e in Events.Due(channel, GameCalendar.Now(save), save.eraDelivered))
            {
                if (e.Repeats) continue;
                save.eraDelivered.Add(e.id);
                return e;
            }
            return null;
        }

        public static string Who(EraEvent e) => GameText.T(e.who, e.whoEn);
        public static string Text(EraEvent e) => GameText.T(e.text, e.textEn);
        public static string Title(EraEvent e) => GameText.T(e.title, e.titleEn);
    }
}
