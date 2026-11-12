namespace MiniChess.Core.Turns
{
    /// <summary>
    /// 턴 처리 단계. 선언 순서가 처리 순서다(기획 v0.7 3.3).
    ///   Turn Start: AP 회복 → 긍정 시작 효과 → 행동 제한 확인 → 시작 시점 예약 효과 → 점령 판정
    ///   Turn End:   종료형 회복 → DoT → 현재 위치의 종료형 장판 → 전 유닛 피로도 → 지속시간 감소·제거
    /// 피로도는 TurnSystem 이 직접 실행하며 상태/칸 효과 훅으로 등록하지 않는다.
    /// 점령 판정은 명세 v0.1 3.3 의 Turn End 에서 자기 Turn Start 로 옮겼다(사용자 확정).
    /// 상태효과/칸 효과는 자신이 반응할 단계와 기준 소유자를 데이터로 지정한다.
    /// StartApRecovery 와 StartCapture 는 시스템 전용 단계라 효과 시점으로 쓸 수 없다.
    /// </summary>
    public enum TurnStep
    {
        StartApRecovery,
        StartPositiveEffects,
        StartRestrictionCheck,
        StartScheduledEffects,
        StartCapture,

        EndHeal,
        EndDamageOverTime,
        EndAreaEffects,
        EndDurationTick,
    }
}
