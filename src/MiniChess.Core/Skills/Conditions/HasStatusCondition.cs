using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Conditions
{
    /// <summary>시전자에게 해당 상태가 걸려 있을 때만 사용 가능(설치 중에만 해체 등).</summary>
    public class HasStatusCondition : SkillCondition
    {
        public string StatusId { get; }

        public HasStatusCondition(string statusId)
        {
            StatusId = statusId;
        }

        public override bool IsMet(GameState state, Unit caster)
        {
            return caster.FindStatus(StatusId) != null;
        }
    }
}
