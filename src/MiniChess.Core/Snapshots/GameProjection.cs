using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Snapshots
{
    /// <summary>
    /// 한 팀 시점의 투영: 상대에게 숨겨야 할 정보를 스냅샷과 이벤트에서 거른다. 서버는 이 결과만 각 클라이언트에 보낸다.
    /// [TBD] 숨길 정보(상대 덫, 예약 포격 조준 칸, 배치 단계의 상대 배치 등)와 공개 시점은 아직 정해지지 않아,
    /// 지금은 아무것도 거르지 않는다. 숨김 규칙은 이 클래스에만 넣는다.
    /// </summary>
    internal static class GameProjection
    {
        public static GameSnapshot Snapshot(GameState state, Team viewer)
        {
            return SnapshotBuilder.Game(state, viewer);
        }

        public static IReadOnlyList<GameEvent> FilterEvents(GameState state, Team viewer, IReadOnlyList<GameEvent> events)
        {
            return events;
        }
    }
}
