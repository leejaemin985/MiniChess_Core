using MiniChess.Core.Combat;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 저주 표식: 표식 소유자가 대상에게 직접 피해를 주면 표식을 소모하고 추가 피해(MarkBonus)를 1회 준다.
    /// 표식 소유자는 상태를 건 유닛의 소환자(분신이 걸었으면 그 본체)이며, 소환물이 아니면 건 유닛 자신이다.
    /// 피해가 0 이거나 보호막에 흡수되어도 발동한다. 추가 피해는 직접 피해가 아니므로 다시 발동하지 않는다.
    /// </summary>
    public class CurseMarkBehavior : StatusBehavior
    {
        public override System.Collections.Generic.IReadOnlyDictionary<string, int> Parameters =>
            new System.Collections.Generic.Dictionary<string, int> { [nameof(BonusDamage)] = BonusDamage };

        public int BonusDamage { get; }

        public CurseMarkBehavior(int bonusDamage)
        {
            BonusDamage = bonusDamage;
        }

        public override void OnDamaged(StatusContext context, DamageRequest request)
        {
            if (request.Type != DamageType.Direct || request.Source == null)
                return;

            Unit markedBy = context.Status.Source;
            Unit owner = markedBy?.SummonOwner ?? markedBy;
            if (owner != request.Source)
                return;

            StatusSystem.Remove(context.State, context.Status, StatusRemoveReason.Consumed);
            DamageSystem.Apply(context.State, new DamageRequest(owner, context.Target, BonusDamage, DamageType.MarkBonus));
        }
    }
}
