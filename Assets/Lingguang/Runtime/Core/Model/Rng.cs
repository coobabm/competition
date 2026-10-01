using System;

namespace Lingguang.Core
{
    /// <summary>
    /// 本局唯一的随机源（SplitMix64）。
    /// 不直接用 System.Random：撤销（Esc）、“电脑”试运行都需要把随机状态一起拷贝/回滚，
    /// System.Random 无法克隆；SplitMix64 只有一个 ulong 状态，跨平台结果一致。
    /// </summary>
    public sealed class Rng
    {
        ulong state;

        public Rng(int seed)
        {
            state = (ulong)(uint)seed ^ 0x9E3779B97F4A7C15UL;
        }

        Rng(ulong rawState) { state = rawState; }

        public Rng Clone() => new Rng(state);

        public ulong NextU64()
        {
            ulong z = state += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>[0, maxExclusive)</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextU64() % (ulong)maxExclusive);
        }

        /// <summary>[minInclusive, maxExclusive)</summary>
        public int Range(int minInclusive, int maxExclusive) => minInclusive + Next(maxExclusive - minInclusive);
    }
}
