using System;

namespace MiniChess.Core.Statuses
{
    /// <summary>
    /// 상태 종류의 정의(불변 데이터). 같은 정의를 여러 StatusEffect 가 공유하므로
    /// Behavior 에 유닛별 진행 값을 저장하면 안 된다.
    /// </summary>
    public class StatusDefinition
    {
        public string Id { get; }

        /// <summary>지속 횟수. 걸리거나 갱신될 때 StatusEffect.Remaining 의 시작값이 된다. 영구 상태면 의미 없음.</summary>
        public int Duration { get; }

        public StatusStackPolicy StackPolicy { get; }

        /// <summary>같은 Id 를 같은 건으로 보는 범위(대상당 하나 / 출처별). StackPolicy 는 같은 건끼리만 적용된다.</summary>
        public StatusStackScope StackScope { get; }

        /// <summary>Remaining 이 1 줄어드는 시점. null 이면 영구 상태(직접 제거하거나 대상이 죽을 때까지 유지).</summary>
        public StatusTiming Decrement { get; }

        public bool IsPermanent => Decrement == null;

        /// <summary>Behavior.OnTrigger 가 호출되는 시점. 반복 발동이 없으면 null.</summary>
        public StatusTiming Trigger { get; }

        /// <summary>실제 동작. 표식처럼 존재만 의미 있는 상태면 null.</summary>
        public StatusBehavior Behavior { get; }

        /// <summary>건 유닛이 사망하면 함께 제거되는지(호위처럼 건 유닛과 연결된 상태).</summary>
        public bool RemoveOnSourceDeath { get; }

        public StatusDefinition(
            string id,
            int duration,
            StatusStackPolicy stackPolicy,
            StatusTiming decrement,
            StatusTiming trigger = null,
            StatusBehavior behavior = null,
            bool removeOnSourceDeath = false,
            StatusStackScope stackScope = StatusStackScope.PerTarget)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("상태 Id 가 비어 있음", nameof(id));
            if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration), $"상태 '{id}': Duration 은 1 이상");

            Id = id;
            Duration = duration;
            StackPolicy = stackPolicy;
            Decrement = decrement;
            Trigger = trigger;
            Behavior = behavior;
            RemoveOnSourceDeath = removeOnSourceDeath;
            StackScope = stackScope;
        }
    }
}
