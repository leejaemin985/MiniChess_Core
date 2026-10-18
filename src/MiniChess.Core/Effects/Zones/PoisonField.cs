using MiniChess.Core.Statuses;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 독 장판: 적이 칸에 들어오면(지나가는 칸 포함, 이동 방식 무관) 중독을 건다. 장판 자체의 피해는 없고 머물러도 추가 효과는 없다.
    /// 설치 순간 이미 서 있던 적의 중독은 설치 스킬이 따로 건다. 아군과 대상 지정 불가 유닛(봉인 등)은 걸리지 않는다.
    /// </summary>
    public class PoisonField : ZoneCellEffect
    {
        public StatusDefinition Poison { get; }

        public override CellEffectLayer Layer => CellEffectLayer.AreaEffect;
        public override string Kind => CellEffectKinds.PoisonField;

        public PoisonField(FieldZone zone, StatusDefinition poison) : base(zone)
        {
            Poison = poison;
        }

        public override CellEnterResult OnEnter(CellEffectContext context)
        {
            if (context.Unit.Team != Zone.OwnerTeam && context.Unit.IsTargetable)
                StatusSystem.Apply(context.State, Poison, context.Unit, Zone.Source, Zone.OwnerTeam);

            return CellEnterResult.Continue;
        }
    }
}
