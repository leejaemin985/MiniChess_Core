namespace MiniChess.Core.Statuses
{
    /// <summary>스킬이 아닌 상태 원인(StatusEffect.OriginId) 값. 스킬로 건 상태는 스킬 Id 를 원인으로 쓴다.</summary>
    public static class StatusOrigins
    {
        /// <summary>기본 공격에 붙는 상태(점령 보상 등).</summary>
        public const string BasicAttack = "BASIC_ATTACK";
    }
}
