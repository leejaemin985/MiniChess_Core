using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Capture;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Movement
{
    /// <summary>
    /// 경로를 한 칸씩 진행하며 칸 효과를 발동시킨다. 걷기/돌진/넉백/당기기/워프 공통.
    /// AP 와 행동 가능 여부는 알지 못한다(각 행동이 검사한다).
    /// </summary>
    public static class MovementResolver
    {
        /// <summary>
        /// path 를 따라 유닛을 이동시킨다(현재 위치 제외, 도착 칸 포함).
        /// 워프는 도착 칸 하나만 담아 넘긴다. 한 칸 이동할 때마다 UnitMovedEvent 를 기록한다.
        ///
        /// 다음 경우 그 자리에서 멈춘다.
        ///   - 다음 칸에 들어갈 수 없음(벽/유닛/범위 밖) → 들어가기 전 칸에서 멈춤
        ///   - 칸 효과가 Stop 을 반환 → 그 칸에서 멈춤 (ignoreStopRequests 면 계속 진행)
        ///   - 칸 효과로 유닛이 사망 → 그 칸에서 멈춤
        /// </summary>
        /// <param name="initiator">이동을 일으킨 유닛. 자신이면 자발적 이동으로 보고 이번 턴 이동 칸 수에 누적한다.</param>
        /// <param name="kind">이동 방식(이벤트 기록 및 정책 판정용).</param>
        /// <param name="ignoreStopRequests">칸 효과의 Stop 요청을 무시하고 계속 진행한다(돌진 등). 효과 자체는 발동한다.</param>
        internal static MovementResult Resolve(
            GameState state, Unit unit, Unit initiator, MoveKind kind, IReadOnlyList<Position> path, bool ignoreStopRequests = false)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (path == null) throw new ArgumentNullException(nameof(path));
            if (!unit.IsPlaced) throw new InvalidOperationException($"유닛 {unit.Id} 는 보드 위에 없음");

            Board board = state.Board;
            Position from = unit.Position.Value;
            Position lastPosition = from;
            int cellsMoved = 0;

            foreach (Position next in path)
            {
                if (!board.CanPlace(next))
                    break;

                Position previous = unit.Position.Value;
                board.Relocate(unit, next);
                state.Events.Record(new UnitMovedEvent(unit, previous, next, kind, initiator));
                lastPosition = next;
                cellsMoved++;

                bool stopRequested = TriggerEnter(state, unit) && !ignoreStopRequests;
                if (stopRequested || !unit.IsAlive)
                    break;
            }

            // 이동 거리 제한은 일반 경로 이동에만 걸리므로, 누적도 일반 이동만 센다(돌진/도약 등 스킬 이동은 제외).
            if (cellsMoved > 0 && kind == MoveKind.Path && MovementControl.IsVoluntary(unit, initiator))
                unit.TurnState.AddVoluntaryCellsMoved(cellsMoved);

            if (cellsMoved > 0 && unit.IsAlive)
                TriggerStop(state, unit);

            if (cellsMoved > 0)
                CaptureSystem.OnOccupancyChanged(state);

            bool wasInterrupted = cellsMoved < path.Count;
            return new MovementResult(unit, from, unit.Position ?? lastPosition, cellsMoved, wasInterrupted);
        }

        /// <summary>
        /// 두 유닛의 자리를 맞바꾼다(경로 없음). 각자 도착 칸의 효과가 a → b 순으로 발동한다(OnEnter 후 OnStop).
        /// 이미 도착한 칸이므로 Stop 요청은 의미가 없다. 앞선 발동으로 경기가 끝나면 나머지는 처리하지 않는다.
        /// </summary>
        /// <param name="initiator">교환을 일으킨 유닛. 이동 이벤트 기록용.</param>
        internal static void ResolveSwap(GameState state, Unit a, Unit b, Unit initiator)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));

            Position positionA = a.Position.Value;
            Position positionB = b.Position.Value;

            state.Board.Swap(a, b);
            state.Events.Record(new UnitMovedEvent(a, positionA, positionB, MoveKind.Swap, initiator));
            state.Events.Record(new UnitMovedEvent(b, positionB, positionA, MoveKind.Swap, initiator));

            foreach (Unit unit in new[] { a, b })
            {
                if (state.IsGameOver)
                    break;
                if (!unit.IsAlive)
                    continue;

                TriggerEnter(state, unit);
                if (unit.IsAlive)
                    TriggerStop(state, unit);
            }

            CaptureSystem.OnOccupancyChanged(state);
        }

        /// <summary>현재 칸의 모든 효과에 OnEnter 를 호출한다. 하나라도 Stop 이면 true.</summary>
        private static bool TriggerEnter(GameState state, Unit unit)
        {
            CellEffectContext context = CreateContext(state, unit);
            bool stopRequested = false;

            // 발동 중 효과가 제거/교체될 수 있으므로 목록을 먼저 복사한다.
            foreach (ICellEffect effect in context.Cell.GetEffectsInOrder().ToList())
            {
                // 한 효과가 Stop 을 요청해도, 이미 들어온 칸의 나머지 효과는 모두 발동한다.
                if (effect.OnEnter(context) == CellEnterResult.Stop)
                    stopRequested = true;

                if (!unit.IsAlive)
                    break;
            }

            return stopRequested;
        }

        private static void TriggerStop(GameState state, Unit unit)
        {
            CellEffectContext context = CreateContext(state, unit);

            foreach (ICellEffect effect in context.Cell.GetEffectsInOrder().ToList())
            {
                effect.OnStop(context);

                if (!unit.IsAlive)
                    break;
            }
        }

        private static CellEffectContext CreateContext(GameState state, Unit unit)
        {
            BoardCell cell = state.Board.GetCell(unit.Position.Value);
            return new CellEffectContext(state, cell, unit);
        }
    }
}
