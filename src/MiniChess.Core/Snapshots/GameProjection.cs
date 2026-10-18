using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;

namespace MiniChess.Core.Snapshots
{
    /// <summary>
    /// 한 팀 시점의 투영: 상대에게 숨겨야 할 정보를 스냅샷과 이벤트에서 거른다. 서버는 이 결과만 각 클라이언트에 보낸다.
    /// 숨김 규칙은 이 클래스에만 둔다. 지금 규칙(사용자 확정):
    ///   - 상대 덫(ROOT_TRAP): 설치 위치를 숨긴다. 밟아서 발동하면 그 칸을 알린다(발동 이벤트, 소모로 인한 제거).
    ///   - 상대 예약 포격: 조준 칸을 숨긴다. 착탄할 때만 알린다(착탄 이벤트). 시전자 사망으로 취소된 포격도 숨긴다.
    ///   - 상대가 그 스킬을 썼다는 사실(시전자, 스킬 Id)은 알린다. 지정 칸과 범위만 비운다.
    ///   - 배치 단계에서 상대 배치(유닛과 위치)를 숨긴다. 상대가 확정했다는 사실(HasPlaced, 빈 배치 이벤트)은 알리고,
    ///     두 팀이 모두 확정해 전투가 시작되면 공개한다.
    /// 그 밖의 정보는 숨기지 않는다.
    /// </summary>
    internal static class GameProjection
    {
        public static GameSnapshot Snapshot(GameState state, Team viewer)
        {
            GameSnapshot full = SnapshotBuilder.Game(state, viewer);
            bool hidePlacement = state.Phase == GamePhase.Setup;

            return new GameSnapshot(
                full.Viewer, full.Phase, full.CurrentTeam, full.TurnNumber, full.Winner, full.EndReason, full.LastEventSequence,
                full.BoardWidth, full.BoardHeight, full.Cells, full.Players,
                full.Units.Where(unit => !hidePlacement || unit.Team == viewer).ToList(),
                full.CellEffects.Where(effect => !IsHiddenEffect(effect.Kind, effect.OwnerTeam, viewer)).ToList(),
                full.Strikes.Where(strike => strike.SourceTeam == viewer).ToList(),
                full.Capture);
        }

        public static IReadOnlyList<GameEvent> FilterEvents(GameState state, Team viewer, IReadOnlyList<GameEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));

            var visible = new List<GameEvent>(events.Count);
            for (int i = 0; i < events.Count; i++)
            {
                GameEvent gameEvent = events[i];
                if (IsHidden(gameEvent, viewer))
                    continue;

                if (gameEvent is SkillUsedEvent used && state.FindUnit(used.CasterId)?.Team != viewer && CastHasHiddenResult(events, i, viewer))
                    gameEvent = WithoutTargets(used);

                // 상대 배치는 전투가 시작될 때까지 숨긴다(이 묶음에서 전투가 시작되었으면 그대로 공개).
                if (gameEvent is UnitsPlacedEvent placed && placed.Team != viewer && state.Phase == GamePhase.Setup)
                    gameEvent = new UnitsPlacedEvent(placed.Team, Array.Empty<UnitSnapshot>()) { Sequence = placed.Sequence };

                visible.Add(gameEvent);
            }

            return visible;
        }

        /// <summary>viewer 에게 숨기는 칸 효과인지: 상대가 설치한 덫.</summary>
        private static bool IsHiddenEffect(string kind, Team? ownerTeam, Team viewer)
        {
            return kind == CellEffectKinds.RootTrap && ownerTeam.HasValue && ownerTeam.Value != viewer;
        }

        private static bool IsHidden(GameEvent gameEvent, Team viewer)
        {
            switch (gameEvent)
            {
                case CellEffectPlacedEvent placed:
                    return IsHiddenEffect(placed.Effect.Kind, placed.Effect.OwnerTeam, viewer);
                case CellEffectRemovedEvent removed:
                    // 밟아서 소모된 덫은 발동 이벤트로 이미 알렸다. 수명 종료/덮어쓰기는 위치가 드러나므로 숨긴다.
                    return removed.Reason != CellEffectRemoveReason.Consumed && IsHiddenEffect(removed.Kind, removed.OwnerTeam, viewer);
                case StrikeScheduledEvent scheduled:
                    return scheduled.Strike.SourceTeam != viewer;
                case StrikeCancelledEvent cancelled:
                    return cancelled.Strike.SourceTeam != viewer;
                default:
                    return false;
            }
        }

        /// <summary>events[castIndex] 의 스킬 사용으로 생긴 이벤트(다음 스킬 사용 전까지) 중 숨기는 것이 있는지.</summary>
        private static bool CastHasHiddenResult(IReadOnlyList<GameEvent> events, int castIndex, Team viewer)
        {
            for (int i = castIndex + 1; i < events.Count && !(events[i] is SkillUsedEvent); i++)
            {
                if (IsHidden(events[i], viewer))
                    return true;
            }

            return false;
        }

        /// <summary>시전자와 스킬만 남기고 지정 칸과 범위를 비운 사본(순번은 그대로).</summary>
        private static SkillUsedEvent WithoutTargets(SkillUsedEvent used)
        {
            return new SkillUsedEvent(used.CasterId, used.SkillId, Array.Empty<Position>(), AreaRotation.R0, Array.Empty<Position>())
            {
                Sequence = used.Sequence,
            };
        }
    }
}
