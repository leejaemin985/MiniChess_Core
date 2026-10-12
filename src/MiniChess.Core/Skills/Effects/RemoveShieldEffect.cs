using System.Collections.Generic;
using MiniChess.Core.Combat;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>범위 안 대상에게서 해당 Id 의 보호막을 남은 양과 무관하게 제거한다(ApplyShieldEffect 의 짝). 없으면 아무것도 하지 않는다.</summary>
    public class RemoveShieldEffect : SkillEffect
    {
        public string ShieldId { get; }

        public TargetFilter Filter { get; }

        public RemoveShieldEffect(string shieldId, TargetFilter filter = TargetFilter.AllyOrSelf)
        {
            ShieldId = shieldId;
            Filter = filter;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(ShieldId, path, nameof(ShieldId), issues);
        }

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
                DamageSystem.RemoveShield(context.State, target, ShieldId);
        }
    }
}
