using MiniChess.Core.Common;

namespace MiniChess.Core.Tokens
{
    /// <summary>보드 위의 토큰 하나. 한 팀의 턴 시작에 생겨 그 턴 동안만 있고, 그 팀 유닛만 먹을 수 있다.</summary>
    public class Token
    {
        public TokenKind Kind { get; }
        public Position Position { get; }

        /// <summary>이 토큰이 생긴 턴의 팀. 이 팀 유닛만 먹을 수 있다.</summary>
        public Team Team { get; }

        public Token(TokenKind kind, Position position, Team team)
        {
            Kind = kind;
            Position = position;
            Team = team;
        }
    }
}
