namespace MiniChess.Core.Data
{
    /// <summary>한 경기에 적용되는 규칙 묶음. 맵과 유닛 데이터는 별도로 관리한다.</summary>
    public class GameRuleData
    {
        public ApRuleData Ap { get; set; }
        public ActionCostData ActionCost { get; set; }
        public MatchRuleData Match { get; set; }
        public CaptureRuleData Capture { get; set; }

        /// <summary>유닛 한 턴의 행동 제한. 지정하지 않으면 명세 규칙(전투 행동 턴당 1회, 전투 후 이동 금지).</summary>
        public TurnActionRuleData TurnAction { get; set; } = new TurnActionRuleData();
    }
}
