using UnityEngine;
public sealed class RetroCrtAudioProbe : MonoBehaviour
{
    public volatile float peak;
    public volatile int samples;
    private void OnAudioFilterRead(float[] data,int channels)
    {
        float p=peak;
        for(int i=0;i<data.Length;i++){float v=data[i]<0?-data[i]:data[i];if(v>p)p=v;}
        peak=p;samples+=data.Length;
    }
}
