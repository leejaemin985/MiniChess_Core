namespace MiniChess.Core.Combat
{
    /// <summary>
    /// 피해의 종류. 피해 감소/보호막/호위 등이 종류별로 다르게 판정할 수 있도록 구분한다.
    /// 종류 자체가 면역을 의미하지는 않는다.
    /// </summary>
    public enum DamageType
    {
        /// <summary>기본 공격, 공격 스킬의 직접 피해.</summary>
        Direct,

        /// <summary>화상/독/출혈 등 지속 피해.</summary>
        DoT,

        /// <summary>장판 피해.</summary>
        Area,

        /// <summary>표식 등에 의한 추가 피해.</summary>
        MarkBonus,

        /// <summary>호위 등으로 다른 유닛에게 전가된 피해.</summary>
        Redirected,

        /// <summary>턴 종료 시 라운드에 따라 증가하는 피로도 피해.</summary>
        Fatigue,
    }
}
