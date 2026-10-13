using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 돌진 이동: 시전자가 지정 칸까지 직선으로 이동한다.
    /// 지나는 칸의 효과(덫 등)는 모두 발동해 피해/상태를 그대로 받지만, 멈춤 요청은 무시하고 지정 칸까지 간다.
    /// 이동 중 사망하면 그 자리에서 멈춘다.
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
