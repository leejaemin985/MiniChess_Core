using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Events
{
    /// <summary>한 팀의 배치 확정. PvP 에서 상대에게 숨길지는 서버가 정한다.</summary>
    public class UnitsPlacedEvent : GameEvent
    {
        public Team Team { get; }
        public IReadOnlyList<Unit> Units { get; }

        public UnitsPlacedEvent(Team team, IReadOnlyList<Unit> units)
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

    public class GameEndedEvent : GameEvent
    {
        /// <summary>승리 팀. 무승부면 null.</summary>
        public Team? Winner { get; }

        public GameEndedEvent(Team? winner)
        {
            Winner = winner;
        }
    }
}
