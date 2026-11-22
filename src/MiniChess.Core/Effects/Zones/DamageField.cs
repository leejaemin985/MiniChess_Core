using MiniChess.Core.Combat;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 피해 장판: 장판 주인의 상대 팀 Turn End(장판 단계)에 장판 위 그 팀 유닛과 장애물(중립이라 적으로 맞는다)에 피해를 준다(화염지대).
    /// 상태이상이나 이동 제한은 추가하지 않는다.
    /// </summary>
    public class DamageField : ZoneCellEffect
    {
        public int DamageAmount { get; }

        public override CellEffectLayer Layer => CellEffectLayer.AreaEffect;
        public override string Kind => CellEffectKinds.DamageField;

        public DamageField(FieldZone zone, int damageAmount) : base(zone)
        {
            DamageAmount = damageAmount;
        }

        internal override ZoneCellEffect Fork(FieldZone zone) => new DamageField(zone, DamageAmount);

        protected override void OnZoneTurnStep(CellTurnContext context)
        {
            if (context.Turn.Step != TurnStep.EndAreaEffects || context.Turn.ActiveTeam == Zone.OwnerTeam)
                return;

            Unit occupant = context.Cell.Occupant;
            if (occupant == null)
                ObstacleDamage.Apply(context.State, Zone.Source, context.Cell.Position, DamageAmount, DamageType.Area);
            else if (occupant.Team == context.Turn.ActiveTeam)
                DamageSystem.Apply(context.State, new DamageRequest(Zone.Source, occupant, DamageAmount, DamageType.Area));
        }
    }
}
