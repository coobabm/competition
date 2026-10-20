using System;
using System.Collections.Generic;
namespace LingGuangV05.XingGuang
{
    public sealed class XgDataReviewRow
    {
        public XgAskedEntry record;
        public string key, kind, title, titleEn, bookId;
        public int stage;
        public long sequence;
    }
    public sealed class XgDataReviewPage
    {
        public readonly List<XgDataReviewRow> rows=new List<XgDataReviewRow>();
        public int total, offset;
        public bool HasMore=>offset+rows.Count<total;
    }
    public sealed partial class XgSim
    {
        public const string DataReviewNode="data.review";
        public bool DataReviewOpen=>Has(DataReviewNode);
        public void RecordInstalledDataForReview(XgDownload file)
        {
            if(file==null||!file.installed||string.IsNullOrEmpty(file.fileId))return;
            var d=XgCatalog.Dataset(file.dataset);
            AppendLearning(new XgAskedEntry { sourceKind="data", bankId=file.dataset,
                question=d?.name??file.dataset, questionEn=d?.nameEn??file.dataset, eventKey="data-installed:"+file.fileId });
        }
        /// <summary>All persisted learning, plus truthful legacy receipts. Viewing never manufactures history.</summary>
        public XgDataReviewPage QueryDataReview(string text="",int offset=0,int limit=30)
        {
            var page=new XgDataReviewPage{offset=Math.Max(0,offset)};
            if(!DataReviewOpen)return page;
            var rows=new List<XgDataReviewRow>();var events=new HashSet<string>();var books=new HashSet<string>();
            foreach(var e in LearningRecordsNewest())
            {
                if(!string.IsNullOrEmpty(e.eventKey))events.Add(e.eventKey);
                if(e.sourceKind=="book")books.Add(e.bankId??"");
                rows.Add(new XgDataReviewRow{record=e,key="record:"+e.id,kind=e.sourceKind,title=e.question??"",titleEn=e.questionEn??"",
                    bookId=e.sourceKind=="book"?e.bankId:null,stage=e.learningStage,sequence=e.id});
            }
            if(S.dataFiles!=null)foreach(var f in S.dataFiles)
            {
                if(f==null||!f.installed||events.Contains("data-installed:"+f.fileId))continue;
                var d=XgCatalog.Dataset(f.dataset);
                rows.Add(new XgDataReviewRow{key="file:"+f.fileId,kind="data",title=d?.name??f.dataset,titleEn=d?.nameEn??f.dataset});
            }
            if(S.yyReplies!=null)foreach(var reply in S.yyReplies)
            {
                if(reply==null||events.Contains("yy:"+reply.id))continue;
                rows.Add(new XgDataReviewRow{key="old-chat:"+reply.id,kind="chat",title=(reply.question??"")+"\n"+(reply.answer??""),titleEn=(reply.question??"")+"\n"+(reply.answer??"")});
            }
            if(S.bookCompletions!=null)foreach(var completion in S.bookCompletions)
            {
                if(completion==null||books.Contains(completion.bookId))continue;
                var book=XgBooks.Get(completion.bookId);if(book==null)continue;books.Add(completion.bookId);
                rows.Add(new XgDataReviewRow{key="old-book:"+completion.key,kind="book",bookId=book.id,title=book.name,titleEn=book.nameEn,stage=completion.stage});
            }
            // Include saved reading progress even when a pre-history save never recorded completion dates.
            if(S.books!=null)foreach(var read in S.books)
            {
                if(read==null||read.count<=0||!books.Add(read.key))continue;
                var book=XgBooks.Get(read.key);if(book==null)continue;
                rows.Add(new XgDataReviewRow{key="reading:"+read.key,kind="book",bookId=read.key,title=book.name,titleEn=book.nameEn});
            }
            rows.Sort((a,b)=>{int stage=b.stage.CompareTo(a.stage);if(stage!=0)return stage;
                if(a.record!=null&&b.record!=null)return CompareLearning(a.record,b.record);
                if(a.record!=null)return -1;if(b.record!=null)return 1;return string.CompareOrdinal(a.key,b.key);});
            limit=Math.Max(1,Math.Min(100,limit));
            foreach(var row in rows)
            {
                if(!string.IsNullOrEmpty(text)&&!LearningContains(row.title,text)&&!LearningContains(row.titleEn,text)
                    &&!LearningContains(row.record?.aiAnswer,text)&&!LearningContains(row.record?.playerAnswer,text))continue;
                if(page.total++<page.offset)continue;if(page.rows.Count<limit)page.rows.Add(row);
            }
            return page;
        }
    }
}
