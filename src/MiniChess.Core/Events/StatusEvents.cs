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

    /// <summary>상태효과가 제거되었다. 한 유닛에는 같은 Id 의 상태가 하나뿐이므로 (대상, 상태 Id)로 가리킨다.</summary>
    public class StatusRemovedEvent : GameEvent
    {
        public int TargetId { get; }
        public string StatusId { get; }
        public StatusRemoveReason Reason { get; }

        public StatusRemovedEvent(int targetId, string statusId, StatusRemoveReason reason)
        {
            TargetId = targetId;
            StatusId = statusId;
            Reason = reason;
        }
    }
}
