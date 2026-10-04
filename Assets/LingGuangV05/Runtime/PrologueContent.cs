using System;
using LingGuangV05.Core;
using UnityEngine;

namespace LingGuangV05.Runtime
{
    /// <summary>The prologue's lines (Resources/LingGuangV05/Prologue/prologue_lines.json) and voice clip, loaded once.</summary>
    public static class PrologueContent
    {
        public const string LinesPath = "LingGuangV05/Prologue/prologue_lines";
        static PrologueLines lines;

        public static PrologueLines Lines
        {
            get
            {
                if (lines != null) return lines;
                var asset = Resources.Load<TextAsset>(LinesPath);
                try { lines = PrologueLines.Parse(asset != null ? asset.text : "{\"lines\":{}}"); }
                catch (FormatException error) { Debug.LogError("[Prologue] " + error.Message); lines = PrologueLines.Parse("{\"lines\":{}}"); }
                return lines;
            }
        }

        /// <summary>A prologue line in the current language.</summary>
        public static string L(string key) => Lines.Get(key, GameText.IsEnglish);

        /// <summary>The recorded "do you want to shut down?" (placeholder: macOS system voice, replace before release).</summary>
        public static AudioClip ShutdownVoice() => Resources.Load<AudioClip>(GameText.IsEnglish ? "LingGuangV05/Prologue/shutdown_voice_en" : "LingGuangV05/Prologue/shutdown_voice_zh");
    }
}
