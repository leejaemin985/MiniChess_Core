namespace MiniChess.Core.Movement
{
    /// <summary>
    /// 위치 변경의 방식. 자발적/외부 여부는 이 값이 아니라 "이동을 일으킨 유닛이 이동하는 유닛 자신인지"로 판정한다.
    /// (예: 같은 워프라도 자신을 옮기면 자발적 이동, 다른 유닛이 옮기면 외부 이동)
    /// </summary>
    public enum MoveKind
    {
        /// <summary>기본 경로 이동. 직교 직선, 칸당 AP.</summary>
        Path,

        /// <summary>돌진. 공격을 포함하는 특수 이동.</summary>
        Dash,

        /// <summary>위치 교환.</summary>
        Swap,

        /// <summary>순간이동(경로 없이 도착 칸만).</summary>
        Warp,

        /// <summary>밀려남.</summary>
        Knockback,

        /// <summary>당겨짐.</summary>
        Pull,
    }
}
