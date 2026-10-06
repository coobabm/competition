using UnityEngine;

namespace LingGuangV05.CaseMenu
{
    /// <summary>A captured click commits on release over the same physical switch.</summary>
    [DisallowMultipleComponent]
    public sealed class CaseMenuButton : MonoBehaviour
    {
        public Transform keycap;
        public Renderer ring;
        public Light feedbackLight;
        public LineRenderer pulse;
        public Color accent = new Color(.22f, .78f, 1f);
        public bool isStart;
        public bool ReducedMotion { get; set; }
        public bool IsPressed { get; private set; }
        public bool IsHovered { get; private set; }
        Vector3 rest;
        bool hasRest;
        float travel, travelVelocity, glow, glowVelocity, pulseAge = 2f;
        MaterialPropertyBlock properties;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");

        void Awake() { CaptureRest(); }
        void CaptureRest()
        {
            if (keycap != null && !hasRest) { rest = keycap.localPosition; hasRest = true; }
            if (properties == null) properties = new MaterialPropertyBlock();
        }

        public void SetHovered(bool value) { IsHovered = value; }
        public void BeginPress() { if (IsHovered && isActiveAndEnabled) IsPressed = true; }
        public bool EndPress(bool overSameButton)
        {
            bool activate = IsPressed && overSameButton && isActiveAndEnabled;
            IsPressed = false;
            if (activate) Pulse();
            return activate;
        }
        public void CancelPress() { IsPressed = false; }
        public void Pulse() { pulseAge = 0; }

        void Update()
        {
            CaptureRest();
            float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
            travel = Mathf.SmoothDamp(travel, IsPressed ? .012f : 0, ref travelVelocity, .045f, Mathf.Infinity, dt);
            if (keycap != null) keycap.localPosition = rest + Vector3.forward * travel;
            pulseAge += dt;
            float flash = Mathf.Exp(-pulseAge * 8f) * 2.8f;
            glow = Mathf.SmoothDamp(glow, (IsHovered ? 2.5f : .35f) + (IsPressed ? 1.5f : 0) + flash,
                ref glowVelocity, .065f, Mathf.Infinity, dt);
            if (ring != null)
            {
                ring.GetPropertyBlock(properties);
                properties.SetColor(Emission, accent * glow);
                ring.SetPropertyBlock(properties);
            }
            if (feedbackLight != null) feedbackLight.intensity = .03f + glow * .055f;
            if (pulse != null)
            {
                pulse.enabled = pulseAge < .5f && !ReducedMotion;
                if (pulse.enabled)
                {
                    float t = pulseAge / .5f;
                    pulse.transform.localScale = Vector3.one * (1 + t * .65f);
                    Color c = accent; c.a = (1 - t) * .5f;
                    pulse.startColor = pulse.endColor = c;
                    pulse.widthMultiplier = Mathf.Lerp(.005f, .0005f, t);
                }
            }
        }
        void OnDisable()
        {
            IsHovered = false; IsPressed = false;
            if (hasRest && keycap != null) keycap.localPosition = rest;
            if (pulse != null) pulse.enabled = false;
        }
    }
}
