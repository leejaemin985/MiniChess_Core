using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills
{
    /// <summary>지정한 칸을 기준으로 효과가 적용될 칸들. 보드 밖 칸은 포함하지 않는다.</summary>
    public abstract class SkillArea : SkillComponent
    {
        /// <param name="rotation">플레이어가 고른 회전. 회전하지 않는 범위는 무시한다.</param>
        public abstract IReadOnlyList<Position> GetCells(GameState state, Unit caster, Position target, AreaRotation rotation = AreaRotation.R0);
    }
}
