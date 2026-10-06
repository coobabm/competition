using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace LingGuangV05.UI.Tests
{
    public sealed class ChessFeedbackTests
    {
        [Test]
        public void FeedbackExposesThePresentationOnlyContract()
        {
            var type = Find();
            foreach (string name in new[] { "Init", "Move", "Selected", "Trail", "Check", "Outcome", "ClearFeedback" })
                Assert.That(type.GetMethod(name), Is.Not.Null, name);
            Assert.That(type.GetProperty("Busy"), Is.Not.Null);
            Assert.That(type.GetProperty("PlayerCombo"), Is.Not.Null);
        }

        [Test]
        public void ClearingPendingFeedbackCompletesExactlyOnceAndRestoresBoardPose()
        {
            using (var f = new Fixture())
            {
                int completed = 0;
                Vector2 rest = f.Board.anchoredPosition;
                f.Move(true, true, () => completed++);
                Assert.That(f.Busy, Is.True);
                f.Call("ClearFeedback"); f.Call("ClearFeedback");
                Assert.That(completed, Is.EqualTo(1));
                Assert.That(f.Busy, Is.False);
                Assert.That(f.Board.anchoredPosition, Is.EqualTo(rest));
                Assert.That(f.Board.localScale, Is.EqualTo(Vector3.one));
            }
        }

        [Test]
        public void CaptureFeedbackNeverPausesTheSharedSimulationClock()
        {
            float before = Time.timeScale;
            try
            {
                using (var f = new Fixture())
                {
                    f.Move(true, true, null);
                    for (int i = 0; i < 20; i++) f.Call("Step", .05f);
                    Assert.That(Time.timeScale, Is.EqualTo(before));
                    var juice = f.Host.GetComponents<MonoBehaviour>().First(c => c.GetType().Name == "XgJuice");
                    Assert.That(juice.GetType().GetField("LocalHitStop").GetValue(juice), Is.True);
                }
            }
            finally { Time.timeScale = before; }
        }

        [Test]
        public void EachSideHasItsOwnCosmeticCaptureChain()
        {
            using (var f = new Fixture())
            {
                f.Move(true, true, null); Assert.That(f.Combo, Is.EqualTo(1));
                f.Move(false, false, null); Assert.That(f.Combo, Is.EqualTo(1), "A black quiet move does not reset White's chain.");
                f.Move(true, true, null); Assert.That(f.Combo, Is.EqualTo(2));
                f.Move(true, false, null); Assert.That(f.Combo, Is.Zero);
                f.Call("ClearFeedback"); Assert.That(f.Combo, Is.Zero);
            }
        }

        [Test]
        public void HidingTheBoardCancelsAnimationAndUnblocksThePanelExactlyOnce()
        {
            using (var f = new Fixture())
            {
                int completed = 0;
                f.Move(true, true, () => completed++);
                f.Visible = false;
                f.Call("Step", .016f); f.Call("Step", .016f); f.Call("ClearFeedback");
                Assert.That(completed, Is.EqualTo(1));
                Assert.That(f.Busy, Is.False);
                Assert.That(f.Host.GetComponentsInChildren<Image>(true)
                    .Where(image => image.name.Contains("Ghost")).All(image => !image.gameObject.activeSelf), Is.True);
            }
        }

        [Test]
        public void QuietMoveAnimationFinishesOnceAndReusesTwoNonInteractiveGhosts()
        {
            using (var f = new Fixture())
            {
                int completed = 0;
                for (int move = 0; move < 4; move++)
                {
                    f.Move(true, false, () => completed++);
                    for (int step = 0; step < 16; step++) f.Call("Step", .05f);
                }
                Assert.That(completed, Is.EqualTo(4));
                Assert.That(f.Busy, Is.False);
                var ghosts = f.Host.GetComponentsInChildren<Image>(true).Where(image => image.name.Contains("Ghost")).ToArray();
                Assert.That(ghosts.Length, Is.EqualTo(2));
                Assert.That(ghosts.All(image => !image.raycastTarget), Is.True);
                Assert.That(f.Host.GetComponentsInChildren<Graphic>(true).All(graphic => !graphic.raycastTarget), Is.True);
            }
        }

        [Test]
        public void SilhouetteFlashShaderKeepsTheUiStencilAndClipContracts()
        {
            var shader = Resources.Load<Shader>("LingGuangV05/Chess/ChessPieceFlash");
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            try
            {
                foreach (string property in new[] { "_MainTex", "_Flash", "_Stencil", "_StencilComp", "_StencilReadMask", "_StencilWriteMask", "_ColorMask" })
                    Assert.That(material.HasProperty(property), Is.True, property);
                string source = System.IO.File.ReadAllText(UnityEditor.AssetDatabase.GetAssetPath(shader));
                Assert.That(source, Does.Contain("UNITY_UI_CLIP_RECT"));
                Assert.That(source, Does.Contain("UNITY_UI_ALPHACLIP"));
            }
            finally { UnityEngine.Object.DestroyImmediate(material); }
        }

        static Type Find()
        {
            var type = Type.GetType("LingGuangV05.Desktop.Games.ChessFeedback, Assembly-CSharp");
            Assert.That(type, Is.Not.Null, "ChessFeedback must exist without changing ChessGame.");
            return type;
        }

        sealed class Fixture : IDisposable
        {
            public readonly GameObject Host;
            public readonly RectTransform Board;
            public readonly Type Type;
            readonly Component feedback;
            readonly Sprite sprite;
            readonly Texture2D texture;
            readonly FieldInfo soundPool;
            readonly object originalSoundPool;
            public bool Visible = true;
            public bool Busy => (bool)Type.GetProperty("Busy").GetValue(feedback);
            public int Combo => (int)Type.GetProperty("PlayerCombo").GetValue(feedback);

            public Fixture()
            {
                Type = Find();
                Host = new GameObject("Chess FX test board", typeof(RectTransform));
                Board = (RectTransform)Host.transform; Board.sizeDelta = Vector2.one * 600;
                Board.anchoredPosition = new Vector2(-190, 0);
                texture = new Texture2D(2, 2);
                sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
                var juice = System.Type.GetType("LingGuangV05.Desktop.XingGuang.XgJuice, Assembly-CSharp", true);
                soundPool = juice.GetNestedType("Sfx").GetField("sources", BindingFlags.Static | BindingFlags.NonPublic);
                originalSoundPool = soundPool.GetValue(null); soundPool.SetValue(null, null);
                feedback = Host.AddComponent(Type);
                Call("Init", Board, TMPro.TMP_Settings.defaultFontAsset, (Func<bool>)(() => Visible));
            }
            public void Call(string name, params object[] args)
            {
                var method = Type.GetMethod(name);
                Assert.That(method, Is.Not.Null, name);
                method.Invoke(feedback, args);
            }
            public void Move(bool white, bool capture, Action completed)
                => Call("Move", sprite, capture ? sprite : null, new Vector2(-75, 0), new Vector2(0, 75),
                    new Vector2(0, 75), white, completed);
            public void Dispose()
            {
                soundPool.SetValue(null, originalSoundPool);
                UnityEngine.Object.DestroyImmediate(Host);
                UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
