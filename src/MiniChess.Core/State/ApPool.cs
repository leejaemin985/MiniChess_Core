using System;

namespace MiniChess.Core.State
{
    /// <summary>
    /// 플레이어가 보유한 AP. 최대치 안에서 더하고 빼는 것만 담당한다.
    /// 언제 얼마나 회복/이월할지는 턴 시스템이 규칙 데이터를 보고 결정한다.
    /// </summary>
    public class ApPool
    {
        public int Current { get; private set; }
        public int Max { get; private set; }

        public ApPool(int max, int start)
        {
            if (max < 0) throw new ArgumentOutOfRangeException(nameof(max));
            if (start < 0 || start > max) throw new ArgumentOutOfRangeException(nameof(start));

            Max = max;
            Current = start;
        }

        public bool CanSpend(int amount)
        {
            return amount >= 0 && amount <= Current;
        }

        /// <summary>AP 를 소비한다. 부족하면 소비하지 않고 false.</summary>
        internal bool TrySpend(int amount)
        {
            if (!CanSpend(amount))
                return false;

            Current -= amount;
            return true;
        }

        /// <summary>AP 를 회복한다. 최대치 초과분은 버린다.</summary>
        internal void Recover(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            Current = Math.Min(Max, Current + amount);
        }

        /// <summary>스냅샷 복원용: 보유 AP 를 그대로 맞춘다.</summary>
        internal void Restore(int current)
        {
            Current = Math.Max(0, Math.Min(Max, current));
        }

        /// <summary>보유 AP 를 0 으로 만든다. 이월을 허용하지 않는 규칙에서 사용.</summary>
        internal void Clear()
        {
            Current = 0;
        }
    }
}
