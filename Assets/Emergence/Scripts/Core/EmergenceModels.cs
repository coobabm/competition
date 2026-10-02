using System;
using System.Collections.Generic;

namespace Emergence
{
    public enum RunPhase { Grow, Prepare, Shop, Victory, Defeat }
    public enum NodeKind { Normal, Sensitive, Capacitor, Delay, Projector, Core }
    public enum NodeVariant { None, Gold, Resonant, Multiple }
    public enum ContentCategory { Synapse, Memory, Drug, Star }
    public enum EventKind { Fire, Rescore, Signal, Charge, Multiplier, Structure, Complete }

    [Serializable] public sealed class ChargeUnit
    {
        public int sourceId, layer, tick;
        public ChargeUnit Copy() { return (ChargeUnit)MemberwiseClone(); }
    }

    [Serializable] public sealed class NodeState
    {
        public int id, q, r, charge, layer, coreSources;
        public NodeKind kind;
        public NodeVariant variant;
        public bool fired;
        public List<ChargeUnit> contributions = new List<ChargeUnit>();
        public int Threshold { get { return kind == NodeKind.Sensitive ? 1 : kind == NodeKind.Capacitor ? 3 : 2; } }
        public int BaseScore { get { return kind == NodeKind.Sensitive ? 7 : kind == NodeKind.Capacitor ? 15 : kind == NodeKind.Projector ? 5 : kind == NodeKind.Core ? 20 : 10; } }
        public NodeState Copy()
        {
            NodeState n = (NodeState)MemberwiseClone();
            n.contributions = contributions.ConvertAll(x => x.Copy());
            return n;
        }
    }
    [Serializable] public sealed class NodeCandidate { public NodeKind kind; public NodeVariant variant; }
    [Serializable] public sealed class EdgeState
    {
        public int sourceId, targetId, delay = 1, strength = 1;
        public bool echo, blocked;
        public string origin = "天然";
    }
    [Serializable] public sealed class EdgeModification
    {
        public int sourceId, targetId, extraDelay;
        public bool bridge, blocked, echo;
    }
    [Serializable] public sealed class SynapseState { public string id; public int sourceId = -1, targetId = -1; }
    [Serializable] public sealed class ItemState { public string id; }
    [Serializable] public sealed class DrugEffect { public string id; public int nodeId; }
    [Serializable] public sealed class ShopOffer { public string id; public int price; public bool sold, bossReward; }
    [Serializable] public sealed class StructureLevel { public string id; public int level = 1; }
    [Serializable] public sealed class RunState
    {
        public int version = 1, seed, rngState, round = 1, target, roundScore, currency = 4;
        public int clicksRemaining = 2, movesRemaining = 1, memoriesUsed, totalScore, bestScore;
        public int refreshCost = 2, lastReward, nextNodeId = 1, drugsUsed, twinA = -1, twinB = -1;
        public bool twinAwarded, bossRelaxed, bossRewardClaimed;
        public RunPhase phase;
        public string bossName = "", bossDescription = "", lastStructure = "", lastMessage = "";
        public List<NodeState> nodes = new List<NodeState>();
        public List<NodeCandidate> candidates = new List<NodeCandidate>();
        public List<SynapseState> synapses = new List<SynapseState>();
        public List<ItemState> inventory = new List<ItemState>();
        public List<ShopOffer> shop = new List<ShopOffer>();
        public List<EdgeModification> modifications = new List<EdgeModification>();
        public List<DrugEffect> drugs = new List<DrugEffect>();
        public List<StructureLevel> structures = new List<StructureLevel>();
        public List<string> history = new List<string>();

        public RunState Copy()
        {
            RunState s = (RunState)MemberwiseClone();
            s.nodes = nodes.ConvertAll(n => n.Copy());
            s.candidates = candidates.ConvertAll(n => new NodeCandidate { kind = n.kind, variant = n.variant });
            s.synapses = synapses.ConvertAll(n => new SynapseState { id = n.id, sourceId = n.sourceId, targetId = n.targetId });
            s.inventory = inventory.ConvertAll(n => new ItemState { id = n.id });
            s.shop = shop.ConvertAll(n => new ShopOffer { id = n.id, price = n.price, sold = n.sold, bossReward = n.bossReward });
            s.modifications = modifications.ConvertAll(n => new EdgeModification { sourceId = n.sourceId, targetId = n.targetId, bridge = n.bridge, blocked = n.blocked, echo = n.echo, extraDelay = n.extraDelay });
            s.drugs = drugs.ConvertAll(n => new DrugEffect { id = n.id, nodeId = n.nodeId });
            s.structures = structures.ConvertAll(n => new StructureLevel { id = n.id, level = n.level });
            s.history = new List<string>(history);
            return s;
        }
    }
    [Serializable] public sealed class ScoreEvent
    {
        public int tick, nodeId = -1, sourceId = -1, synapseIndex = -1;
        public EventKind kind;
        public double value, charge, multiplier;
        public string text;
    }
    [Serializable] public sealed class StimulusResult
    {
        public bool success, stillPropagating;
        public string error = "", structureName = "";
        public int score, longestLayer, maxWave, rescores, structureLevel = 1;
        public double charge, multiplier = 1;
        public List<ScoreEvent> events = new List<ScoreEvent>();
        public List<int> firedIds = new List<int>();
    }
    public sealed class ContentDefinition
    {
        public string id, name, description;
        public ContentCategory category;
        public int price, targetCount;
        public bool rare;
    }
}
