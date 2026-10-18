using MiniChess.Core.Combat;
using MiniChess.Core.Events;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 속박 덫: 적이 칸에 들어오면(OnEnterTile) 피해 + 속박을 주고 진행 중인 이동을 멈춘다(명세 9.2 [설계안]).
    /// 덫의 진입 이벤트(피해/멈춤)와 속박 상태는 분리되어 있어, 멈춤을 무시하는 이동(돌진)도 속박은 받는다.
    /// 아군은 발동시키지 않는다.
    /// </summary>
    public class RootTrap : ZoneCellEffect
    {
        public int Damage { get; }
        public StatusDefinition Root { get; }

        /// <summary>발동 후 사라지는지.</summary>
        public bool SingleUse { get; }

        public override CellEffectLayer Layer => CellEffectLayer.Trap;
        public override string Kind => CellEffectKinds.RootTrap;

        public RootTrap(FieldZone zone, int damage, StatusDefinition root, bool singleUse) : base(zone)
        {
            Damage = damage;
            Root = root;
            SingleUse = singleUse;
        }

        public override CellEnterResult OnEnter(CellEffectContext context)
        {
            if (context.Unit.Team == Zone.OwnerTeam)
                return CellEnterResult.Continue;

            if (Damage > 0)
                DamageSystem.Apply(context.State, new DamageRequest(Zone.Source, context.Unit, Damage, DamageType.Area));

            if (context.Unit.IsAlive)
                StatusSystem.Apply(context.State, Root, context.Unit, Zone.Source, Zone.OwnerTeam);

            if (SingleUse && context.Cell.Contains(this))
                CellEffectSystem.Remove(context.State, context.Cell.Position, Layer, CellEffectRemoveReason.Consumed);

            return CellEnterResult.Stop;
        }
    }
}
