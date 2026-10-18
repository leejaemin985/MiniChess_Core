using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>자기 자신(현재 칸)만 지정한다.</summary>
    public class SelfTargeting : SkillTargeting
    {
        public override bool IsSelfOnly => true;

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster)
        {
            if (caster.IsPlaced)
                yield return caster.Position.Value;
        }
    }
}
