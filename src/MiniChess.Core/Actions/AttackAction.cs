using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Skills;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Actions
{
    /// <summary>
    /// 플레이어가 자기 유닛으로 적 유닛을 기본 공격하는 명령.
    /// 벽은 공격을 막지 않으며 사거리만 판정한다.
    /// </summary>
    public class AttackAction
    {
        public Unit Attacker { get; }
        public Unit Target { get; }

        public AttackAction(Unit attacker, Unit target)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Target = target ?? throw new ArgumentNullException(nameof(target));
        }

        public AttackFailReason Validate(GameState state)
        {
            AttackFailReason attackerReason = ValidateAttacker(state, Attacker);
            if (attackerReason != AttackFailReason.None) return attackerReason;

            if (!Target.IsOnBoard) return AttackFailReason.InvalidTarget;
            if (Target.Team == Attacker.Team) return AttackFailReason.TargetNotEnemy;

            if (!RangeCalculator.IsInRange(Attacker.Position.Value, Target.Position.Value, Attacker.Stats.AttackRange))
                return AttackFailReason.OutOfRange;

            if (!state.GetPlayer(Attacker.Team).Ap.CanSpend(state.Rules.ActionCost.BasicAttackCost))
                return AttackFailReason.NotEnoughAp;

            return AttackFailReason.None;
        }

        public AttackResult Execute(GameState state)
        {
            AttackFailReason reason = Validate(state);
            if (reason != AttackFailReason.None)
                throw new InvalidOperationException($"공격 불가: {reason}");

            int eventStart = state.Events.Count;

            state.GetPlayer(Attacker.Team).Ap.TrySpend(state.Rules.ActionCost.BasicAttackCost);
            Attacker.TurnState.MarkCombatActionUsed();

            var request = new DamageRequest(Attacker, Target, Attacker.Stats.Attack, DamageType.Direct);
            DamageOutcome outcome = DamageSystem.Apply(state, request);
            ApplyOnHitStatuses(state);

            return new AttackResult(Attacker, Target, outcome.AppliedAmount, outcome.Killed, state.Events.Since(eventStart));
        }

        /// <summary>
        /// "기본 공격 적중 시 상태"(공격자 자신의 것 → 팀 버프 순)를 대상에게 건다.
        /// [가정] 피해가 0 이거나 보호막에 모두 흡수되어도 적중으로 본다. 대상이 죽었거나 경기가 끝났으면 걸지 않는다.
        /// </summary>
        private void ApplyOnHitStatuses(GameState state)
        {
            IEnumerable<StatusDefinition> statuses = Attacker.BasicAttackStatuses
                .Concat(state.GetPlayer(Attacker.Team).BasicAttackStatuses);

            foreach (StatusDefinition status in statuses.ToList())
            {
                if (state.IsGameOver || !Target.IsOnBoard)
                    return;

                StatusSystem.Apply(state, status, Target, Attacker);
            }
        }

        /// <summary>대상과 무관하게 attacker 가 지금 기본 공격을 할 수 있는지(AP 제외). 장애물 공격과 공용.</summary>
        internal static AttackFailReason ValidateAttacker(GameState state, Unit attacker)
        {
            if (state.Phase != GamePhase.Battle) return AttackFailReason.NotBattlePhase;
            if (attacker.Team != state.CurrentTeam) return AttackFailReason.NotYourTurn;
            if (!attacker.IsOnBoard) return AttackFailReason.AttackerNotOnBoard;
            if (attacker.TurnState.ActionsEnded) return AttackFailReason.ActionsEnded;
            if (attacker.TurnState.CombatActionUsed) return AttackFailReason.AlreadyActed;
            if (StatusSystem.BlocksBasicAttack(state, attacker)) return AttackFailReason.BlockedByStatus;

            return AttackFailReason.None;
        }

        /// <summary>
        /// attacker 의 사거리 안에 있는 살아 있는 적 유닛 목록.
        /// 사거리만 판정하며, 턴/행동 여부/AP 는 검사하지 않는다(범위 표시용).
        /// </summary>
        public static List<Unit> GetTargetsInRange(GameState state, Unit attacker)
        {
            if (!attacker.IsPlaced)
                return new List<Unit>();

            List<Position> positions = RangeCalculator.GetPositionsInRange(
                state.Board, attacker.Position.Value, attacker.Stats.AttackRange);

            return TargetFilter.Enemy.GetUnits(state.Board, positions, attacker);
        }
    }
}
