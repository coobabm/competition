using LingGuang.Core;
using LingGuang.Game;
using NUnit.Framework;
using UnityEngine;

namespace LingGuang.Tests
{
    public sealed class NeuronVisualTests
    {
        GameObject root;

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [TestCase("conduct")]
        [TestCase("cone")]
        [TestCase("star")]
        [TestCase("converge")]
        [TestCase("loop")]
        public void GeneratedArt_IsLoadableAsSprite(string name)
        {
            var sprite = Resources.Load<Sprite>("NeuronArt/" + name);
            Assert.That(sprite, Is.Not.Null, "Missing generated neuron sprite: " + name);
            Assert.That(sprite.bounds.size.x, Is.EqualTo(1f).Within(0.01f));
        }

        // Conduct is covered by ConductA01Tests: it now uses the supplied procedural A01 art.
        [TestCase(Shape.Cone, "cone")]
        [TestCase(Shape.Instinct, "star")]
        [TestCase(Shape.Converge, "converge")]
        public void LiveSpark_UsesMatchingGeneratedArt(Shape shape, string expected)
        {
            var s = RunState.CreateSandbox(new GameConfig());
            var sp = s.AddSpark(shape, 3, 4, 0);
            root = new GameObject("NeuronVisualTest");
            var view = SparkView.Create(root.transform, null);
            view.Apply(s, sp, s.NodeOf(sp), 0, 2, false, false, false);
            var art = view.transform.Find("neuron");
            Assert.That(art, Is.Not.Null, "SparkView must replace the procedural placeholder with generated art");
            var renderer = art.GetComponent<SpriteRenderer>();
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sprite, Is.SameAs(Resources.Load<Sprite>("NeuronArt/" + expected)));
            Assert.That(view.transform.Find("core").GetComponent<SpriteRenderer>().enabled, Is.False,
                "The old bright disc must not obscure the generated soma");
        }

        [TestCase(Shape.Conduct)]
        [TestCase(Shape.Cone)]
        [TestCase(Shape.Instinct)]
        [TestCase(Shape.Converge)]
        public void PlacementGhost_AllSixRotationsAlignWithGrid(Shape shape)
        {
            var s = RunState.CreateSandbox(new GameConfig());
            root = new GameObject("NeuronGhostTest");
            var view = SparkView.Create(root.transform, null);
            for (int dir = 0; dir < 6; dir++)
            {
                view.ApplyGhost(s, shape, dir);
                var art = view.transform.Find("neuron");
                Assert.That(art, Is.Not.Null);
                var output = art.localRotation * Vector3.up;
                Assert.That(Vector3.Dot(output, BoardView.DirVec(dir)), Is.GreaterThan(0.999f),
                    "Generated art's primary UP output must rotate onto the actual hex direction");
            }
        }
    }
}
