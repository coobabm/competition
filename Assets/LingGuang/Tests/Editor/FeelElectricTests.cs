using LingGuang.Game;
using MoreMountains.Feedbacks;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace LingGuang.Tests
{
    public sealed class FeelElectricTests
    {
        [Test]
        public void Pool_IsBoundedIdempotentAndUsesNativeFeelFeedbacks()
        {
            var root = new GameObject("FeelPoolTest");
            try
            {
                var effects = root.AddComponent<FeelElectricFeedback>();
                effects.Initialize();
                int children = root.GetComponentsInChildren<Transform>(true).Length;
                effects.Initialize();
                Assert.That(root.GetComponentsInChildren<Transform>(true).Length, Is.EqualTo(children));
                var players = root.GetComponentsInChildren<MMF_Player>(true);
                Assert.That(players.Length, Is.EqualTo(FeelElectricFeedback.ImpactCapacity + FeelElectricFeedback.ArcCapacity));
                var feedbacks = players.SelectMany(p => p.FeedbacksList).ToArray();
                Assert.That(feedbacks.OfType<MMF_Scale>().Count(), Is.EqualTo(FeelElectricFeedback.ImpactCapacity));
                Assert.That(feedbacks.OfType<MMF_SpriteRenderer>().Count(), Is.EqualTo(FeelElectricFeedback.ImpactCapacity * 3));
                Assert.That(feedbacks.OfType<MMF_Particles>().Count(), Is.EqualTo(FeelElectricFeedback.ImpactCapacity));
                Assert.That(feedbacks.OfType<MMF_NeuralElectricArc>().Count(), Is.EqualTo(FeelElectricFeedback.ArcCapacity));
                foreach (var player in players)
                {
                    Assert.That(player.InitializationMode, Is.EqualTo(MMFeedbacks.InitializationModes.Script));
                    Assert.That(player.AutoPlayOnStart || player.AutoPlayOnEnable, Is.False);
                    Assert.That(player.ForcedTimescaleMode, Is.EqualTo(TimescaleModes.Scaled));
                }
                foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(particles.main.maxParticles, Is.EqualTo(12));
                    Assert.That(particles.main.simulationSpace, Is.EqualTo(ParticleSystemSimulationSpace.World));
                    Assert.That(particles.main.loop, Is.False);
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    Assert.That(renderer.sharedMaterial, Is.SameAs(Gfx.Additive), "Do not clone a material per effect.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ClearAndReduceFlash_HideVisualsWithoutChangingGlobalTime()
        {
            var root = new GameObject("FeelClearTest");
            try
            {
                var effects = root.AddComponent<FeelElectricFeedback>();
                effects.Initialize();
                var lines = root.GetComponentsInChildren<LineRenderer>(true);
                var sprites = root.GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var line in lines) line.enabled = true;
                foreach (var sprite in sprites) sprite.color = Color.white;
                float timeScale = Time.timeScale;
                effects.SetMode(true, true);
                Assert.That(effects.Reduced && effects.FastForward, Is.True);
                Assert.That(lines.All(line => !line.enabled), Is.True);
                // Arc heads are hidden; impact sprite alpha is zeroed.
                Assert.That(sprites.All(sprite => !sprite.enabled || sprite.color.a == 0), Is.True);
                Assert.That(Time.timeScale, Is.EqualTo(timeScale));
                effects.Clear();
                Assert.That(root.GetComponentsInChildren<ParticleSystem>(true).All(p => p.particleCount == 0), Is.True);
                Assert.That(root.GetComponentsInChildren<MMF_Player>(true).All(p => !p.IsPlaying), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ElectricEffects_AreImplementedByAnInstalledFeelPlayer()
        {
            var type = typeof(FxPlayer).Assembly.GetType("LingGuang.Game.FeelElectricFeedback");
            Assert.That(type, Is.Not.Null, "Light/electric feedback must actually use the installed Feel plugin.");
            var root = new GameObject("FeelElectricTest");
            try
            {
                var component = root.AddComponent(type);
                type.GetMethod("Initialize").Invoke(component, null);
                bool found = false;
                foreach (var item in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (item.GetType().FullName == "MoreMountains.Feedbacks.MMF_Player") found = true;
                Assert.That(found, Is.True, "Creating hand-coded effects without MMF_Player is not Feel integration.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
