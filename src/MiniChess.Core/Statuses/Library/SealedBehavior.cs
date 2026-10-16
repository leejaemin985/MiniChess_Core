using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 봉인: 전투에서 잠시 빠진다. 이동(자발/외부 모두)·기본 공격·스킬 불가, 대상 지정 불가, 피해와 회복 무효.
    /// 칸은 계속 점유해 다른 유닛의 이동을 막지만, 사거리나 대상 지정 경로를 막는 벽으로 취급하지는 않는다.
    /// </summary>
    public class SealedBehavior : StatusBehavior
    {
        public override bool BlocksBasicAttack(StatusContext context)
        {
            return true;
        }

        public override bool BlocksSkillUse(StatusContext context)
        {
            return true;
        }

        public override bool BlocksHpChange(StatusContext context)
        {
            return true;
        }

        public override bool MakesUntargetable => true;

        public override MoveBlockReason CheckMove(StatusContext context, Unit initiator, MoveKind kind, int cells)
        {
            return MoveBlockReason.Sealed;
        }
    }
}
