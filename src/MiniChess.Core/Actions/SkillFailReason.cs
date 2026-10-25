namespace MiniChess.Core.Actions
{
    public enum SkillFailReason
    {
        None,
        NotBattlePhase,
        NotYourTurn,

        /// <summary>요청한 Id 의 유닛이 없음.</summary>
        UnknownUnit,

        /// <summary>요청한 팀의 유닛이 아님.</summary>
        NotYourUnit,

        /// <summary>시전자가 보드 위에 없거나 사망.</summary>
        CasterNotOnBoard,

        /// <summary>시전자가 가진 스킬이 아니거나, 경기 스킬 목록에 정의가 없음.</summary>
        SkillNotOwned,

        /// <summary>스킬의 필수 설정(TBD 수치 등)이 비어 있음.</summary>
        ConfigMissing,

        /// <summary>이번 턴에 더 이상 행동할 수 없는 유닛.</summary>
        ActionsEnded,

        /// <summary>스킬 사용 조건을 만족하지 않음.</summary>
        ConditionNotMet,

        /// <summary>지정할 수 없는 칸.</summary>
        InvalidTarget,

        NotEnoughAp,

        /// <summary>상태효과가 스킬 사용을 막음(봉인 등).</summary>
        BlockedByStatus,

        /// <summary>요청 형식이 잘못됨(요청 없음, 존재하지 않는 팀 등).</summary>
        InvalidRequest,

        /// <summary>이번 턴에 이미 이 스킬을 씀. 각 스킬은 유닛당 턴에 한 번.</summary>
        AlreadyUsedThisTurn,
    }
}
