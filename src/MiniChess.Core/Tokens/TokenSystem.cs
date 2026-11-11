using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Tokens
{
    /// <summary>
    /// 토큰 생성/먹기/소멸(사용자 확정 규칙).
    ///   - 생성: 토큰 라운드(TokenRuleData)의 각 팀 턴 시작에, 유닛을 놓을 수 있는 칸(벽/유닛/장애물 없음) 중 점령 칸을 뺀 칸에서
    ///     무작위로 하나(GameState.Random). 장판과 덫이 있는 칸은 허용한다. 빈 칸이 없으면 그 턴은 만들지 않는다.
    ///   - 먹기: 그 턴 팀의 유닛이 토큰 칸에 들어가면(지나가기 포함, 이동 방식 무관) 먹는다. 상대 팀 유닛은 먹지 않는다.
    ///     칸 효과(덫/장판)가 먼저 발동하고, 그 뒤에도 살아 있으면 먹는다. 소환은 칸에 들어가는 것으로 보지 않는다.
    ///     최대치(AP/체력)에 막혀 효과가 줄거나 없어도 토큰은 사라진다.
    ///   - 소멸: 먹지 않은 토큰은 그 팀의 턴 종료(종료 단계 처리 전)에 사라진다.
    /// </summary>
    public static class TokenSystem
    {
        /// <summary>라운드(1부터). 선공이 홀수 턴, 후공이 짝수 턴이라 전체 턴 1·2 가 1 라운드다.</summary>
        public static int RoundOf(int turnNumber)
        {
            return (turnNumber + 1) / 2;
        }

        /// <summary>그 라운드에 나오는 토큰 종류. 나오지 않는 라운드면 null.</summary>
        public static TokenKind? KindForRound(TokenRuleData rule, int round)
        {
            if (rule == null || rule.Sequence == null || rule.Sequence.Count == 0 || rule.PeriodRounds <= 0)
                return null;

            int sinceStart = round - rule.StartRound;
            if (sinceStart < 0 || sinceStart % rule.PeriodRounds != 0)
                return null;

            return rule.Sequence[sinceStart / rule.PeriodRounds % rule.Sequence.Count];
        }

        /// <summary>현재 팀의 턴 시작: 토큰 라운드면 토큰을 만든다.</summary>
        internal static void OnTurnStart(GameState state)
        {
            TokenKind? kind = KindForRound(state.Rules.Token, RoundOf(state.TurnNumber));
            if (kind == null)
                return;

            List<Position> candidates = state.Board.AllCells()
                .Where(cell => !cell.IsCaptureTile && state.Board.CanPlace(cell.Position))
                .Select(cell => cell.Position)
                .ToList();
            if (candidates.Count == 0)
                return;

            var token = new Token(kind.Value, candidates[state.Random.Next(candidates.Count)], state.CurrentTeam);
            state.Token = token;
            state.Events.Record(new TokenSpawnedEvent(token.Kind, token.Position, token.Team));
        }

        /// <summary>턴 종료: 먹지 않은 토큰을 없앤다.</summary>
        internal static void OnTurnEnd(GameState state)
        {
            Token token = state.Token;
            if (token == null)
                return;

            state.Token = null;
            state.Events.Record(new TokenExpiredEvent(token.Kind, token.Position, token.Team));
        }

        /// <summary>유닛이 칸에 들어갔다(칸 효과 발동 뒤). 그 칸의 토큰을 먹을 수 있으면 먹는다.</summary>
        internal static void OnUnitEntered(GameState state, Unit unit)
        {
            Token token = state.Token;
            if (token == null || state.IsGameOver || !unit.IsAlive || unit.Team != token.Team || unit.Position != token.Position)
                return;

            state.Token = null;
            TokenRuleData rule = state.Rules.Token;
            ApPool ap = state.GetPlayer(unit.Team).Ap;

            if (token.Kind == TokenKind.Ap)
            {
                int before = ap.Current;
                ap.Recover(rule.ApAmount);
                state.Events.Record(new TokenConsumedEvent(token.Kind, token.Position, token.Team, unit.Id, ap.Current - before, ap.Current));
                return;
            }

            // 회복량은 뒤따르는 UnitHealedEvent 에 담긴다.
            state.Events.Record(new TokenConsumedEvent(token.Kind, token.Position, token.Team, unit.Id, 0, ap.Current));
            DamageSystem.Heal(state, null, unit, rule.HealAmount);
        }
    }
}
