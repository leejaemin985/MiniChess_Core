namespace MiniChess.Core.Actions
{
    public enum MoveFailReason
    {
        None,
        NotBattlePhase,
        NotYourTurn,

        /// <summary>요청한 Id 의 유닛이 없음.</summary>
        UnknownUnit,

        /// <summary>요청한 팀의 유닛이 아님.</summary>
        NotYourUnit,
        UnitNotOnBoard,

        /// <summary>전투 행동 후라 자발적 이동/추가 전투 행동 불가.</summary>
        AlreadyActed,

        /// <summary>상하좌우 직선이 아니거나 제자리.</summary>
        NotStraightLine,

        /// <summary>경로에 벽/유닛/범위 밖 칸이 있음.</summary>
        PathBlocked,

        NotEnoughAp,

        /// <summary>이번 턴에 더 이상 행동할 수 없는 유닛(예: 분신 소환 직후).</summary>
        ActionsEnded,

        /// <summary>속박 등 이동 불가 상태.</summary>
        Rooted,

        /// <summary>이동 거리 제한 상태의 한도 초과.</summary>
        DistanceLimited,

        /// <summary>설치 상태라 자발적 이동 불가.</summary>
        Installed,

        /// <summary>봉인 상태라 이동 불가.</summary>
        Sealed,
    }
}
