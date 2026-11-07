using System;
using System.Collections.Generic;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;

namespace MiniChess.Core.Controller
{
    /// <summary>이동 미리보기(도착 칸을 고른 뒤 확정 전): 지나갈 칸, AP 비용, 지금 이동할 수 없으면 그 이유.</summary>
    public class MovePreview
    {
        /// <summary>지나갈 칸(출발 칸 제외, 마지막이 도착 칸). 직선이 아니거나 제자리면 빈 목록.</summary>
        public IReadOnlyList<Position> Path { get; }

        /// <summary>경로를 끝까지 갔을 때의 AP. 실제 차감은 덫 등으로 도중에 멈추면 그만큼 줄어든다.</summary>
        public int Cost { get; }

        /// <summary>지금 이 칸으로 이동할 수 있는지. None 이면 가능.</summary>
        public MoveFailReason FailReason { get; }

        public MovePreview(IReadOnlyList<Position> path, int cost, MoveFailReason failReason)
        {
            Path = path ?? Array.Empty<Position>();
            Cost = cost;
            FailReason = failReason;
        }
    }
}
