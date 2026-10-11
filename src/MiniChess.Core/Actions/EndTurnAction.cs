using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Actions
{
    /// <summary>플레이어가 자기 턴을 끝내는 명령. AP 가 남아 있어도 언제든 가능하다.</summary>
    internal class EndTurnAction
    {
        public Team Team { get; }

        public EndTurnAction(Team team)
        {
            Team = team;
        }

        public EndTurnFailReason Validate(GameState state)
        {
            if (state.Phase != GamePhase.Battle) return EndTurnFailReason.NotBattlePhase;
            if (Team != state.CurrentTeam) return EndTurnFailReason.NotYourTurn;

            return EndTurnFailReason.None;
        }

        /// <summary>턴을 종료하고 다음 턴을 시작한다.</summary>
        public EndTurnResult Execute(GameState state)
        {
            EndTurnFailReason reason = Validate(state);
            if (reason != EndTurnFailReason.None)
                throw new InvalidOperationException($"턴 종료 불가: {reason}");

            int eventStart = state.Events.Count;
            TurnSystem.EndTurn(state);
            return new EndTurnResult(Team, state.CurrentTeam, state.TurnNumber, state.Events.Since(eventStart));
        }
    }

    public class EndTurnResult
    {
        public Team EndedTeam { get; }

        /// <summary>다음 턴의 팀. 처리 중 경기가 끝났으면 의미 없음(GameState.IsGameOver 확인).</summary>
        public Team NextTeam { get; }

        public int TurnNumber { get; }

        /// <summary>턴 종료/다음 턴 시작 처리로 발생한 이벤트(발생 순).</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        public EndTurnResult(Team endedTeam, Team nextTeam, int turnNumber, IReadOnlyList<GameEvent> events)
        {
            EndedTeam = endedTeam;
            NextTeam = nextTeam;
            TurnNumber = turnNumber;
            Events = events;
        }
    }
}
