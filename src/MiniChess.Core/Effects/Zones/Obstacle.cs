using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 장애물: 칸을 막아 유닛이 들어갈 수 없게 한다(Board.CanPlace). 공격/스킬 사거리는 막지 않는다.
    /// 체력만 있는 중립 유닛처럼 피해(ObstacleDamage)를 받아 체력이 0 이 되면 파괴된다. 수명은 구역(FieldZone)이 관리한다.
    /// </summary>
    public class Obstacle : ZoneCellEffect
    {
        public override CellEffectLayer Layer => CellEffectLayer.Obstacle;
        public override string Kind => CellEffectKinds.Obstacle;

        public int MaxHp { get; }

        /// <summary>남은 체력. 0 이 되면 파괴(제거)된다.</summary>
        public int Hp { get; private set; }

        public Obstacle(FieldZone zone, int maxHp) : this(zone, maxHp, maxHp)
        {
        }

        public Obstacle(FieldZone zone, int maxHp, int hp) : base(zone)
        {
            MaxHp = maxHp;
            Hp = hp;
        }

        internal override ZoneCellEffect Fork(FieldZone zone) => new Obstacle(zone, MaxHp, Hp);

        /// <summary>피해를 받는다(amount &gt; 0). 체력이 0 이 되면 파괴된다. 파괴되었으면 true.</summary>
        internal bool TakeDamage(GameState state, Unit source, Position position, int amount, DamageType type)
        {
            int applied = System.Math.Min(Hp, amount);
            Hp -= applied;
            state.Events.Record(new ObstacleDamagedEvent(source?.Id, position, type, amount, applied, Hp, MaxHp));

            if (Hp > 0)
                return false;

            CellEffectSystem.Remove(state, position, Layer, CellEffectRemoveReason.Destroyed);
            return true;
        }
    }
}
