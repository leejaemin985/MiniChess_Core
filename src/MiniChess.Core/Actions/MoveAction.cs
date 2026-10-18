using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Actions
{
    /// <summary>
    /// 플레이어가 자기 유닛을 직선으로 이동시키는 명령.
    /// 턴/행동 여부/AP 같은 플레이어 규칙만 검사하고, 실제 이동은 MovementResolver 에 맡긴다.
    /// </summary>
    internal class MoveAction
    {
        public Unit Unit { get; }
        public Position Destination { get; }

        public MoveAction(Unit unit, Position destination)
        {
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Destination = destination;
        }

        public MoveFailReason Validate(GameState state)
        {
            if (state.Phase != GamePhase.Battle) return MoveFailReason.NotBattlePhase;
            if (Unit.Team != state.CurrentTeam) return MoveFailReason.NotYourTurn;
            if (!Unit.IsOnBoard) return MoveFailReason.UnitNotOnBoard;

            IReadOnlyList<Position> path = GetPath(state);

            switch (MovementControl.Check(state, Unit, Unit, MoveKind.Path, path.Count))
            {
                case MoveBlockReason.ActionsEnded: return MoveFailReason.ActionsEnded;
                case MoveBlockReason.VoluntaryMoveLocked: return MoveFailReason.AlreadyActed;
                case MoveBlockReason.Rooted: return MoveFailReason.Rooted;
                case MoveBlockReason.DistanceLimited: return MoveFailReason.DistanceLimited;
                case MoveBlockReason.Installed: return MoveFailReason.Installed;
                case MoveBlockReason.Sealed: return MoveFailReason.Sealed;
                case MoveBlockReason.Stunned: return MoveFailReason.Stunned;
            }

            if (path.Count == 0) return MoveFailReason.NotStraightLine;
            if (!state.Board.IsPathClear(path)) return MoveFailReason.PathBlocked;

            PlayerState player = state.GetPlayer(Unit.Team);
            if (!player.Ap.CanSpend(GetCost(state, path.Count))) return MoveFailReason.NotEnoughAp;

            return MoveFailReason.None;
        }

        /// <summary>
        /// 이동을 실행한다. AP 는 실제로 이동한 칸 수만큼만 차감한다(덫 등으로 도중에 멈춘 경우 포함).
        /// </summary>
        public MovementResult Execute(GameState state)
        {
            MoveFailReason reason = Validate(state);
            if (reason != MoveFailReason.None)
                throw new InvalidOperationException($"이동 불가: {reason}");

            int eventStart = state.Events.Count;

            IReadOnlyList<Position> path = GetPath(state);
            MovementResult result = MovementResolver.Resolve(state, Unit, Unit, MoveKind.Path, path);

            PlayerState player = state.GetPlayer(Unit.Team);
            player.Ap.TrySpend(GetCost(state, result.CellsMoved));

            // 칸 효과에 의한 사망은 DamageSystem 에서 이미 처리된다.
            result.Events = state.Events.Since(eventStart);
            return result;
        }

        private IReadOnlyList<Position> GetPath(GameState state)
        {
            return state.Board.GetStraightPath(Unit.Position.Value, Destination);
        }

        private static int GetCost(GameState state, int cells)
        {
            return cells * state.Rules.ActionCost.MoveCostPerCell;
        }
    }
}
