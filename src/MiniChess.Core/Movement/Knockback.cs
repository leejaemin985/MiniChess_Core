using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Movement
{
    /// <summary>
    /// 밀어내기 코어 규칙(사용자 확정).
    /// 방향: 미는 유닛과 대상의 세로 차이가 가로 차이 이상이면 상하, 가로 차이가 더 크면 좌우. 미는 유닛에서 멀어지는 쪽.
    /// 이동: 그 방향으로 한 칸씩 밀리며 지나는 칸의 효과가 발동한다(덫의 멈춤도 적용).
    /// 벽/유닛/장애물/보드 끝에 막히면 그 앞에서 멈추고 추가 피해는 없다.
    /// </summary>
    public static class Knockback
    {
        /// <summary>pusher 가 target 을 미는 방향(상하좌우 단위 벡터). 같은 칸이면 (0, 0).</summary>
        public static (int X, int Y) GetDirection(Position pusher, Position target)
        {
            int dx = target.X - pusher.X;
            int dy = target.Y - pusher.Y;

            return Math.Abs(dy) >= Math.Abs(dx)
                ? (0, Math.Sign(dy))
                : (Math.Sign(dx), 0);
        }

        /// <summary>
        /// target 을 pusher 반대쪽으로 cells 칸 민다. 대상이 보드 위에 없거나, 상태효과가 외부 이동을 막으면(속박 정책 등) 밀지 않는다.
        /// </summary>
        /// <returns>이동 결과. 밀지 않았으면 null.</returns>
        internal static MovementResult Push(GameState state, Unit target, Unit pusher, int cells)
        {
            if (!target.IsOnBoard || !pusher.IsPlaced || cells <= 0)
                return null;

            Position from = target.Position.Value;
            (int dx, int dy) = GetDirection(pusher.Position.Value, from);
            if (dx == 0 && dy == 0)
                return null;

            var path = new List<Position>();
            for (int i = 1; i <= cells; i++)
                path.Add(from.Offset(dx * i, dy * i));

            if (MovementControl.Check(state, target, pusher, MoveKind.Knockback, path.Count) != MoveBlockReason.None)
                return null;

            return MovementResolver.Resolve(state, target, pusher, MoveKind.Knockback, path);
        }
    }
}
