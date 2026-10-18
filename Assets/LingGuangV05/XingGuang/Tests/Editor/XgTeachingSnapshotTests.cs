using NUnit.Framework;
using UnityEngine;

namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgTeachingSnapshotTests
    {
        [Test]
        public void QuestionSnapshotSurvivesBoardMutationAndSaveReload()
        {
            var sim = new XgSim();
            var card = new XgCard { teachingImageKind = "grid", teachingImageKey = 2,
                teachingImageKeys = new[] { 2, 0, 1, 2 }, teachingPosition = "original-position" };
            sim.RecordTeachingQuestion("选择图片", "Select pictures", true, false,
                "选第二张", "Choose the second", "mnist", card, "1 3", "2");
            card.teachingImageKeys[0] = 99;
            card.teachingPosition = "next-position";
            var loaded = JsonUtility.FromJson<XgState>(JsonUtility.ToJson(sim.S));
            var note = loaded.teachingNotebook[0];
            CollectionAssert.AreEqual(new[] { 2, 0, 1, 2 }, note.teachingImageKeys);
            Assert.AreEqual("original-position", note.teachingPosition);
            Assert.AreEqual("1 3", note.answerText);
            Assert.AreEqual("2", note.aiAnswer);
            Assert.IsFalse(note.playerAnswer);
        }

        [Test]
        public void NotebookKeepsFirstFourQuestionsAndNewestHistoryWithinLimit()
        {
            var sim = new XgSim();
            for (int i = 0; i < 300; i++)
                sim.RecordTeachingQuestion("Q" + i, "Q" + i, true, true, "", "", "sense");
            Assert.AreEqual(256, sim.S.teachingNotebook.Count);
            for (int i = 0; i < 4; i++) Assert.AreEqual("Q" + i, sim.S.teachingNotebook[i].text);
            Assert.AreEqual("Q48", sim.S.teachingNotebook[4].text);
            Assert.AreEqual("Q299", sim.S.teachingNotebook[255].text);
            Assert.AreEqual(300, sim.S.teachingNoteSerial);
        }
    }
}
