using LingGuang.Core;
using LingGuang.Game;
using NUnit.Framework;
using UnityEngine;

namespace LingGuang.Tests
{
    public sealed class NeuralWaveStyleTests
    {
        GameObject root;
        [TearDown] public void Cleanup() { if (root != null) Object.DestroyImmediate(root); }

        NeuralWaveField Field()
        {
            root = new GameObject("WaveFieldTest");
            var field = root.AddComponent<NeuralWaveField>();
            field.Initialize();
            return field;
        }

        [Test]
        public void DistantSources_ArePreservedForWaterInterference()
        {
            var field = Field();
            field.Emit(Vector3.zero, 0.6f, 10);
            field.Emit(Vector3.right * 8, 1, 10.1f);
            Assert.That(field.ActiveCount(10.1f), Is.EqualTo(2), "Distant impacts must produce intersecting water waves, not merge into one sweep.");
            field.Upload(10.1f);
            var waves = field.FieldMaterial.GetVectorArray("_Waves");
            Assert.That(waves[0].z, Is.EqualTo(10));
            Assert.That(waves[0].x, Is.Zero);
        }

        [Test]
        public void NearbyBurst_CoalescesWithoutMovingOrRestartingItsOrigin()
        {
            var field = Field();
            field.Emit(Vector3.zero, 0.6f, 10);
            field.Emit(Vector3.right * 0.15f, 1, 10.1f);
            field.Upload(10.1f);
            Assert.That(field.ActiveCount(10.1f), Is.EqualTo(1));
            Assert.That(field.FieldMaterial.GetVectorArray("_Waves")[0].z, Is.EqualTo(10));
            Assert.That(field.FieldMaterial.GetVectorArray("_Waves")[0].x, Is.Zero);
        }

        [Test]
        public void WaterSurface_ExposesWaveGeometryAndRefractionControls()
        {
            var field = Field();
            var material = field.FieldMaterial;
            Assert.That(material.HasProperty("_WaveGeometry"), Is.True, "Water needs a damped circular wave train, not dot-grid illumination.");
            var shader = Shader.Find("LingGuang/PixelComposite");
            var compositor = new Material(shader);
            try { Assert.That(compositor.HasProperty("_WaterRefraction"), Is.True); }
            finally { Object.DestroyImmediate(compositor); }
        }

        [Test]
        public void BurstStorage_IsBounded_ExpiresAndClears()
        {
            var field = Field();
            for (int i = 0; i < 100; i++) field.Emit(Vector3.right * i, 1, i * 0.3f);
            Assert.That(field.ActiveCount(29.7f), Is.InRange(1, NeuralWaveField.Capacity));
            field.Upload(29.7f);
            Assert.That(field.FieldMaterial.GetVectorArray("_Waves").Length, Is.EqualTo(NeuralWaveField.Capacity));
            Assert.That(field.ActiveCount(29.7f + NeuralWaveField.Lifetime + 0.1f), Is.Zero);
            field.Clear();
            Assert.That(field.ActiveCount(29.7f), Is.Zero);
            foreach (var wave in field.FieldMaterial.GetVectorArray("_Waves")) Assert.That(wave.w, Is.Zero);
        }

        [Test]
        public void InvalidEmissions_AreIgnored_AndReduceFlashLowersGain()
        {
            var field = Field();
            field.Emit(Vector3.zero, 0, 0);
            field.Emit(Vector3.zero, float.NaN, 0);
            field.Emit(new Vector3(float.PositiveInfinity, 0), 1, 0);
            field.Emit(Vector3.zero, 1, float.NaN);
            Assert.That(field.ActiveCount(0), Is.Zero);
            field.Upload(0);
            float normal = field.FieldMaterial.GetFloat("_WaveGain");
            field.ReduceFlash = true;
            field.Upload(0);
            Assert.That(field.FieldMaterial.GetFloat("_WaveGain"), Is.LessThan(normal * 0.3f));
        }

        [Test]
        public void HemisphereMeshes_ShareOneField_AndDecorationDoesNotAddRules()
        {
            root = new GameObject("WaveBoardTest");
            var state = RunState.CreateSandbox(new GameConfig());
            int sparks = state.sparks.Count, edges = state.edges.Count;
            var board = root.AddComponent<BoardView>();
            board.Init(state, null);
            Assert.That(board.WaveField, Is.Not.Null);
            var meshes = root.transform.Find("Brain silhouette").GetComponentsInChildren<MeshRenderer>();
            Assert.That(meshes.Length, Is.EqualTo(2));
            foreach (var mesh in meshes) Assert.That(mesh.sharedMaterial, Is.SameAs(board.WaveField.FieldMaterial));
            board.WaveField.Emit(Vector3.zero, 1, 0);
            Assert.That(state.sparks.Count, Is.EqualTo(sparks));
            Assert.That(state.edges.Count, Is.EqualTo(edges));
        }

        [Test]
        public void BrainField_HasSupportedWaveShader()
        {
            var shader = Shader.Find("LingGuang/NeuralWave");
            Assert.That(shader, Is.Not.Null, "Water surface shader is required.");
            Assert.That(shader.isSupported, Is.True);
        }

        [Test]
        public void Neurons_HaveCompactLuminousCoreOverExistingArt()
        {
            root = new GameObject("WaveCoreTest");
            var state = RunState.CreateSandbox(new GameConfig());
            // A01's compact nucleus is now in its own shader; imported art still uses the glint overlay.
            var spark = state.AddSpark(Shape.Cone, 3, 4, 0);
            var view = SparkView.Create(root.transform, null);
            view.Apply(state, spark, state.NodeOf(spark), 0, 2, false, false, false);
            var core = view.transform.Find("glint").GetComponent<SpriteRenderer>();
            Assert.That(core.enabled, Is.True);
            Assert.That(core.transform.localScale.x, Is.LessThanOrEqualTo(0.16f));
            Assert.That(view.transform.Find("neuron").GetComponent<SpriteRenderer>().sprite, Is.Not.Null);
        }
    }
}
