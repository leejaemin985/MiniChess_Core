using System;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Turns;
using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Effects
{
    public enum CellEffectPlaceFailReason
    {
        None,
        OutOfBounds,
        Wall,

        /// <summary>점령 칸에 설치할 수 없는 레이어(덫, 장애물).</summary>
        CaptureTileRestricted,
    }

    /// <summary>
    /// 칸 효과의 설치/제거/턴 단계 처리.
    ///   - 레이어마다 칸당 1개. 같은 레이어에 새로 설치하면 기존 효과를 덮어쓴다(장판 위 장판 → 교체).
    ///   - 다른 레이어는 공존한다(장판 + 덫).
    ///   - 점령 칸에는 덫/장애물 설치 불가, 장판은 허용(명세 3.5, 11.3).
    /// </summary>
    public static class CellEffectSystem
    {
        public static bool IsAllowedOnCaptureTile(CellEffectLayer layer)
        {
            switch (layer)
            {
                case CellEffectLayer.Trap: return false;
                case CellEffectLayer.AreaEffect: return true;
                case CellEffectLayer.Obstacle: return false;
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        public static CellEffectPlaceFailReason CanPlace(GameState state, Position position, ICellEffect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));

            return CanPlace(state, position, effect.Layer);
        }

        /// <summary>해당 레이어의 효과를 이 칸에 설치할 수 있는지(지정 가능 칸 판정용).</summary>
        public static CellEffectPlaceFailReason CanPlace(GameState state, Position position, CellEffectLayer layer)
        {
            if (!state.Board.IsInBounds(position)) return CellEffectPlaceFailReason.OutOfBounds;

            BoardCell cell = state.Board.GetCell(position);
            if (cell.IsWall) return CellEffectPlaceFailReason.Wall;
            if (cell.IsCaptureTile && !IsAllowedOnCaptureTile(layer)) return CellEffectPlaceFailReason.CaptureTileRestricted;

            return CellEffectPlaceFailReason.None;
        }

        /// <summary>규칙을 검사하고 설치한다. 같은 레이어의 기존 효과는 제거(Replaced) 후 설치된다.</summary>
        internal static CellEffectPlaceFailReason Place(GameState state, Position position, ICellEffect effect)
        {
            CellEffectPlaceFailReason reason = CanPlace(state, position, effect);
            if (reason != CellEffectPlaceFailReason.None)
                return reason;

            Remove(state, position, effect.Layer, CellEffectRemoveReason.Replaced);

            state.Board.GetCell(position).SetEffect(effect);
            state.Events.Record(new CellEffectPlacedEvent(SnapshotBuilder.CellEffect(position, effect)));
            return CellEffectPlaceFailReason.None;
        }

        /// <summary>해당 레이어의 효과를 제거한다. 제거할 효과가 없으면 false.</summary>
        internal static bool Remove(GameState state, Position position, CellEffectLayer layer, CellEffectRemoveReason reason)
        {
            BoardCell cell = state.Board.GetCell(position);
            ICellEffect existing = cell.GetEffect(layer);
            if (existing == null)
                return false;

            cell.RemoveEffect(layer);
            state.Events.Record(new CellEffectRemovedEvent(position, existing.Layer, existing.Kind, reason));
            return true;
        }

        /// <summary>
        /// 턴 단계 처리. 칸을 (y, x) 오름차순으로, 칸 안에서는 레이어 순으로 ICellTurnEffect 를 호출한다.
        /// [정책] 같은 단계 안의 칸 처리 순서는 명세 TBD 에 대한 임시 결정이다.
        /// </summary>
        internal static void RunStep(TurnContext context)
        {
            GameState state = context.State;

            foreach (BoardCell cell in state.Board.AllCells())
            {
                foreach (ICellTurnEffect effect in cell.GetEffectsInOrder().OfType<ICellTurnEffect>().ToList())
                {
                    if (state.IsGameOver)
                        return;

                    // 앞선 효과 처리로 제거/교체되었을 수 있다.
                    if (!cell.Contains(effect))
                        continue;

                    effect.OnTurnStep(new CellTurnContext(context, cell));
                }
            }
        }
    }
}
