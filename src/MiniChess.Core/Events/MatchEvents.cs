using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Events
{
    /// <summary>한 팀의 배치 확정. PvP 에서 상대에게 숨길지는 서버가 정한다.</summary>
    public class UnitsPlacedEvent : GameEvent
    {
        public Team Team { get; }
        /// <summary>배치된 직후의 유닛들.</summary>
        public IReadOnlyList<UnitSnapshot> Units { get; }

        public UnitsPlacedEvent(Team team, IReadOnlyList<UnitSnapshot> units)
        {
            Team = team;
            Units = units;
        }
    }

    public class TurnStartedEvent : GameEvent
    {
        public Team Team { get; }
        public int TurnNumber { get; }

        public TurnStartedEvent(Team team, int turnNumber)
        {
            Team = team;
            TurnNumber = turnNumber;
        }
    }

    public class TurnEndedEvent : GameEvent
    {
        public Team Team { get; }
        public int TurnNumber { get; }

        public TurnEndedEvent(Team team, int turnNumber)
        {
            Team = team;
            TurnNumber = turnNumber;
        }
    }

    public enum GameEndReason
    {
        /// <summary>한쪽 또는 양쪽 전멸.</summary>
        Elimination,

        /// <summary>최대 턴 도달 후 판정.</summary>
        TurnLimit,

        /// <summary>한쪽 연결이 끊김. 전투 중이면 끊긴 팀이 지고, 배치 단계면 승자 없이 끝난다.</summary>
        Disconnect,

        /// <summary>서버 오류로 경기가 무효 처리됨. 승자 없음.</summary>
        Aborted,
    }

    public class GameEndedEvent : GameEvent
    {
        /// <summary>승리 팀. 무승부면 null.</summary>
        public Team? Winner { get; }

        public GameEndReason Reason { get; }

        public GameEndedEvent(Team? winner, GameEndReason reason)
        {
            Winner = winner;
            Reason = reason;
        }
    }
}
