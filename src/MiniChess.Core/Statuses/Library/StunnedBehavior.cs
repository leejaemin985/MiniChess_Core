using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 기절: 스스로 하는 행동을 모두 막는다(자발적 이동, 기본 공격, 장애물 공격, 모든 스킬).
    /// 다른 유닛이 일으키는 일은 그대로 받는다: 외부 강제 이동(넉백, 위치 교환 등), 피해/회복, 상태 부여, 대상 지정.
    /// </summary>
    public class StunnedBehavior : StatusBehavior
    {
        public override bool BlocksBasicAttack(StatusContext context)
        {
            return true;
        }

        public override bool BlocksSkillUse(StatusContext context)
        {
            return true;
        }

        public override MoveBlockReason CheckMove(StatusContext context, Unit initiator, MoveKind kind, int cells)
        {
            return MovementControl.IsVoluntary(context.Target, initiator) ? MoveBlockReason.Stunned : MoveBlockReason.None;
        }
    }
}
