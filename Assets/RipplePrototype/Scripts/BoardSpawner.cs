using UnityEngine;

/// 按权重随机铺一盘存在。改 seed 可以复现同一盘，方便对比调参前后的手感。
public class BoardSpawner : MonoBehaviour
{
    [System.Serializable]
    public class Weight
    {
        public BeingType type;
        public float weight = 1f;
    }

    public int seed = 1;
    public int count = 60;
    public Rect area = new Rect(-7f, -4f, 14f, 8f);
    public Weight[] weights =
    {
        new Weight { type = BeingType.Bubble, weight = 6f },
        new Weight { type = BeingType.Stone,  weight = 1.5f },
        new Weight { type = BeingType.Mirror, weight = 1f },
        new Weight { type = BeingType.Seed,   weight = 1f },
        new Weight { type = BeingType.Bond,   weight = 1f },
        new Weight { type = BeingType.Shadow, weight = 1f },
    };

    void Start() => Spawn();

    /// 重开：newSeed = true 换一盘新的，false 重玩同一盘
    public void Respawn(bool newSeed)
    {
        if (newSeed) seed++;
        RippleSim.I.ClearBoard();
        Spawn();
    }

    void Spawn()
    {
        var rng = new System.Random(seed);
        float total = 0f;
        foreach (var w in weights) total += w.weight;

        for (int i = 0; i < count; i++)
        {
            BeingType t = Pick(rng, total);
            float r = Being.DefaultRadius(t);

            // 尝试几次找一个不重叠的位置
            Vector2 pos = default;
            for (int tries = 0; tries < 20; tries++)
            {
                pos = new Vector2(
                    area.xMin + (float)rng.NextDouble() * area.width,
                    area.yMin + (float)rng.NextDouble() * area.height);
                if (!Overlaps(pos, r)) break;
            }

            var go = new GameObject(t.ToString());
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var b = go.AddComponent<Being>();
            b.radius = r;
            b.facingDeg = (float)rng.NextDouble() * 360f;
            b.SetType(t);
            RippleSim.I.Register(b);
        }
    }

    BeingType Pick(System.Random rng, float total)
    {
        float x = (float)rng.NextDouble() * total;
        foreach (var w in weights)
        {
            x -= w.weight;
            if (x <= 0f) return w.type;
        }
        return weights[weights.Length - 1].type;
    }

    bool Overlaps(Vector2 p, float r)
    {
        foreach (Transform child in transform)
        {
            var b = child.GetComponent<Being>();
            if (b != null && !b.dead && Vector2.Distance(b.Pos, p) < b.radius + r + 0.05f) return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.25f);
        Gizmos.DrawWireCube(area.center, area.size);
    }
}
