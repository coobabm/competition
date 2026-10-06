using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public static class XgStoryMilestones
    {
        public static readonly string[] Nodes = { "bt.hidden", "bt.vision", "bt.sequence", "bt.gate", "bt.residual", "bt.attention", "bt.spatial" };
        public static readonly string[] Beats = { "bt_hidden", "bt_specialty_vision", "bt_specialty_sequence", "bt_gate", "bt_residual", "bt_attention", "bt_spatial" };
        /// <summary>The stage each milestone belongs to: its story beat plays once the player has left that stage (design v1.1).</summary>
        static readonly int[] ReachedAt = { 2, 3, 3, 4, 0, 5, 0 };

        public static IEnumerable<string> Pending(XgState state, Func<string, bool> fired)
        {
            for (int i = 0; i < Nodes.Length; i++)
            {
                bool reached = ReachedAt[i] > 0 ? state.stage >= ReachedAt[i] : state.unlocked.Contains(Nodes[i]);
                if (reached && !state.migratedBeats.Contains(Beats[i]) && !fired(Beats[i])) yield return Nodes[i];
            }
        }
        public static IEnumerable<int> MigrationStages(XgState state)
        {
            if (state.migratedBeats.Contains("bt_hidden")) yield return 2;
            if (state.migratedBeats.Contains("bt_specialty_vision") || state.migratedBeats.Contains("bt_specialty_sequence")) yield return 3;
            if (state.migratedBeats.Contains("bt_gate") || state.migratedBeats.Contains("bt_residual")) yield return 4;
            if (state.migratedBeats.Contains("bt_attention") || state.migratedBeats.Contains("bt_spatial")) yield return 5;
        }
        public static bool WallMayOpen(XgState state, string wall, bool messageDelivered)
        { return messageDelivered && state.walls.Contains(wall); }
    }
}
