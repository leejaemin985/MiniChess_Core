using System;
using System.Collections.Generic;
using MiniChess.Core.Events;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 시전자에게 걸린 상태를 푼다. 걸려 있지 않으면 아무것도 하지 않는다.
    /// 풀었을 때만 OnRemoved 효과를 시전자 기준(ForCaster)으로 실행한다(예: 해체 시 보호막 제거).
    /// </summary>
    public class RemoveSelfStatusEffect : SkillEffect
    {
        public string StatusId { get; }

        /// <summary>상태를 풀었을 때 실행할 효과.</summary>
        public IReadOnlyList<SkillEffect> OnRemoved { get; }

        public RemoveSelfStatusEffect(string statusId, IReadOnlyList<SkillEffect> onRemoved = null)
        {
            StatusId = statusId;
            OnRemoved = onRemoved ?? Array.Empty<SkillEffect>();
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(StatusId, path, nameof(StatusId), issues);
            CollectAll(OnRemoved, $"{path}.{nameof(OnRemoved)}", issues);
        }

        public override void Apply(SkillContext context)
        {
            StatusEffect existing = context.Caster.FindStatus(StatusId);
            if (existing == null)
                return;

            StatusSystem.Remove(context.State, existing, StatusRemoveReason.Removed);
            ApplyAll(OnRemoved, context.ForCaster());
        }
    }
}
