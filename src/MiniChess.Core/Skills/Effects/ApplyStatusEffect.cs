using System.Collections.Generic;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>범위 안 대상에게 상태효과를 건다. 중첩 처리는 상태 정의의 StackPolicy 를 따른다.</summary>
    public class ApplyStatusEffect : SkillEffect
    {
        /// <summary>걸 상태. 상태 자체의 수치가 TBD 면 정의를 만들 수 없으므로 null 로 둔다.</summary>
        public StatusDefinition Status { get; }

        public TargetFilter Filter { get; }

        public ApplyStatusEffect(StatusDefinition status, TargetFilter filter = TargetFilter.Enemy)
        {
            Status = status;
            Filter = filter;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Status, path, nameof(Status), issues);
        }

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
            {
                if (context.State.IsGameOver)
                    return;

                StatusSystem.Apply(context.State, Status, target, context.Caster, context.Skill.Id);
            }
        }
    }
}
