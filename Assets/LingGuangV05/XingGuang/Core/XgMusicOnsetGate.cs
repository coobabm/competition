using System;
namespace LingGuangV05.XingGuang
{
    /// <summary>Small RMS rise detector for actual AudioSource output. No BPM, synthetic beat clock or audio writes.</summary>
    public sealed class XgMusicOnsetGate
    {
        double mean,previous,lastTime=-1,lastPeak=-10;
        int observations;
        public void Reset(){mean=previous=0;lastTime=-1;lastPeak=-10;observations=0;}
        public bool Observe(float[] samples,double now)
        {
            if(samples==null||samples.Length==0||double.IsNaN(now)||double.IsInfinity(now)){Reset();return false;}
            if(lastTime>=0&&(now<lastTime||now-lastTime>.2))Reset();
            double energy=0;
            for(int i=0;i<samples.Length;i++)
            {
                float value=samples[i];if(float.IsNaN(value)||float.IsInfinity(value)){Reset();return false;}
                energy+=(double)value*value;
            }
            energy=Math.Sqrt(energy/samples.Length);
            bool peak=observations>=3&&energy>.002&&energy>mean*1.35&&energy>previous*1.15&&now-lastPeak>=.16;
            mean=observations==0?energy:mean*.8+energy*.2;previous=energy;lastTime=now;observations++;
            if(peak)lastPeak=now;
            return peak;
        }
    }
}
