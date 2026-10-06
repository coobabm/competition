using UnityEngine;

namespace LingGuangV05.CaseMenu
{
    public static class CaseMenuMotion
    {
        public static float SmootherStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        public static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            t = Mathf.Clamp01(t);
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
        }

        public static Quaternion Rotation(Quaternion from, Quaternion to, float t)
        {
            return Quaternion.Slerp(from, to, SmootherStep(t));
        }

        public static float FrameStep(float unscaledDeltaTime)
        {
            return Mathf.Clamp(unscaledDeltaTime, 0, .05f);
        }
    }
}
