using System;
using System.Collections.Generic;
using MiniChess.Core.State;

namespace MiniChess.Core.Common
{
    /// <summary>사거리 모양.</summary>
    public enum RangeShape
    {
        /// <summary>체비셰프 거리(대각선 포함 정사각형). 공격/스킬 사거리의 기본.</summary>
        Square,

        /// <summary>상하좌우 직선 위의 칸만.</summary>
        Orthogonal,
    }

    /// <summary>
    /// 사거리 계산. 기본은 대각선을 포함해 한 칸씩 넓어지는 정사각형 범위(체비셰프 거리)다.
    ///   사거리 1 → 주변 8칸, 사거리 2 → 주변 24칸 (자기 칸 제외)
    /// 벽은 사거리 판정에 영향을 주지 않는다(벽 칸도 결과에 포함되며 호출하는 쪽이 판단).
    /// </summary>
    public static class RangeCalculator
    {
        /// <summary>두 칸 사이의 거리. 가로/세로 차이 중 큰 값.</summary>
        public static int GetDistance(Position a, Position b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            return Math.Max(dx, dy);
        }

        /// <summary>target 이 from 기준 사거리 안에 있는지. 자기 칸은 포함하지 않는다.</summary>
        public static bool IsInRange(Position from, Position target, int range, RangeShape shape = RangeShape.Square)
        {
            if (shape == RangeShape.Orthogonal && from.X != target.X && from.Y != target.Y)
                return false;

            int distance = GetDistance(from, target);
            return distance >= 1 && distance <= range;
        }

        /// <summary>center 기준 사거리 안의 보드 위 칸들. 자기 칸과 보드 밖 칸은 제외한다.</summary>
        public static List<Position> GetPositionsInRange(Board board, Position center, int range, RangeShape shape = RangeShape.Square)
        {
            var positions = new List<Position>();

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -range; dy <= range; dy++)
                {
                    var position = new Position(center.X + dx, center.Y + dy);
                    if (IsInRange(center, position, range, shape) && board.IsInBounds(position))
                        positions.Add(position);
                }
            }

            return positions;
        }
    }
}
