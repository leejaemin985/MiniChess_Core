using System;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Movement
{
    /// <summary>
    /// 유닛 상태에 따른 이동 가능 여부 판정. 경로/칸 조건은 Board 가, AP 는 각 행동이 따로 검사한다.
    ///
    /// 1. 행동 종료: 이번 턴 행동이 끝난 유닛(분신 소환 직후 등)의 자발적 이동(이동하는 유닛 = 이동을 일으킨 유닛)을 막는다.
    ///    외부 이동(넉백/당기기/다른 유닛의 워프)은 예외이며, 옮겨진 유닛의 행동 기록은 바뀌지 않는다.
    /// 2. 상태효과: 속박/이동 거리 제한 등. 어떤 이동 종류에 적용할지는 각 상태가 정한다.
    /// </summary>
    public static class MovementControl
    {
        public static bool IsVoluntary(Unit mover, Unit initiator)
        {
            return ReferenceEquals(mover, initiator);
        }

        /// <param name="mover">이동하는 유닛.</param>
        /// <param name="initiator">이동을 일으킨 유닛(자신이면 자발적 이동).</param>
        /// <param name="kind">이동 방식.</param>
        /// <param name="cells">이번 이동의 칸 수(경로 길이).</param>
        public static MoveBlockReason Check(GameState state, Unit mover, Unit initiator, MoveKind kind, int cells)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (mover == null) throw new ArgumentNullException(nameof(mover));

            if (IsVoluntary(mover, initiator) && mover.TurnState.ActionsEnded)
                return MoveBlockReason.ActionsEnded;

            return StatusSystem.CheckMove(state, mover, initiator, kind, cells);
        }

        /// <summary>
        /// 걸린 상태효과가 이 방식의 이동(1칸 기준)을 막지 않는지. 행동 종료는 보지 않는다.
        /// 스킬 사용 조건과 지정 후보 판정(옮겨질 유닛 거르기)에 쓴다.
        /// </summary>
        public static bool IsAllowedByStatuses(GameState state, Unit mover, Unit initiator, MoveKind kind)
        {
            return StatusSystem.CheckMove(state, mover, initiator, kind, cells: 1) == MoveBlockReason.None;
        }
    }
}
