using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using LingGuangV05.Core.Story;
using LingGuangV05.Core.Forum;
using LingGuangV05.Runtime.Story;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    /// <summary>Earned social feedback and an explicit-approval commission; no real model API or disk file writes.</summary>
    public sealed class XgEraAdvantageBridge : MonoBehaviour
    {
        static string T(string zh,string en)=>LingGuangV05.Runtime.GameText.T(zh,en);
        XingGuangController controller;
        StoryDirector director;
        XgSim bound;
        StoryState state;
        XgEraDeliveryDesk desk;
        float poll;
        string factKey="";
        bool wasCelebrating;
        public StoryState State => state;
        public int Stage => bound!=null?bound.S.stage:0;
        public bool Ended => bound!=null && (bound.S.chapterComplete || !string.IsNullOrEmpty(bound.S.ending));
        bool Live => controller!=null && controller.runtime!=null && controller.runtime.Sim!=null && !controller.runtime.Sim.InPrologue && !Ended;
        public bool CanBeginBatch => Live && state!=null && state.HasFired("era_delivery_brief") && TranslationReady;
        bool TranslationReady => bound!=null && bound.S.stageSequence>=5 && bound.BestAcc("translate")>=.6;
        bool Flag(string key) => state!=null && state.GetFlag(key)>0;
        public string WorkStatus
        {
            get
            {
                if(state==null)return T("等待工作台初始化","Waiting for the workbench to start up");
                if(Flag("era.batch.delivered"))return T("已由你确认交付；回执保存在文件夹。","Delivered with your approval; the receipt is saved in the folder.");
                if(Flag("era.batch.ready"))return T("初稿已经准备好，还没有替你发送。","The drafts are ready. Nothing has been sent for you.");
                if(!Flag("era.batch.started"))return T("先接下这批，再把本体接到「亲友委托」。","Take this batch first, then wire the model to the family commission.");
                if(controller.Host.Blocker!=null)return controller.Host.Blocker;
                var task=bound.Wiring.Node(XgSim.EraDeliveryTask);
                return task!=null && task.state==XgTaskState.Run?T("正在用已接入的电力和算力处理，断线会暂停。","Working on the power and compute you connected; it pauses if the line drops."):T("等待接线：本体 → 亲友委托 · 商品文案交付","Waiting for wiring: the model to the family commission, product copy");
            }
        }
        public void Bind(XingGuangController owner)
        {
            controller=owner; director=GetComponent<StoryDirector>();
            if(desk==null){desk=GetComponent<XgEraDeliveryDesk>();if(desk==null)desk=gameObject.AddComponent<XgEraDeliveryDesk>();desk.Bind(this);}
        }
        void Rebind()
        {

            bound=controller.Sim;state=controller.runtime.Sim.S.story;state.Repair();

            var ownerState=state;
            bound.HookLife(XgSim.EraDeliveryTask,
                ()=>ReferenceEquals(state,ownerState)&&Live&&TranslationReady&&ownerState.HasFired("era_delivery_brief"),
                ()=>ReferenceEquals(state,ownerState)&&ownerState.GetFlag("era.batch.started")>0&&ownerState.GetFlag("era.batch.on")>0&&ownerState.GetFlag("era.batch.ready")==0,
                on=>{if(!ReferenceEquals(state,ownerState))return;if(on&&(!CanBeginBatch||!Flag("era.batch.started")||Flag("era.batch.ready")))return;ownerState.SetFlag("era.batch.on",on?1:0);Changed();});
            factKey="";poll=0;
        }

        void Update()
        {
            if(controller==null||controller.Sim==null||controller.runtime==null||controller.runtime.Sim==null)return;
            if(!ReferenceEquals(bound,controller.Sim)||!ReferenceEquals(state,controller.runtime.Sim.S.story))Rebind();
            if(director==null)director=GetComponent<StoryDirector>();
            if(director!=null && director.Context!=null) director.Context.SetExternal("era.finished",Ended?1:0);
            if(!Live||director==null||!director.Active)return;
            if(Flag("era.batch.on")&&!Flag("era.batch.ready"))
            {
                var job=bound.Wiring.Node(XgSim.EraDeliveryTask);
                double rate=job!=null&&job.state==XgTaskState.Run&&job.model=="self"&&controller.Host.Blocker==null?job.rate:0;
                int made=EraAdvantageStory.AdvanceBatch(state,Math.Min(Time.unscaledDeltaTime,1),rate,controller.runtime.Sim.S.day,controller.runtime.Sim.S.gameSeconds);
                if(made>0){for(int i=0;i<made;i++)bound.NotifyEraDeliveryItem();controller.runtime.MarkDirty();if(Flag("era.batch.ready"))bound.RefreshWiring(controller.Host);}
            }
            poll-=Time.unscaledDeltaTime;if(poll>0)return;poll=.25f;
            PublishFacts();
        }
        void PublishFacts()
        {
            if(director.Context==null)return;
            int active=0;foreach(var n in bound.Wiring.nodes)if(n.lane==XgWireLane.Task&&n.state==XgTaskState.Run)active++;
            var ctx=director.Context;
            ctx.SetExternal("era.autoCorrect",bound.S.autoCorrect);
            ctx.SetExternal("era.activeJobs",active);
            ctx.SetExternal("era.translateReady",TranslationReady?1:0);
            if(state.HasFired("era_choose_work")&&!Flag("era.celebration.started"))
            {state.SetFlag("era.celebration.started",1);state.SetFlag("era.celebration.until",state.clock+30);controller.runtime.MarkDirty();}
            bool celebrating=Stage>=6&&Flag("era.batch.delivered")&&Flag("era.expert.passed")&&(!state.HasFired("era_choose_work")||state.clock<state.GetFlag("era.celebration.until"));
            ctx.SetExternal("era.celebrating",celebrating?1:0);
            wasCelebrating=celebrating;
            var key=new StringBuilder().Append(Stage).Append('/').Append((int)bound.S.autoCorrect).Append('/').Append(active).Append('/').Append(TranslationReady).Append('/').Append(celebrating);
            foreach(var f in state.flags)if(f.key.StartsWith("era.",StringComparison.Ordinal))key.Append(f.key).Append('=').Append(f.value).Append(';');
            key.Append(state.fired.Count);
            string next=key.ToString();if(next!=factKey){factKey=next;director.Emit("state");}
        }
        void Changed(){if(controller!=null&&controller.runtime!=null)controller.runtime.MarkDirty();poll=0;factKey="";if(director!=null)director.Emit("state");}
        public void EvaluateRequested()
        {
            if(!Live||state==null)return;
            bool expert=Flag("era.expert.requested")&&!Flag("era.expert.passed")&&state.HasFired("era_expert_invite");
            bool challenge=Flag("era.challenge.requested")&&!Flag("era.challenge.passed");
            if(!expert&&!challenge)return;
            string dataset=expert?"translate":state.GetFirst("era.challenge.request")?.text;
            int attempt=(int)state.GetFlag("era.exam.attempt");
            var seen=new List<string>();
            foreach(var entry in state.firsts)if(entry.id.StartsWith("era.exam.signatures.",StringComparison.Ordinal))seen.AddRange(entry.text.Split('\n'));
            var result=bound.RunEraFreshExam(dataset,attempt+1,controller.Host,seen);
            state.SetFlag("era.exam.attempt",attempt+1);
            state.RecordFirst(new StoryFirst{id="era.exam.result."+attempt,text=result.transcript??result.reason,value=result.accuracy});
            if(result.valid)
            {
                state.RecordFirst(new StoryFirst{id="era.exam.signatures."+attempt,text=string.Join("\n",result.signatures)});
                EraAdvantageStory.RecordExam(state,dataset,result.right,result.count,result.transcript,true);
            }
            else if(controller.View!=null)controller.View.ShowToast(result.reason,5);
            Changed();
        }
        public string LastExamText
        {
            get{int n=state!=null?(int)state.GetFlag("era.exam.attempt")-1:-1;return n>=0?state.GetFirst("era.exam.result."+n)?.text??"":"";}
        }
        public string ReadText(string name)
        {
            if(state==null||bound==null)return null;
            switch(name)
            {
                case "eraIdentityReply":return Flag("era.identity.reply")?(state.GetFlag("era.identity.reply")==1?T("就我一个。","Just me."):T("我自己慢慢做的。","I did it myself, slowly.")):T("我还没有回答。","I have not answered yet.");
                case "eraContextOutcome":return state.GetFlag("era.context.choice")==1?T("这份草稿只用了刚才给出的库存信息，没有把周五到货说成明天发货。是我看过以后，才按下发送。","The draft used only the stock information just given and did not turn a Friday arrival into tomorrow's shipment. I read it first and only then pressed send."):T("我选择这次自己回。草稿留下了，它没有替我发送，也没有替我作出承诺。","I chose to answer myself this time. The draft stays; it did not send anything or promise anything for me.");
                case "eraExpertResult":var proof=state.GetFirst("era.expert.receipt");return proof==null?T("还没有新的验收记录。","No new check on record yet."):T("这一次的20道独立翻译题，答对了"+Math.Round(proof.value*20).ToString(CultureInfo.InvariantCulture)+"道。题目和它的答案都留在文件夹里。","It got "+Math.Round(proof.value*20).ToString(CultureInfo.InvariantCulture)+" of this round's 20 fresh translation questions right. The questions and its answers are kept in the folder.");
                case "eraDeliverySummary":return T("20份商品英文页已完成；缺少单位和交期的两条另列，没有混入交付。","All 20 English product pages are done; the two missing units and delivery dates are listed apart and not mixed into the delivery.");
                case "eraRescueSummary":return T("自动处理成功的记录已经有 "+((int)bound.S.autoCorrect).ToString(CultureInfo.InvariantCulture)+" 条。不是按钮上的演示，是它刚刚完成的活。","It has already handled "+((int)bound.S.autoCorrect).ToString(CultureInfo.InvariantCulture)+" cases on its own. Not a demo on a button: work it just finished.");
                case "eraTeamSummary":int n=0;foreach(var t in bound.Wiring.nodes)if(t.lane==XgWireLane.Task&&t.state==XgTaskState.Run)n++;return T(n+" 项任务正在同一台电脑上运行。钱、电、模型和接线都记在同一本账里。",n+" jobs are running on the same computer. Money, power, models and wiring all sit in the same account book.");
                default:return null;
            }
        }
        public void BeginBatch()
        {
            if(EraAdvantageStory.BeginBatch(state,CanBeginBatch)){Changed();OpenWiring();}
        }
        public void ConfirmDelivery()
        {
            if(!CanSendToCousin()||!EraAdvantageStory.ConfirmDelivery(state))return;
            PlayerLine("cousin",T("这批20份先交给你。有两处资料不全，我单独列出来了，没有乱填。","Here are the first 20. Two are missing information; I listed them separately and did not fill in guesses."));Changed();
        }
        public void ChooseIdentity(int choice)
        {
            if(!CanSendToCousin()||!EraAdvantageStory.ChooseIdentity(state,choice))return;
            PlayerLine("cousin",choice==1?T("就我一个。","Just me."):T("我自己慢慢做的。","I did it myself, slowly."));Changed();
        }
        public void ChooseContext(int choice)
        {
            if(!Live || (choice==1&&!CanSendToCousin()))return;
            if(!EraAdvantageStory.ChooseContext(state,choice))return;
            if(choice==1)PlayerLine("cousin",EraAdvantageStory.Draft);
            Changed();
        }
        public void RequestChallenge()
        {
            if(!Live)return;
            string dataset=bound.Run((XgTrack)bound.S.selected).dataset;
            if(EraAdvantageStory.RequestChallenge(state,Stage,dataset))Changed();
        }
        public void RequestExpert()
        {
            if(!Live)return;
            var forum=LingGuangV05.Desktop.Tieba.TiebaHub.Instance;
            if(forum==null||!forum.CanMessage(ForumLibrary.ZhouNow)||!EraAdvantageStory.RequestExpert(state,Stage))return;
            forum.SendAuthoredChoice(ForumLibrary.ZhouNow,T("你好，我刚交完一批商品英文页。想请你看看译文，能换一组新的材料吗？我再做一遍，拿不准的也标出来。","Hi, I have just handed in a batch of English product pages. Could you look over the translations and give me a fresh set of material? I will do it again and mark the parts I am unsure about."));Changed();
            if(forum.View!=null)forum.View.Open("chat", ForumLibrary.ZhouNow);
        }
        bool CanSendToCousin()
        {
            var yy=LingGuangV05.Desktop.YY.YYChatHub.Instance;
            bool ready=Live&&yy!=null&&yy.S!=null&&yy.Conversation("cousin")!=null;
            if(!ready&&controller!=null&&controller.View!=null)controller.View.ShowToast(T("YY 还没有就绪，内容留在文件夹，暂不发送。","YY is not ready yet; the content stays in the folder and nothing is sent."),4);
            return ready;
        }
        static void PlayerLine(string contact,string text){var yy=LingGuangV05.Desktop.YY.YYChatHub.Instance;if(yy!=null)yy.SendAuthoredChoice(contact,text);}
        public void OpenWiring(){if(controller!=null){controller.Open();controller.View?.ShowTab("wiring");controller.View?.Wiring.FocusTask(XgSim.EraDeliveryTask);}}
        public void OpenTraining()
        {
            if(controller==null)return;
            if(Flag("era.expert.requested")&&!Flag("era.expert.passed"))bound.S.selected=(int)XgTrack.Sequence;
            controller.Open();controller.View?.ShowTab("train");
            if(Flag("era.expert.requested")&&!Flag("era.expert.passed"))controller.View?.ShowToast(T("检验目标是翻译。请在序列训练页选择翻译数据，再回文件夹运行新题验收。","The check is on translation. Pick the translation data on the sequence training page, then come back to the folder and run the fresh-question check."),6);
        }
    }
}
