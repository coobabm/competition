using System;
using System.Collections.Generic;

namespace LingGuang.Core
{
    public sealed class Spark
    {
        public int id;
        public Shape shape;
        public int dir;
        public int cell;
        public int light;
        public int nodeId;
        public bool placedByPlayer;
        public int placedRound = -100;      // global round index when placed
        public int noAnchorStreak;
        public int fireCountTotal;
        // characters (流光, P1)
        public bool isCharacter;
        public int personality = -1;
        public int stayRounds;
        public int spawnStage;
        public int thrOverride = -1;
        public int outOverride = -1;
        public int[] dirsOverride;
        public int maxRelation;
        public int familiarRounds;
        public Spark Clone() => (Spark)MemberwiseClone();
    }

    /// <summary>A firing unit: a single spark or a memory (several sparks that fire together).</summary>
    public sealed class Node
    {
        public int id;
        public List<int> sparks = new List<int>();
        public bool isMemory;
        public int charge;
        public int thresholdRaise;          // refire raises, reset each round
        public bool firedThisRound;
        public int fireCountRound;
        public int triggerCount;            // lifetime fires
        public bool triggeredThisRound;
        public int plasticity;
        public int consecutiveTriggered;
        public int consecutiveIdle;
        public Region region;
        public string title;
        public int lastFireInst = -1;
        public Node Clone()
        {
            var n = (Node)MemberwiseClone();
            n.sparks = new List<int>(sparks);
            return n;
        }
    }

    public sealed class Edge
    {
        public int from, to;                // spark ids
        public int count;
        public int idleRounds;
        public bool pruned;
        public int stp;
        public bool conductedThisRound;
        public Edge Clone() => (Edge)MemberwiseClone();
        public static long Key(int from, int to) => ((long)from << 32) | (uint)to;
        public long KeyOf => Key(from, to);
    }

    public sealed class RegionState
    {
        public Region region;
        public bool sedimented;
        public int plasticity;
        public int consecutiveTriggered;
        public int consecutiveIdle;
        public bool triggeredThisRound;
        public RegionState Clone() => (RegionState)MemberwiseClone();
    }

    public sealed class Pickup
    {
        public int cell;
        public string itemId;
        public int roundsLeft;
        public Pickup Clone() => (Pickup)MemberwiseClone();
    }

    public sealed class ActiveEffect
    {
        public string sourceId;     // item/drug/gate id
        public Effect effect;
        public int roundsLeft;      // -1 = permanent / while held
        public int delayRounds;     // >0: not active yet
        public ActiveEffect Clone() => (ActiveEffect)MemberwiseClone();
    }

    public enum Phase { Prepare, Playing, RoundSettled, GameOver, Victory }

    public sealed class RoundScore
    {
        public int stage, round, target, score;
        public bool passed;
        public string topPattern;
    }

    public enum SimEventType
    {
        Fire, Deliver, Conduct, Lost, RippleSpawn, RippleMerge, RippleApply, WeakLinkDeliver,
        PickupCollected, EdgeLevelUp, PatternRecognized, ScoreDelta, Leak, BeatEnd, ChainEnd, Error,
        Lighthouse, InfiniteLoop
    }

    public sealed class SimEvent
    {
        public SimEventType type;
        public int beat;
        public int cell = -1;
        public int cellB = -1;
        public int sparkId = -1;
        public int nodeId = -1;
        public int amount;
        public int light;
        public float mult;
        public int level;
        public int radius;
        public int charge;
        public int threshold;
        public int fireIndex;       // nth fire of this node in the chain (1-based)
        public bool viaRipple;
        public bool isStart;
        public string text;
        public List<int> cells;
        public override string ToString()
        {
            return $"[b{beat}] {type} cell={cell}{(cellB >= 0 ? "->" + cellB : "")} amt={amount} light={light} chg={charge}/{threshold} {text}";
        }
    }

    public sealed class ChainStats
    {
        public int maxBeat;             // L
        public int maxFiresInBeat;      // W (excluding beat 0)
        public bool reverb;
        public int fires;
        public int conducts;
        public int rippleConducts;
        public bool convergeBurst;
        public bool diverge;
        public int lightGained;
        public float multGained;
        public string pattern;
        public bool aborted;
        public bool infinite;
        public int loopPeriod;
        public int loopCycleLight;
        public float loopCycleMult;
    }
}
