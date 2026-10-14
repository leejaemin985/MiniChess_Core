using System.Collections.Generic;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>범위 안 대상을 시전자 반대쪽으로 밀어낸다. 방향과 막힘 처리는 코어 규칙(Knockback)을 따른다.</summary>
    public class KnockbackEffect : SkillEffect
    {
        /// <summary>밀어내는 칸 수. [TBD 가능]</summary>
        public int? Cells { get; }

        public TargetFilter Filter { get; }

        public KnockbackEffect(int? cells, TargetFilter filter = TargetFilter.Enemy)
        {
            Cells = cells;
            Filter = filter;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Cells, path, nameof(Cells), issues);
        }

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
            {
                if (context.State.IsGameOver)
                    return;

                Knockback.Push(context.State, target, context.Caster, Cells.Value);
            }
        }
    }
}
