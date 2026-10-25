using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 옮길 유닛과 도착 칸을 차례로 지정한다.
    ///   1. 사거리 안(시전자 칸 포함)에서 조건에 맞고, 시전자가 MovedBy 방식으로 옮길 수 있는 유닛(상태효과로 막힌 유닛 제외).
    ///   2. 사거리 안의 빈 칸(벽/유닛/장애물 없음). 거리는 둘 다 시전자 기준이다.
    /// </summary>
    public class UnitToCellTargeting : SkillTargeting
    {
        private readonly MultiUnitTargeting _unitTargeting;
        private readonly CellTargeting _cellTargeting;

        public TargetFilter Filter { get; }

        /// <summary>사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        /// <summary>지정 유닛이 시전자에 의해 옮겨지는 이동 방식.</summary>
        public MoveKind MovedBy { get; }

        public override int TargetCount => 2;

        public UnitToCellTargeting(TargetFilter filter, int? range, MoveKind movedBy)
        {
            Filter = filter;
            Range = range;
            MovedBy = movedBy;
            _unitTargeting = new MultiUnitTargeting(filter, range, count: 1, movedBy: movedBy);
            _cellTargeting = new CellTargeting(range, CellRequirement.Empty);
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
        }

        public override IEnumerable<Position> GetReach(GameState state, Unit caster)
        {
            return _unitTargeting.GetReach(state, caster);
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster)
        {
            return _unitTargeting.GetCandidates(state, caster);
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster, IReadOnlyList<Position> chosen)
        {
            if (chosen.Count == 0)
                return GetCandidates(state, caster);
            if (chosen.Count == 1)
                return _cellTargeting.GetCandidates(state, caster);

            return new List<Position>();
        }
    }
}
