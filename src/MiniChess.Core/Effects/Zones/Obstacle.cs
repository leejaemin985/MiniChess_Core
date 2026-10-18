using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 장애물: 칸을 막아 유닛이 들어갈 수 없게 한다(Board.CanPlace). 공격/스킬 사거리는 막지 않는다.
    /// 기본 공격(AttackObstacleAction)에 Hits 번 맞으면 파괴된다. 피해량과 무관하고, 스킬 피해로는 부서지지 않는다.
    /// 수명은 구역(FieldZone)이 관리한다.
    /// </summary>
    public class Obstacle : ZoneCellEffect
    {
        public override CellEffectLayer Layer => CellEffectLayer.Obstacle;
        public override string Kind => CellEffectKinds.Obstacle;

        /// <summary>파괴까지 남은 피격 횟수.</summary>
        public int HitsRemaining { get; private set; }

        public Obstacle(FieldZone zone, int hits) : base(zone)
        {
            HitsRemaining = hits;
        }

        /// <summary>기본 공격 1회를 받는다. 남은 횟수가 0 이 되면 파괴된다. 파괴되었으면 true.</summary>
        internal bool TakeHit(GameState state, Unit attacker, Position position)
        {
            HitsRemaining--;
            state.Events.Record(new ObstacleHitEvent(attacker.Id, position, HitsRemaining));

            if (HitsRemaining > 0)
                return false;

            CellEffectSystem.Remove(state, position, Layer, CellEffectRemoveReason.Destroyed);
            return true;
        }
    }
}
