using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public sealed class CaseMenuTests
    {
        private static Type Find(string name)
        {
            var type = Type.GetType("LingGuangV05.CaseMenu." + name + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, name + " must exist");
            return type;
        }

        [Test] public void SceneLocalMenuControllerExists()
        {
            Assert.That(typeof(MonoBehaviour).IsAssignableFrom(Find("CaseMenuController")), Is.True);
        }

        [TestCase(-1f, 0f)] [TestCase(0f, 0f)] [TestCase(.5f, .5f)]
        [TestCase(1f, 1f)] [TestCase(2f, 1f)]
        public void CameraEasingIsClampedAndReachesEndpoints(float t, float expected)
        {
            var method = Find("CaseMenuMotion").GetMethod("SmootherStep");
            Assert.That(method, Is.Not.Null);
            Assert.That((float)method.Invoke(null, new object[] { t }), Is.EqualTo(expected).Within(.00001f));
        }

        [Test] public void CameraPathHasExactEndpoints()
        {
            var method = Find("CaseMenuMotion").GetMethod("Bezier");
            Assert.That(method, Is.Not.Null);
            var a = new Vector3(-2, 1, -3); var b = new Vector3(-1, 1, -2);
            var c = new Vector3(1, 1, -2); var d = new Vector3(1, 1, -1);
            Assert.That((Vector3)method.Invoke(null, new object[] { a, b, c, d, 0f }), Is.EqualTo(a));
            Assert.That((Vector3)method.Invoke(null, new object[] { a, b, c, d, 1f }), Is.EqualTo(d));
        }

        [Test] public void CameraRotationUsesTheSameEasingAsPosition()
        {
            var method = Find("CaseMenuMotion").GetMethod("Rotation");
            Assert.That(method, Is.Not.Null);
            var start = Quaternion.Euler(16, 31, 0);
            for (int i = 0; i <= 20; i++)
            {
                float t = i / 20f;
                var actual = (Quaternion)method.Invoke(null, new object[] { start, Quaternion.identity, t });
                float s = t * t * t * (t * (t * 6 - 15) + 10);
                Assert.That(Quaternion.Angle(actual, Quaternion.Slerp(start, Quaternion.identity, s)), Is.LessThan(.05f));
            }
        }

        [Test] public void ALoadingHitchCannotSkipMostOfTheCameraFlight()
        {
            var method = Find("CaseMenuMotion").GetMethod("FrameStep");
            Assert.That(method, Is.Not.Null);
            Assert.That((float)method.Invoke(null, new object[] { .016f }), Is.EqualTo(.016f).Within(.0001f));
            Assert.That((float)method.Invoke(null, new object[] { 2f }), Is.EqualTo(.05f).Within(.0001f));
            Assert.That((float)method.Invoke(null, new object[] { -1f }), Is.EqualTo(0f));
        }

        [Test] public void ReleaseOffButtonCancelsClick()
        {
            var type = Find("CaseMenuButton");
            var go = new GameObject("Case button test");
            try
            {
                var button = go.AddComponent(type);
                type.GetMethod("SetHovered").Invoke(button, new object[] { true });
                type.GetMethod("BeginPress").Invoke(button, null);
                type.GetMethod("SetHovered").Invoke(button, new object[] { false });
                Assert.That((bool)type.GetMethod("EndPress").Invoke(button, new object[] { false }), Is.False);
                Assert.That((bool)type.GetProperty("IsPressed").GetValue(button), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] public void ReleaseOnCapturedButtonClicksExactlyOnce()
        {
            var type = Find("CaseMenuButton");
            var go = new GameObject("Case button test");
            try
            {
                var button = go.AddComponent(type);
                type.GetMethod("SetHovered").Invoke(button, new object[] { true });
                type.GetMethod("BeginPress").Invoke(button, null);
                Assert.That((bool)type.GetMethod("EndPress").Invoke(button, new object[] { true }), Is.True);
                Assert.That((bool)type.GetMethod("EndPress").Invoke(button, new object[] { true }), Is.False);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test] public void MainSceneIsPreservedAndMenuIsASeparateAsset()
        {
            Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/LingGuangV05/Scenes/Main.unity"), Is.Not.Null);
            Assert.That(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>("Assets/LingGuangV05/Scenes/ComputerCaseMenu.unity"), Is.Not.Null);
        }
    }
}
