using NUnit.Framework;
namespace LingGuangV05.XingGuang.Tests
{
    public sealed class XgDataReviewTests
    {
        static XgSim Open()
        {
            var sim=new XgSim();sim.S.stage=2;sim.S.unlocked.Add(XgSim.DataReviewNode);
            sim.LearningClock=()=>new XgLearningTime(20160601,3600);return sim;
        }
        [Test] public void GateDoesNotRevealHistoryBeforePurchase()
        {
            var sim=new XgSim();sim.RecordAsked("arith","2+2?","4","4");
            Assert.IsFalse(sim.DataReviewOpen);Assert.AreEqual(0,sim.QueryDataReview().total);
            sim.S.unlocked.Add(XgSim.DataReviewNode);Assert.AreEqual(1,sim.QueryDataReview().total);
            var n=XgCatalog.Node(XgSim.DataReviewNode);Assert.NotNull(n);Assert.AreEqual("brain.window",n.parent);Assert.AreEqual(2,n.stage);Assert.AreEqual(150,n.cost);
        }
        [Test] public void PaginationAndSearchPreserveAllQuestionsAndTheirStage()
        {
            var sim=Open();for(int i=0;i<75;i++)sim.RecordAsked("arith","question "+i,"a","b");
            var first=sim.QueryDataReview("",0,30);var last=sim.QueryDataReview("",60,30);
            Assert.AreEqual(75,first.total);Assert.AreEqual(30,first.rows.Count);Assert.IsTrue(first.HasMore);
            Assert.AreEqual(15,last.rows.Count);Assert.IsFalse(last.HasMore);Assert.AreEqual(75,sim.S.askedLog.Count);
            Assert.AreEqual(2,first.rows[0].stage);Assert.AreEqual("question 74",first.rows[0].title);
            Assert.AreEqual(1,sim.QueryDataReview("question 74").total);
            var input=sim.LearningRetestInput(first.rows[0].record.id);Assert.NotNull(input);Assert.AreEqual("question 74",input.question);
        }
        [Test] public void InstalledFilesAreIdempotentAndViewingOldReceiptsDoesNotInventDates()
        {
            var sim=Open();var file=new XgDownload{fileId="review-test",dataset="mnist",installed=true};sim.S.dataFiles.Add(file);
            var legacy=sim.QueryDataReview();Assert.AreEqual(1,legacy.total);Assert.AreEqual(0,legacy.rows[0].stage);Assert.IsNull(legacy.rows[0].record);Assert.IsEmpty(sim.S.askedLog);
            sim.RecordInstalledDataForReview(file);sim.RecordInstalledDataForReview(file);
            var page=sim.QueryDataReview();Assert.AreEqual(1,page.total);Assert.AreEqual("data",page.rows[0].kind);Assert.AreEqual(2,page.rows[0].stage);
            Assert.IsNull(sim.LearningRetestInput(page.rows[0].record.id),"A whole data pack is not a question to send to the model");
        }
        [Test] public void LegacyQuestionKeepsUnknownStageAndCompletionIsIncluded()
        {
            var sim=Open();sim.S.askedLog.Add(new XgAskedEntry{bankId="arith",question="legacy question",sourceKind="question"});
            sim.RecordBookCompletion("example","读过的选段","Read excerpt","example-complete");
            var page=sim.QueryDataReview();Assert.AreEqual(2,page.total);
            Assert.AreEqual("book",page.rows[0].kind);Assert.AreEqual(2,page.rows[0].stage);
            Assert.AreEqual(0,page.rows[1].stage);Assert.AreEqual(0,page.rows[1].record.gameDate);
        }
    }
}
