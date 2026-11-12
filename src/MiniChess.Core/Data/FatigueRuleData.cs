namespace MiniChess.Core.Data
{
    /// <summary>라운드별로 증가하는 전 유닛 턴 종료 피해. 한 라운드는 양 팀의 턴 한 번씩이다.</summary>
    public class FatigueRuleData
    {
        public int StartRound { get; set; } = 8;
        public int InitialDamage { get; set; } = 1;
        public int DamageIncreasePerRound { get; set; } = 1;
    }
}
