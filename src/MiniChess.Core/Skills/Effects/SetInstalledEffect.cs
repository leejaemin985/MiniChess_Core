using System;
using System.Collections.Generic;
using MiniChess.Core.Events;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 시전자(박격포)를 설치하거나(Installed = true) 해체한다(false). Unit.IsInstalled 를 바꾸며 상태효과가 아니다.
    /// 이미 그 값이면 아무것도 하지 않는다. 실제로 바뀌었을 때만 UnitInstallChangedEvent 를 남기고
    /// OnChanged 효과를 시전자 기준(ForCaster)으로 실행한다(예: 설치 시 보호막, 해체 시 보호막 제거).
    /// </summary>
    public class SetInstalledEffect : SkillEffect
    {
        /// <summary>바꿀 값. true 면 설치, false 면 해체.</summary>
        public bool Installed { get; }

        /// <summary>값이 실제로 바뀌었을 때 실행할 효과.</summary>
        public IReadOnlyList<SkillEffect> OnChanged { get; }

        public SetInstalledEffect(bool installed, IReadOnlyList<SkillEffect> onChanged = null)
        {
            Installed = installed;
            OnChanged = onChanged ?? Array.Empty<SkillEffect>();
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            CollectAll(OnChanged, $"{path}.{nameof(OnChanged)}", issues);
        }

        public override void Apply(SkillContext context)
        {
            if (context.Caster.IsInstalled == Installed)
                return;

            context.Caster.IsInstalled = Installed;
            context.State.Events.Record(new UnitInstallChangedEvent(context.Caster.Id, Installed));
            ApplyAll(OnChanged, context.ForCaster());
        }
    }
}
