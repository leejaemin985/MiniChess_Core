using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 스킬 한 번으로 설치된 칸 효과 묶음(장판 구역, 덫 1개 등). 수명은 칸이 아니라 구역 단위로 센다.
    /// 일부 칸이 다른 장판에 덮어써져도 남은 칸은 구역 수명을 따른다(명세 3.4 "남은 전체 장판 오브젝트의 수명 관리").
    /// 수명은 설치한 쪽 소유자의 Turn End(EndDurationTick)마다 1 줄며, 설치한 턴의 종료는 세지 않는다.
    /// </summary>
    public class FieldZone
    {
        private readonly List<(Position Position, ZoneCellEffect Effect)> _cells = new List<(Position, ZoneCellEffect)>();

        public string SkillId { get; }

        /// <summary>설치한 유닛. 설치 후 사망해도 구역은 남는다.</summary>
        public Unit Source { get; }

        public Team OwnerTeam { get; }

        /// <summary>남은 수명(설치한 쪽 소유자의 턴 종료 횟수).</summary>
        public int Remaining { get; private set; }

        public int PlacedTurnNumber { get; }

        private int _lastTickTurnNumber = -1;

        public FieldZone(string skillId, Unit source, int lifetime, int placedTurnNumber)
        {
            SkillId = skillId;
            Source = source;
            OwnerTeam = source.Team;
            Remaining = lifetime;
            PlacedTurnNumber = placedTurnNumber;
        }

        internal void Register(Position position, ZoneCellEffect effect)
        {
            _cells.Add((position, effect));
        }

        /// <summary>턴 종료 시 수명 감소. 같은 턴에 여러 칸에서 호출되어도 한 번만 줄인다.</summary>
        internal void TickLifetime(GameState state)
        {
            if (_lastTickTurnNumber == state.TurnNumber || state.TurnNumber == PlacedTurnNumber)
                return;

            _lastTickTurnNumber = state.TurnNumber;
            Remaining--;

            if (Remaining <= 0)
                RemoveAll(state, CellEffectRemoveReason.Expired);
        }

        internal void RemoveAll(GameState state, CellEffectRemoveReason reason)
        {
            foreach ((Position position, ZoneCellEffect effect) in _cells)
            {
                if (state.Board.GetCell(position).Contains(effect))
                    CellEffectSystem.Remove(state, position, effect.Layer, reason);
            }
        }
    }
}
