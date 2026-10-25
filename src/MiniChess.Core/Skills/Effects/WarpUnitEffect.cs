using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 첫 번째 지정 칸의 유닛을 두 번째 지정 칸으로 순간이동시킨다(MoveKind.Warp, 경로 없이 벽/유닛을 넘음). UnitToCellTargeting 과 함께 쓴다.
    /// 시전자가 일으킨 이동이라 옮겨지는 유닛이 시전자 자신이면 자발적 이동, 다른 유닛이면 외부 이동이다.
    /// 도착 칸의 효과(덫 등)는 발동하고, 옮겨진 유닛의 행동 기록(이미 쓴 기본 공격/스킬 등)은 바뀌지 않는다.
    /// </summary>
    public class WarpUnitEffect : SkillEffect
    {
        public override void Apply(SkillContext context)
        {
            Unit unit = context.State.Board.GetLivingUnit(context.Targets[0]);
            if (unit == null)
                return;

            MovementResolver.Resolve(context.State, unit, context.Caster, MoveKind.Warp, new[] { context.Targets[1] });
        }
    }
}
