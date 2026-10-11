namespace MiniChess.Core.Skills.Areas
{
    /// <summary>범위 모양의 회전(시계 방향). R0 은 그리드에 그린 모양 그대로.</summary>
    public enum AreaRotation
    {
        R0,
        R90,
        R180,
        R270,
    }

    public static class AreaRotationExtensions
    {
        /// <summary>시계 방향으로 90도 돌린 값(회전 버튼 한 번).</summary>
        public static AreaRotation Next(this AreaRotation rotation)
        {
            return (AreaRotation)(((int)rotation + 1) % 4);
        }
    }
}
