using System;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Forum;

namespace LingGuangV05.Desktop.Tieba
{
    // E2 patch-3 (delayed-floor state key). Pure functions of the forum library and context, public for the
    // standalone editor check TiebaDelayedFloorKeyChecks. No caching: the library is scanned on every call.
    public sealed partial class TiebaView
    {
        /// <summary>
        /// One character per floor with a positive afterSeconds, in library order ('1' once that delay has passed).
        /// The test is the afterSeconds clause of ForumLibrary.View: hidden while !helpPosted || secondsSinceHelp &lt; afterSeconds.
        /// forum.json currently has exactly three such floors (help_bios: 40, 90, 150).
        /// </summary>
        public static string DelayedFloorKey(ForumLibrary library, ForumContext c)
        {
            if (library == null || c == null) return "";
            var sb = new StringBuilder(8);
            foreach (var t in library.Threads)
                foreach (var f in t.floors)
                    if (f.afterSeconds > 0) sb.Append(c.helpPosted && c.secondsSinceHelp >= f.afterSeconds ? '1' : '0');
            return sb.ToString();
        }

        /// <summary>
        /// One character per distinct thread/floor "requires" flag, in ordinal order ('1' while c.flag says it is set).
        /// Covers qc.trap, which raises no hub.Changed; asked.*/answered.* are also in DataRequestKey.
        /// </summary>
        public static string FlagGateKey(ForumLibrary library, ForumContext c)
        {
            if (library == null || c == null) return "";
            var flags = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in library.Threads)
            {
                if (!string.IsNullOrEmpty(t.requires)) flags.Add(t.requires);
                foreach (var f in t.floors) if (!string.IsNullOrEmpty(f.requires)) flags.Add(f.requires);
            }
            var sb = new StringBuilder(flags.Count);
            foreach (var id in flags) sb.Append(c.flag != null && c.flag(id) ? '1' : '0');
            return sb.ToString();
        }
    }
}
