using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Events
{
    /// <summary>상태효과가 새로 걸리거나 갱신되었다. 값은 걸린(갱신된) 직후 기준이다.</summary>
    public class StatusAppliedEvent : GameEvent
    {
        public int TargetId { get; }
        public StatusSnapshot Status { get; }

        /// <summary>기존 효과를 갱신한 것이면 true.</summary>
        public bool Refreshed { get; }

        public StatusAppliedEvent(int targetId, StatusSnapshot status, bool refreshed)
        {
            TargetId = targetId;
            Status = status;
            Refreshed = refreshed;
        }
    }

    public enum StatusRemoveReason
    {
        Expired,
        Replaced,
        TargetDied,

        /// <summary>상태를 건 유닛이 사망(RemoveOnSourceDeath 인 상태만).</summary>
        SourceDied,

        /// <summary>효과를 발휘하고 소모됨(저주 표식 등).</summary>
        Consumed,

        Removed,
    }

    /// <summary>
    /// 상태효과가 제거되었다. 출처별 상태는 한 유닛에 같은 Id 가 여러 건일 수 있으므로 (대상, 상태 Id, 건 유닛, 원인)으로 가리킨다.
    /// 대상당 하나인 상태는 (대상, 상태 Id)만으로 충분하다.
    /// </summary>
    public class StatusRemovedEvent : GameEvent
    {
        public int TargetId { get; }
        public string StatusId { get; }
        public StatusRemoveReason Reason { get; }

        /// <summary>제거된 건을 건 유닛 Id. 유닛이 아닌 출처면 null.</summary>
        public int? SourceId { get; }

        /// <summary>제거된 건의 원인(StatusEffect.OriginId). 모르면 null.</summary>
        public string OriginId { get; }

        public StatusRemovedEvent(int targetId, string statusId, StatusRemoveReason reason, int? sourceId = null, string originId = null)
        {
            TargetId = targetId;
            StatusId = statusId;
            Reason = reason;
            SourceId = sourceId;
            OriginId = originId;
        }
    }
}
