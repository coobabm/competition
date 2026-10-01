using UnityEngine;

/// “爽感”层：音阶随连锁深度升高、震屏随触发数累积、大连锁时短暂慢动作。
/// 不改任何规则，只放大反馈——拿掉它游戏照样能玩，但会差很多。
public class RippleFeedback : MonoBehaviour
{
    [Header("声音")]
    [Tooltip("留空会自动生成一个木琴音色")]
    public AudioClip blip;
    [Range(0f, 1f)] public float volume = 0.35f;
    public int voices = 16;

    [Header("震屏")]
    public float shakePerTrigger = 0.015f;
    public float maxShake = 0.25f;
    public float shakeRecover = 0.8f;

    [Header("慢动作")]
    public int[] slowMoMilestones = { 15, 30, 60 };
    public float slowMoScale = 0.35f;
    public float slowMoDuration = 0.35f;

    static readonly int[] Pentatonic = { 0, 2, 4, 7, 9 }; // 五声音阶：怎么叠都不难听

    AudioSource[] pool;
    int nextVoice, triggersThisTurn, soundsThisFrame;
    float shake, slowUntil;
    Transform cam;
    Vector3 camBase;

    void Start()
    {
        var sim = RippleSim.I;
        sim.OnTriggered += HandleTrigger;
        sim.OnTurnEnded += HandleTurnEnd;

        if (blip == null) blip = MakeBlip();
        pool = new AudioSource[voices];
        for (int i = 0; i < voices; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false;
        }

        if (Camera.main != null)
        {
            cam = Camera.main.transform;
            camBase = cam.position;
        }
    }

    void OnDestroy()
    {
        if (RippleSim.I == null) return;
        RippleSim.I.OnTriggered -= HandleTrigger;
        RippleSim.I.OnTurnEnded -= HandleTurnEnd;
    }

    void HandleTrigger(Being source, int depth)
    {
        if (source != null) triggersThisTurn++;

        // 连锁越深，音越高（五声音阶，三个八度封顶）
        if (soundsThisFrame < 4)
        {
            soundsThisFrame++;
            int semis = Mathf.Min(Pentatonic[depth % 5] + 12 * (depth / 5), 36);
            var src = pool[nextVoice++ % pool.Length];
            src.clip = blip;
            src.volume = volume;
            src.pitch = Mathf.Pow(2f, semis / 12f);
            src.Play();
        }

        shake = Mathf.Min(maxShake, shake + shakePerTrigger);

        foreach (int m in slowMoMilestones)
            if (triggersThisTurn == m) slowUntil = Time.unscaledTime + slowMoDuration;
    }

    void HandleTurnEnd(RippleSim.TurnResult r) => triggersThisTurn = 0;

    void LateUpdate()
    {
        soundsThisFrame = 0;

        if (RippleSim.I != null)
            RippleSim.I.simSpeed = Time.unscaledTime < slowUntil ? slowMoScale : 1f;

        if (cam == null) return;
        shake = Mathf.MoveTowards(shake, 0f, shakeRecover * Time.deltaTime);
        float t = Time.time * 25f;
        Vector3 offset = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * 2f * shake;
        cam.position = camBase + offset;
    }

    static AudioClip MakeBlip()
    {
        const int rate = 44100;
        int n = (int)(rate * 0.35f);
        var data = new float[n];
        const float f = 523.25f; // C5
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Exp(-t * 12f) * Mathf.Clamp01(t * 400f);
            data[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.3f * Mathf.Sin(4f * Mathf.PI * f * t)) * env * 0.5f;
        }
        var clip = AudioClip.Create("blip", n, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
