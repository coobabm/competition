using System;
using System.Globalization;
using System.Linq;
using System.Text;
using LingGuangV05.XingGuang;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgFilesWindow
    {
        int recordPageOffset;
        XgSim recordPageOwner;
        string recordPageFolder = "";
        int csvOffset, csvTotal;
        XgSim csvOwner;
        string csvKind;
        RectTransform recordPreviewPager;
        TMP_Text recordPreviewPage;
        Button recordPreviewPrevious, recordPreviewNext;
        static bool IsRecordCsv(string id) => id == "answers" || id == "asked";
        void BuildRecordPreviewPager(RectTransform detail)
        {
            recordPreviewPager=Rect("Record preview pages",detail,Vector2.zero,Vector2.right,new Vector2(12,4),new Vector2(-12,36));
            var previous=Button(recordPreviewPager,"Previous preview page",Vector2.zero,new Vector2(40,32),()=>{csvOffset=Math.Max(0,csvOffset-50);ShowRecordCsv(true);});
            previous.text="‹";recordPreviewPrevious=previous.transform.parent.GetComponent<Button>();
            var next=Button(recordPreviewPager,"Next preview page",new Vector2(-40,0),new Vector2(40,32),()=>{csvOffset+=50;ShowRecordCsv(true);},true);
            next.text="›";recordPreviewNext=next.transform.parent.GetComponent<Button>();
            recordPreviewPage=Text(Rect("Page",recordPreviewPager,Vector2.zero,Vector2.one,new Vector2(44,0),new Vector2(-44,0)),"",13);
            recordPreviewPage.alignment=TextAlignmentOptions.Center;
            recordPreviewPager.gameObject.SetActive(false);
        }
        void ShowRecordCsv(bool resetScroll)
        {
            if(Sim==null||!IsRecordCsv(previewId))return;
            if(!ReferenceEquals(csvOwner,Sim)||csvKind!=previewId){csvOwner=Sim;csvKind=previewId;csvOffset=0;}
            string text=previewId=="answers"?AnswerCsv():AskedCsv();
            if(csvOffset>=csvTotal&&csvOffset>0){csvOffset=Math.Max(0,((csvTotal-1)/50)*50);text=previewId=="answers"?AnswerCsv():AskedCsv();}
            ShowPreview(text,null,resetScroll);
        }
        void RefreshRecordPreviewPager()
        {
            bool show=folder.Length==0&&IsRecordCsv(previewId);
            recordPreviewPager.gameObject.SetActive(show);
            previewViewport.offsetMin=new Vector2(14,show?44:12);
            if(!show)return;
            recordPreviewPrevious.interactable=csvOffset>0;
            recordPreviewNext.interactable=csvOffset+50<csvTotal;
            recordPreviewPage.text=(csvOffset/50+1)+" / "+Math.Max(1,(csvTotal+49)/50);
        }
        void RecordPageButton(ref int index, string label, int offset)
        {
            var text = Button(rows,"Record page",new Vector2(0,-(index+1)*46),new Vector2(260,42),()=>{recordPageOffset=offset;previewId=null;Refresh();});
            var rect=(RectTransform)text.transform.parent;rect.anchorMin=rect.anchorMax=new Vector2(0,1);
            text.text=label;index++;
        }
        string FolderTitle(string id)
        {
            switch(id){case "packs":return T("训练数据","Training data");case "notes":return T("标注记录","Labelled questions");case "images":return T("训练图片","Training images");case "models":return T("模型检查点","Model checkpoints");default:return id;}
        }
        string EmptyRecords=>T("当前存档没有这类记录。","No records of this kind in the current save.");
        string RecordsKey()
        {
            var asked=Sim.S.askedLog;
            return "|"+Sim.S.stage+":"+Sim.Has("train.checkpoint")+":"+Sim.S.teachingNoteSerial
                +":"+Sim.S.learningRecordSerial+":"+recordPageOffset+":"+(asked==null?0:asked.Count)+":"+(asked==null||asked.Count==0?0:asked[asked.Count-1].at)
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
            var page=Sim.QueryTeachingNotes(csvOffset,50);csvTotal=page.total;
            if(page.total==0)return EmptyRecords;
            var b=new StringBuilder(T("记录ID,游戏时间,题目,玩家答案,题目标签,AI答案\n","Record ID,Game time,Question,Player answer,Question label,AI answer\n"));
            foreach(var n in page.records)
            {
                if(n==null)continue;
                b.Append(n.id).Append(',').Append(Csv(Sim.LearningTimestamp(Sim.LearningRecord(n.learningRecordId)))).Append(',').Append(Csv(T(n.text,n.textEn))).Append(',')
                    .Append(Csv(string.IsNullOrEmpty(n.answerText)?T(n.playerAnswer?"是":"否",n.playerAnswer?"Yes":"No"):n.answerText)).Append(',')
                    .Append(Csv(T(n.truth?"是":"否",n.truth?"Yes":"No"))).Append(',').AppendLine(Csv(n.aiAnswer));
            }
            return b.ToString();
        }
        string AskedCsv()
        {
            var page=Sim.QueryLearningRecords(new XgLearningQuery{offset=csvOffset,limit=50});csvTotal=page.total;
            if(page.total==0)return EmptyRecords;
            var b=new StringBuilder(T("记录ID,来源类型,题库,题目,玩家答案,AI答案,纠正,游戏时间\n","Record ID,Source kind,Bank,Question,Player answer,AI answer,Correction,Game time\n"));
            foreach(var n in page.records)
            {
                if(n==null)continue;
                b.Append(n.id).Append(',').Append(Csv(n.sourceKind)).Append(',').Append(Csv(n.bankId)).Append(',').Append(Csv(T(n.question,n.questionEn))).Append(',').Append(Csv(n.playerAnswer)).Append(',')
                    .Append(Csv(n.aiAnswer)).Append(',').Append(Csv(n.correction)).Append(',').AppendLine(Csv(Sim.LearningTimestamp(n)));
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
            return T("游戏时间：","Game time: ")+Sim.LearningTimestamp(Sim.LearningRecord(n.learningRecordId))+"\n"+T(n.text,n.textEn)+"\n\n"+T("你的答案：","Your answer: ")
                +(string.IsNullOrEmpty(n.answerText)?T(n.playerAnswer?"是":"否",n.playerAnswer?"Yes":"No"):n.answerText)
                +"\n"+T("题目标签：","Question label: ")+T(n.truth?"是":"否",n.truth?"Yes":"No")
                +(string.IsNullOrEmpty(n.aiAnswer)?"":"\n"+Sim.AiDisplayName+": "+n.aiAnswer)
                +"\n\n"+T(n.why,n.whyEn);
        }
        void BuildRecords(ref int index)
        {
            if(folder=="notes"||folder=="images")
            {
                if(!ReferenceEquals(recordPageOwner,Sim)||recordPageFolder!=folder){recordPageOwner=Sim;recordPageFolder=folder;recordPageOffset=0;}
                var page=Sim.QueryTeachingNotes(recordPageOffset,50,folder=="images"?(Func<XgTeachingNote,bool>)PictureAvailable:null);
                if(recordPageOffset>=page.total&&recordPageOffset>0){recordPageOffset=Math.Max(0,((page.total-1)/50)*50);page=Sim.QueryTeachingNotes(recordPageOffset,50,folder=="images"?(Func<XgTeachingNote,bool>)PictureAvailable:null);}
                if(recordPageOffset>0)RecordPageButton(ref index,T("上一页","Previous"),Math.Max(0,recordPageOffset-50));
                foreach(var note in page.records)
                {
                    bool image=PictureAvailable(note);
                    var saved=note;
                    string label=folder=="images"?T("题图_","Question image_")+note.id+".png":T("标注_","Label_")+note.id+".txt";
                    AddRow(index++,folder+":"+note.id,label,NoteText(note),false,false,image?(Func<Texture2D>)(()=>XgTeachingPictures.ForNote(saved)):null);
                }
                if(page.HasMore)RecordPageButton(ref index,T("下一页","Next"),recordPageOffset+50);
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
            RefreshRecordPreviewPager();
        }
    }
}
