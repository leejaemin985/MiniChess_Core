using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 상하좌우 직선 이동의 도착 칸을 지정한다(돌진 등). 사거리 이내이고 경로(도착 칸 포함)에 벽/유닛이 없어야 한다.
    /// </summary>
    public class StraightMoveTargeting : SkillTargeting
    {
        /// <summary>최대 이동 칸 수. [TBD 가능]</summary>
        public int? Range { get; }

        public StraightMoveTargeting(int? range)
        {
            Range = range;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
        }

        public override IEnumerable<Position> GetReach(GameState state, Unit caster)
        {
            return CellsInRange(state, caster, Range.Value, RangeShape.Orthogonal);
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster)
        {
            if (!caster.IsPlaced)
                yield break;

            Position center = caster.Position.Value;
            foreach (Position position in RangeCalculator.GetPositionsInRange(state.Board, center, Range.Value, RangeShape.Orthogonal))
            {
                if (state.Board.IsPathClear(state.Board.GetStraightPath(center, position)))
                    yield return position;
            }
        }
    }
}
