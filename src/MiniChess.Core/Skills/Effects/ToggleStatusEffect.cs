using System;
using System.Collections.Generic;
using MiniChess.Core.Events;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 시전자에게 상태를 켜고 끈다(설치/해체처럼 같은 버튼이 상태에 따라 바뀌는 스킬).
    /// 걸려 있으면 제거한 뒤 OnDisable 효과를, 없으면 건 뒤 OnEnable 효과를 순서대로 실행한다.
    /// </summary>
    public class ToggleStatusEffect : SkillEffect
    {
        /// <summary>켜고 끌 상태. 수치가 TBD 면 null.</summary>
        public StatusDefinition Status { get; }

        /// <summary>상태를 켠 뒤 실행할 효과(설치 보호막 부여 등).</summary>
        public IReadOnlyList<SkillEffect> OnEnable { get; }

        /// <summary>상태를 끈 뒤 실행할 효과(설치 보호막 제거 등).</summary>
        public IReadOnlyList<SkillEffect> OnDisable { get; }

        public ToggleStatusEffect(StatusDefinition status, IReadOnlyList<SkillEffect> onEnable = null, IReadOnlyList<SkillEffect> onDisable = null)
        {
            Status = status;
            OnEnable = onEnable ?? Array.Empty<SkillEffect>();
            OnDisable = onDisable ?? Array.Empty<SkillEffect>();
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Status, path, nameof(Status), issues);

            for (int i = 0; i < OnEnable.Count; i++)
                OnEnable[i].CollectConfigIssues($"{path}.{nameof(OnEnable)}[{i}]", issues);

            for (int i = 0; i < OnDisable.Count; i++)
                OnDisable[i].CollectConfigIssues($"{path}.{nameof(OnDisable)}[{i}]", issues);
        }

        public override void Apply(SkillContext context)
        {
            StatusEffect existing = context.Caster.FindStatus(Status.Id);
            if (existing != null)
            {
                StatusSystem.Remove(context.State, existing, StatusRemoveReason.Removed);
                ApplyAll(context, OnDisable);
            }
            else
            {
                StatusSystem.Apply(context.State, Status, context.Caster, context.Caster);
                ApplyAll(context, OnEnable);
            }
        }

        private static void ApplyAll(SkillContext context, IReadOnlyList<SkillEffect> effects)
        {
            foreach (SkillEffect effect in effects)
            {
                if (context.State.IsGameOver)
                    return;

                effect.Apply(context);
            }
        }
    }
}
