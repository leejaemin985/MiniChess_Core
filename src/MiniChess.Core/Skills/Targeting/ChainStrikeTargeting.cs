using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 적을 1~MaxTargets 명 차례로 지정한 뒤, 마지막 대상 주변 1칸(정사각형, 대각선 포함) 중 하나를 착지 칸으로 지정한다(연속 베기).
    ///   1. 시전자 사거리 안의 적.
    ///   2. 앞 대상의 연쇄 거리 안에 있는 다른 적(같은 적은 다시 못 고름), 또는 앞 대상의 착지 칸 → 착지 칸을 고르면 지정이 끝난다.
    /// 착지 칸은 빈 칸이거나 시전자가 지금 서 있는 칸이다. 착지 칸까지 지정을 마칠 수 없는 대상은 후보에서 뺀다.
    /// 거리는 모두 정사각형(대각선 포함), 벽은 지정을 막지 않는다. 적 칸과 착지 칸은 겹치지 않아 고른 칸으로 둘을 구분한다.
    /// </summary>
    public class ChainStrikeTargeting : SkillTargeting
    {

        /// <summary>첫 대상을 고르는 시전자 기준 사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        /// <summary>앞 대상 기준으로 다음 대상을 고를 수 있는 거리(칸). [TBD 가능]</summary>
        public int? ChainRange { get; }

        /// <summary>최대 대상 수.</summary>
        public int MaxTargets { get; }

        /// <summary>대상 최대 수 + 착지 칸.</summary>
        public override int TargetCount => MaxTargets + 1;

        public ChainStrikeTargeting(int? range, int? chainRange, int maxTargets)
        {
            Range = range;
            ChainRange = chainRange;
            MaxTargets = maxTargets;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Range, path, nameof(Range), issues);
            Require(ChainRange, path, nameof(ChainRange), issues);
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
            if (!caster.IsPlaced || IsComplete(state, caster, chosen))
                return Enumerable.Empty<Position>();

            if (chosen.Count == 0)
            {
                return EnemiesAround(state, caster, caster.Position.Value, Range.Value, chosen)
                    .Where(enemy => CanFinish(state, caster, new List<Position> { enemy }));
            }

            Position last = chosen[chosen.Count - 1];
            IEnumerable<Position> landings = LandingCells(state, caster, last);
            if (chosen.Count >= MaxTargets)
                return landings;

            IEnumerable<Position> nextEnemies = EnemiesAround(state, caster, last, ChainRange.Value, chosen)
                .Where(enemy => CanFinish(state, caster, new List<Position>(chosen) { enemy }));
            return nextEnemies.Concat(landings);
        }

        /// <summary>마지막 지정이 앞 대상의 착지 칸이면 끝(대상을 MaxTargets 명 고른 뒤에는 착지 칸만 고를 수 있다).</summary>
        public override bool IsComplete(GameState state, Unit caster, IReadOnlyList<Position> chosen)
        {
            if (chosen.Count < 2)
                return false;

            return LandingCells(state, caster, chosen[chosen.Count - 2]).Contains(chosen[chosen.Count - 1]);
        }

        /// <summary>지정한 칸 중 대상(적) 칸. 마지막 착지 칸을 뺀 나머지다.</summary>
        public static IReadOnlyList<Position> GetTargetCells(IReadOnlyList<Position> targets)
        {
            return targets.Take(targets.Count - 1).ToList();
        }

        /// <summary>chosen(마지막이 대상)에서 착지 칸까지 지정을 마칠 수 있는지.</summary>
        private bool CanFinish(GameState state, Unit caster, List<Position> chosen)
        {
            Position last = chosen[chosen.Count - 1];
            if (LandingCells(state, caster, last).Any())
                return true;
            if (chosen.Count >= MaxTargets)
                return false;

            return EnemiesAround(state, caster, last, ChainRange.Value, chosen)
                .Any(enemy => CanFinish(state, caster, new List<Position>(chosen) { enemy }));
        }

        /// <summary>center 에서 range 안(center 제외)의, 아직 고르지 않은 적 칸.</summary>
        private static IEnumerable<Position> EnemiesAround(GameState state, Unit caster, Position center, int range, IReadOnlyList<Position> chosen)
        {
            foreach (Position position in RangeCalculator.GetPositionsInRange(state.Board, center, range, RangeShape.Square))
            {
                if (!chosen.Contains(position) && TargetFilter.Enemy.Matches(caster, state.Board.GetLivingUnit(position)))
                    yield return position;
            }
        }

        /// <summary>target 주변 1칸(정사각형) 중 착지할 수 있는 칸(빈 칸 또는 시전자가 서 있는 칸).</summary>
        private static IEnumerable<Position> LandingCells(GameState state, Unit caster, Position target)
        {
            foreach (Position cell in RangeCalculator.GetPositionsInRange(state.Board, target, 1, RangeShape.Square))
            {
                if (state.Board.CanPlace(cell) || cell == caster.Position.Value)
                    yield return cell;
            }
        }
    }
}
