using LingGuangV05.XingGuang;
using UnityEngine;
using static LingGuangV05.Desktop.XingGuang.XgUi;
namespace LingGuangV05.Desktop.XingGuang
{
    public sealed partial class XgTrainPage
    {
        readonly float[] rhythmSamples=new float[256];
        readonly XgMusicOnsetGate rhythmGate=new XgMusicOnsetGate();
        XgSim rhythmOwner;
        AudioClip rhythmClip;
        float rhythmDeadline,rhythmNextSample,rhythmLastPosition,lastRhythmText=-10;
        bool rhythmPending,rhythmHand;
        XgTrack rhythmTrack;
        internal void CancelRhythmFeedback() { rhythmPending=false; }
        void NormalRoundPulse()
        {Fx.Shockwave(Fx.At(epochBar,new Vector2(epochBar.rect.width*.5f,0)),XgDark.Accent,60,.3f,5);}
        void QueueRoundPulse(XgEpoch epoch)
        {
            if(!Sim.DailyRhythmFeedback){rhythmPending=false;NormalRoundPulse();return;}
            if(Fx.Reduced){rhythmPending=false;NormalRoundPulse();return;}
            if(rhythmPending&&ReferenceEquals(rhythmOwner,Sim)&&rhythmTrack==Track&&Time.unscaledTime<rhythmDeadline)
            {rhythmHand|=epoch.hand;return;} // Coalesce automatic rounds; never queue delayed old outcomes.
            rhythmPending=false;rhythmHand=epoch.hand;
            if(!LingGuangV05.Desktop.GameMusic.TryReadMusicOutput(rhythmSamples,out rhythmClip,out rhythmLastPosition))
            {NormalRoundPulse();RhythmText(false);return;}
            rhythmGate.Reset();rhythmGate.Observe(rhythmSamples,Time.unscaledTimeAsDouble);
            rhythmOwner=Sim;rhythmTrack=Track;rhythmPending=true;
            rhythmDeadline=Time.unscaledTime+1;rhythmNextSample=Time.unscaledTime+.033f;
        }
        void TickRhythm()
        {
            if(!rhythmPending)return;
            float now=Time.unscaledTime;
            if(!ReferenceEquals(rhythmOwner,Sim)||rhythmTrack!=Track||!view.Visible||view.Tab!="train"||!Sim.DailyRhythmFeedback||now>rhythmDeadline+.2f)
            {rhythmPending=false;return;}
            if(Fx.Reduced){rhythmPending=false;NormalRoundPulse();return;}
            if(now<rhythmNextSample)return;rhythmNextSample=now+.033f;
            bool audible=LingGuangV05.Desktop.GameMusic.TryReadMusicOutput(rhythmSamples,out var clip,out float position);
            if(!audible||clip!=rhythmClip||position+.05f<rhythmLastPosition||position-rhythmLastPosition>.2f||now>=rhythmDeadline)
            {rhythmPending=false;NormalRoundPulse();RhythmText(false);return;}
            rhythmLastPosition=position;
            if(!rhythmGate.Observe(rhythmSamples,Time.unscaledTimeAsDouble))return;
            rhythmPending=false;NormalRoundPulse();RhythmText(true);
        }
        void RhythmText(bool accent)
        {
            if(!rhythmHand&&Time.unscaledTime-lastRhythmText<5)return;lastRhythmText=Time.unscaledTime;
            Fx.Float(Fx.At(epochBar,new Vector2(0,36)),accent?T("本轮完成 · 随音乐重音","Round complete · music accent"):
                T("本轮完成 · 无清晰重音，使用普通提示","Round complete · no clear accent; normal cue"),XgDark.Accent,13,20,.85f);
        }
    }
}
