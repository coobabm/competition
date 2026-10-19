using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    // A test owns its resource-catalog change and restores the prior semantic catalog.
    sealed class XgYyCatalogTestScope : IDisposable
    {
        [Serializable] sealed class Personas { public XgPersonaPack[] packs; public XgPersonaTag[] tags; }
        [Serializable] sealed class Unlocks { public XgChatUnlock[] unlocks; }
        readonly string personas, unlocks;
        public XgYyCatalogTestScope()
        {
            personas = JsonUtility.ToJson(new Personas { packs = XgSim.PersonaPacks.ToArray(), tags = XgSim.PersonaTags.ToArray() });
            unlocks = JsonUtility.ToJson(new Unlocks { unlocks = XgSim.ChatUnlocks.ToArray() });
            XgSim.LoadYyLearning(Resources.Load<TextAsset>("LingGuangV05/YY/persona_packs").text,
                Resources.Load<TextAsset>("LingGuangV05/YY/chat_unlocks").text);
        }
        public void Dispose() { XgSim.LoadYyLearning(personas, unlocks); }

        // Model a successfully delivered authored YY reply through the same public callback as the UI.
        public static void DeliverUnlock(XgSim sim, string node)
        {
            var gate = XgSim.ChatUnlocks.FirstOrDefault(g => g.node == node);
            if (gate == null || sim.ChatNodeReady(node)) return;
            Assert.That(sim.S.stage, Is.GreaterThanOrEqualTo(gate.stage), node);
            Assert.That(gate.answer, Is.Not.Empty, node);
            Assert.That(sim.RegisterYyReply(gate.phrase, gate.answer, true), Is.GreaterThan(0), node);
            Assert.IsTrue(sim.ChatNodeReady(node), node);
        }
    }
}
