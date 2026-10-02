using System.Collections.Generic;
using UnityEngine;

public enum RippleKind
{
    Normal, // 触发存在的普通涟漪
    Gray    // 影子发出的“染灰”涟漪：不触发，只把存在染灰
}

/// 一种涟漪的参数。在 RippleSim 的 Inspector 里调这些数，就是在调手感。
[System.Serializable]
public class RippleSpec
{
    public float maxRadius = 2.2f;
    public float speed = 6f;
    [Tooltip("推力。负数 = 吸引（牵挂）")]
    public float push = 2f;
    [Tooltip("0 = 整圈；>0 = 扇形的半角（度），用于镜子")]
    public float halfAngleDeg = 0f;
    public Color color = Color.white;
}

/// 一圈正在扩散的涟漪：逻辑上就是“圆心 + 不断变大的半径”。
public class Ripple
{
    public Vector2 center;
    public Vector2 dir = Vector2.right;
    public float radius, maxRadius, speed, push, halfAngleDeg;
    public RippleKind kind;
    public int depth;            // 连锁深度：玩家触碰 = 0，被它触发的 = 1，以此类推
    public Color color;
    public LineRenderer line;

    // 每圈涟漪对每个存在只生效一次
    public readonly HashSet<Being> hit = new HashSet<Being>();

    public bool Done => radius >= maxRadius;

    /// 判定：存在的边缘是否已被波前扫到（扇形涟漪还要判断角度）
    public bool Covers(Being b)
    {
        Vector2 to = b.Pos - center;
        float d = to.magnitude;
        if (d > radius + b.radius) return false;
        if (halfAngleDeg <= 0f || d < 0.001f) return true;
        return Vector2.Angle(dir, to) <= halfAngleDeg;
    }

    /// 表现层：用 LineRenderer 画圆环/圆弧，越往外越细、越淡
    public void UpdateVisual()
    {
        if (line == null) return;

        float t = Mathf.Clamp01(radius / maxRadius);
        Color c = color;
        c.a *= 1f - t * t;
        line.startColor = line.endColor = c;
        line.startWidth = line.endWidth = Mathf.Lerp(0.14f, 0.02f, t);

        if (halfAngleDeg <= 0f)
        {
            const int seg = 64;
            line.loop = true;
            line.positionCount = seg;
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                line.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }
        else
        {
            const int seg = 24;
            line.loop = false;
            line.positionCount = seg;
            float baseAng = Mathf.Atan2(dir.y, dir.x);
            float half = halfAngleDeg * Mathf.Deg2Rad;
            for (int i = 0; i < seg; i++)
            {
                float a = baseAng - half + 2f * half * i / (seg - 1);
                line.SetPosition(i, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }
    }
}
