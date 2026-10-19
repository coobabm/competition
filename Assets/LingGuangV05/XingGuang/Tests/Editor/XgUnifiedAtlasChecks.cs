using System;
using System.Collections.Generic;
using System.Linq;

namespace LingGuangV05.XingGuang.Tests
{
    // Also run by the isolated .NET harness with its own serializer. No Unity scene or runtime required.
    public static class XgUnifiedAtlasChecks
    {
        public static int Run(Func<XgState,string> serialize, Func<string,XgState> deserialize)
        {
            int checks=0;
            Action<bool,string> need=(ok,message)=>{checks++;if(!ok)throw new InvalidOperationException(message);};
            var sim=new XgSim();sim.S.teachingFlow=true;sim.S.stage=6;sim.S.abilitiesEmerged=new List<int>{1};
            need(!XgUnifiedAtlasCatalog.CanReplay(sim,1),"New saves require the purchased viewer even for ability 1");
            need(!XgUnifiedAtlasCatalog.AbilityOpen(sim,6),"A stage fixture alone must not grant learned abilities");
            need(!XgUnifiedAtlasCatalog.CanReplay(sim,0)&&!XgUnifiedAtlasCatalog.CanReplay(sim,7),"Invalid stage IDs are rejected");
            sim.S.unlocked.Add(XgUnifiedAtlasCatalog.ViewerNode);
            need(XgUnifiedAtlasCatalog.CanReplay(sim,1),"Purchased viewer opens an already learned ability");
            need(!XgUnifiedAtlasCatalog.CanReplay(sim,2),"Viewer purchase never grants new abilities");
            sim.S.abilitiesEmerged.Add(2);
            need(XgUnifiedAtlasCatalog.CanReplay(sim,2),"Both rights permit stage 2");
            var fresh=new XgSim();fresh.S.teachingFlow=true;
            need(!XgUnifiedAtlasCatalog.CanReplay(fresh,2),"A second save inherits no rights");
            fresh.S.teachingFlow=false;
            need(XgUnifiedAtlasCatalog.CanReplay(fresh,1),"Existing legacy viewer entitlement is retained");
            need(!XgUnifiedAtlasCatalog.CanReplay(fresh,6),"Legacy viewer still cannot unlock future abilities");
            need(!XgUnifiedAtlasCatalog.ViewerOpen(null)&&!XgUnifiedAtlasCatalog.CanReplay(null,1),"Missing save grants no rights");
            sim.S.unlocked.Remove(XgUnifiedAtlasCatalog.ViewerNode);
            need(!XgUnifiedAtlasCatalog.CanReplay(sim,2),"Revoking viewer rights immediately seals an already learned ability");
            sim.S.unlocked.Add(XgUnifiedAtlasCatalog.ViewerNode);sim.S.abilitiesEmerged.Remove(2);
            need(!XgUnifiedAtlasCatalog.CanReplay(sim,2),"Revoking learned ability immediately seals its replay despite viewer purchase");
            sim.S.abilitiesEmerged.Add(2);
            var sparse=new XgSim();sparse.S.stage=6;sparse.S.teachingFlow=true;sparse.S.abilitiesEmerged=null;
            sparse.S.unlocked.Add(XgUnifiedAtlasCatalog.ViewerNode);
            need(XgUnifiedAtlasCatalog.CanReplay(sparse,1)&&!XgUnifiedAtlasCatalog.CanReplay(sparse,6),"Null legacy ability history does not fabricate learned stages");

            sim.S.lessonCollected.Clear();sim.S.lessonDiscovered.Clear();sim.S.quizAnswered.Clear();sim.S.phenomena.seen.Clear();
            Func<string,XgAtlasEntry> entry=id=>XgUnifiedAtlasCatalog.Entries(sim,XgAtlasCategory.Phenomena,false).Find(x=>x.id==id);
            need(!entry("lesson.overfit").unlocked,"Uncollected knowledge stays sealed");
            need(!entry("lesson.overfit").body.Contains(XgTrainingLessons.Card("overfit").body),"A sealed card must not leak its explanation");
            sim.S.lessonDiscovered.Add("overfit");
            need(!entry("lesson.overfit").unlocked,"Discovery pending collection does not grant collection rights");
            sim.S.lessonCollected.Add("overfit");sim.S.quizAnswered.Add("q1a");sim.S.phenomena.seen.Add("xor");
            need(entry("lesson.overfit").unlocked && entry("quiz.q1a").unlocked && entry("ph.xor").unlocked,"All three original knowledge sources remain accessible");
            need(!entry("quiz.q1b").unlocked,"Unanswered quiz stays sealed");
            sim.S.unlocked.Remove("transformer");
            need(!XgUnifiedAtlasCatalog.NetworkOpen(sim,"transformer"),"Future architecture is not freely unlocked");
            sim.S.unlocked.Add("transformer");
            need(XgUnifiedAtlasCatalog.NetworkOpen(sim,"transformer"),"Purchased architecture is retained");
            sim.Run(XgTrack.Vision).arch="lenet";sim.S.unlocked.Remove("lenet");
            need(XgUnifiedAtlasCatalog.NetworkOpen(sim,"lenet"),"A legacy run preserves the existing topology entitlement");
            need(!XgUnifiedAtlasCatalog.NetworkOpen(sim,"unknown"),"Unknown architecture rejected");
            foreach(bool en in new[]{false,true})
            {
                var sealedSim=new XgSim();sealedSim.S.teachingFlow=true;sealedSim.S.stage=6;
                var sealedCards=XgUnifiedAtlasCatalog.Entries(sealedSim,XgAtlasCategory.Phenomena,en);
                need(sealedCards.Where(x=>!x.unlocked).All(x=>x.title==(en?"Not collected":"尚未收录")),"Uncollected titles remain sealed in both languages");
                var sealedNets=XgUnifiedAtlasCatalog.Entries(sealedSim,XgAtlasCategory.Networks,en);
                need(sealedNets.Where(x=>!x.unlocked).All(x=>x.title==(en?"Locked network":"未解锁结构")&&!x.body.Contains(en?"Attention":"注意力")),"Unowned networks do not leak diagram explanations");
            }

            sim.S.lessonAtlasUnread=true;sim.S.stagePresentations=new List<int>{2};
            var before=serialize(sim.S);int events=0;
            sim.StageAdvanced+=_=>events++;sim.Emerged+=(_,__)=>events++;sim.NodeBought+=_=>events++;
            for(int pass=0;pass<3;pass++)foreach(bool english in new[]{false,true})foreach(XgAtlasCategory category in Enum.GetValues(typeof(XgAtlasCategory)))
            {
                var entries=XgUnifiedAtlasCatalog.Entries(sim,category,english);
                need(entries.Count>0,"All three categories have entries in both languages");
                need(entries.All(x=>!string.IsNullOrWhiteSpace(x.title)&&!string.IsNullOrWhiteSpace(x.body)),"Every locked/unlocked entry has localized text");
                if(category==XgAtlasCategory.Abilities)need(entries.Count==6,"Six ability entries remain stable");
                foreach(var item in entries)if(item.category==XgAtlasCategory.Abilities)XgUnifiedAtlasCatalog.CanReplay(sim,item.stage);
            }
            foreach(var lesson in new[]{XgTinyLesson.XorPerceptron,XgTinyLesson.XorMlp,XgTinyLesson.Zero})
            {
                var first=new XgTinyRun(lesson,XgTinyRun.Seed).Finish();var second=new XgTinyRun(lesson,XgTinyRun.Seed).Finish();
                need(first.Done && second.Done,"Independent replay finishes its tiny computation");
                need(first.loss.SequenceEqual(second.loss),"Fixed-seed replay has deterministic computed values");
            }
            need(events==0,"Reading and replay emit no ability, emergence or purchase events");
            need(before==serialize(sim.S),"Every saved field, including pending stage and unread collection, is unchanged");
            var restored=new XgSim(deserialize(before));var restoredBefore=serialize(restored.S);
            need(XgUnifiedAtlasCatalog.CanReplay(restored,2),"Purchased and learned rights survive JSON round-trip");
            need(XgUnifiedAtlasCatalog.Entries(restored,XgAtlasCategory.Phenomena,false).Find(x=>x.id=="lesson.overfit").unlocked,"Collected card survives round-trip");
            need(XgUnifiedAtlasCatalog.NetworkOpen(restored,"transformer"),"Purchased network survives round-trip");
            need(restoredBefore==serialize(restored.S),"Reading restored collection is also read-only");
            return checks;
        }
#if UNITY_EDITOR
        [NUnit.Framework.Test]
        public static void UnityRoundTripAndReadOnlyReplay()
        {
            NUnit.Framework.Assert.Greater(Run(s=>UnityEngine.JsonUtility.ToJson(s),s=>UnityEngine.JsonUtility.FromJson<XgState>(s)),50);
        }
#endif
    }
}
