using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>칸 지정 조건.</summary>
    public enum CellRequirement
    {
        /// <summary>벽이 아닌 칸(유닛 유무 무관).</summary>
        NotWall,

        /// <summary>유닛을 놓을 수 있는 빈 칸(벽/유닛 없음).</summary>
        Empty,
    }

    /// <summary>사거리 안의 칸 하나를 지정한다(설치/소환/범위 중심 등). 기본적으로 시전자 칸은 제외한다.</summary>
    public class CellTargeting : SkillTargeting
    {
        /// <summary>사거리(칸). [TBD 가능]</summary>
        public int? Range { get; }

        public RangeShape Shape { get; }
        public CellRequirement Requirement { get; }

        /// <summary>시전자 자신의 칸도 지정할 수 있는지(자기 중심 장판 등). Empty 조건과는 함께 쓰지 않는다.</summary>
        public bool IncludeCasterCell { get; }

        /// <summary>
        /// 설치형 스킬이면 설치할 레이어. 지정하면 그 레이어를 설치할 수 없는 칸(예: 점령 칸의 덫)은 지정 대상에서 뺀다.
        /// </summary>
        public CellEffectLayer? PlaceableLayer { get; }

        public CellTargeting(
            int? range,
            CellRequirement requirement,
            RangeShape shape = RangeShape.Square,
            bool includeCasterCell = false,
            CellEffectLayer? placeableLayer = null)
        {
            Range = range;
            Requirement = requirement;
            Shape = shape;
            IncludeCasterCell = includeCasterCell;
            PlaceableLayer = placeableLayer;
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

            if (IncludeCasterCell && CanPlaceLayer(state, center))
                yield return center;

            foreach (Position position in RangeCalculator.GetPositionsInRange(state.Board, center, Range.Value, Shape))
            {
                if (Satisfies(state.Board, position) && CanPlaceLayer(state, position))
                    yield return position;
            }
        }

        public override IEnumerable<Position> GetReach(GameState state, Unit caster)
        {
            if (IncludeCasterCell && caster.IsPlaced)
                yield return caster.Position.Value;

            foreach (Position position in CellsInRange(state, caster, Range.Value, Shape))
                yield return position;
        }

        private bool CanPlaceLayer(GameState state, Position position)
        {
            return PlaceableLayer == null
                || CellEffectSystem.CanPlace(state, position, PlaceableLayer.Value) == CellEffectPlaceFailReason.None;
        }

        private bool Satisfies(Board board, Position position)
        {
            switch (Requirement)
            {
                case CellRequirement.NotWall: return !board.GetCell(position).IsWall;
                case CellRequirement.Empty: return board.CanPlace(position);
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
