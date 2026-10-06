using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.UI.Tests
{
    public sealed class ChessPresentationTests
    {
        static Type Find(string name)
        {
            var type = Type.GetType("LingGuangV05.Desktop.Games." + name + ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, name);
            return type;
        }

        [TestCase('P', 0)] [TestCase('R', 1)] [TestCase('N', 2)]
        [TestCase('B', 3)] [TestCase('Q', 4)] [TestCase('K', 5)]
        [TestCase('p', 6)] [TestCase('r', 7)] [TestCase('n', 8)]
        [TestCase('b', 9)] [TestCase('q', 10)] [TestCase('k', 11)] [TestCase('.', -1)]
        public void SpriteGridMatchesBothCompleteChessSets(char piece, int expected)
        {
            Assert.That(Find("ChessSprites").GetMethod("Index").Invoke(null, new object[] { piece }), Is.EqualTo(expected));
        }

        [Test] public void CheckedKingMotionIsSubtleAndReducedMotionCanStopIt()
        {
            var method = Find("ChessPieceView").GetMethod("CheckOffset");
            Assert.That(method, Is.Not.Null);
            for (int i = 0; i < 100; i++)
            {
                var t = i * .021f;
                var offset = (Vector2)method.Invoke(null, new object[] { t, false });
                Assert.That(offset.magnitude, Is.LessThanOrEqualTo(2.1f));
                Assert.That((Vector2)method.Invoke(null, new object[] { t, true }), Is.EqualTo(Vector2.zero));
            }
        }

        [TestCase(-300f, -300f, true, 0, 0)]
        [TestCase(299.9f, 299.9f, true, 7, 7)]
        [TestCase(300f, 0f, false, -1, -1)]
        [TestCase(-300.1f, 0f, false, -1, -1)]
        public void BoardCoordinateMappingRejectsOutsideDrops(float x, float y, bool valid, int file, int rank)
        {
            var method = Find("ChessPanel").GetMethod("TrySquare", BindingFlags.Static | BindingFlags.Public);
            Assert.That(method, Is.Not.Null);
            object[] args = { new Vector2(x, y), -1, -1 };
            Assert.That(method.Invoke(null, args), Is.EqualTo(valid));
            Assert.That(args[1], Is.EqualTo(file)); Assert.That(args[2], Is.EqualTo(rank));
        }
    }
}
