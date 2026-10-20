using System;
using System.Collections.Generic;
using LingGuangV05.Desktop.XingGuang;
using LingGuangV05.Runtime;
using LingGuangV05.XingGuang;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        YYState dateOwner;List<YYMessage> dateMessages;int dateCount=-1;
        YYMessage birthdayEvidence,anniversaryEvidence;
        // Evidence remains the actual persisted YY message, including its sender and game time.
        // This cached read-only projection never writes history, a disclosure flag or affection.
        void RefreshDateEvidence()
        {
            var messages=Conversation(GirlfriendId)?.messages;
            if(ReferenceEquals(dateOwner,S)&&ReferenceEquals(dateMessages,messages)&&dateCount==(messages?.Count??0))return;
            dateOwner=S;dateMessages=messages;dateCount=messages?.Count??0;birthdayEvidence=anniversaryEvidence=null;
            if(messages==null)return;
            foreach(var line in messages)
            {
                if(line==null||line.from!=GirlfriendId||line.kind!=YYKind.Text)continue;
                if(XgDailyLife.DisclosesBirthday(line.text))birthdayEvidence=line;
                if(XgDailyKnownDates.DisclosesAnniversary(line.text))anniversaryEvidence=line;
            }
        }
        public string KnownDateReminder(DateTime today,out string evidence)
        {
            evidence="";
            var controller=XingGuangController.Instance;
            if(controller==null||controller.runtime!=runtime||runtime?.Sim==null||!controller.BoundTo(runtime.Sim)
                ||!EndingBoundTo(runtime.Sim)||controller.Sim?.DailyKnownDateReminders!=true)return "";
            RefreshDateEvidence();string result="";
            if(birthdayEvidence!=null&&XgDailyKnownDates.DaysUntil(today,9,27)<=7)
            {result=GameText.T("本机提醒：晴雯生日9月27日","Reminder: Qingwen's birthday, 27 September");evidence=birthdayEvidence.text;}
            if(anniversaryEvidence!=null&&XgDailyKnownDates.DaysUntil(today,1,1)<=7)
            {result=GameText.T("纪念日：晴雯认1月1日","Anniversary: Qingwen counts 1 January");evidence=anniversaryEvidence.text;}
            return result;
        }
    }
}
