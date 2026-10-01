using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// 涟漪模拟的核心：一回合 = 玩家一次触碰 → 涟漪扩散 → 触发存在 → 新涟漪 …… → 全部静止。
/// 逻辑用固定步长推进（结果可复现），表现每帧刷新。
public class RippleSim : MonoBehaviour
{
    public static RippleSim I { get; private set; }

    [Header("涟漪参数（在这里调手感）")]
    public RippleSpec touch  = new RippleSpec { maxRadius = 1.6f, speed = 6f,  push = 1.5f, color = new Color(1f, 1f, 1f) };
    public RippleSpec bubble = new RippleSpec { maxRadius = 2.2f, speed = 6f,  push = 2f,   color = new Color(1f, 0.85f, 0.4f) };
    public RippleSpec stone  = new RippleSpec { maxRadius = 3.6f, speed = 5f,  push = 4f,   color = new Color(1f, 0.6f, 0.35f) };
    public RippleSpec scar   = new RippleSpec { maxRadius = 3.0f, speed = 7f,  push = 2.5f, color = new Color(1f, 0.95f, 0.85f) };
    public RippleSpec mirror = new RippleSpec { maxRadius = 6.0f, speed = 10f, push = 1.5f, halfAngleDeg = 18f, color = new Color(0.45f, 0.9f, 1f) };
    public RippleSpec bond   = new RippleSpec { maxRadius = 2.6f, speed = 5f,  push = -3f,  color = new Color(1f, 0.5f, 0.75f) };
    public RippleSpec shadow = new RippleSpec { maxRadius = 2.0f, speed = 4f,  push = 0.5f, color = new Color(0.6f, 0.45f, 0.85f) };

    [Header("模拟")]
    [Tooltip("被碰到后多久才发出自己的涟漪。连锁的“节奏感”主要靠它")]
    public float reactDelay = 0.08f;
    public float fixedStep = 1f / 120f;
    [Tooltip("模拟速度倍率，慢动作用（RippleFeedback 会改它）")]
    public float simSpeed = 1f;
    [Tooltip("速度衰减（越大越快停下）")]
    public float damping = 4f;
    public Rect bounds = new Rect(-8f, -4.5f, 16f, 9f);
    [Tooltip("可选：涟漪材质。想要辉光就用 URP/Particles/Unlit + Additive + HDR 颜色")]
    public Material lineMaterial;

    // 给表现层（音效、震屏、UI）订阅的事件
    public event Action<Being, int> OnTriggered;   // 触发者（玩家触碰时为 null）、连锁深度
    public event Action<TurnResult> OnTurnEnded;

    public struct TurnResult { public int turn, triggers, maxDepth; }

    readonly List<Being> beings = new List<Being>();
    readonly List<Ripple> ripples = new List<Ripple>();
    readonly List<Pending> pending = new List<Pending>();
    struct Pending { public float time; public Action act; }

    float simTime, accumulator, turnStartTime;
    bool turnActive;
    int turn, turnTriggers, turnMaxDepth;
    TurnResult last;
    Material defaultMat;

    public bool Busy => turnActive;

    void Awake() => I = this;

    void Start()
    {
        foreach (var b in FindObjectsByType<Being>(FindObjectsSortMode.None)) Register(b);
    }

    public void Register(Being b)
    {
        if (b != null && !beings.Contains(b)) beings.Add(b);
    }

    // ───────────── 主循环 ─────────────
    void Update()
    {
        if (!turnActive)
        {
            if (PointerDown(out Vector2 p)) StartTurn(p);
            return;
        }

        accumulator += Time.deltaTime * simSpeed;
        int guard = 0;
        while (accumulator >= fixedStep && guard++ < 30)
        {
            Step(fixedStep);
            accumulator -= fixedStep;
        }

        for (int i = 0; i < ripples.Count; i++) ripples[i].UpdateVisual();

        bool timeout = simTime - turnStartTime > 20f;
        if ((ripples.Count == 0 && pending.Count == 0 && AllSettled()) || timeout) EndTurn();
    }

    void StartTurn(Vector2 p)
    {
        turn++;
        turnActive = true;
        turnTriggers = 0;
        turnMaxDepth = 0;
        turnStartTime = simTime;
        accumulator = 0f;
        Emit(p, touch, RippleKind.Normal, 0, null, Vector2.right);
    }

    void Step(float dt)
    {
        simTime += dt;

        // 1) 到时间的反应 → 发出新涟漪
        for (int i = 0; i < pending.Count; i++)
        {
            if (pending[i].time > simTime) continue;
            var act = pending[i].act;
            pending.RemoveAt(i);
            i--;
            act();
        }

        // 2) 涟漪扩散 + 命中判定
        for (int i = 0; i < ripples.Count; i++)
        {
            var r = ripples[i];
            r.radius = Mathf.Min(r.radius + r.speed * dt, r.maxRadius);
            for (int j = 0; j < beings.Count; j++)
            {
                var b = beings[j];
                if (b == null || b.dead || r.hit.Contains(b)) continue;
                if (!r.Covers(b)) continue;
                r.hit.Add(b);
                b.OnHit(r, this);
            }
        }
        for (int i = ripples.Count - 1; i >= 0; i--)
        {
            if (!ripples[i].Done) continue;
            if (ripples[i].line != null) Destroy(ripples[i].line.gameObject);
            ripples.RemoveAt(i);
        }

        // 3) 移动 + 互相挤开（棋盘布局就是这样被涟漪“推”出来的）
        float decay = Mathf.Exp(-damping * dt);
        for (int i = 0; i < beings.Count; i++)
        {
            var b = beings[i];
            if (b == null || b.dead) continue;
            Vector2 p = b.Pos + b.velocity * dt;
            p.x = Mathf.Clamp(p.x, bounds.xMin + b.radius, bounds.xMax - b.radius);
            p.y = Mathf.Clamp(p.y, bounds.yMin + b.radius, bounds.yMax - b.radius);
            b.transform.position = p;
            b.velocity *= decay;
        }
        for (int i = 0; i < beings.Count; i++)
        for (int j = i + 1; j < beings.Count; j++)
        {
            var a = beings[i]; var c = beings[j];
            if (a == null || c == null || a.dead || c.dead) continue;
            Vector2 d = c.Pos - a.Pos;
            float min = a.radius + c.radius, dist = d.magnitude;
            if (dist >= min || dist < 1e-5f) continue;
            Vector2 n = d / dist * ((min - dist) * 0.5f);
            a.transform.position = a.Pos - n;
            c.transform.position = c.Pos + n;
        }

        beings.RemoveAll(b => b == null || b.dead);
    }

    bool AllSettled()
    {
        for (int i = 0; i < beings.Count; i++)
            if (beings[i] != null && beings[i].velocity.sqrMagnitude > 0.0004f) return false;
        return true;
    }

    void EndTurn()
    {
        turnActive = false;
        foreach (var r in ripples) if (r.line != null) Destroy(r.line.gameObject);
        ripples.Clear();
        pending.Clear();
        foreach (var b in beings) if (b != null) { b.velocity = Vector2.zero; b.OnTurnEnd(); }

        last = new TurnResult { turn = turn, triggers = turnTriggers, maxDepth = turnMaxDepth };
        OnTurnEnded?.Invoke(last);
    }

    /// 清空棋盘（重开用）
    public void ClearBoard()
    {
        foreach (var r in ripples) if (r.line != null) Destroy(r.line.gameObject);
        ripples.Clear();
        pending.Clear();
        foreach (var b in beings) if (b != null) { b.dead = true; Destroy(b.gameObject); }
        beings.Clear();
        turnActive = false;
        turn = 0;
        last = default;
    }

    // ───────────── 给 Being 调用的接口 ─────────────
    /// 存在被碰到后，延迟 reactDelay 发出自己的涟漪
    public void React(Being b, RippleSpec spec, RippleKind kind, int depth, bool killAfter, Vector2 dir = default)
    {
        pending.Add(new Pending
        {
            time = simTime + reactDelay,
            act = () =>
            {
                if (b == null) return;
                Emit(b.Pos, spec, kind, depth, b, dir == default ? Vector2.right : dir);
                if (killAfter) b.Kill();
            }
        });
    }

    void Emit(Vector2 center, RippleSpec spec, RippleKind kind, int depth, Being source, Vector2 dir)
    {
        var r = new Ripple
        {
            center = center,
            dir = dir.normalized,
            radius = 0f,
            maxRadius = spec.maxRadius,
            speed = spec.speed,
            push = spec.push,
            halfAngleDeg = spec.halfAngleDeg,
            kind = kind,
            depth = depth,
            color = spec.color,
            line = CreateLine()
        };
        if (source != null) r.hit.Add(source); // 不打自己
        ripples.Add(r);

        if (kind == RippleKind.Normal)
        {
            if (source != null) turnTriggers++;
            turnMaxDepth = Mathf.Max(turnMaxDepth, depth);
            OnTriggered?.Invoke(source, depth);
        }
    }

    LineRenderer CreateLine()
    {
        if (lineMaterial == null && defaultMat == null) defaultMat = new Material(Shader.Find("Sprites/Default"));
        var go = new GameObject("Ripple");
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.sharedMaterial = lineMaterial != null ? lineMaterial : defaultMat;
        lr.numCapVertices = 2;
        lr.sortingOrder = 10;
        lr.positionCount = 0;
        return lr;
    }

    // ───────────── 输入（新旧输入系统都兼容）─────────────
    bool PointerDown(out Vector2 world)
    {
        world = default;
        var cam = Camera.main;
        if (cam == null) return false;

        Vector2 screen;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            screen = Mouse.current.position.ReadValue();
        else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            screen = Touchscreen.current.primaryTouch.position.ReadValue();
        else return false;
#else
        if (!Input.GetMouseButtonDown(0)) return false;
        screen = Input.mousePosition;
#endif
        // 点在左上角调试按钮上时不算触碰
        if (screen.x < 250f && screen.y > Screen.height - 70f) return false;

        world = cam.ScreenToWorldPoint(screen);
        return true;
    }

    // ───────────── 原型用的调试 UI ─────────────
    void OnGUI()
    {
        GUI.Label(new Rect(12, 10, 500, 24), $"Turn {last.turn}   Triggers {last.triggers}   Max chain {last.maxDepth}   Beings {beings.Count}");
        var spawner = FindFirstObjectByType<BoardSpawner>();
        if (spawner == null) return;
        if (GUI.Button(new Rect(12, 36, 110, 26), "Same board")) spawner.Respawn(false);
        if (GUI.Button(new Rect(128, 36, 110, 26), "New board")) spawner.Respawn(true);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}
