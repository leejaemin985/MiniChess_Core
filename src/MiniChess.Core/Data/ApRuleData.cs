namespace MiniChess.Core.Data
{
    /// <summary>플레이어 공용 AP 의 보유/회복 규칙.</summary>
    public class ApRuleData
    {
        /// <summary>보유 가능한 최대 AP. 회복 시 초과분은 버린다.</summary>
        public int MaxAp { get; set; }

        /// <summary>첫 행동 가능 턴의 시작 보유량.</summary>
        public int StartAp { get; set; }

        /// <summary>
        /// 선공 이점 보정: 후공(MatchRuleData.FirstTeam 이 아닌 팀)의 시작 보유량에 더하는 AP. 한 번만 더하며(최대치까지),
        /// 이후에는 일반 AP 와 똑같이 이월/회복/최대치 규칙을 따른다(따로 소멸 처리 없음). 0 이면 보정 없음.
        /// 값의 근거는 ASSUMPTIONS.md "선공 보정" 참고.
        /// </summary>
        public int SecondPlayerStartBonus { get; set; }

        /// <summary>자기 턴 시작 시 회복량.</summary>
        public int TurnRecoveryAp { get; set; }

        /// <summary>남은 AP 를 다음 자기 턴으로 이월할지.</summary>
        public bool CarryOver { get; set; }
    }
}
