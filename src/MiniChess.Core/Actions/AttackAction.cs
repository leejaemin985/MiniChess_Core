using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Actions
{
    /// <summary>
    /// 플레이어가 자기 유닛으로 적 유닛을 기본 공격하는 명령.
    /// 벽은 공격을 막지 않으며 사거리만 판정한다.
    /// </summary>
    internal class AttackAction
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

            if (!Target.IsOnBoard || !Target.IsTargetable) return AttackFailReason.InvalidTarget;
            if (Target.Team == Attacker.Team) return AttackFailReason.TargetNotEnemy;

            return ValidateRangeAndAp(state, Attacker, Target.Position.Value);
        }

        public AttackResult Execute(GameState state)
        {
            AttackFailReason reason = Validate(state);
            if (reason != AttackFailReason.None)
                throw new InvalidOperationException($"공격 불가: {reason}");

            int eventStart = state.Events.Count;

            PayCost(state, Attacker);
            state.Events.Record(new UnitAttackedEvent(Attacker.Id, Target.Id));

            int damage = Attacker.Stats.Attack + StatusSystem.ConsumeBasicAttackBonus(state, Attacker);
            var request = new DamageRequest(Attacker, Target, damage, DamageType.Direct);
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

                StatusSystem.Apply(state, status, Target, Attacker, StatusOrigins.BasicAttack);
            }
        }

        /// <summary>대상과 무관하게 attacker 가 지금 기본 공격을 할 수 있는지(AP 제외). 장애물 공격과 공용.</summary>
        internal static AttackFailReason ValidateAttacker(GameState state, Unit attacker)
        {
            if (state.Phase != GamePhase.Battle) return AttackFailReason.NotBattlePhase;
            if (attacker.Team != state.CurrentTeam) return AttackFailReason.NotYourTurn;
            if (!attacker.IsOnBoard) return AttackFailReason.AttackerNotOnBoard;
            if (attacker.TurnState.ActionsEnded) return AttackFailReason.ActionsEnded;
            if (attacker.TurnState.BasicAttackUsed) return AttackFailReason.AlreadyUsedThisTurn;
            if (StatusSystem.BlocksBasicAttack(state, attacker)) return AttackFailReason.BlockedByStatus;

            return AttackFailReason.None;
        }

        /// <summary>대상 칸이 사거리 안이고 기본 공격 AP 가 있는지. 장애물 공격과 공용.</summary>
        internal static AttackFailReason ValidateRangeAndAp(GameState state, Unit attacker, Position target)
        {
            if (!RangeCalculator.IsInRange(attacker.Position.Value, target, attacker.Stats.AttackRange))
                return AttackFailReason.OutOfRange;

            if (!state.GetPlayer(attacker.Team).Ap.CanSpend(state.Rules.ActionCost.BasicAttackCost))
                return AttackFailReason.NotEnoughAp;

            return AttackFailReason.None;
        }

        /// <summary>기본 공격 비용(AP, 이번 턴 기본 공격 사용)을 치른다. 장애물 공격과 공용.</summary>
        internal static void PayCost(GameState state, Unit attacker)
        {
            state.GetPlayer(attacker.Team).Ap.TrySpend(state.Rules.ActionCost.BasicAttackCost);
            attacker.TurnState.MarkBasicAttackUsed();
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
