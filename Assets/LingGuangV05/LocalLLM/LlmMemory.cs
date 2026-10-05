using System;
using System.Collections.Generic;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.LLM
{
    /// <summary>
    /// Writes the lab AI's memory book after an exchange (stage 5+), mem0-style: one low-priority background request
    /// sees the exchange plus the most similar existing notes and answers ADD / UPDATE / DELETE / NONE under a GBNF
    /// grammar (XgSim.MemoryBook.cs). It is sent only after the visible reply is on screen and only while the model
    /// has no other chat queued or running; otherwise, and without a model, the offline cue rule writes instead.
    /// </summary>
    public static class LlmMemory
    {
        public const int MaxTokens = 120;
        public const float Temperature = .2f;

        /// <summary>The owner said <paramref name="playerLine"/> and it answered <paramref name="aiLine"/>.</summary>
        public static void AfterExchange(XgSim lab, string playerLine, string aiLine)
        {
            if (lab == null || !lab.MemoryOpen || string.IsNullOrWhiteSpace(playerLine)) return;
            var llm = LocalLlm.Instance;
            if (llm == null || !llm.Ready || llm.ChatBusy) { lab.RememberOffline(playerLine); return; }
            var messages = lab.MemoryExtractionMessages(playerLine, aiLine, out var shown);
            var state = lab.S;
            Send(llm, messages, MemorySampling(), reply =>
            {
                // A save switch or reset while it was thinking: the old book is gone, write nothing.
                if (!ReferenceEquals(lab.S, state)) return;
                if (reply == null) { lab.RememberOffline(playerLine); return; }
                lab.RememberFromModel(reply, shown);
            });
        }

        public static XgSampling MemorySampling() => new XgSampling
        {
            grammar = XgSim.MemoryGrammar(GameText.IsEnglish), topP = .9f, topK = 20, presencePenalty = 0, frequencyPenalty = 0, repeatPenalty = 1, repeatLastN = 64,
        };

        /// <summary>
        /// The one place the extractor reaches the model: LocalLlm's background lane, which refuses the request while
        /// a visible reply is waiting or running or the player is typing; then done(null) writes the offline note.
        /// </summary>
        static void Send(LocalLlm llm, List<KeyValuePair<string, string>> messages, XgSampling sampling, Action<string> done)
        {
            if (!llm.ChatBackground(messages, MaxTokens, Temperature, done, sampling)) done(null);
        }
    }
}
