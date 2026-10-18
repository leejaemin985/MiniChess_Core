using MiniChess.Core.Combat;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 피해 장판: 장판 위 적을 그 적 소유자의 Turn End(장판 단계)에 피해를 준다(화염지대).
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

        protected override void OnZoneTurnStep(CellTurnContext context)
        {
            Unit occupant = context.Cell.Occupant;
            if (context.Turn.Step != TurnStep.EndAreaEffects || occupant == null)
                return;

            if (occupant.Team != Zone.OwnerTeam && occupant.Team == context.Turn.ActiveTeam)
                DamageSystem.Apply(context.State, new DamageRequest(Zone.Source, occupant, DamageAmount, DamageType.Area));
        }
    }
}
