using MiniChess.Core.Combat;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>지속 피해(화상, 중독 등): 발동 시점마다 피해를 준다. 피해 출처는 상태를 건 유닛.</summary>
    public class DamageOverTimeBehavior : StatusBehavior
    {
        public int DamagePerTrigger { get; }

        public DamageOverTimeBehavior(int damagePerTrigger)
        {
            DamagePerTrigger = damagePerTrigger;
        }

        public override void OnTrigger(StatusContext context)
        {
            var request = new DamageRequest(context.Status.Source, context.Target, DamagePerTrigger, DamageType.DoT);
            DamageSystem.Apply(context.State, request);
        }
    }
}
