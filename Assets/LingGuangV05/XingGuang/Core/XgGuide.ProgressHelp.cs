using System;
using System.Collections.Generic;

namespace LingGuangV05.XingGuang
{
    public sealed partial class XgState
    {
        public bool parameterLessonStarted, sampleLessonStarted, parameterLessonDone, sampleLessonDone;
        public double parameterLessonBaseline, sampleLessonBaseline;
        public int progressLessonVersion;
    }

    public sealed class XgProgressHelp
    {
        public XgGuideStep step;
        public string firstZh, firstEn, secondZh, secondEn;
        public string Text(bool english) => (english ? firstEn : firstZh) + "\n" + (english ? secondEn : secondZh);
    }

    public static partial class XgGuide
    {
        public const string ParameterLessonId = "explain.parameters", SampleLessonId = "explain.samples";

        // The explanation and click destination are one immutable snapshot of the guide's plan.
        public static XgProgressHelp ProgressHelp(XgSim sim, double money, double vramMB, bool parameters)
        {
            var h = new XgProgressHelp();
            if (sim == null) return h;
            var wallet = new Wallet { money = money, vram = vramMB };
            var run = sim.Run(TrainableTrack(sim));
            void Say(string a, string ae, string b, string be) { h.firstZh=a;h.firstEn=ae;h.secondZh=b;h.secondEn=be; }
            XgGuideStep Training() => Step("progress.train", XgGuideKind.Ability, "练到 C 级", "Train to grade C", "", "", Lab, "train", "label:训练一轮|Train 1 epoch", TrackArg(TrainableTrack(sim)));
            XgGuideStep Teaching() => Step("progress.label", XgGuideKind.Ability, "去教页答题", "Answer in Teach", "", "", Lab, "teach", "name:Paper", LabelDesk(sim)?.id ?? "");
            if (sim.S.stage >= 6 || sim.NextAbility == 0)
            {
                var steps = new List<XgGuideStep>(); ScaleItemSteps(sim, wallet, steps);
                h.step = steps.Count > 0 ? steps[0] : Step("progress.final", XgGuideKind.Finale, "前往终章", "Open Finale", "", "", Lab, "final", "", "");
                if (steps.Count > 0) Say("购买当前缺少的科技加成。", "Buy the missing technology upgrade.", "参数、样本和科技加成共同提升规模。", "Parameters, samples and technology raise effective scale.");
                else Say("继续增加有效参数和训练样本。", "Keep growing effective parameters and training samples.", "规模达标后，去终章页预训练。", "When ready, start pre-training in Finale.");
                return h;
            }
            if (ChapterWaits(sim) && TabOpen(sim, Lab, "train"))
            {
                h.step=ConfirmStage(sim);
                if(h.step.app!=Lab||h.step.tab!="train")
                    Say("先完成当前阶段的剧情任务。","Finish the current chapter's story task first.","完成后再返回练页参加能力考。","Then return to Train for the ability exam.");
                else Say("已可参加能力考，去训练页试试。", "The ability exam is available in Train.", "通过考试后确认进入下一阶段。", "Pass the exam, then confirm the next stage."); return h;
            }
            int next=sim.NextAbility;
            bool parametersReady=sim.ParamsLineKnown(next), samplesReady=sim.SamplesLineKnown(next);
            // Test both first: the one-ready branches delegate to each other, never recursively when both are ready.
            if(parametersReady&&samplesReady)
            {
                bool trainOpen=TabOpen(sim,Lab,"train");h.step=trainOpen?Training():Teaching();
                Say("参数和样本都已达标。","Parameters and samples are both ready.",
                    trainOpen?"回练页继续训练，等待能力考开放。":"先完成教页引导，再进入训练。",
                    trainOpen?"Continue training while the ability exam becomes available.":"Finish the teaching introduction to open Train.");return h;
            }
            if (parameters && parametersReady)
            {
                h=ProgressHelp(sim,money,vramMB,false);
                h.firstZh="有效参数已达标，接下来积累样本。";h.firstEn="Effective parameters are ready; gather samples next.";
                return h;
            }
            if(!parameters&&samplesReady)
            {
                h=ProgressHelp(sim,money,vramMB,true);
                h.firstZh="有效样本已达标，接下来提升参数。";h.firstEn="Effective samples are ready; grow parameters next.";
                return h;
            }
            if (parameters && sim.CountState(run)!=XgCountState.Counted && XgSim.ParamsK(run)>sim.TrainedParamsK)
            {
                if(!TabOpen(sim,Lab,"train"))
                {
                    h.step=Teaching();Say("先在教页答题，开放训练入口。","Answer in Teach to unlock training.",
                        "开放后把模型练到 C 级，计入参数。","Then train to grade C to count its parameters.");return h;
                }
                h.step=Training();
                Say("模型已有 "+XgSim.ParamsText(XgSim.ParamsK(run))+"，练到 C 级计入。", "The model has "+XgSim.ParamsText(XgSim.ParamsK(run))+" parameters; train to C to count them.",
                    "训练后，新参数会计入顶部进度。", "Training adds the new parameters to the top progress bar.");return h;
            }
            h.step=!parameters?PendingDataImport(sim,wallet):null;
            if(!parameters&&h.step==null)
            {
                var clean=XgChanceHint.Step(sim);
                if(clean!=null&&clean.target=="name:CleanDatasetNoise"&&TabOpen(sim,clean.app,clean.tab))h.step=clean;
            }
            if(h.step==null)h.step=BarStepFor(sim,money,vramMB,parameters);
            if(h.step==null)h.step=parameters&&TabOpen(sim,Lab,"train")?Training():Teaching();
            if(h.step.id=="data.install")
                Say("有下载完成的数据包，先安装。", "A downloaded data pack is ready to install.", "从资料文件夹拖进练页的数据包安装区。", "Drag it from Training files into Data Pack Installation in Train.");
            else if(h.step.id=="ability.card")
                Say("显存不足，先到商城添加显卡。", "VRAM is limiting the model; add a graphics card in the shop.", "扩容后再训练到 C 级，计入新参数。", "Then train to grade C to count the larger model.");
            else if(h.step.tab=="items")
                Say("到道具页查看可用的模型结构。","Check available model structures in Items.",
                    "获得更大结构后，练到 C 级计入参数。","After getting a larger structure, train to grade C to count its parameters.");
            else if(h.step.id=="ability.batch")
                Say("购买下一批训练数据，补充有效样本。","Buy the next batch of training data for effective samples.",
                    "下载后，从资料文件夹拖进练页安装。","After downloading, drag it from Training files into Train to install.");
            else if(h.step.id=="broadband")
                Say("升级宽带，缩短数据包下载时间。","Upgrade broadband to shorten data downloads.",
                    "下载完成后仍需安装，才计入样本。","Install finished downloads for their samples to count.");
            else if(h.step.id=="ability.track")
                Say("到标注台为这条训练线积累样本。","Label examples for this training track.",
                    "样本够用后，回练页训练更大的模型。","Then return to Train to train the larger model.");
            else if(h.step.tab=="tree"&&h.step.target.StartsWith("node:",StringComparison.Ordinal))
            {
                var node=XgCatalog.Node(h.step.target.Substring(5));
                string name=node==null?"对应科技":sim.NodeName(node), en=node==null?"the upgrade":node.nameEn;
                Say("去科技购买「"+name+"」。", "Buy "+en+" in Tech.",
                    parameters?"购买后回练页，练到 C 级计入参数。":"下载的数据包需要拖进练页安装。",
                    parameters?"Then train to grade C to count the parameters.":"Downloaded packs must be dragged into Train to install.");
                if(node!=null)
                {h.firstZh="购买「"+name+"」¥"+sim.NodeCost(node).ToString("0")+"。";h.firstEn="Buy "+en+" for ¥"+sim.NodeCost(node).ToString("0")+".";}
                if(h.firstZh.Length>30)h.firstZh="去科技购买当前推荐节点。";
                if(parameters&&node!=null&&(node.kind==XgNodeKind.Width||node.kind==XgNodeKind.Depth))
                {var size=XgSim.ParamsText(sim.ParamsAfterNode(run,node,wallet));h.secondZh="模型可达 "+size+"，练到 C 级计入。";h.secondEn="The model can reach "+size+"; train to grade C to count it.";}
                if(parameters&&node!=null&&node.kind==XgNodeKind.Dataset)
                {h.secondZh="先安装所需数据，再回练页训练。";h.secondEn="Install the required data, then return to Train.";}
                if(!parameters&&node!=null&&node.kind!=XgNodeKind.Dataset)
                {h.secondZh="先解锁这个前置，再按提示积累样本。";h.secondEn="Unlock this prerequisite, then follow the sample guidance.";}
            }
            else if(h.step.tab=="tree")
                Say("到科技树查看网络扩容与结构升级。","Check network expansion and structures in Tech.",
                    "购买后回练页，练到 C 级计入参数。","Then train to grade C for the new parameters to count.");
            else if(h.step.target=="name:CleanDatasetNoise")
                Say("清理错误样本，减少噪声扣减。", "Clean incorrect samples to reduce the noise penalty.", "整理后，更多样本能计入有效进度。", "More samples will then count toward effective progress.");
            else if(parameters && h.step.tab=="train")
                Say("把模型训练到 C 级，参数才计入。", "Train the model to grade C for its parameters to count.",
                    sim.TeachingTreeOpen?"想容纳更多参数，可购买网络扩容科技。":"先在教页答题，逐步开放训练与科技。",
                    sim.TeachingTreeOpen?"Network expansion upgrades let the model hold more parameters.":"Answer in Teach to unlock training and technology.");
            else if(h.step.id.Contains("download"))
                Say("数据包正在下载，完成后再安装。", "A data pack is downloading; install it when ready.", "也可以继续答题积累样本。", "You can keep answering questions for samples meanwhile.");
            else
                Say(parameters?"先在教页答题，开放训练入口。":"答对题目可积累有效样本。",
                    parameters?"Answer in Teach to unlock training.":"Correct answers add effective samples.",
                    parameters?"开放后把模型练到 C 级，计入参数。":sim.CanInstallData?"安装已购买的数据包也能增加样本。":"继续答题，逐步开放更多训练功能。",
                    parameters?"Then train the model to grade C to count its parameters.":sim.CanInstallData?"Installing purchased data packs also adds samples.":"Keep answering to unlock more training features.");
            return h;
        }

        public static void ObserveProgressLessons(XgSim sim)
        {
            if(sim==null)return;var s=sim.S;
            if(s.parameterLessonStarted&&!s.parameterLessonDone&&sim.TrainedParamsK>s.parameterLessonBaseline+1e-9)
            {s.parameterLessonDone=true;s.progressLessonVersion++;}
            if(s.sampleLessonStarted&&!s.sampleLessonDone&&sim.TrainedSamples>s.sampleLessonBaseline+1e-9)
            {s.sampleLessonDone=true;s.progressLessonVersion++;}
        }

        public static bool CompleteProgressLesson(XgSim sim,string id)
        {
            if(sim==null)return false;
            if(id==ParameterLessonId&&!sim.S.parameterLessonDone){sim.S.parameterLessonDone=true;sim.S.progressLessonVersion++;return true;}
            if(id==SampleLessonId&&!sim.S.sampleLessonDone){sim.S.sampleLessonDone=true;sim.S.progressLessonVersion++;return true;}
            return false;
        }

        static XgGuideStep ProgressLesson(XgSim sim,double money,double vram)
        {
            ObserveProgressLessons(sim);
            if(sim.NextAbility==0||ChapterWaits(sim))return null;
            bool first=sim.ParamsProgress(sim.NextAbility)<=sim.SamplesProgress(sim.NextAbility);
            for(int i=0;i<2;i++)
            {
                bool p=i==0?first:!first;
                if(p?sim.S.parameterLessonDone||sim.ParamsProgress(sim.NextAbility)>=1:sim.S.sampleLessonDone||sim.SamplesProgress(sim.NextAbility)>=1)continue;
                var help=ProgressHelp(sim,money,vram,p);var destination=help.step;
                if(destination==null||!TabOpen(sim,destination.app,destination.tab))continue;
                if(p&&!sim.S.parameterLessonStarted){sim.S.parameterLessonStarted=true;sim.S.parameterLessonBaseline=sim.TrainedParamsK;sim.S.progressLessonVersion++;}
                if(!p&&!sim.S.sampleLessonStarted){sim.S.sampleLessonStarted=true;sim.S.sampleLessonBaseline=sim.TrainedSamples;sim.S.progressLessonVersion++;}
                return Step(p?ParameterLessonId:SampleLessonId,XgGuideKind.Ability,
                    p?"让模型容纳更多参数，再练到 C 级":"让它多见题目：答题或安装数据包",
                    p?"Expand the model, then train to grade C":"Show it more examples: answer or install data packs",
                    help.Text(false),help.Text(true),destination.app,destination.tab,destination.target,destination.arg);
            }
            return null;
        }
    }
}
