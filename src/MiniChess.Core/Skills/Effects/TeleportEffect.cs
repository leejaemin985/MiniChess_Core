using MiniChess.Core.Movement;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 순간이동: 시전자를 지정 칸으로 옮긴다. 경로가 없어 벽/유닛을 넘어간다.
    /// 도착 칸의 효과(덫 등)는 발동한다. 시전자 스스로의 이동(자발적 이동)이다.
    /// </summary>
    public class TeleportEffect : SkillEffect
    {
        public override void Apply(SkillContext context)
        {
            MovementResolver.Resolve(context.State, context.Caster, context.Caster, MoveKind.Warp, new[] { context.Target });
        }
    }
}
