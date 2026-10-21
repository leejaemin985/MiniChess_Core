namespace MiniChess.Core.Statuses
{
    /// <summary>
    /// 같은 상태(Definition Id)를 "같은 건"으로 보는 범위. 같은 건끼리만 StackPolicy(갱신/교체/무시)가 적용되고,
    /// 다른 건이면 따로 걸려 각자 횟수를 세고 각자 발동한다.
    /// </summary>
    public enum StatusStackScope
    {
        /// <summary>대상당 하나. 누가 어떤 스킬로 걸었든 같은 건이다(호위, 저주 표식, 차징, 설치, 봉인 등).</summary>
        PerTarget,

        /// <summary>
        /// 출처별. 건 유닛 + 원인(스킬 Id 등, StatusEffect.OriginId)이 같을 때만 같은 건이다(사용자 확정: 중독, 화상, 속박, 기절, 이동 제한).
        /// a 가 i 스킬로 다시 걸면 갱신, a 가 j 스킬로 걸거나 b 가 걸면 별개로 함께 걸린다.
        /// </summary>
        PerSource,
    }
}
