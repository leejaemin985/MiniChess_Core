using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 이동 거리 제한: 이번 턴 자발적 이동의 누적 칸 수가 MaxCells 를 넘는 이동을 막는다.
    /// 명령을 나누어도 누적으로 판정한다. 일반 경로 이동(Path)에만 적용하며, 돌진 등 스킬 이동과 외부 강제 이동은 막지 않는다.
    /// </summary>
    public class DistanceLimitBehavior : StatusBehavior
    {
        public override System.Collections.Generic.IReadOnlyDictionary<string, int> Parameters =>
            new System.Collections.Generic.Dictionary<string, int> { [nameof(MaxCells)] = MaxCells };

        public int MaxCells { get; }

        public DistanceLimitBehavior(int maxCells)
        {
            MaxCells = maxCells;
        }

        public override MoveBlockReason CheckMove(StatusContext context, Unit initiator, MoveKind kind, int cells)
        {
            if (kind != MoveKind.Path || !MovementControl.IsVoluntary(context.Target, initiator))
                return MoveBlockReason.None;

            int movedThisTurn = context.Target.TurnState.VoluntaryCellsMoved;
            return movedThisTurn + cells > MaxCells ? MoveBlockReason.DistanceLimited : MoveBlockReason.None;
        }
    }
}
