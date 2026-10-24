using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Events
{
    /// <summary>포격이 조준(예약)되었다. 표시 계층이 공개 여부를 정한다.</summary>
    public class StrikeScheduledEvent : GameEvent
    {
        public StrikeSnapshot Strike { get; }

        public StrikeScheduledEvent(StrikeSnapshot strike)
        {
            Strike = strike;
        }
    }

    /// <summary>예약 포격이 착탄했다. 이어서 범위 안 적의 피격 이벤트가 기록된다.</summary>
    public class StrikeLandedEvent : GameEvent
    {
        public StrikeSnapshot Strike { get; }

        public StrikeLandedEvent(StrikeSnapshot strike)
        {
            Strike = strike;
        }
    }
}
