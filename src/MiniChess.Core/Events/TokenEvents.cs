using MiniChess.Core.Common;
using MiniChess.Core.Tokens;

namespace MiniChess.Core.Events
{
    /// <summary>팀의 턴 시작에 토큰이 생겼다.</summary>
    public class TokenSpawnedEvent : GameEvent
    {
        public TokenKind Kind { get; }
        public Position Position { get; }
        public Team Team { get; }

        public TokenSpawnedEvent(TokenKind kind, Position position, Team team)
        {
            Kind = kind;
            Position = position;
            Team = team;
        }
    }

    /// <summary>
    /// 유닛이 토큰 칸에 들어가 토큰을 먹었다.
    /// 힐 토큰은 이 이벤트 뒤에 UnitHealedEvent 가 따로 기록된다(회복이 막힌 유닛이면 없음).
    /// </summary>
    public class TokenConsumedEvent : GameEvent
    {
        public TokenKind Kind { get; }
        public Position Position { get; }
        public Team Team { get; }
        public int UnitId { get; }

        /// <summary>AP 토큰: 실제로 오른 AP(최대치에 막힌 몫은 빠진다, 0 일 수 있음). 힐 토큰: 0(회복량은 UnitHealedEvent).</summary>
        public int Amount { get; }

        /// <summary>먹은 뒤 그 팀의 AP.</summary>
        public int TeamAp { get; }

        public TokenConsumedEvent(TokenKind kind, Position position, Team team, int unitId, int amount, int teamAp)
        {
            Kind = kind;
            Position = position;
            Team = team;
            UnitId = unitId;
            Amount = amount;
            TeamAp = teamAp;
        }
    }

    /// <summary>아무도 먹지 않은 토큰이 턴 종료로 사라졌다.</summary>
    public class TokenExpiredEvent : GameEvent
    {
        public TokenKind Kind { get; }
        public Position Position { get; }
        public Team Team { get; }

        public TokenExpiredEvent(TokenKind kind, Position position, Team team)
        {
            Kind = kind;
            Position = position;
            Team = team;
        }
    }
}
