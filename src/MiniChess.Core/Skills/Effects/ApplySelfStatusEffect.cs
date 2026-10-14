using System;
using System.Collections.Generic;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 시전자에게 상태를 건다. 이미 걸려 있으면 아무것도 하지 않는다.
    /// 새로 걸었을 때만 OnApplied 효과를 시전자 기준(ForCaster)으로 실행한다(예: 설치 시 보호막).
    /// </summary>
    public class ApplySelfStatusEffect : SkillEffect
    {
        /// <summary>걸 상태. 수치가 TBD 면 null.</summary>
        public StatusDefinition Status { get; }

        /// <summary>상태를 새로 걸었을 때 실행할 효과.</summary>
        public IReadOnlyList<SkillEffect> OnApplied { get; }

        public ApplySelfStatusEffect(StatusDefinition status, IReadOnlyList<SkillEffect> onApplied = null)
        {
            Status = status;
            OnApplied = onApplied ?? Array.Empty<SkillEffect>();
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Status, path, nameof(Status), issues);
            CollectAll(OnApplied, $"{path}.{nameof(OnApplied)}", issues);
        }

        public override void Apply(SkillContext context)
        {
            if (context.Caster.FindStatus(Status.Id) != null)
                return;

            StatusSystem.Apply(context.State, Status, context.Caster, context.Caster);
            ApplyAll(OnApplied, context.ForCaster());
        }
    }
}
