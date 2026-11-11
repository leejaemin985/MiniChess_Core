namespace MiniChess.Core.Tokens
{
    /// <summary>토큰 종류.</summary>
    public enum TokenKind
    {
        /// <summary>먹은 유닛의 체력을 회복한다(TokenRuleData.HealAmount).</summary>
        Heal,

        /// <summary>먹은 유닛의 팀이 AP 를 얻는다(TokenRuleData.ApAmount).</summary>
        Ap,
    }
}
