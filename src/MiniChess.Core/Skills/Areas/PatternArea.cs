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
    /// 모든 범위는 플레이어가 고른 회전(시계 방향 90도 단위)으로 기준점을 중심으로 모양을 돌린다(사용자 확정: 모양과 무관하게 회전 가능).
    /// </summary>
    public class PatternArea : SkillArea
    {
        /// <summary>선택한 칸 하나.</summary>
        public static readonly PatternArea SingleCell = new PatternArea(AreaShape.SingleCell, AreaAnchor.Target);

        /// <summary>[TBD 가능]</summary>
        public AreaShape Shape { get; }

        public AreaAnchor Anchor { get; }

        /// <summary>회전하면 덮는 칸이 달라지는지(대칭 모양이나 한 칸이면 false). 회전 버튼 표시와 봇의 회전 후보 판단용.</summary>
        public bool ChangesWithRotation => Shape != null && Shape.ChangesWithRotation();

        public PatternArea(AreaShape shape, AreaAnchor anchor)
        {
            Shape = shape;
            Anchor = anchor;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Shape, path, nameof(Shape), issues);
        }

        public override IReadOnlyList<Position> GetCells(GameState state, Unit caster, Position target, AreaRotation rotation = AreaRotation.R0)
        {
            var cells = new List<Position>();
            Position origin = Anchor == AreaAnchor.Caster ? caster.Position.Value : target;

            foreach ((int dx, int dy) in Shape.GetOffsets(rotation))
            {
                var position = new Position(origin.X + dx, origin.Y + dy);
                if (state.Board.IsInBounds(position))
                    cells.Add(position);
            }

            return cells;
        }
    }
}
