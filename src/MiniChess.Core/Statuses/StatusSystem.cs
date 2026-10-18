using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Movement;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Statuses
{
    /// <summary>상태효과의 부여/제거/턴 단계 처리.</summary>
    public static class StatusSystem
    {
        /// <summary>
        /// 대상에게 상태를 건다. 같은 Id 가 이미 있으면 정의의 StackPolicy 를 따른다.
        /// 적용(또는 갱신)된 효과를 반환한다. 무시되었거나 대상이 보드 위에 없으면 null.
        /// </summary>
        internal static StatusEffect Apply(GameState state, StatusDefinition definition, Unit target, Unit source, Team sourceTeam)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (!target.IsOnBoard)
                return null;

            StatusEffect existing = target.FindStatus(definition.Id);
            if (existing != null)
            {
                switch (definition.StackPolicy)
                {
                    case StatusStackPolicy.Ignore:
                        return null;

                    // 같은 정의면 갱신한다. 같은 Id 라도 정의가 다르면(점령 보상 화상과 스킬 화상 등) 수치가 다를 수 있어 새 정의로 교체한다.
                    case StatusStackPolicy.Refresh when ReferenceEquals(existing.Definition, definition):
                        existing.Reapply(source, sourceTeam, state.TurnNumber);
                        state.Events.Record(new StatusAppliedEvent(existing, refreshed: true));
                        return existing;

                    case StatusStackPolicy.Refresh:

                    case StatusStackPolicy.Replace:
                        Remove(state, existing, StatusRemoveReason.Replaced);
                        break;
                }
            }

            var status = new StatusEffect(definition, target, source, sourceTeam, state.TurnNumber);
            target.AddStatus(status);
            state.Events.Record(new StatusAppliedEvent(status, refreshed: false));
            definition.Behavior?.OnApplied(new StatusContext(state, status));
            return status;
        }

        internal static StatusEffect Apply(GameState state, StatusDefinition definition, Unit target, Unit source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source), "출처 유닛이 없으면 sourceTeam 을 직접 지정");

            return Apply(state, definition, target, source, source.Team);
        }

        internal static void Remove(GameState state, StatusEffect status, StatusRemoveReason reason)
        {
            if (!status.Target.RemoveStatus(status))
                return;

            state.Events.Record(new StatusRemovedEvent(status, reason));
            status.Definition.Behavior?.OnRemoved(new StatusContext(state, status));
        }

        internal static void RemoveAll(GameState state, Unit unit, StatusRemoveReason reason)
        {
            foreach (StatusEffect status in unit.Statuses.ToList())
                Remove(state, status, reason);
        }

        /// <summary>source 가 건 상태 중 RemoveOnSourceDeath 인 것을 모두 제거한다(건 유닛 사망 시).</summary>
        internal static void RemoveLinkedTo(GameState state, Unit source)
        {
            foreach (StatusEffect status in FindBySource(state, source).Where(s => s.Definition.RemoveOnSourceDeath))
                Remove(state, status, StatusRemoveReason.SourceDied);
        }

        /// <summary>source 가 모든 유닛에게 건 상태(스냅샷). 반환 후 제거해도 안전하다.</summary>
        public static List<StatusEffect> FindBySource(GameState state, Unit source)
        {
            return state.AllUnits()
                .SelectMany(unit => unit.Statuses)
                .Where(status => status.Source == source)
                .ToList();
        }

        /// <summary>
        /// 턴 단계 처리. 상태마다 발동(Trigger) 후 감소(Decrement) 순으로 처리한다.
        /// [정책] 처리 순서(유닛 Id → 부여 순)는 명세 TBD 에 대한 임시 결정.
        /// </summary>
        internal static void RunStep(TurnContext context)
        {
            GameState state = context.State;

            foreach (Unit unit in GetPlacedUnitsById(state))
            {
                // 처리 중 제거(만료, 사망 등)가 일어나므로 스냅샷을 순회하고,
                // 매번 아직 걸려 있는지 확인한다. 사망 시 상태가 모두 제거되므로 사망 확인도 겸한다.
                foreach (StatusEffect status in unit.Statuses.ToList())
                {
                    if (state.IsGameOver)
                        return;

                    if (!unit.HasStatus(status))
                        continue;

                    if (status.Matches(status.Definition.Trigger, context))
                        status.Definition.Behavior?.OnTrigger(new StatusContext(state, status));

                    if (unit.HasStatus(status) && status.Matches(status.Definition.Decrement, context))
                        Decrement(state, status);
                }
            }
        }

        /// <summary>걸린 상태 중 이 이동을 막는 것이 있으면 처음 찾은 이유를 반환한다.</summary>
        internal static MoveBlockReason CheckMove(GameState state, Unit mover, Unit initiator, MoveKind kind, int cells)
        {
            foreach (StatusEffect status in mover.Statuses)
            {
                StatusBehavior behavior = status.Definition.Behavior;
                if (behavior == null)
                    continue;

                MoveBlockReason reason = behavior.CheckMove(new StatusContext(state, status), initiator, kind, cells);
                if (reason != MoveBlockReason.None)
                    return reason;
            }

            return MoveBlockReason.None;
        }

        /// <summary>피해를 받은 대상의 상태들에 알린다(부여 순). 처리 중 제거된 상태는 건너뛴다.</summary>
        internal static void NotifyDamaged(GameState state, Unit target, DamageRequest request)
        {
            foreach (StatusEffect status in target.Statuses.ToList())
            {
                if (state.IsGameOver || !target.IsAlive)
                    return;

                if (target.HasStatus(status))
                    status.Definition.Behavior?.OnDamaged(new StatusContext(state, status), request);
            }
        }

        /// <summary>걸린 상태 중 기본 공격을 막는 것이 있는지.</summary>
        internal static bool BlocksBasicAttack(GameState state, Unit attacker)
        {
            return attacker.Statuses.Any(status =>
                status.Definition.Behavior != null && status.Definition.Behavior.BlocksBasicAttack(new StatusContext(state, status)));
        }

        internal static bool BlocksSkillUse(GameState state, Unit caster)
        {
            return caster.Statuses.Any(status =>
                status.Definition.Behavior != null && status.Definition.Behavior.BlocksSkillUse(new StatusContext(state, status)));
        }

        internal static bool BlocksHpChange(GameState state, Unit unit)
        {
            return unit.Statuses.Any(status =>
                status.Definition.Behavior != null && status.Definition.Behavior.BlocksHpChange(new StatusContext(state, status)));
        }

        /// <summary>기본 공격 추가 피해를 주는 상태들의 합을 구하고, 그 상태들을 소모(Consumed)한다.</summary>
        internal static int ConsumeBasicAttackBonus(GameState state, Unit attacker)
        {
            int total = 0;

            foreach (StatusEffect status in attacker.Statuses.ToList())
            {
                int bonus = status.Definition.Behavior?.BasicAttackBonus(new StatusContext(state, status)) ?? 0;
                if (bonus <= 0)
                    continue;

                total += bonus;
                Remove(state, status, StatusRemoveReason.Consumed);
            }

            return total;
        }

        private static void Decrement(GameState state, StatusEffect status)
        {
            status.Remaining--;
            if (status.Remaining <= 0)
                Remove(state, status, StatusRemoveReason.Expired);
        }

        private static IEnumerable<Unit> GetPlacedUnitsById(GameState state)
        {
            return state.AllUnits()
                .Where(unit => unit.IsPlaced)
                .OrderBy(unit => unit.Id)
                .ToList();
        }
    }
}
