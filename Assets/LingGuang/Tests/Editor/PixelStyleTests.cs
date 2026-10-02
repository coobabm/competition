using LingGuang.Core;
using LingGuang.Game;
using NUnit.Framework;
using UnityEngine;

namespace LingGuang.Tests
{
    public sealed class PixelStyleTests
    {
        GameObject root;

        [TearDown]
        public void Cleanup()
        {
            if (root != null && root.TryGetComponent<PixelBoardPresentation>(out var pixels))
                Lifecycle(pixels, "OnDisable");
            if (root != null) Object.DestroyImmediate(root);
        }

        static void Lifecycle(PixelBoardPresentation pixels, string method)
        {
            typeof(PixelBoardPresentation).GetMethod(method,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(pixels, null);
        }

        [Test]
        public void PixelCompositeShader_IsAvailableAndSupported()
        {
            var shader = Shader.Find("LingGuang/PixelComposite");
            Assert.That(shader, Is.Not.Null, "The pixel board needs a separate sharp-core / smooth-glow compositor.");
            Assert.That(shader.isSupported, Is.True);
        }

        // A01 Conduct has its own cyan filament shader; source PNGs remain unchanged.
        [TestCase(Shape.Cone)]
        [TestCase(Shape.Instinct)]
        [TestCase(Shape.Converge)]
        public void NeuronArt_UsesCoolMonochromeMaterialWithoutChangingSourceArt(Shape shape)
        {
            var s = RunState.CreateSandbox(new GameConfig());
            var sp = s.AddSpark(shape, 3, 4, 0);
            root = new GameObject("PixelStyleTest");
            var view = SparkView.Create(root.transform, null);
            view.Apply(s, sp, s.NodeOf(sp), 0, 2, false, false, false);
            var renderer = view.transform.Find("neuron").GetComponent<SpriteRenderer>();
            Assert.That(renderer.sharedMaterial.HasProperty("_Monochrome"), Is.True);
            Assert.That(renderer.sharedMaterial.GetFloat("_Monochrome"), Is.EqualTo(1f));
            var properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            var color = properties.GetColor("_Color");
            Assert.That(color.b, Is.GreaterThan(color.r), "Idle bodies should be blue, not the previous amber palette.");
        }

        [Test]
        public void IdleBoard_GridDoesNotCompeteWithNeurons()
        {
            root = new GameObject("PixelBoardTest");
            var board = root.AddComponent<BoardView>();
            board.Init(RunState.CreateSandbox(new GameConfig()), null);
            var properties = new MaterialPropertyBlock();
            foreach (Transform item in root.transform.Find("Cells"))
            {
                if (!item.name.StartsWith("outline")) continue;
                item.GetComponent<SpriteRenderer>().GetPropertyBlock(properties);
                var c = properties.GetColor("_Color");
                Assert.That(Mathf.Max(c.r, c.g, c.b) * c.a, Is.LessThan(0.12f));
            }
        }

        [TestCase(1920, 1080, 640, 360)]
        [TestCase(3840, 2160, 549, 309)]
        [TestCase(1280, 720, 640, 360)]
        [TestCase(0, 0, 1, 1)]
        public void PixelBuffer_AdaptsToWindowWithoutZeroSizedTextures(int width, int height, int x, int y)
        {
            Assert.That(PixelBoardPresentation.BufferSize(width, height), Is.EqualTo(new Vector2Int(x, y)));
        }

        [Test]
        public void PixelCamera_PreservesPickingAndRestoresStateOnDisable()
        {
            root = new GameObject("PixelCameraTest");
            var camera = root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 6;
            camera.cullingMask = 123;
            camera.transform.position = new Vector3(1, 2, -10);
            var screen = new Vector3(Screen.width * 0.71f, Screen.height * 0.28f, 10);
            var before = camera.ScreenToWorldPoint(screen);
            var pixels = root.AddComponent<PixelBoardPresentation>();
            // Runtime-only component: invoke lifecycle explicitly in EditMode rather
            // than adding ExecuteAlways and allocating cameras while editing scenes.
            Lifecycle(pixels, "OnEnable");
            Assert.That(pixels.WorldTexture, Is.Not.Null);
            Assert.That(pixels.WorldTexture.antiAliasing, Is.EqualTo(1));
            Assert.That(camera.targetTexture, Is.Null, "Do not alter the input camera's screen coordinates.");
            Assert.That(Vector3.Distance(before, camera.ScreenToWorldPoint(screen)), Is.LessThan(0.0001f));
            Assert.That(pixels.Display.raycastTarget, Is.False, "The compositor must not intercept board clicks.");
            Assert.That(pixels.Display.canvas.sortingOrder, Is.LessThan(10), "HUD stays above the pixel world.");
            pixels.enabled = false;
            Lifecycle(pixels, "OnDisable");
            Assert.That(camera.cullingMask, Is.EqualTo(123));
            Assert.That(pixels.WorldTexture, Is.Null);
            Assert.That(pixels.WorldCamera, Is.Null);
            pixels.enabled = true;
            Lifecycle(pixels, "OnEnable");
            Assert.That(root.GetComponentsInChildren<Camera>().Length, Is.EqualTo(2));
            pixels.enabled = false;
            Lifecycle(pixels, "OnDisable");
            Assert.That(root.GetComponentsInChildren<Camera>().Length, Is.EqualTo(1));
        }

        [Test]
        public void WaterRefraction_IsBoundedAndStopsWhenReducedOrCleared()
        {
            root = new GameObject("WaterRefractionTest");
            var camera = root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.transform.position = new Vector3(0, 0, -10);
            var pixels = root.AddComponent<PixelBoardPresentation>();
            Lifecycle(pixels, "OnEnable");
            var field = root.AddComponent<NeuralWaveField>();
            field.Initialize();
            field.SetLobe(0, Vector2.zero, new Vector2(4, 4));
            pixels.WaveField = field;
            field.Emit(Vector3.zero, 1, Time.time);
            Lifecycle(pixels, "LateUpdate");
            var material = pixels.Display.material;
            Assert.That(material.GetFloat("_WaterRefraction"), Is.InRange(0.001f, 0.04f));
            Assert.That(material.GetVector("_WaveGeometry").x, Is.EqualTo(NeuralWaveField.Lifetime));
            Assert.That(material.GetVectorArray("_Waves").Length, Is.EqualTo(NeuralWaveField.Capacity));
            Assert.That(camera.targetTexture, Is.Null);
            Assert.That(pixels.Display.raycastTarget, Is.False);
            pixels.ReduceFlash = true;
            Lifecycle(pixels, "LateUpdate");
            Assert.That(material.GetFloat("_WaterRefraction"), Is.Zero);
            pixels.ReduceFlash = false;
            field.Clear();
            Lifecycle(pixels, "LateUpdate");
            Assert.That(material.GetFloat("_WaterRefraction"), Is.Zero);
            pixels.WaveField = null;
            Lifecycle(pixels, "LateUpdate");
            Assert.That(material.GetFloat("_WaterRefraction"), Is.Zero);
        }

        [Test]
        public void EdgePulse_FollowsStableGeometryInsteadOfRandomLightning()
        {
            root = new GameObject("PixelEdgeTest");
            var edge = EdgeView.Create(root.transform);
            edge.Set(Vector3.zero, Vector3.right, 0, false, false, false, false, false);
            var midpoint = edge.SamplePath(0.5f);
            edge.Disturb();
            edge.Stabilize();
            edge.Tick();
            Assert.That(edge.SamplePath(0.5f), Is.EqualTo(midpoint));
            Assert.That(edge.SamplePath(0).x, Is.LessThan(edge.SamplePath(1).x));
        }
    }
}
