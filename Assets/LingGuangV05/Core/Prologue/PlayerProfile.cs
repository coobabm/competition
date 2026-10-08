using System;

namespace LingGuangV05.Core
{
    /// <summary>
    /// What the game remembers about the player outside any one save: that the opening story has been finished once,
    /// and the setup they chose in it (the AI's name, how it calls itself and the player, the personality sentence and
    /// its axes). A new game that finds a valid profile skips the opening and starts right after it, with this setup.
    /// Stored next to the save as <c>player-profile.json</c> by the runtime; this class is only the data and its rules.
    /// </summary>
    [Serializable]
    public sealed class PlayerProfile
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        /// <summary>The opening was played to the end of its setup (or an old save proves it was).</summary>
        public bool openingFinished;
        public PrologueProfile setup = new PrologueProfile();
        /// <summary>Where it was learned: "setup" (the player just finished the opening) or "save" (read from an older save).</summary>
        public string source = "";

        /// <summary>True if a new game may skip the opening with this profile.</summary>
        public static bool IsUsable(PlayerProfile profile)
            => profile != null && profile.version == CurrentVersion && profile.openingFinished && PrologueProfile.Problem(profile.setup) == null;

        /// <summary>The profile of a player who has just finished the opening setup. Null if the setup is not acceptable.</summary>
        public static PlayerProfile FromSetup(PrologueProfile setup, string source = "setup")
        {
            if (PrologueProfile.Problem(setup) != null) return null;
            return new PlayerProfile { openingFinished = true, setup = Trimmed(setup), source = source ?? "" };
        }

        /// <summary>
        /// The profile an older save proves: a save whose prologue is done and whose setup fields are valid.
        /// Null for a save still in the prologue, from before the prologue existed (no setup was ever chosen), or without a usable setup.
        /// </summary>
        public static PlayerProfile FromState(GameState state)
        {
            if (state == null || state.prologue != 2) return null;
            var setup = new PrologueProfile
            {
                name = state.aiName ?? "", self = state.aiSelf ?? "", callMe = state.aiCallMe ?? "", personality = state.personality ?? "",
                warmth = state.targetWarmth, play = state.targetPlay, opinion = state.targetOpinion,
                words = state.toneWords == null ? new System.Collections.Generic.List<string>() : new System.Collections.Generic.List<string>(state.toneWords),
            };
            return FromSetup(setup, "save");
        }

        /// <summary>A copy of the setup, safe to hand to <c>CompletePrologue</c> (which keeps what it is given).</summary>
        public PrologueProfile CopyOfSetup() => Trimmed(setup);

        static PrologueProfile Trimmed(PrologueProfile p) => new PrologueProfile
        {
            name = (p.name ?? "").Trim(), self = (p.self ?? "").Trim(), callMe = (p.callMe ?? "").Trim(), personality = (p.personality ?? "").Trim(),
            warmth = p.warmth, play = p.play, opinion = p.opinion,
            words = p.words == null ? new System.Collections.Generic.List<string>() : new System.Collections.Generic.List<string>(p.words),
        };
    }
}
