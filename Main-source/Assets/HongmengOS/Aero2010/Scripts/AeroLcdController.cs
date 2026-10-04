using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace HongmengOS.Aero2010
{
    /// <summary>LCD backlight and local audio. No CRT discharge, warping or global settings.</summary>
    public sealed class AeroLcdController : MonoBehaviour
    {
        public RawImage display;
        public Camera sourceCamera;
        public AeroLcdInputSurface inputSurface;
        public Michsky.DreamOS.AudioManager desktopAudio;
        public Renderer powerLamp;
        public GameObject osd;
        public TMP_Text osdLabel;
        [Range(.25f,1)] public float brightness=.92f;
        public bool initialPower=true;
        public bool IsPowered {get; private set;}
        public bool IsTransitioning => transition!=null;
        public bool CanInteract => isActiveAndEnabled&&IsPowered&&transition==null;
        AudioSource audioSource;
        AudioClip click;
        Coroutine transition;
        MaterialPropertyBlock lampBlock;
        float osdUntil;
        void Awake()
        {
            audioSource=gameObject.AddComponent<AudioSource>();audioSource.playOnAwake=false;audioSource.spatialBlend=0;
            const int rate=44100;var data=new float[2205];var random=new System.Random(2311);
            for(int i=0;i<data.Length;i++){float t=(float)i/rate;data[i]=(float)(random.NextDouble()*2-1)*Mathf.Exp(-t*120)*.42f+Mathf.Sin(t*2300)*Mathf.Exp(-t*180)*.24f;}
            click=AudioClip.Create("LCD mechanical switch",data.Length,1,rate,false);click.SetData(data,0);
            lampBlock=new MaterialPropertyBlock();IsPowered=initialPower;Apply(IsPowered?brightness:0);inputSurface.SetInputEnabled(IsPowered);sourceCamera.enabled=IsPowered;
            if(osd!=null)osd.SetActive(false);
            if(GetComponent<AeroLcdResolution>()==null)gameObject.AddComponent<AeroLcdResolution>();
        }
        void Update(){if(osd!=null&&osd.activeSelf&&Time.unscaledTime>osdUntil)osd.SetActive(false);}
        void OnEnable(){if(inputSurface!=null)inputSurface.SetInputEnabled(IsPowered);if(sourceCamera!=null)sourceCamera.enabled=IsPowered;if(lampBlock!=null)Apply(IsPowered?brightness:0);}
        void OnDestroy(){if(click!=null)Destroy(click);}
        void OnDisable(){if(transition!=null){StopCoroutine(transition);transition=null;}if(inputSurface!=null)inputSurface.SetInputEnabled(false);if(osd!=null)osd.SetActive(false);if(lampBlock!=null)Apply(IsPowered?brightness:0);}
        public void PlayDesktopClick(){if(CanInteract&&desktopAudio!=null)desktopAudio.PlayMouseStroke();}
        public void TogglePower(){SetPower(!IsPowered);}
        public void PowerOff(){SetPower(false);}
        public void SetPower(bool value)
        {
            if(!isActiveAndEnabled||transition!=null||value==IsPowered)return;
            transition=StartCoroutine(Change(value));
        }
        IEnumerator Change(bool value)
        {
            audioSource.PlayOneShot(click,.25f);inputSurface.SetInputEnabled(false);if(osd!=null)osd.SetActive(false);
            sourceCamera.enabled=true;float from=IsPowered?brightness:0,to=value?brightness:0;
            for(float t=0;t<.18f;t+=Time.unscaledDeltaTime){Apply(Mathf.Lerp(from,to,t/.18f));yield return null;}
            IsPowered=value;Apply(to);sourceCamera.enabled=value;inputSurface.SetInputEnabled(value);transition=null;
        }
        void Apply(float value)
        {
            if(display!=null)display.color=new Color(value,value,value,1);
            if(powerLamp!=null){Color c=value>.01f?new Color(.06f,.42f,1):new Color(.012f,.018f,.026f);powerLamp.GetPropertyBlock(lampBlock);lampBlock.SetColor("_BaseColor",c);lampBlock.SetColor("_EmissionColor",c*1.4f);powerLamp.SetPropertyBlock(lampBlock);}
        }
        public void Brighter(){Adjust(.05f);}
        public void Dimmer(){Adjust(-.05f);}
        public void StandardPreset(){if(!CanInteract)return;brightness=.92f;Adjust(0);}
        void Adjust(float delta){if(!CanInteract)return;brightness=Mathf.Clamp(brightness+delta,.25f,1);Apply(brightness);audioSource.PlayOneShot(click,.15f);ShowOsd();}
        public void ShowOsd(){if(!CanInteract||osd==null)return;osd.SetActive(true);osdUntil=Time.unscaledTime+4;if(osdLabel!=null)osdLabel.text="Brightness   "+Mathf.RoundToInt(brightness*100)+"%\n\nStandard   •   1920 × 1080 / 60 Hz";}
    }
}
