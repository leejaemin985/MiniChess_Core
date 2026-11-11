namespace MiniChess.Core.Data
{
    /// <summary>한 경기에 적용되는 규칙 묶음. 맵과 유닛 데이터는 별도로 관리한다.</summary>
    public class GameRuleData
    {
        public ApRuleData Ap { get; set; }
        public ActionCostData ActionCost { get; set; }
        public MatchRuleData Match { get; set; }
        public CaptureRuleData Capture { get; set; }

        /// <summary>토큰 규칙. null 이면 토큰이 나오지 않는다.</summary>
        public TokenRuleData Token { get; set; }
    }
}
