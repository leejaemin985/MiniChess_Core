using MiniChess.Core.Turns;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>구역(FieldZone)에 속한 칸 효과의 기반. 수명 감소는 구역이 처리한다.</summary>
    public abstract class ZoneCellEffect : ICellTurnEffect
    {
        public FieldZone Zone { get; }

        public abstract CellEffectLayer Layer { get; }

        protected ZoneCellEffect(FieldZone zone)
        {
            Zone = zone;
        }

        public virtual CellEnterResult OnEnter(CellEffectContext context)
        {
            return CellEnterResult.Continue;
        }

        public virtual void OnStop(CellEffectContext context) { }

        public void OnTurnStep(CellTurnContext context)
        {
            OnZoneTurnStep(context);

            if (context.Turn.Step == TurnStep.EndDurationTick
                && context.Turn.ActiveTeam == Zone.OwnerTeam
                && context.Cell.Contains(this))
            {
                Zone.TickLifetime(context.State);
            }
        }

        /// <summary>턴 단계마다 호출된다(장판 피해/회복 등). 수명 감소 전에 호출된다.</summary>
        protected virtual void OnZoneTurnStep(CellTurnContext context) { }
    }
}
