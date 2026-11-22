using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.State;

namespace MiniChess.Core.Combat
{
    /// <summary>
    /// 장애물 피해. 규칙: 장애물은 체력만 있는 중립 유닛이다. 적을 치는 피해는 어느 팀 것이든 맞고, 피해 외 효과는 받지 않는다.
    /// 장애물은 칸 효과라 DamageSystem 대신 여기서 피해를 받는다. 부서져도 승패와 무관하다.
    /// </summary>
    public static class ObstacleDamage
    {
        /// <summary>칸의 장애물. 보드 밖이거나 장애물이 없으면 null.</summary>
        public static Obstacle Find(GameState state, Position position)
        {
            if (!state.Board.IsInBounds(position))
                return null;

            return state.Board.GetCell(position).GetEffect(CellEffectLayer.Obstacle) as Obstacle;
        }

        /// <summary>칸의 장애물에 피해를 준다. 장애물이 없거나 경기가 끝났으면 아무것도 하지 않는다. 파괴되었으면 true.</summary>
        public static bool Apply(GameState state, Unit source, Position position, int amount, DamageType type)
        {
            if (state.IsGameOver || amount <= 0)
                return false;

            Obstacle obstacle = Find(state, position);
            return obstacle != null && obstacle.TakeDamage(state, source, position, amount, type);
        }

        /// <summary>칸들(순서대로, 같은 칸은 한 번)의 장애물에 피해를 준다. 범위 스킬/포격 공용.</summary>
        public static void ApplyInCells(GameState state, Unit source, IEnumerable<Position> cells, int amount, DamageType type)
        {
            foreach (Position position in cells.Distinct())
            {
                if (state.IsGameOver)
                    return;

                Apply(state, source, position, amount, type);
            }
        }

        /// <summary>보드 위 모든 장애물 칸((y, x) 순). 피로도처럼 전체에 주는 피해용.</summary>
        public static List<Position> AllPositions(GameState state)
        {
            return state.Board.AllCells()
                .Where(cell => cell.GetEffect(CellEffectLayer.Obstacle) is Obstacle)
                .Select(cell => cell.Position)
                .ToList();
        }
    }
}
