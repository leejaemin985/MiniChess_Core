using System;
using System.Collections.Generic;

namespace MiniChess.Core.Data
{
    /// <summary>
    /// 유닛 종류별 고정 능력치. setter 는 데이터 로딩(기본값/JSON)용이며,
    /// 로딩 이후에는 수정하지 않는다. 같은 인스턴스를 여러 유닛이 공유한다.
    /// </summary>
    public class UnitBaseStats : IReadOnlyUnitBaseStats
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MaxHp { get; set; }

        /// <summary>기본 공격 피해량.</summary>
        public int Attack { get; set; }

        /// <summary>기본 공격 사거리(칸).</summary>
        public int AttackRange { get; set; }

        /// <summary>이동 AP 배율. 이동 비용 = 칸 수 × 칸당 AP × 이 값(0 이면 이동에 AP 를 쓰지 않는다).</summary>
        public int MoveCostMultiplier { get; set; } = 1;

        /// <summary>보유 스킬 Id(슬롯 순).</summary>
        public IReadOnlyList<string> SkillIds { get; set; } = Array.Empty<string>();
    }
}
