namespace MiniChess.Core.Statuses.Library
{
    /// <summary>차징: 다음 기본 공격에 추가 피해를 더하고 소모된다. 스킬 피해에는 적용되지 않는다.</summary>
    public class ChargedBehavior : StatusBehavior
    {
        public override System.Collections.Generic.IReadOnlyDictionary<string, int> Parameters =>
            new System.Collections.Generic.Dictionary<string, int> { [nameof(BonusDamage)] = BonusDamage };

        public int BonusDamage { get; }

        public ChargedBehavior(int bonusDamage)
        {
            BonusDamage = bonusDamage;
        }

        public override int BasicAttackBonus(StatusContext context)
        {
            return BonusDamage;
        }
    }
}
