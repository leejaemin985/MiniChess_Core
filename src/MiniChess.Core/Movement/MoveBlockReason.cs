namespace MiniChess.Core.Movement
{
    /// <summary>유닛 상태 때문에 이동이 막히는 이유. 원인별로 구분한다(하나의 CanMove 로 합치지 않는다).</summary>
    public enum MoveBlockReason
    {
        None,

        /// <summary>이번 턴 행동이 끝난 유닛의 자발적 이동.</summary>
        ActionsEnded,

        /// <summary>전투 행동 후의 자발적 이동.</summary>
        VoluntaryMoveLocked,

        /// <summary>속박 등 이동 불가 상태.</summary>
        Rooted,

        /// <summary>이동 거리 제한 상태(볼라 등)의 한도 초과.</summary>
        DistanceLimited,

        /// <summary>설치 상태라 자발적 이동 불가(박격포).</summary>
        Installed,

        /// <summary>봉인 상태라 어떤 이동도 불가.</summary>
        Sealed,
    }
}
