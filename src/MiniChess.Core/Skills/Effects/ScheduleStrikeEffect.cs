using System.Collections.Generic;
using MiniChess.Core.Effects.Scheduled;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 범위 칸에 포격을 예약(조준)한다. 즉시 피해는 없고, 시전자 소유자의 다음 턴 시작에 착탄한다.
    /// 범위는 조준 시점에 고정된다.
    /// </summary>
    public class ScheduleStrikeEffect : SkillEffect
    {
        /// <summary>착탄 시 범위 안 적과 장애물 각각에게 주는 피해. [TBD 가능]</summary>
        public int? Damage { get; }

        public ScheduleStrikeEffect(int? damage)
        {
            Damage = damage;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Damage, path, nameof(Damage), issues);
        }

        public override void Apply(SkillContext context)
        {
            var strike = new ScheduledStrike(
                context.Skill.Id, context.Caster, context.AffectedCells, Damage.Value, context.State.TurnNumber);

            ScheduledStrikeSystem.Schedule(context.State, strike);
        }

        public override bool DamagesObstacles => Damage > 0;
    }
}
