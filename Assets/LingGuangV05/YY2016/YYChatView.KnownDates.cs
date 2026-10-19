using LingGuangV05.Runtime;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatView
    {
        string KnownDateTip()
        {
            if(hub?.S?.selected!=YYChatHub.GirlfriendId)return "";
            if(string.IsNullOrEmpty(hub.KnownDateReminder(Today(),out var quote)))return "";
            return GameText.T("晴雯在这段聊天里说过：\n","Qingwen said in this chat:\n")+quote;
        }
    }
}
