using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Conditions
{
    /// <summary>
    /// 걸린 상태효과가 이 방식의 자발적 이동을 막지 않을 때만 사용 가능(속박 중 돌진 불가 등).
    /// 어떤 상태가 막는지는 각 상태가 이동 방식(Kind)을 보고 정한다. 행동 잠금은 스킬 공통 검사가 처리한다.
    /// </summary>
    public class CanMoveCondition : SkillCondition
    {
        public MoveKind Kind { get; }

        public CanMoveCondition(MoveKind kind)
        {
            Kind = kind;
        }

        public override bool IsMet(GameState state, Unit caster)
        {
            return MovementControl.IsAllowedByStatuses(state, caster, caster, Kind);
        }
    }
}
