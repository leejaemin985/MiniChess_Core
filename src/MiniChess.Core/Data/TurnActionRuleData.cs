namespace MiniChess.Core.Data
{
    /// <summary>
    /// 유닛 한 턴의 행동 제한. 기본값은 명세 규칙(전투 행동 턴당 1회, 전투 행동 후 자발적 이동 금지).
    /// 둘 다 끄면 AP 가 있는 한 이동과 전투 행동을 순서 상관없이 계속할 수 있다.
    /// 스킬 자체가 행동을 끝내는 경우(SkillDefinition.EndsCasterActions, 예: 분신 소환)는 이 값과 상관없이 적용된다.
    /// </summary>
    public class TurnActionRuleData
    {
        /// <summary>전투 행동(기본 공격/전투 스킬)을 유닛당 턴에 한 번만 쓸 수 있는지.</summary>
        public bool CombatOncePerTurn { get; set; } = true;

        /// <summary>전투 행동 후 자발적 이동(일반 이동/자신을 옮기는 스킬)이 막히는지.</summary>
        public bool LockMoveAfterCombat { get; set; } = true;

        /// <summary>
        /// 기본 공격과 각 스킬을 유닛당 턴에 한 번씩만 쓸 수 있는지(서로 다른 것은 함께 쓸 수 있다: 스킬1 + 스킬2 + 기본 공격).
        /// 이동 스킬도 포함한다. CombatOncePerTurn 이 켜져 있으면 그쪽이 더 엄격하다.
        /// </summary>
        public bool EachActionOncePerTurn { get; set; }
    }
}
