using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Conditions
{
    /// <summary>시전자(박격포)의 설치 여부(Unit.IsInstalled)가 Required 일 때만 사용 가능(설치 중에만 해체, 설치 중이 아닐 때만 충격탄).</summary>
    public class InstalledCondition : SkillCondition
    {
        public bool Required { get; }

        public InstalledCondition(bool required)
        {
            Required = required;
        }

        public override bool IsMet(GameState state, Unit caster)
        {
            return caster.IsInstalled == Required;
        }
    }
}
