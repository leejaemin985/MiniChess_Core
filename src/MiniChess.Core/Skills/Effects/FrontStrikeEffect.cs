using System.Collections.Generic;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 전방 공격: 진행 방향(시전 시작 위치 → 지정 칸)으로 시전자 바로 앞 칸의 적에게 직접 피해를 준다.
    /// 이동 효과 뒤에 두면 "이동 후 전방 공격"이 된다. 앞 칸이 비었거나 벽/아군이면, 또는 시전자가 죽었으면 아무것도 하지 않는다.
    /// </summary>
    public class FrontStrikeEffect : SkillEffect
    {
        /// <summary>전방 적에게 주는 피해. [TBD 가능]</summary>
        public int? Damage { get; }

        public FrontStrikeEffect(int? damage)
        {
            Damage = damage;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Damage, path, nameof(Damage), issues);
        }

        public override void Apply(SkillContext context)
        {
            Unit caster = context.Caster;
            if (!caster.IsOnBoard)
                return;

            (int dx, int dy) = context.CasterOrigin.DirectionTo(context.Target);
            Position front = caster.Position.Value.Offset(dx, dy);

            Unit target = context.State.Board.GetLivingUnit(front);
            if (TargetFilter.Enemy.Matches(caster, target))
                DamageSystem.Apply(context.State, new DamageRequest(caster, target, Damage.Value, DamageType.Direct));
        }
    }
}
