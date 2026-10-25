using MiniChess.Core.Combat;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 회복 장판: 장판 위 아군을 그 아군 소유자의 Turn Start(긍정 시작 효과 단계)에 회복한다(명세 6.2).
    /// 아군만 대상이므로 "대상 소유자의 턴"과 "시전자 소유자의 턴"은 같은 턴이다.
    /// </summary>
    public class HealField : ZoneCellEffect
    {
        public int HealAmount { get; }

        public override CellEffectLayer Layer => CellEffectLayer.AreaEffect;
        public override string Kind => CellEffectKinds.HealField;

        public HealField(FieldZone zone, int healAmount) : base(zone)
        {
            HealAmount = healAmount;
        }

        internal override ZoneCellEffect Fork(FieldZone zone) => new HealField(zone, HealAmount);

        protected override void OnZoneTurnStep(CellTurnContext context)
        {
            Unit occupant = context.Cell.Occupant;
            if (context.Turn.Step != TurnStep.StartPositiveEffects || occupant == null)
                return;

            if (occupant.Team == Zone.OwnerTeam && occupant.Team == context.Turn.ActiveTeam)
                DamageSystem.Heal(context.State, Zone.Source, occupant, HealAmount);
        }
    }
}
