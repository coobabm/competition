using System;
using System.Linq;
using LingGuangV05.XingGuang;
using NUnit.Framework;

public sealed class XgPublicDiscourseTests
{
    // Synthetic catalog fixtures, never shipped as story text.
    const string Row="{\"id\":\"sample\",\"stage\":2,\"since\":20160622,\"platform\":\"forum\",\"route\":\"\",\"kind\":\"historical\",\"requiresArtifact\":false,\"sourceIds\":[\"paper\"],\"requiredPublicEventIds\":[],\"titleZh\":\"测试\",\"titleEn\":\"Test\",\"bodyZh\":\"测试正文\",\"bodyEn\":\"Test body\"}";
    static string Json(string row=Row)=>"{\"sources\":[{\"id\":\"paper\",\"title\":\"Test source\",\"since\":20160622,\"url\":\"https://example.invalid/source\"}],\"records\":["+row+"]}";
    static XgPublicDiscourseContext Context(int stage=2,int date=20160622,string platform="forum",bool installed=true,string route="",string artifact="",string[] events=null)
        =>new XgPublicDiscourseContext(stage,date,platform,installed?new[]{platform}:Array.Empty<string>(),route,artifact,events);

    [Test] public void StageDateAndInstalledPlatformAreIndependentGates()
    {
        var catalog=XgPublicDiscourseCatalog.Load(Json());
        Assert.That(catalog.Available(Context()).Count,Is.EqualTo(1));
        Assert.That(catalog.Available(Context(stage:1)),Is.Empty);
        Assert.That(catalog.Available(Context(date:20160621)),Is.Empty);
        Assert.That(catalog.Available(Context(platform:"news")),Is.Empty);
        Assert.That(catalog.Available(Context(installed:false)),Is.Empty);
        Assert.That(catalog.Available(Context(stage:6,date:20170613)).Single().id,Is.EqualTo("sample"),"archives remain voluntarily readable");
    }
    [Test] public void RouteDoesNotInventAnArtifactOrPublicExperience()
    {
        var row=Row.Replace("\"route\":\"\"","\"route\":\"online\"").Replace("\"requiresArtifact\":false","\"requiresArtifact\":true")
            .Replace("\"requiredPublicEventIds\":[]","\"requiredPublicEventIds\":[\"test.service.used\"]");
        var catalog=XgPublicDiscourseCatalog.Load(Json(row));
        Assert.That(catalog.Available(Context(route:"online")),Is.Empty);
        Assert.That(catalog.Available(Context(route:"online",artifact:"public-1")),Is.Empty);
        Assert.That(catalog.Available(Context(route:"local_only",artifact:"public-1",events:new[]{"test.service.used"})),Is.Empty);
        Assert.That(catalog.Available(Context(route:"online",artifact:"public-1",events:new[]{"test.service.used"})).Count,Is.EqualTo(1));
    }
    [Test] public void PublicProjectionCopiesEvidenceCollections()
    {
        var installed=new[]{"forum"};var events=new[]{"receipt"};
        var context=new XgPublicDiscourseContext(2,20160622,"forum",installed,publicEventIds:events);
        installed[0]="yy";events[0]="other";
        Assert.That(context.Installed("forum"),Is.True);Assert.That(context.HasPublicEvent("receipt"),Is.True);
    }
    [Test] public void LoaderRejectsMissingTranslationFutureSourcesAndInvalidLocalPublicClaims()
    {
        Assert.Throws<FormatException>(()=>XgPublicDiscourseCatalog.Load(Json(Row.Replace("\"bodyEn\":\"Test body\"","\"bodyEn\":\"\""))));
        Assert.Throws<FormatException>(()=>XgPublicDiscourseCatalog.Load(Json().Replace("\"since\":20160622,\"url\"","\"since\":20160623,\"url\"")));
        Assert.Throws<FormatException>(()=>XgPublicDiscourseCatalog.Load(Json(Row.Replace("\"route\":\"\"","\"route\":\"local_only\"").Replace("\"requiresArtifact\":false","\"requiresArtifact\":true"))));
        Assert.Throws<FormatException>(()=>XgPublicDiscourseCatalog.Load(Json(Row+","+Row)));
    }
    [Test] public void LateSaveLoadsArchivesWithoutBacklogNotifications()
    {
        var state=new XgPublicDiscourseDeliveryState();var catalog=XgPublicDiscourseCatalog.Load(Json());var context=Context();
        XgPublicDiscourseSchedule.Initialize(state,2,false);
        for(int i=0;i<1000;i++)XgPublicDiscourseSchedule.Advance(state,2,1,true,false);
        Assert.That(XgPublicDiscourseSchedule.Eligible(state,catalog.All[0],context,catalog),Is.False);
        Assert.That(catalog.Available(context).Count,Is.EqualTo(1));
    }
    [Test] public void SafeForegroundTimeAndSuccessfulReceiptControlOneStagePrompt()
    {
        var state=new XgPublicDiscourseDeliveryState();var catalog=XgPublicDiscourseCatalog.Load(Json());var context=Context();var row=catalog.All[0];
        XgPublicDiscourseSchedule.Initialize(state,2,true);
        XgPublicDiscourseSchedule.Advance(state,2,3600,false,false);
        Assert.That(state.safeSeconds,Is.EqualTo(0));
        for(int i=0;i<59;i++)XgPublicDiscourseSchedule.Advance(state,2,1,true,false);
        Assert.That(XgPublicDiscourseSchedule.Eligible(state,row,context,catalog),Is.False);
        XgPublicDiscourseSchedule.Advance(state,2,1,true,false);
        Assert.That(XgPublicDiscourseSchedule.Eligible(state,row,context,catalog),Is.True);
        XgPublicDiscourseSchedule.Advance(state,2,1,true,true);
        Assert.That(XgPublicDiscourseSchedule.Eligible(state,row,context,catalog),Is.False);
        for(int i=0;i<30;i++)XgPublicDiscourseSchedule.Advance(state,2,1,true,false);
        Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,row,context,catalog),Is.True);
        Assert.That(XgPublicDiscourseSchedule.RecordDelivered(state,row,context,catalog),Is.False);
        Assert.That(state.notifiedIds,Is.EquivalentTo(new[]{"sample"}));
    }
}
