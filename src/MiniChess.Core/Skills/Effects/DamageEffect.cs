using System.Collections.Generic;
using MiniChess.Core.Combat;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>범위 안 대상에게 피해를 준다. 적을 치는 피해(Enemy/Any)면 범위 안 장애물(중립)도 맞는다.</summary>
    public class DamageEffect : SkillEffect
    {
        /// <summary>피해량. [TBD 가능]</summary>
        public int? Amount { get; }

        public TargetFilter Filter { get; }
        public DamageType Type { get; }

        public DamageEffect(int? amount, TargetFilter filter = TargetFilter.Enemy, DamageType type = DamageType.Direct)
        {
            Amount = amount;
            Filter = filter;
            Type = type;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Amount, path, nameof(Amount), issues);
        }

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
            {
                if (context.State.IsGameOver)
                    return;

                DamageSystem.Apply(context.State, new DamageRequest(context.Caster, target, Amount.Value, Type));
            }

            if (DamagesObstacles)
                ObstacleDamage.ApplyInCells(context.State, context.Caster, context.AffectedCells, Amount.Value, Type);
        }

        public override bool DamagesObstacles => Amount > 0 && (Filter == TargetFilter.Enemy || Filter == TargetFilter.Any);
    }
}
