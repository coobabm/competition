using LingGuangV05.Desktop.XingGuang;

namespace LingGuangV05.Desktop.YY
{
    public sealed partial class YYChatHub
    {
        public XgAppAcquisitionHost IntroHost=>XgAppAcquisitionHost.Instance!=null&&XgAppAcquisitionHost.Instance.Current
            &&ReferenceEquals(XgAppAcquisitionHost.Instance.GetComponent<XingGuangController>()?.runtime,runtime)?XgAppAcquisitionHost.Instance:null;
        public bool IntroPlaying=>IntroHost?.Playing==true;
        public int IntroRevision=>IntroHost?.Revision??-1;
        public void OpenIntroLink(YYMessage message)
        {if(message!=null&&Conversation(EraGroup)?.messages.Contains(message)==true)IntroHost?.OpenLink(message.introApp);}
    }
}
