namespace MiniChess.Core.Actions
{
    public enum AttackFailReason
    {
        None,
        NotBattlePhase,
        NotYourTurn,

        /// <summary>요청한 Id 의 유닛이 없음.</summary>
        UnknownUnit,

        /// <summary>요청한 팀의 유닛이 아님.</summary>
        NotYourUnit,

        /// <summary>공격하는 유닛이 보드 위에 없거나 사망.</summary>
        AttackerNotOnBoard,

        /// <summary>대상이 보드 위에 없거나 사망.</summary>
        InvalidTarget,

        TargetNotEnemy,
        OutOfRange,
        NotEnoughAp,

        /// <summary>이번 턴에 더 이상 행동할 수 없는 유닛(예: 분신 소환 직후).</summary>
        ActionsEnded,

        /// <summary>상태효과가 기본 공격을 막음(설치 중인 박격포 등).</summary>
        BlockedByStatus,

        /// <summary>요청 형식이 잘못됨(요청 없음, 존재하지 않는 팀 등).</summary>
        InvalidRequest,

        /// <summary>이번 턴에 이미 기본 공격(장애물 공격 포함)을 함. 기본 공격은 유닛당 턴에 한 번.</summary>
        AlreadyUsedThisTurn,
    }
}
