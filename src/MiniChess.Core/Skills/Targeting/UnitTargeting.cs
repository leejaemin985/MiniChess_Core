using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>사거리 안에서 조건에 맞는 살아 있는 유닛 하나를 지정한다. 벽은 지정을 막지 않는다.</summary>
    public class UnitTargeting : SkillTargeting
    {
        public TargetFilter Filter { get; }

        /// <summary>사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        public RangeShape Shape { get; }

        public UnitTargeting(TargetFilter filter, int? range, RangeShape shape = RangeShape.Square)
        {
            Filter = filter;
            Range = range;
            Shape = shape;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster)
        {
            if (!caster.IsPlaced)
                yield break;

            Position center = caster.Position.Value;

            if (Filter.Matches(caster, caster))
                yield return center;

            foreach (Position position in RangeCalculator.GetPositionsInRange(state.Board, center, Range.Value, Shape))
            {
                Unit occupant = state.Board.GetCell(position).Occupant;
                if (occupant != null && occupant.IsAlive && Filter.Matches(caster, occupant))
                    yield return position;
            }
        }
    }
}
