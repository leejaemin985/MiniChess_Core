using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Areas
{
    /// <summary>범위가 퍼지는 기준점.</summary>
    public enum AreaAnchor
    {
        /// <summary>플레이어가 선택한 칸 기준.</summary>
        Target,

        /// <summary>시전자 위치 기준.</summary>
        Caster,
    }

    /// <summary>
    /// 모양(AreaShape)과 기준점으로 정하는 범위. 보드 밖 칸은 제외하고, 벽 칸은 포함한다(효과가 각자 판단).
    /// Rotate 면 플레이어가 고른 회전(시계 방향 90도 단위)으로 기준점을 중심으로 모양을 돌린다.
    /// </summary>
    public class PatternArea : SkillArea
    {
        /// <summary>선택한 칸 하나.</summary>
        public static readonly PatternArea SingleCell = new PatternArea(AreaShape.SingleCell, AreaAnchor.Target);

        /// <summary>[TBD 가능]</summary>
        public AreaShape Shape { get; }

        public AreaAnchor Anchor { get; }
        public bool Rotate { get; }

        public PatternArea(AreaShape shape, AreaAnchor anchor, bool rotate = false)
        {
            Shape = shape;
            Anchor = anchor;
            Rotate = rotate;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Shape, path, nameof(Shape), issues);
        }

        public override IReadOnlyList<Position> GetCells(GameState state, Unit caster, Position target, AreaRotation rotation = AreaRotation.R0)
        {
            var cells = new List<Position>();
            Position origin = Anchor == AreaAnchor.Caster ? caster.Position.Value : target;

            foreach ((int dx, int dy) in Shape.GetOffsets(Rotate ? rotation : AreaRotation.R0))
            {
                var position = new Position(origin.X + dx, origin.Y + dy);
                if (state.Board.IsInBounds(position))
                    cells.Add(position);
            }

            return cells;
        }
    }
}
