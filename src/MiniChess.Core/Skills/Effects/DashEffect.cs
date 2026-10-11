using System;
using System.Collections.Generic;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 돌진: 지정 칸까지 직선 이동한 뒤, 진행 방향 바로 앞 칸에 적이 있으면 그 적에게 피해를 준다.
    /// 이동 중 칸 효과(덫 등)는 모두 발동해 피해/상태를 그대로 받지만, 멈춤 요청은 무시하고 도착 칸까지 간다.
    /// 이동 중 사망하면 그 자리에서 멈추고 공격하지 않는다. 앞 칸이 비었거나 벽/아군이면 공격하지 않는다.
    /// </summary>
    public class DashEffect : SkillEffect
    {
        /// <summary>도착 후 전방 공격 피해. [TBD 가능]</summary>
        public int? Damage { get; }

        public DashEffect(int? damage)
        {
            Damage = damage;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Damage, path, nameof(Damage), issues);
        }

        public override void Apply(SkillContext context)
        {
            GameState state = context.State;
            Unit caster = context.Caster;
            Position from = caster.Position.Value;

            IReadOnlyList<Position> path = state.Board.GetStraightPath(from, context.Target);
            MovementResolver.Resolve(state, caster, caster, MoveKind.Dash, path, ignoreStopRequests: true);

            if (!caster.IsAlive || state.IsGameOver)
                return;

            Position arrived = caster.Position.Value;
            var front = new Position(
                arrived.X + Math.Sign(context.Target.X - from.X),
                arrived.Y + Math.Sign(context.Target.Y - from.Y));

            Unit occupant = state.Board.GetLivingUnit(front);
            if (TargetFilter.Enemy.Matches(caster, occupant))
                DamageSystem.Apply(state, new DamageRequest(caster, occupant, Damage.Value, DamageType.Direct));
        }
    }
}
