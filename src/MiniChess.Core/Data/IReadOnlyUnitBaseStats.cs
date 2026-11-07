using System.Collections.Generic;

namespace MiniChess.Core.Data
{
    /// <summary>유닛 기본 능력치의 읽기 전용 뷰. 게임 로직은 이 인터페이스로만 참조한다.</summary>
    public interface IReadOnlyUnitBaseStats
    {
        string Id { get; }
        string Name { get; }
        int MaxHp { get; }

        /// <summary>기본 공격 피해량.</summary>
        int Attack { get; }

        /// <summary>기본 공격 사거리(칸).</summary>
        int AttackRange { get; }

        /// <summary>이동 AP 배율. 이동 비용 = 칸 수 × 칸당 AP × 이 값(0 이면 이동에 AP 를 쓰지 않는다).</summary>
        int MoveCostMultiplier { get; }

        /// <summary>보유 스킬 Id(슬롯 순). 미정 슬롯은 포함하지 않는다.</summary>
        IReadOnlyList<string> SkillIds { get; }
    }
}
