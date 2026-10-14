using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 돌진: 시전자가 지정 칸까지 직선으로 이동한다. <b>멈추지 않는 이동</b>이다.
    ///   - 지나는 칸의 효과(덫 등)는 모두 발동해 피해/상태는 그대로 받는다.
    ///   - 효과의 멈춤 요청(덫의 정지 등)은 무시하고 지정 칸까지 간다.
    ///   - 이동 중 사망한 경우에만 그 자리에서 멈춘다.
    /// </summary>
    public class DashEffect : SkillEffect
    {
        public override void Apply(SkillContext context)
        {
            IReadOnlyList<Position> path = context.State.Board.GetStraightPath(context.CasterOrigin, context.Target);
            MovementResolver.Resolve(context.State, context.Caster, context.Caster, MoveKind.Dash, path, ignoreStopRequests: true);
        }
    }
}
