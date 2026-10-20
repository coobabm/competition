using System;
using System.Linq;
using LingGuangV05.XingGuang;
using NUnit.Framework;
using UnityEngine;

public sealed class XgPublicDiscourseResourceTests
{
    static XgPublicDiscourseCatalog Load()
    {
        var asset=Resources.Load<TextAsset>("LingGuangV05/Epilogue/ending_public_discourse");
        Assert.That(asset,Is.Not.Null);return XgPublicDiscourseCatalog.Load(asset.text);
    }
    [Test] public void ApprovedThirtyStageFifteenRouteAndThreeSharedRecordsArePresent()
    {
        var catalog=Load();Assert.That(catalog.All.Count,Is.EqualTo(48));
        foreach(int stage in Enumerable.Range(1,6))
        {
            var rows=catalog.All.Where(r=>r.id.StartsWith("D"+stage+"-",StringComparison.Ordinal)).ToArray();
            Assert.That(rows.Length,Is.EqualTo(5));Assert.That(rows.Select(r=>r.platform),Is.EquivalentTo(new[]{"news","forum","site","miaoyu","yy"}));
        }
        foreach(string route in new[]{"online","public_model","local_only"})
        {
            var rows=catalog.All.Where(r=>r.route==route).ToArray();Assert.That(rows.Length,Is.EqualTo(5));
            Assert.That(rows.Select(r=>r.platform),Is.EquivalentTo(new[]{"news","forum","site","miaoyu","yy"}));
        }
        foreach(var record in catalog.All)
        {Assert.That(record.Title(false),Is.Not.Empty);Assert.That(record.Title(true),Is.Not.Empty);Assert.That(record.Body(false),Is.Not.Empty);Assert.That(record.Body(true),Is.Not.Empty);}
    }
    [Test] public void FutureHeadlineCannotAppearInThe2016Game()
    {
        var catalog=Load();
        var before=new XgPublicDiscourseContext(6,20161231,"news",new[]{"news"});
        Assert.That(catalog.Available(before).Any(r=>r.id.StartsWith("H-",StringComparison.Ordinal)),Is.False);
        var after=new XgPublicDiscourseContext(6,20170613,"news",new[]{"news"});
        Assert.That(catalog.Get("H-01",after).kind,Is.EqualTo("historical"));
        Assert.That(catalog.Get("H-01",after).bodyZh,Does.Contain("2017-06-12"));
        Assert.That(catalog.Source("S07").url,Does.EndWith("1706.03762v1"));
    }
    [Test] public void EverySpecificReleaseConsequenceRequiresAnArtifactAndPublicEvent()
    {
        var catalog=Load();
        foreach(var record in catalog.All.Where(r=>r.route=="online"||r.route=="public_model"))
        {
            Assert.That(record.requiresArtifact,Is.True,record.id);Assert.That(record.requiredPublicEventIds,Is.Not.Empty,record.id);
            var missing=new XgPublicDiscourseContext(6,20170613,record.platform,new[]{record.platform},record.route,"artifact");
            Assert.That(catalog.Visible(record,missing),Is.False,record.id);
            var ready=new XgPublicDiscourseContext(6,20170613,record.platform,new[]{record.platform},record.route,"artifact",record.requiredPublicEventIds);
            Assert.That(catalog.Visible(record,ready),Is.True,record.id);
        }
    }
    [Test] public void BrowserExcerptsDoNotPretendTheNativePlatformIsInstalled()
    {
        var catalog=Load();var browser=new XgPublicDiscourseContext(1,20160601,"news",new[]{"news"});
        var unavailable=new XgPublicDiscourseContext(1,20160601,"miaoyu",new[]{"news"});
        Assert.That(catalog.Available(unavailable),Is.Empty);
        Assert.That(catalog.BrowserArchive(browser,"miaoyu").Select(r=>r.id),Is.EquivalentTo(new[]{"D1-M"}));
        Assert.That(browser.Installed("miaoyu"),Is.False);
        Assert.That(catalog.BrowserArchive(new XgPublicDiscourseContext(6,20170613,"news",Array.Empty<string>()),"yy"),Is.Empty);
    }
    [Test] public void LocalOnlyPublicRecordsContainNoLocalReleaseNote()
    {
        var records=Load().All.Where(r=>r.route=="local_only").ToArray();
        foreach(var row in records)
        {
            Assert.That(row.requiresArtifact,Is.False);
            Assert.That(row.bodyZh,Does.Not.Contain("发布入口仍未启用").And.Not.Contain("无人知晓的决定").And.Not.Contain("文件留在本机"));
            Assert.That(row.bodyEn,Does.Not.Contain("release endpoint").And.Not.Contain("decision nobody knows"));
        }
        Assert.That(records.Single(r=>r.id=="R4-W").scope,Is.EqualTo("public_sample"));
    }
    [Test] public void ExistingNewsTopicsAreLinkedRatherThanRepublishedAsNewHeadlines()
    {
        var catalog=Load();
        Assert.That(catalog.All.Single(r=>r.id=="D1-B").legacyNewsIds,Contains.Item("lz2_t01_alphago"));
        Assert.That(catalog.All.Single(r=>r.id=="D5-B").legacyNewsIds,Contains.Item("n0928"));
        var state=new XgPublicDiscourseDeliveryState();XgPublicDiscourseSchedule.Initialize(state,5,true);
        for(int i=0;i<70;i++)XgPublicDiscourseSchedule.Advance(state,5,1,true,false);
        var context=new XgPublicDiscourseContext(5,20161001,"news",new[]{"news"});
        Assert.That(XgPublicDiscourseSchedule.Eligible(state,catalog.All.Single(r=>r.id=="D5-B"),context,catalog),Is.False);
    }
    static void Tick(XgPublicDiscourseDeliveryState state,int stage,int seconds)
    {for(int i=0;i<seconds;i++)XgPublicDiscourseSchedule.Advance(state,stage,1,true,false);}
    [Test] public void CrossStageDeliveryHonorsGlobalAndRollingWindowLimits()
    {
        var catalog=Load();var state=new XgPublicDiscourseDeliveryState();
        var first=catalog.All.Single(r=>r.id=="D2-F");var second=catalog.All.Single(r=>r.id=="D3-B");var third=catalog.All.Single(r=>r.id=="D4-Y");
        var a=new XgPublicDiscourseContext(2,20161231,"forum",new[]{"forum"});
        var b=new XgPublicDiscourseContext(3,20161231,"news",new[]{"news"});
        var c=new XgPublicDiscourseContext(4,20161231,"yy",new[]{"yy"});
        XgPublicDiscourseSchedule.Initialize(state,2,true);Tick(state,2,60);
        Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,first,a,catalog),Is.True);
        Tick(state,3,179);Assert.That(XgPublicDiscourseSchedule.Eligible(state,second,b,catalog),Is.False);
        Tick(state,3,1);Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,second,b,catalog),Is.True);
        Tick(state,4,419);Assert.That(XgPublicDiscourseSchedule.Eligible(state,third,c,catalog),Is.False,"two prompts remain in the 600-second window");
        Tick(state,4,1);Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,third,c,catalog),Is.True);
    }
    [Test] public void RepeatedPlatformWaitsThreeHundredSafeSecondsAcrossStages()
    {
        var catalog=Load();var state=new XgPublicDiscourseDeliveryState();
        var first=catalog.All.Single(r=>r.id=="D2-F");var second=catalog.All.Single(r=>r.id=="D3-F");
        var a=new XgPublicDiscourseContext(2,20161231,"forum",new[]{"forum"});
        var b=new XgPublicDiscourseContext(3,20161231,"forum",new[]{"forum"});
        XgPublicDiscourseSchedule.Initialize(state,2,true);Tick(state,2,60);
        Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,first,a,catalog),Is.True);
        Tick(state,3,299);Assert.That(XgPublicDiscourseSchedule.Eligible(state,second,b,catalog),Is.False);
        Tick(state,3,1);Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,second,b,catalog),Is.True);
    }
}
