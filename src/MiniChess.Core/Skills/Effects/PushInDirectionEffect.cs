using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 범위 안 대상을 두 번째 지정 칸 쪽(시전자가 고른 방향)으로, 그 칸까지의 거리만큼(최대 Cells) 민다. PushDirectionTargeting 과 함께 쓴다.
    /// 강제 이동이며 막힘 처리는 코어 규칙(Knockback)을 따른다. 막힌 방향이면 밀리지 않고, 다른 방향으로 바꾸지 않는다.
    /// </summary>
    public class PushInDirectionEffect : SkillEffect
    {
        /// <summary>밀어내는 최대 칸 수. [TBD 가능]</summary>
        public int? Cells { get; }

        public TargetFilter Filter { get; }

        public PushInDirectionEffect(int? cells, TargetFilter filter = TargetFilter.Enemy)
        {
            Cells = cells;
            Filter = filter;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Cells, path, nameof(Cells), issues);
        }

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
            {
                if (context.State.IsGameOver)
                    return;

                Knockback.PushToward(context.State, target, context.Caster, GetDirection(target, context.Targets), GetCells(target, context.Targets));
            }
        }

        /// <summary>지정 칸으로 예상 도착 칸을 구한다(미리보기용). 대상이 없으면 null.</summary>
        public Position? Predict(GameState state, Unit caster, IReadOnlyList<Position> targets)
        {
            Unit target = state.Board.GetLivingUnit(targets[0]);
            if (target == null)
                return null;

            return Knockback.PredictDestination(state, target, caster, GetDirection(target, targets), GetCells(target, targets));
        }

        /// <summary>대상에서 고른 칸까지의 거리(최대 Cells).</summary>
        private int GetCells(Unit target, IReadOnlyList<Position> targets)
        {
            Position from = target.Position.Value, to = targets[1];
            return System.Math.Min(Cells.Value, System.Math.Max(System.Math.Abs(to.X - from.X), System.Math.Abs(to.Y - from.Y)));
        }

        private static (int X, int Y) GetDirection(Unit target, IReadOnlyList<Position> targets)
        {
            return target.Position.Value.DirectionTo(targets[1]);
        }
    }
}
