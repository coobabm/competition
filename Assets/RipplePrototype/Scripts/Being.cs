using UnityEngine;

public enum BeingType { Bubble, Stone, Mirror, Seed, Bond, Shadow, Scar }

/// 棋盘上的一个“存在”。每种存在只有一条规则——涌现来自它们彼此之间的连锁。
[RequireComponent(typeof(SpriteRenderer))]
public class Being : MonoBehaviour
{
    public BeingType type = BeingType.Bubble;
    public float radius = 0.35f;
    [Tooltip("仅镜子：反射扇形的朝向（度）")]
    public float facingDeg;

    [HideInInspector] public Vector2 velocity;
    [HideInInspector] public bool gray;
    [HideInInspector] public int grayHits;
    [HideInInspector] public bool reactedThisTurn;
    [HideInInspector] public bool sprouting;
    [HideInInspector] public bool dead;

    int hp;
    float punch, flash, deathT;
    SpriteRenderer sr;
    Transform facingDot;

    public Vector2 Pos => transform.position;
    public Vector2 Facing => new Vector2(Mathf.Cos(facingDeg * Mathf.Deg2Rad), Mathf.Sin(facingDeg * Mathf.Deg2Rad));

    public static float DefaultRadius(BeingType t) => t switch
    {
        BeingType.Seed => 0.22f,
        BeingType.Stone => 0.45f,
        BeingType.Shadow => 0.4f,
        _ => 0.33f
    };

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = CircleSprite.Get();
        SetType(type);
    }

    public void SetType(BeingType t)
    {
        type = t;
        hp = t == BeingType.Stone ? 2 : 1;
        RefreshLook();
    }

    // ───────────── 规则：被涟漪碰到时怎么反应 ─────────────
    public void OnHit(Ripple r, RippleSim sim)
    {
        if (dead) return;

        // 1) 物理：被推开（或被牵挂吸引）。这会改变棋盘布局 → 影响之后的连锁
        Vector2 to = Pos - r.center;
        float d = to.magnitude;
        if (d > 1e-4f) velocity += to / d * r.push * (1f - Mathf.Clamp01(d / r.maxRadius));
        punch = 1f;

        // 2) 染灰涟漪：只染灰，不触发
        if (r.kind == RippleKind.Gray)
        {
            if (type != BeingType.Shadow && type != BeingType.Scar && !gray)
            {
                gray = true;
                RefreshLook();
            }
            return;
        }

        flash = 1f;
        int next = r.depth + 1;

        // 3) 灰色的存在：第一次被碰会“挡住”涟漪；第二次被碰会变成疤，并爆发
        if (gray)
        {
            grayHits++;
            if (grayHits >= 2)
            {
                gray = false;
                grayHits = 0;
                SetType(BeingType.Scar);
                reactedThisTurn = true;
                sim.React(this, sim.scar, RippleKind.Normal, next, false);
            }
            else RefreshLook();
            return;
        }

        // 每个存在每回合最多触发一次 → 连锁必然会结束
        if (reactedThisTurn) return;

        switch (type)
        {
            case BeingType.Bubble: // 破裂，发出涟漪，然后消失
                reactedThisTurn = true;
                sim.React(this, sim.bubble, RippleKind.Normal, next, true);
                break;

            case BeingType.Stone: // 要碰两次，碎时涟漪特别大
                hp--;
                if (hp <= 0)
                {
                    reactedThisTurn = true;
                    sim.React(this, sim.stone, RippleKind.Normal, next, true);
                }
                else RefreshLook();
                break;

            case BeingType.Mirror: // 朝固定方向射出一道扇形涟漪
                reactedThisTurn = true;
                sim.React(this, sim.mirror, RippleKind.Normal, next, false, Facing);
                break;

            case BeingType.Seed: // 不立刻反应，回合结束时长成泡
                if (!sprouting) { sprouting = true; RefreshLook(); }
                break;

            case BeingType.Bond: // 发出吸引型涟漪，把周围的存在拉近
                reactedThisTurn = true;
                sim.React(this, sim.bond, RippleKind.Normal, next, false);
                break;

            case BeingType.Shadow: // 发出染灰涟漪
                reactedThisTurn = true;
                sim.React(this, sim.shadow, RippleKind.Gray, next, false);
                break;

            case BeingType.Scar: // 每回合都能再次爆发
                reactedThisTurn = true;
                sim.React(this, sim.scar, RippleKind.Normal, next, false);
                break;
        }
    }

    public void OnTurnEnd()
    {
        reactedThisTurn = false;
        if (sprouting)
        {
            sprouting = false;
            radius = DefaultRadius(BeingType.Bubble);
            SetType(BeingType.Bubble);
        }
    }

    public void Kill()
    {
        dead = true;
        deathT = 0f;
    }

    // ───────────── 表现 ─────────────
    Color BaseColor() => type switch
    {
        BeingType.Bubble => new Color(1f, 0.82f, 0.35f),
        BeingType.Stone  => hp >= 2 ? new Color(0.55f, 0.5f, 0.45f) : new Color(0.75f, 0.62f, 0.5f),
        BeingType.Mirror => new Color(0.45f, 0.9f, 1f),
        BeingType.Seed   => sprouting ? new Color(0.7f, 1f, 0.5f) : new Color(0.35f, 0.75f, 0.35f),
        BeingType.Bond   => new Color(1f, 0.5f, 0.7f),
        BeingType.Shadow => new Color(0.35f, 0.25f, 0.5f),
        BeingType.Scar   => new Color(1f, 0.95f, 0.85f),
        _ => Color.white
    };

    Color CurrentColor()
    {
        Color c = BaseColor();
        if (gray) c = Color.Lerp(c, new Color(0.35f, 0.35f, 0.38f), grayHits > 0 ? 0.5f : 0.75f);
        return c;
    }

    void RefreshLook()
    {
        if (sr == null) return;
        sr.color = CurrentColor();

        if (type == BeingType.Mirror && facingDot == null)
        {
            var go = new GameObject("facing");
            go.transform.SetParent(transform, false);
            var dsr = go.AddComponent<SpriteRenderer>();
            dsr.sprite = CircleSprite.Get();
            dsr.color = Color.white;
            dsr.sortingOrder = sr.sortingOrder + 1;
            facingDot = go.transform;
        }
        if (facingDot != null) facingDot.gameObject.SetActive(type == BeingType.Mirror);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        punch = Mathf.MoveTowards(punch, 0f, dt * 5f);
        flash = Mathf.MoveTowards(flash, 0f, dt * 6f);

        float s = radius * 2f * (1f + 0.35f * punch);
        if (dead)
        {
            deathT += dt * 4f;
            s *= 1f + deathT * 0.8f;
            Color c = CurrentColor();
            c.a = Mathf.Clamp01(1f - deathT);
            sr.color = c;
            if (deathT >= 1f) Destroy(gameObject);
        }
        else
        {
            sr.color = Color.Lerp(CurrentColor(), Color.white, flash * 0.8f);
        }
        transform.localScale = new Vector3(s, s, 1f);

        if (facingDot != null)
        {
            facingDot.localPosition = (Vector3)(Facing * 0.38f);
            facingDot.localScale = Vector3.one * 0.3f;
        }
    }
}
