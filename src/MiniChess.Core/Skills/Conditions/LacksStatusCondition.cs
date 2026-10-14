using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Conditions
{
    /// <summary>시전자에게 해당 상태가 걸려 있지 않을 때만 사용 가능(설치 중이 아닐 때만 충격탄 등).</summary>
    public class LacksStatusCondition : SkillCondition
    {
        public string StatusId { get; }

        public LacksStatusCondition(string statusId)
        {
            StatusId = statusId;
        }

        public override bool IsMet(GameState state, Unit caster)
        {
            return caster.FindStatus(StatusId) == null;
        }
    }
}
