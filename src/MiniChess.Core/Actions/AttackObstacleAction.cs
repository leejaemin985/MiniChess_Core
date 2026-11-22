using System;
using System.Collections.Generic;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Actions
{
    /// <summary>
    /// 기본 공격으로 장애물을 치는 명령. 양 팀 모두 칠 수 있다.
    /// 검사와 비용은 유닛 기본 공격과 같다(사거리, AP, 턴당 기본 공격 1회, 상태효과 차단). 공격력만큼 피해를 주고, 적중 상태는 걸지 않는다.
    /// </summary>
    internal class AttackObstacleAction
    {
        public Unit Attacker { get; }
        public Position Target { get; }

        public AttackObstacleAction(Unit attacker, Position target)
        {
            Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
            Target = target;
        }

        public AttackFailReason Validate(GameState state)
        {
            AttackFailReason attackerReason = AttackAction.ValidateAttacker(state, Attacker);
            if (attackerReason != AttackFailReason.None) return attackerReason;

            if (ObstacleDamage.Find(state, Target) == null) return AttackFailReason.InvalidTarget;

            return AttackAction.ValidateRangeAndAp(state, Attacker, Target);
        }

        /// <summary>장애물을 친다. 파괴되었으면 true.</summary>
        public AttackObstacleResult Execute(GameState state)
        {
            AttackFailReason reason = Validate(state);
            if (reason != AttackFailReason.None)
                throw new InvalidOperationException($"장애물 공격 불가: {reason}");

            int eventStart = state.Events.Count;

            AttackAction.PayCost(state, Attacker);

            state.Events.Record(new ObstacleHitEvent(Attacker.Id, Target));
            bool destroyed = ObstacleDamage.Apply(state, Attacker, Target, Attacker.Stats.Attack, DamageType.Direct);

            return new AttackObstacleResult(Attacker, Target, destroyed, state.Events.Since(eventStart));
        }

        /// <summary>attacker 의 사거리 안에 있는 장애물 칸(범위 표시용). 턴/행동/AP 는 검사하지 않는다.</summary>
        public static List<Position> GetObstaclesInRange(GameState state, Unit attacker)
        {
            var positions = new List<Position>();
            if (!attacker.IsPlaced)
                return positions;

            foreach (Position position in AttackAction.GetReach(state, attacker))
            {
                if (ObstacleDamage.Find(state, position) != null)
                    positions.Add(position);
            }

            return positions;
        }
    }

    public class AttackObstacleResult
    {
        public Unit Attacker { get; }
        public Position Target { get; }
        public bool Destroyed { get; }

        /// <summary>이 공격으로 발생한 이벤트(발생 순).</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        public AttackObstacleResult(Unit attacker, Position target, bool destroyed, IReadOnlyList<GameEvent> events)
        {
            Attacker = attacker;
            Target = target;
            Destroyed = destroyed;
            Events = events;
        }
    }
}
