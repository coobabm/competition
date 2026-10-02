using System.Collections.Generic;
using LingGuang.Core;
using UnityEngine;

namespace LingGuang.Game
{
    /// <summary>Shared generated neuron sprites. Art never changes simulation directions or values.</summary>
    public static class NeuronArt
    {
        static readonly Dictionary<Shape, Sprite> sprites = new Dictionary<Shape, Sprite>();

        public static Sprite Get(Shape shape)
        {
            if (shape == Shape.Conduct) return ConductA01View.Icon;
            if (sprites.TryGetValue(shape, out var cached)) return cached;
            string name = shape switch
            {
                Shape.Conduct => "conduct",
                Shape.Cone => "cone",
                Shape.Converge => "converge",
                Shape.Instinct => "star",
                _ => null
            };
            var sprite = name == null ? null : Resources.Load<Sprite>("NeuronArt/" + name);
            sprites[shape] = sprite;
            return sprite;
        }

        // Source art points UP (90 degrees); hex direction 0 points upper-right (60 degrees).
        public static Quaternion Rotation(int direction)
        {
            int normalized = (direction % 6 + 6) % 6;
            return Quaternion.Euler(0f, 0f, -30f - normalized * 60f);
        }
    }
}
