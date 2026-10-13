using System;

namespace MiniChess.Core.Common
{
    /// <summary>보드 위 좌표. (0,0) 은 좌하단 기준.</summary>
    public readonly struct Position : IEquatable<Position>
    {
        public readonly int X;
        public readonly int Y;

        public Position(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>target 쪽 방향(가로/세로 각각 -1, 0, 1). 같은 칸이면 (0, 0).</summary>
        public (int X, int Y) DirectionTo(Position target) => (Math.Sign(target.X - X), Math.Sign(target.Y - Y));

        /// <summary>(dx, dy) 만큼 떨어진 칸.</summary>
        public Position Offset(int dx, int dy) => new Position(X + dx, Y + dy);

        /// <summary>target 쪽으로 한 칸 나아간 칸. 같은 칸이면 자기 자신.</summary>
        public Position StepToward(Position target)
        {
            (int dx, int dy) = DirectionTo(target);
            return Offset(dx, dy);
        }

        public bool Equals(Position other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Position other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(Position a, Position b) => a.Equals(b);
        public static bool operator !=(Position a, Position b) => !a.Equals(b);
    }
}
