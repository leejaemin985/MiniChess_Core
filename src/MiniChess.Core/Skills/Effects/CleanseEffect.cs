using System.Linq;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 범위 안 대상에게 상대 팀이 건 상태(StatusEffect.SourceTeam 이 대상의 팀과 다름)를 모두 지운다. 걸린 순서대로 지운다.
    /// 상태 종류와 상관없이 누가 걸었는지로만 정한다. 덫처럼 설치물이 건 상태는 설치한 쪽 팀이 건 것으로 본다.
    /// </summary>
    public class CleanseEffect : SkillEffect
    {
        public TargetFilter Filter { get; }

        public CleanseEffect(TargetFilter filter)
        {
            Filter = filter;
        }

        /// <summary>target 에게서 지울 상태인지(상대 팀이 걸었는지).</summary>
        public static bool IsCleansable(Unit target, StatusEffect status) => status.SourceTeam != target.Team;

        public override void Apply(SkillContext context)
        {
            foreach (Unit target in GetUnitsInArea(context, Filter))
            {
                foreach (StatusEffect status in target.Statuses.Where(status => IsCleansable(target, status)).ToList())
                    StatusSystem.Remove(context.State, status, StatusRemoveReason.Removed);
            }
        }
    }
}
