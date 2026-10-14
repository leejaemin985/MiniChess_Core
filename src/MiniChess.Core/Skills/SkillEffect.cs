using System.Collections.Generic;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills
{
    /// <summary>
    /// 스킬이 실제로 상태를 바꾸는 부분. 스킬의 효과 목록 순서대로 실행된다.
    /// 피해/회복/상태/칸 효과는 각 시스템(DamageSystem, StatusSystem, CellEffectSystem)을 거친다.
    /// </summary>
    public abstract class SkillEffect : SkillComponent
    {
        public abstract void Apply(SkillContext context);

        /// <summary>범위 안에서 조건에 맞는 살아 있는 유닛(범위 칸 순서).</summary>
        protected static List<Unit> GetUnitsInArea(SkillContext context, TargetFilter filter)
        {
            return filter.GetUnits(context.State.Board, context.AffectedCells, context.Caster);
        }

        /// <summary>효과 목록을 순서대로 실행한다. 도중에 경기가 끝나면 나머지는 실행하지 않는다.</summary>
        internal static void ApplyAll(IReadOnlyList<SkillEffect> effects, SkillContext context)
        {
            foreach (SkillEffect effect in effects)
            {
                if (context.State.IsGameOver)
                    return;

                effect.Apply(context);
            }
        }

        /// <summary>효과 목록의 설정 누락을 "{path}[i]" 위치로 모은다.</summary>
        internal static void CollectAll(IReadOnlyList<SkillEffect> effects, string path, List<string> issues)
        {
            for (int i = 0; i < effects.Count; i++)
                effects[i].CollectConfigIssues($"{path}[{i}]", issues);
        }
    }
}
