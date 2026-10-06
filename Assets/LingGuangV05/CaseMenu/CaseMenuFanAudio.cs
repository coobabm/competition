using UnityEngine;

namespace LingGuangV05.CaseMenu
{
    /// <summary>One existing looping voice, driven by the controller's boot clock.</summary>
    public sealed class CaseMenuFanAudio : MonoBehaviour
    {
        private AudioSource source;
        private bool started;
        private double gameplayStopAt = double.NaN;
        private float gameplayStartVolume;

        public void Configure(AudioSource audioSource, AudioClip clip)
        {
            if (source != null) source.Stop();
            source = audioSource;
            started = false;
            gameplayStopAt = double.NaN;
            if (source == null) return;
            source.Stop();
            source.playOnAwake = false;
            source.loop = true;
            source.clip = clip;
            source.volume = 0f;
            source.pitch = .55f;
        }

        public void StartBoot()
        {
            if (source == null || source.clip == null) return;
            started = true;
            SetBootElapsed(0f);
            if (!source.isPlaying) source.Play();
        }

        public void SetBootElapsed(float seconds)
        {
            if (!started || source == null) return;
            Vector2 value = Envelope(seconds);
            source.volume = value.x;
            source.pitch = value.y;
            if (value.x > 0f) return;
            source.Stop();
            started = false;
        }

        public void SetStable() => SetBootElapsed(4.2f);

        public void BeginGameplayCountdown()
        {
            if (!started || source == null || !double.IsNaN(gameplayStopAt)) return;
            gameplayStartVolume = source.volume;
            gameplayStopAt = Time.realtimeSinceStartupAsDouble + 10d;
        }

        public static float GameplayGain(float elapsed)
        {
            if (float.IsNaN(elapsed) || float.IsInfinity(elapsed)) return 0f;
            if (elapsed <= 9.6f) return 1f;
            if (elapsed >= 10f) return 0f;
            return 1f - Mathf.SmoothStep(0f, 1f, (elapsed - 9.6f) / .4f);
        }

        private void Update()
        {
            if (!started || source == null || double.IsNaN(gameplayStopAt)) return;
            double now = Time.realtimeSinceStartupAsDouble;
            source.volume = gameplayStartVolume * GameplayGain((float)(now - (gameplayStopAt - 10d)));
            if (now < gameplayStopAt) return;
            source.Stop();
            started = false;
        }

        public static Vector2 Envelope(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return Vector2.zero;
            if (seconds < .65f)
            {
                float surge = Mathf.SmoothStep(0f, 1f, seconds / .65f);
                return new Vector2(Mathf.Lerp(.03f, .22f, surge), Mathf.Lerp(.55f, 1.5f, surge));
            }
            float settle = Mathf.SmoothStep(0f, 1f, (seconds - 1.15f) / 2.55f);
            return new Vector2(Mathf.Lerp(.22f, .032f, settle), Mathf.Lerp(1.5f, .9f, settle));
        }

        private void OnDisable()
        {
            if (source != null) { source.Stop(); source.volume = 0f; }
            started = false;
            gameplayStopAt = double.NaN;
        }
    }
}
