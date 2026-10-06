using System;
namespace Fogline.Core
{

    /// <summary>
    /// 带种子的确定性随机数（xorshift64*）。状态只有一个 ulong，复制即快照。
    /// 模拟里一律用它，不用 UnityEngine.Random。
    /// </summary>
    [Serializable]
    public class SeededRandom
    {
        // xorshift 在全零状态下只会输出 0，种子为 0 时换成固定的非零值
        private const ulong ZeroSeedReplacement = 0x9E3779B97F4A7C15UL;

        public ulong State;

        public SeededRandom(ulong seed) => State = seed == 0 ? ZeroSeedReplacement : seed;

        public ulong NextULong()
        {
            State ^= State >> 12;
            State ^= State << 25;
            State ^= State >> 27;
            return State * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>[0, 1)，取高 24 位。</summary>
        public float NextFloat() => (NextULong() >> 40) * (1f / (1 << 24));

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentException($"空区间 [{minInclusive}, {maxExclusive})");
            var span = (ulong)((long)maxExclusive - minInclusive);
            return (int)(minInclusive + (long)(NextULong() % span));
        }

        public SeededRandom Clone() => (SeededRandom)MemberwiseClone();
    }
}
