using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 사거리 안에서 조건에 맞는 살아 있는 유닛을 Count 명, 서로 다르게 지정한다.
    /// MoveKind 를 지정하면 그 방식의 이동이 상태효과로 막힌 유닛(예: 속박된 자기 자신)은 후보에서 뺀다.
    /// 벽은 지정을 막지 않는다.
    /// </summary>
    public class MultiUnitTargeting : SkillTargeting
    {
        public TargetFilter Filter { get; }

        /// <summary>사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        public int Count { get; }

        /// <summary>지정 유닛이 시전자에 의해 옮겨지는 스킬이면 그 이동 방식. 아니면 null.</summary>
        public MoveKind? MovedBy { get; }

        public override int TargetCount => Count;

        public MultiUnitTargeting(TargetFilter filter, int? range, int count, MoveKind? movedBy = null)
        {
            Filter = filter;
            Range = range;
            Count = count;
            MovedBy = movedBy;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
        }

        public override IEnumerable<Position> GetReach(GameState state, Unit caster)
        {
            return CellsInRange(state, caster, Range.Value, RangeShape.Square);
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster)
        {
            return GetCandidates(state, caster, new List<Position>());
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster, IReadOnlyList<Position> chosen)
        {
            if (!caster.IsPlaced || chosen.Count >= Count)
                yield break;

            Position center = caster.Position.Value;
            IEnumerable<Position> cells = new[] { center }
                .Concat(RangeCalculator.GetPositionsInRange(state.Board, center, Range.Value, RangeShape.Square));

            foreach (Position position in cells)
            {
                if (chosen.Contains(position))
                    continue;

                Unit occupant = state.Board.GetLivingUnit(position);
                if (Filter.Matches(caster, occupant) && CanBeMoved(state, caster, occupant))
                    yield return position;
            }
        }

        private bool CanBeMoved(GameState state, Unit caster, Unit unit)
        {
            return MovedBy == null || MovementControl.IsAllowedByStatuses(state, unit, caster, MovedBy.Value);
        }
    }
}
