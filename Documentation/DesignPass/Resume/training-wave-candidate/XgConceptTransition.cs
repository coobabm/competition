using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public enum XgConceptTransitionKind { ElementBorn, Merged, Decayed }

    /// <summary>
    /// Transient evidence of an actual creation or R4 removal, never a saved concept.
    /// ElementBorn samples Create's initial w=0/s=1 before R1 pulls; Merged samples the assigned
    /// parent-mean weight before R4; Decayed samples after R4 scaling, immediately before removal.
    /// Weight/Strength describe that event instant, not the concept's eventual end-of-frame state.
    /// </summary>
    public sealed class XgConceptTransition
    {
        public XgConceptTransitionKind Kind { get; }
        public int Id { get; }
        public int ParentA { get; }
        public int ParentB { get; }
        public string Region { get; }
        public double Weight { get; }
        public double Strength { get; }

        internal XgConceptTransition(XgConceptTransitionKind kind, XgConcept cell, int parentA = -1, int parentB = -1)
        {
            Kind = kind; Id = cell.id; Region = cell.region; Weight = cell.w; Strength = cell.s;
            ParentA = parentA; ParentB = parentB;
        }
    }
}
