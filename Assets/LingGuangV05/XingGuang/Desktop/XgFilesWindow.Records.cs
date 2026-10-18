using System;
using System.Globalization;
using System.Linq;
using System.Text;
using LingGuangV05.XingGuang;
using UnityEngine;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgFilesWindow
    {
        string FolderTitle(string id)
        {
            switch(id){case "packs":return T("训练数据","Training data");case "notes":return T("标注记录","Labelled questions");case "images":return T("训练图片","Training images");case "models":return T("模型检查点","Model checkpoints");default:return id;}
        }
        string EmptyRecords=>T("当前存档没有这类记录。","No records of this kind in the current save.");
        string RecordsKey()
        {
            var asked=Sim.S.askedLog;
            return "|"+Sim.S.stage+":"+Sim.Has("train.checkpoint")+":"+Sim.S.teachingNoteSerial
                +":"+(asked==null?0:asked.Count)+":"+(asked==null||asked.Count==0?0:asked[asked.Count-1].at)
                +":"+string.Join(";",Sim.S.models.Select(m=>m.id+":"+m.name+":"+m.starred))
                +":"+string.Join(";",Sim.Runs.Select(r=>r.epoch+":"+r.dataset+":"+r.trainAcc+":"+r.valAcc+":"+r.histTrain.Count))
                +":"+(client!=null?client.rect.width:0);
        }
        string TrainingLog()
        {
            var b=new StringBuilder();bool any=false;
            foreach(var r in Sim.Runs)
            {
                if(r.epoch==0&&r.histTrain.Count==0&&r.histVal.Count==0)continue;
                any=true;var d=XgCatalog.Dataset(r.dataset);
                b.AppendLine(d==null?r.dataset:T(d.name,d.nameEn));
                b.Append(T("已训练轮数：","Completed rounds: ")).AppendLine(r.epoch.ToString());
                b.Append(T("训练准确率：","Training accuracy: ")).AppendLine(r.trainAcc.ToString("P1",CultureInfo.InvariantCulture));
                b.Append(T("验证准确率：","Validation accuracy: ")).AppendLine(r.valAcc.ToString("P1",CultureInfo.InvariantCulture));
                b.AppendLine(T("以下为存档保留的曲线采样（不是日期或轮次编号）：","Retained curve samples below (not dates or round numbers):"));
                b.AppendLine(T("采样,训练准确率,验证准确率","Sample,Training accuracy,Validation accuracy"));
                for(int i=0;i<Math.Max(r.histTrain.Count,r.histVal.Count);i++)
                    b.Append(i+1).Append(',').Append(i<r.histTrain.Count?r.histTrain[i].ToString("F4",CultureInfo.InvariantCulture):"")
                        .Append(',').AppendLine(i<r.histVal.Count?r.histVal[i].ToString("F4",CultureInfo.InvariantCulture):"");
                b.AppendLine();
            }
            return any?b.ToString():EmptyRecords;
        }
        static string Csv(string value)=>"\""+(value??"").Replace("\"","\"\"")+"\"";
        string AnswerCsv()
        {
            if(Sim.S.teachingNotebook==null||Sim.S.teachingNotebook.Count==0)return EmptyRecords;
            var b=new StringBuilder(T("记录ID,题目,玩家答案,题目标签,AI答案\n","Record ID,Question,Player answer,Question label,AI answer\n"));
            foreach(var n in Sim.S.teachingNotebook)
            {
                if(n==null)continue;
                b.Append(n.id).Append(',').Append(Csv(T(n.text,n.textEn))).Append(',')
                    .Append(Csv(string.IsNullOrEmpty(n.answerText)?T(n.playerAnswer?"是":"否",n.playerAnswer?"Yes":"No"):n.answerText)).Append(',')
                    .Append(Csv(T(n.truth?"是":"否",n.truth?"Yes":"No"))).Append(',').AppendLine(Csv(n.aiAnswer));
            }
            return b.ToString();
        }
        string AskedCsv()
        {
            if(Sim.S.askedLog==null||Sim.S.askedLog.Count==0)return EmptyRecords;
            var b=new StringBuilder(T("题库,题目,玩家答案,AI答案,游戏时钟秒\n","Bank,Question,Player answer,AI answer,Game clock seconds\n"));
            foreach(var n in Sim.S.askedLog)
            {
                if(n==null)continue;
                b.Append(Csv(n.bankId)).Append(',').Append(Csv(n.question)).Append(',').Append(Csv(n.playerAnswer)).Append(',')
                    .Append(Csv(n.aiAnswer)).Append(',').AppendLine(n.at.ToString("F2",CultureInfo.InvariantCulture));
            }
            return b.ToString();
        }
        bool PictureAvailable(XgTeachingNote n)
        {
            // Only images of actual answered questions, with the existing desk gate preserved.
            return n!=null&&(Sim.DeskOpen(n.dataset)||Sim.TeachingPackDeskOpen(n.dataset))
                &&(!string.IsNullOrEmpty(n.teachingImageKind)||!string.IsNullOrEmpty(n.patternA)||n.dataset=="mnist");
        }
        string NoteText(XgTeachingNote n)
        {
            return T(n.text,n.textEn)+"\n\n"+T("你的答案：","Your answer: ")
                +(string.IsNullOrEmpty(n.answerText)?T(n.playerAnswer?"是":"否",n.playerAnswer?"Yes":"No"):n.answerText)
                +"\n"+T("题目标签：","Question label: ")+T(n.truth?"是":"否",n.truth?"Yes":"No")
                +(string.IsNullOrEmpty(n.aiAnswer)?"":"\n"+Sim.AiDisplayName+": "+n.aiAnswer)
                +"\n\n"+T(n.why,n.whyEn);
        }
        void BuildRecords(ref int index)
        {
            if(folder=="notes"||folder=="images")
            {
                if(Sim.S.teachingNotebook!=null)foreach(var note in Sim.S.teachingNotebook)
                {
                    if(note==null)continue;
                    bool image=PictureAvailable(note);if(folder=="images"&&!image)continue;
                    var saved=note;
                    string label=folder=="images"?T("题图_","Question image_")+note.id+".png":T("标注_","Label_")+note.id+".txt";
                    AddRow(index++,folder+":"+note.id,label,NoteText(note),false,false,image?(Func<Texture2D>)(()=>XgTeachingPictures.ForNote(saved)):null);
                }
            }
            else if(folder=="models"&&Sim.Has("train.checkpoint"))
            {
                foreach(var m in Sim.S.models)
                {
                    if(m==null)continue;
                    var b=new StringBuilder();b.AppendLine(m.name).AppendLine(m.dataset+" / "+m.arch);
                    b.Append(T("训练轮数：","Training rounds: ")).AppendLine(m.epoch.ToString());
                    b.Append(T("验证准确率：","Validation accuracy: ")).AppendLine(m.acc.ToString("P1",CultureInfo.InvariantCulture));
                    b.Append(T("得分：","Score: ")).AppendLine(m.score.ToString("F1",CultureInfo.InvariantCulture));
                    if(m.date>0)b.Append(T("保存日期：","Saved date: ")).AppendLine(m.date.ToString());
                    b.AppendLine(m.HasWeights?T("保留了权重。","Weights retained."):T("仅保留参数记录，没有权重快照。","Settings retained; no weight snapshot."));
                    AddRow(index++,"model:"+m.id,(string.IsNullOrEmpty(m.name)?T("模型_","Model_")+m.id:m.name)+".ckpt",b.ToString(),false,false);
                }
            }
            if(index==0)ShowPreview(EmptyRecords,null,false);
        }
        void ShowPreview(string text,Func<Texture2D> picture,bool resetScroll)
        {
            preview.text=text??"";
            var texture=picture!=null?picture():null;
            previewImage.texture=texture;previewImage.gameObject.SetActive(texture!=null);
            previewViewport.offsetMax=new Vector2(-14,texture!=null?-204:-12);
            if(texture!=null)
            {
                var r=previewImage.rectTransform;float width=Mathf.Max(1,((RectTransform)r.parent).rect.width-24);
                float scale=Mathf.Min(width/texture.width,180f/texture.height);
                r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(.5f,1);r.anchoredPosition=new Vector2(0,-12);r.sizeDelta=new Vector2(texture.width*scale,texture.height*scale);
            }
            if(resetScroll)previewScroll.verticalNormalizedPosition=1;
        }
    }
}
