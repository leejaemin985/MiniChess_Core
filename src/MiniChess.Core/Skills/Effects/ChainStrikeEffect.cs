using System.Collections.Generic;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 연속 베기: ChainStrikeTargeting 으로 지정한 적(장애물 포함)들을 지정 순서대로 베고, 마지막에 착지 칸으로 순간이동한다.
    /// 한 명만 베면 SingleDamage, 여러 명이면 각자 MultiDamage. 즉시 해결되며 중간 이동은 없다(연출만 대상마다 옮겨 다닌다).
    /// 대상이 베여 죽어도 착지 칸은 지정할 때 정한 칸 그대로다. 착지 칸이 시전자가 서 있는 칸이면 이동하지 않는다.
    /// 착지는 시전자 스스로의 순간이동(MoveKind.Warp)이라 도착 칸의 효과(덫 등)가 발동한다.
    /// </summary>
    public class ChainStrikeEffect : SkillEffect
    {
        /// <summary>한 명만 벨 때 피해. [TBD 가능]</summary>
        public int? SingleDamage { get; }

        /// <summary>여러 명을 벨 때 각자 받는 피해. [TBD 가능]</summary>
        public int? MultiDamage { get; }

        public ChainStrikeEffect(int? singleDamage, int? multiDamage)
        {
            SingleDamage = singleDamage;
            MultiDamage = multiDamage;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(SingleDamage, path, nameof(SingleDamage), issues);
            Require(MultiDamage, path, nameof(MultiDamage), issues);
        }

        public override void Apply(SkillContext context)
        {
            IReadOnlyList<Position> cells = ChainStrikeTargeting.GetTargetCells(context.Targets);
            int damage = cells.Count == 1 ? SingleDamage.Value : MultiDamage.Value;

            foreach (Position cell in cells)
            {
                if (context.State.IsGameOver)
                    return;

                Unit target = context.State.Board.GetLivingUnit(cell);
                if (target != null && TargetFilter.Enemy.Matches(context.Caster, target))
                    DamageSystem.Apply(context.State, new DamageRequest(context.Caster, target, damage, DamageType.Direct));
                else
                    ObstacleDamage.Apply(context.State, context.Caster, cell, damage, DamageType.Direct);
            }

            if (context.State.IsGameOver || !context.Caster.IsAlive || !context.Caster.IsPlaced)
                return;

            Position landing = context.Targets[context.Targets.Count - 1];
            if (landing != context.Caster.Position.Value)
                MovementResolver.Resolve(context.State, context.Caster, context.Caster, MoveKind.Warp, new[] { landing });
        }

        public override bool DamagesObstacles => SingleDamage > 0 || MultiDamage > 0;
    }
}
