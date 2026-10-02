using LingGuang.Core;
using LingGuang.Game;
using NUnit.Framework;
using UnityEngine;

namespace LingGuang.Tests
{
    public sealed class ConductA01Tests
    {
        GameObject root;
        [TearDown] public void Cleanup() { if (root != null) Object.DestroyImmediate(root); }

        [Test]
        public void Geometry_MatchesActualHtmlSeedSeven()
        {
            // Exported by executing the supplied HTML's build() with DEF and seed=7.
            var geometry = new ConductA01Geometry();
            Assert.That(geometry.Axon[0].x, Is.EqualTo(.3306f).Within(.00001f));
            Assert.That(geometry.Axon[6].x, Is.EqualTo(1.07951542f).Within(.00001f));
            Assert.That(geometry.Axon[6].y, Is.EqualTo(-.03635044f).Within(.00001f));
            Assert.That(geometry.Dendrite[6].x, Is.EqualTo(-.74399014f).Within(.00001f));
            Assert.That(geometry.Dendrite[6].y, Is.EqualTo(.03224982f).Within(.00001f));
            Assert.That(geometry.Phase, Is.EqualTo(3.12740652f).Within(.00001f));
            Assert.That(geometry.Strokes.Count, Is.InRange(25, 200));
        }

        [Test]
        public void Geometry_DoesNotConsumeUnityRandom_AndIsDeterministic()
        {
            var state = Random.state;
            var a = new ConductA01Geometry(); var b = new ConductA01Geometry();
            Assert.That(Random.state, Is.EqualTo(state));
            CollectionAssert.AreEqual(a.Axon, b.Axon);
            CollectionAssert.AreEqual(a.Dendrite, b.Dendrite);
            Assert.That(ConductA01Geometry.Along(a.Axon, 0), Is.EqualTo(a.Axon[0]));
            Assert.That(Vector2.Distance(ConductA01Geometry.Along(a.Axon, 1), a.Axon[6]), Is.LessThan(.00001f));
        }

        [Test]
        public void BreathingAndScan_MatchReferenceTiming_AndReduceFlashDisablesScan()
        {
            Assert.That(ConductA01View.BreathAt(.75f), Is.EqualTo(1.1f).Within(.0001f));
            Assert.That(ConductA01View.BreathAt(2.25f), Is.EqualTo(.9f).Within(.0001f));
            Assert.That(ConductA01View.BreathAt(.75f, 0, true), Is.EqualTo(1.025f).Within(.0001f));
            Assert.That(ConductA01View.ScanAt(.7f).x, Is.EqualTo(.072f).Within(.0001f));
            Assert.That(ConductA01View.ScanAt(1.675f).z, Is.EqualTo(.5f).Within(.0001f));
            Assert.That(ConductA01View.ScanAt(2).y, Is.Zero);
            Assert.That(ConductA01View.ScanAt(2).z, Is.Zero);
            Assert.That(ConductA01View.ScanAt(.7f, true).y, Is.Zero);
            Assert.That(ConductA01View.ScanAt(.7f, true).z, Is.Zero);
        }

        [Test]
        public void ConductLiveAndGhost_UseA01_AllDirections_WithoutChangingRules()
        {
            var state = RunState.CreateSandbox(new GameConfig());
            var sp = state.AddSpark(Shape.Conduct, 3, 4, 0);
            root = new GameObject("A01 test");
            var spark = SparkView.Create(root.transform, null);
            spark.Apply(state, sp, state.NodeOf(sp), 0, 2, false, false, false);
            var art = spark.GetComponentInChildren<ConductA01View>();
            Assert.That(art, Is.Not.Null); Assert.That(art.Ready, Is.True);
            Assert.That(spark.transform.Find("neuron").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(spark.transform.Find("core").GetComponent<SpriteRenderer>().enabled, Is.False);
            Assert.That(spark.transform.Find("glow").GetComponent<SpriteRenderer>().enabled, Is.False);
            int children = spark.GetComponentsInChildren<Transform>(true).Length;
            int originalDirection = sp.dir;
            for (int d = 0; d < 6; d++)
            {
                spark.ApplyGhost(state, Shape.Conduct, d);
                Assert.That(Vector3.Dot(art.transform.localRotation * Vector3.right, BoardView.DirVec(d)), Is.GreaterThan(.999f));
                Assert.That(spark.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(children));
            }
            Assert.That(sp.dir, Is.EqualTo(originalDirection));
            art.TickVisual(.7f, .5f, 1, false, false, true);
            Assert.That(art.LastFlicker, Is.Zero);
            Assert.That(NeuronArt.Get(Shape.Conduct), Is.SameAs(ConductA01View.Icon));
            spark.ApplyGhost(state, Shape.Cone, 0);
            Assert.That(art.gameObject.activeSelf, Is.False);
            Assert.That(spark.transform.Find("neuron").GetComponent<SpriteRenderer>().enabled, Is.True);
        }

        [Test]
        public void Instances_ShareResources_AndInitializationIsIdempotent()
        {
            root = new GameObject("A01 pool test");
            var a = root.AddComponent<ConductA01View>();
            Assert.That(a.Initialize(), Is.True);
            int count = root.GetComponentsInChildren<Transform>().Length;
            Assert.That(a.Initialize(2), Is.True);
            Assert.That(root.GetComponentsInChildren<Transform>().Length, Is.EqualTo(count));
            var other = new GameObject("second"); other.transform.SetParent(root.transform);
            var b = other.AddComponent<ConductA01View>(); b.Initialize();
            Assert.That(a.GetComponentInChildren<MeshFilter>().sharedMesh, Is.SameAs(b.GetComponentInChildren<MeshFilter>().sharedMesh));
            Assert.That(a.GetComponentInChildren<MeshRenderer>().sharedMaterial, Is.SameAs(b.GetComponentInChildren<MeshRenderer>().sharedMaterial));
            Assert.That(Shader.Find("LingGuang/ConductA01").isSupported, Is.True);
        }
    }
}
