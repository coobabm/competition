using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public enum XgConceptTransitionKind { ElementBorn, Merged, Decayed }

    /// <summary>Transient evidence of an actual creation or R4 removal, never a saved concept.</summary>
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
