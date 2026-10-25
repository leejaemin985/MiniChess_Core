using MiniChess.Core.Statuses;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 독 장판: 장판 자체의 피해는 없고 적에게 중독을 건다(StatusField).
    /// 들어오거나 지나가면 걸리고, 머무는 동안에는 그 적 소유자의 Turn End(장판 단계)마다 다시 걸려 남은 횟수가 갱신된다
    /// (정화로 지워졌어도 다시 걸린다). 아군과 대상 지정 불가 유닛(봉인 등)은 걸리지 않는다.
    /// </summary>
    public class PoisonField : StatusField
    {
        public StatusDefinition Poison => Status;

        public override string Kind => CellEffectKinds.PoisonField;

        public PoisonField(FieldZone zone, StatusDefinition poison) : base(zone, poison)
        {
        }

        internal override ZoneCellEffect Fork(FieldZone zone) => new PoisonField(zone, Poison);
    }
}
