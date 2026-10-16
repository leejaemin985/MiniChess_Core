using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 밀어낼 유닛과 방향을 차례로 지정한다.
    ///   1. 사거리 안에서 조건에 맞는 유닛(대상 지정 불가 유닛 제외).
    ///   2. 그 유닛의 상하좌우 칸(보드 안) 하나. 고른 칸 쪽이 밀어내는 방향이다.
    /// 막힌 방향도 고를 수 있다(밀기 시도). 실제 도착 칸은 GameController.GetPushPreview 로 미리 볼 수 있다.
    /// </summary>
    public class PushDirectionTargeting : SkillTargeting
    {
        private static readonly (int X, int Y)[] Directions = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        private readonly UnitTargeting _unitTargeting;

        public TargetFilter Filter { get; }

        /// <summary>사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        public override int TargetCount => 2;

        public PushDirectionTargeting(TargetFilter filter, int? range)
        {
            Filter = filter;
            Range = range;
            _unitTargeting = new UnitTargeting(filter, range);
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
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
                return GetDirectionCells(state, chosen[0]);

            return new List<Position>();
        }

        private static IEnumerable<Position> GetDirectionCells(GameState state, Position target)
        {
            foreach ((int dx, int dy) in Directions)
            {
                Position cell = target.Offset(dx, dy);
                if (state.Board.IsInBounds(cell))
                    yield return cell;
            }
        }
    }
}
