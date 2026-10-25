using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 밀어낼 유닛과 방향을 차례로 지정한다.
    ///   1. 사거리 안에서 조건에 맞는 유닛(대상 지정 불가 유닛 제외).
    ///   2. 그 유닛에서 상하좌우로 MaxCells 칸 안의 칸(보드 안) 하나. 고른 칸 쪽이 밀어내는 방향, 고른 칸까지의 거리가 밀어내는 칸 수다.
    /// 막힌 칸도 고를 수 있다(밀기 시도, 막히면 그 앞에서 멈춤). 실제 도착 칸은 GameController.GetPushPreview 로 미리 볼 수 있다.
    /// </summary>
    public class PushDirectionTargeting : SkillTargeting
    {
        private static readonly (int X, int Y)[] Directions = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        private readonly UnitTargeting _unitTargeting;

        public TargetFilter Filter { get; }

        /// <summary>사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        /// <summary>고를 수 있는 최대 밀기 칸 수. [TBD 가능]</summary>
        public int? MaxCells { get; }

        public override int TargetCount => 2;

        public PushDirectionTargeting(TargetFilter filter, int? range, int? maxCells = 1)
        {
            Filter = filter;
            Range = range;
            MaxCells = maxCells;
            _unitTargeting = new UnitTargeting(filter, range);
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
            Require(MaxCells, path, nameof(MaxCells), issues);
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
                return GetDirectionCells(state, chosen[0], MaxCells.Value);

            return new List<Position>();
        }

        private static IEnumerable<Position> GetDirectionCells(GameState state, Position target, int maxCells)
        {
            foreach ((int dx, int dy) in Directions)
            {
                for (int distance = 1; distance <= maxCells; distance++)
                {
                    Position cell = target.Offset(dx * distance, dy * distance);
                    if (!state.Board.IsInBounds(cell))
                        break;

                    yield return cell;
                }
            }
        }
    }
}
