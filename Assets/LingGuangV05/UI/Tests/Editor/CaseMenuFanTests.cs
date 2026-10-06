using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public sealed class CaseMenuFanTests
    {
        [TestCase(0f, .03f, .55f)]
        [TestCase(.65f, .22f, 1.5f)]
        [TestCase(.9f, .22f, 1.5f)]
        [TestCase(1.15f, .22f, 1.5f)]
        [TestCase(3.7f, .032f, .9f)]
        [TestCase(4.2f, .032f, .9f)]
        [TestCase(100f, .032f, .9f)]
        public void EnvelopeStartsQuietlySurgesThenSettles(float seconds, float volume, float pitch)
        {
            var result = Curve()(seconds);
            Assert.That(result.x, Is.EqualTo(volume).Within(.00001f));
            Assert.That(result.y, Is.EqualTo(pitch).Within(.00001f));
        }

        [Test]
        public void InvalidOrNegativeElapsedTimeIsFiniteAndOff()
        {
            var curve = Curve();
            foreach (float value in new[] { -1f, -.0001f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Assert.That(curve(value), Is.EqualTo(Vector2.zero), "Invalid elapsed time must never drive the source");
        }

        [Test]
        public void EnvelopeIsContinuousBoundedAndMonotonicWithinEachRamp()
        {
            var curve = Curve();
            var previous = curve(0f);
            for (int step = 1; step <= 4200; step++)
            {
                float time = step * .001f;
                var current = curve(time);
                Assert.That(current.x, Is.InRange(0f, .22001f), "volume at " + time);
                Assert.That(current.y, Is.InRange(.55f, 1.50001f), "pitch at " + time);
                Assert.That(Mathf.Abs(current.x - previous.x), Is.LessThan(.001f), "volume jump at " + time);
                Assert.That(Mathf.Abs(current.y - previous.y), Is.LessThan(.004f), "pitch jump at " + time);
                if (time <= .65f)
                {
                    Assert.That(current.x, Is.GreaterThanOrEqualTo(previous.x - .00001f));
                    Assert.That(current.y, Is.GreaterThanOrEqualTo(previous.y - .00001f));
                }
                else if (time >= 1.151f)
                {
                    Assert.That(current.x, Is.LessThanOrEqualTo(previous.x + .00001f));
                    Assert.That(current.y, Is.LessThanOrEqualTo(previous.y + .00001f));
                }
                previous = current;
            }
        }

        [Test]
        public void ConfigureReusesOneSourceAndKeepsItSilentBeforePowerOn()
        {
            var type = FanType();
            var host = new GameObject("CaseMenuFanTest");
            var clip = AudioClip.Create("FanTest", 4410, 1, 44100, false);
            try
            {
                var source = host.AddComponent<AudioSource>();
                source.volume = .7f;
                source.spatialBlend = .6f;
                source.playOnAwake = true;
                var fan = host.AddComponent(type);
                type.GetMethod("Configure").Invoke(fan, new object[] { source, clip });
                Assert.That(source.clip, Is.SameAs(clip));
                Assert.That(source.volume, Is.Zero);
                Assert.That(source.playOnAwake, Is.False);
                Assert.That(source.loop, Is.True);
                Assert.That(source.isPlaying, Is.False);
                Assert.That(source.spatialBlend, Is.EqualTo(.6f));
                Assert.That(host.GetComponents<AudioSource>().Length, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        [TestCase(-1f, 1f)]
        [TestCase(0f, 1f)]
        [TestCase(9.599f, 1f)]
        [TestCase(9.6f, 1f)]
        [TestCase(9.8f, .5f)]
        [TestCase(10f, 0f)]
        [TestCase(10.1f, 0f)]
        [TestCase(100f, 0f)]
        public void GameplayGainFadesOnlyInsideTheFinalFourTenths(float elapsed, float expected)
        {
            Assert.That(GameplayCurve()(elapsed), Is.EqualTo(expected).Within(.00001f));
        }

        [Test]
        public void GameplayGainIsContinuousFiniteAndNeverIncreases()
        {
            var gain = GameplayCurve();
            float previous = gain(9.5f);
            for (int i = 1; i <= 600; i++)
            {
                float current = gain(9.5f + i * .001f);
                Assert.That(current, Is.InRange(0f, 1f));
                Assert.That(current, Is.LessThanOrEqualTo(previous));
                Assert.That(previous - current, Is.LessThan(.0041f));
                previous = current;
            }
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
                Assert.That(gain(value), Is.Zero);
        }

        [Test]
        public void CountdownStartsOnlyExplicitlyAndRepeatedCallsCannotExtendItsDeadline()
        {
            var type = FanType();
            var begin = type.GetMethod("BeginGameplayCountdown");
            Assert.That(begin, Is.Not.Null);
            var deadline = type.GetField("gameplayStopAt", BindingFlags.Instance | BindingFlags.NonPublic);
            var update = type.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(deadline, Is.Not.Null);
            Assert.That(update, Is.Not.Null);
            var host = new GameObject("CaseMenuFanCountdownTest");
            var clip = AudioClip.Create("FanCountdownTest", 4410, 1, 44100, false);
            try
            {
                var source = host.AddComponent<AudioSource>();
                var fan = host.AddComponent(type);
                type.GetMethod("Configure").Invoke(fan, new object[] { source, clip });
                Assert.That(double.IsNaN((double)deadline.GetValue(fan)), Is.True, "Configure must not start the countdown");
                type.GetMethod("StartBoot").Invoke(fan, null);
                type.GetMethod("SetStable").Invoke(fan, null);
                Assert.That(double.IsNaN((double)deadline.GetValue(fan)), Is.True, "Boot and stable fan must not start the countdown");

                double before = Time.realtimeSinceStartupAsDouble;
                begin.Invoke(fan, null);
                double after = Time.realtimeSinceStartupAsDouble;
                double originalDeadline = (double)deadline.GetValue(fan);
                Assert.That(originalDeadline, Is.InRange(before + 10d, after + 10d));
                deadline.SetValue(fan, originalDeadline - 2d); // Simulate elapsed wall time without sleeping in EditMode.
                begin.Invoke(fan, null);
                Assert.That((double)deadline.GetValue(fan), Is.EqualTo(originalDeadline - 2d), "Repeated requests must not extend fan life");

                deadline.SetValue(fan, Time.realtimeSinceStartupAsDouble - .01d);
                update.Invoke(fan, null);
                Assert.That(source.volume, Is.Zero, "A missed frame must still stop immediately after the deadline");
                Assert.That(source.isPlaying, Is.False);
                double expiredDeadline = (double)deadline.GetValue(fan);
                begin.Invoke(fan, null);
                Assert.That((double)deadline.GetValue(fan), Is.EqualTo(expiredDeadline), "Calling again after expiry must not restart the fan");
                host.SetActive(false);
                // Non-ExecuteAlways behaviours do not receive this callback in an EditMode fixture.
                if (!Application.isPlaying)
                    type.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fan, null);
                Assert.That(double.IsNaN((double)deadline.GetValue(fan)), Is.True, "Disabling cancels the countdown");
                Assert.That(source.volume, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        private static Func<float, float> GameplayCurve()
        {
            var method = FanType().GetMethod("GameplayGain");
            Assert.That(method, Is.Not.Null);
            return (Func<float, float>)Delegate.CreateDelegate(typeof(Func<float, float>), method);
        }

        private static Func<float, Vector2> Curve()
        {
            var method = FanType().GetMethod("Envelope");
            Assert.That(method, Is.Not.Null);
            return (Func<float, Vector2>)Delegate.CreateDelegate(typeof(Func<float, Vector2>), method);
        }

        private static Type FanType()
        {
            var type = Type.GetType("LingGuangV05.CaseMenu.CaseMenuFanAudio, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, "CaseMenuFanAudio must exist");
            return type;
        }
    }
}
