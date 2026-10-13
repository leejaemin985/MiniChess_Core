using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Conditions
{
    /// <summary>
    /// 예약 효과 개수 제한. 시전자가 이 스킬로 걸어 둔 예약(아직 발동 전)이 Max 개 미만일 때만 사용할 수 있다.
    /// 예약이 발동하면 개수에서 빠진다.
    /// </summary>
    public class MaxActiveStrikesCondition : SkillCondition
    {
        /// <summary>개수를 셀 스킬.</summary>
        public string SkillId { get; }

        /// <summary>시전자당 동시에 걸어 둘 수 있는 예약 수. [TBD 가능]</summary>
        public int? Max { get; }

        public MaxActiveStrikesCondition(string skillId, int? max)
        {
            SkillId = skillId;
            Max = max;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Max, path, nameof(Max), issues);
        }

        public override bool IsMet(GameState state, Unit caster)
        {
            return state.ScheduledStrikes.Count(s => s.Source == caster && s.SkillId == SkillId) < Max.Value;
        }
    }
}
