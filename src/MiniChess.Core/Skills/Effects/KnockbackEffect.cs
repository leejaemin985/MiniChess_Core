using System;
using System.Collections.Generic;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 범위 안 대상을 시전자 반대쪽으로 밀어낸다. 방향과 막힘 처리는 코어 규칙(Knockback)을 따른다.
    /// AfterPush 는 대상마다 밀린 뒤의 칸(막혀서 안 밀렸으면 원래 칸)을 범위로 실행한다. 밀리다 죽은 대상에게는 실행하지 않는다.
    /// </summary>
    public class KnockbackEffect : SkillEffect
    {
        /// <summary>밀어내는 칸 수. [TBD 가능]</summary>
        public int? Cells { get; }

        public TargetFilter Filter { get; }

        /// <summary>밀어낸 뒤 그 대상에게 이어서 실행할 효과(넉백 후 기절 등).</summary>
        public IReadOnlyList<SkillEffect> AfterPush { get; }

        public KnockbackEffect(int? cells, TargetFilter filter = TargetFilter.Enemy, IReadOnlyList<SkillEffect> afterPush = null)
        {
            Cells = cells;
            Filter = filter;
            AfterPush = afterPush ?? Array.Empty<SkillEffect>();
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Cells, path, nameof(Cells), issues);
            CollectAll(AfterPush, $"{path}.{nameof(AfterPush)}", issues);
        }

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
            {
                if (context.State.IsGameOver)
                    return;

                Knockback.Push(context.State, target, context.Caster, Cells.Value);

                if (AfterPush.Count > 0 && target.IsAlive && target.IsOnBoard)
                    ApplyAll(AfterPush, context.AtCell(target.Position.Value));
            }
        }
    }
}
