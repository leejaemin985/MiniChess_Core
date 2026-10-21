using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 상태이상 장판의 기반(독 장판 등). 대상이 칸에 들어오면(지나가는 칸 포함, 이동 방식 무관) 상태를 걸고,
    /// 장판 위에 머무는 동안에는 대상 소유자의 턴마다 ExposureStep 에 다시 건다(사용자 확정: 계속 노출되면 재부여).
    /// 다시 걸 때는 상태의 StackPolicy 를 따른다. Refresh 상태면 남은 횟수가 처음 값으로 갱신되고, 정화 등으로 지워졌으면 새로 걸린다.
    /// 설치 순간 이미 서 있던 대상의 상태는 설치 스킬이 따로 건다.
    /// 새 상태이상 장판은 이 클래스를 상속해 Kind 만 정하면 되고, 대상/시점이 다르면 Affects / ExposureStep 을 바꾼다.
    /// </summary>
    public abstract class StatusField : ZoneCellEffect
    {
        public StatusDefinition Status { get; }

        public override CellEffectLayer Layer => CellEffectLayer.AreaEffect;

        /// <summary>머무는 대상에게 다시 거는 턴 단계(대상 소유자의 턴). 기본은 Turn End 장판 단계(DoT 다음, 지속시간 감소 전).</summary>
        protected virtual TurnStep ExposureStep => TurnStep.EndAreaEffects;

        protected StatusField(FieldZone zone, StatusDefinition status) : base(zone)
        {
            Status = status;
        }

        /// <summary>이 장판의 상태가 걸리는 대상인지. 기본: 설치한 쪽의 적이고 대상 지정 가능(봉인 등 제외).</summary>
        protected virtual bool Affects(Unit unit)
        {
            return unit.Team != Zone.OwnerTeam && unit.IsTargetable;
        }

        public override CellEnterResult OnEnter(CellEffectContext context)
        {
            TryApply(context.State, context.Unit);
            return CellEnterResult.Continue;
        }

        protected override void OnZoneTurnStep(CellTurnContext context)
        {
            Unit occupant = context.Cell.Occupant;
            if (context.Turn.Step != ExposureStep || occupant == null || occupant.Team != context.Turn.ActiveTeam)
                return;

            TryApply(context.State, occupant);
        }

        private void TryApply(GameState state, Unit unit)
        {
            if (Affects(unit))
                StatusSystem.Apply(state, Status, unit, Zone.Source, Zone.OwnerTeam, Zone.SkillId);
        }
    }
}
