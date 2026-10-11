using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills.Conditions
{
    /// <summary>
    /// 시전자가 이 스킬로 설치해 보드에 남아 있는 구역 수가 한도 미만일 때만 사용 가능.
    /// [가정] 한도에 도달하면 새 설치를 거부한다(오래된 것을 교체하지 않는다).
    /// </summary>
    public class MaxActiveZonesCondition : SkillCondition
    {
        public string SkillId { get; }

        /// <summary>동시에 유지할 수 있는 구역 수. [TBD 가능]</summary>
        public int? Max { get; }

        public MaxActiveZonesCondition(string skillId, int? max)
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
            return CountActiveZones(state, caster, SkillId) < Max.Value;
        }

        public static int CountActiveZones(GameState state, Unit caster, string skillId)
        {
            return state.Board.AllCells()
                .SelectMany(cell => cell.GetEffectsInOrder().OfType<ZoneCellEffect>())
                .Select(effect => effect.Zone)
                .Where(zone => zone.Source == caster && zone.SkillId == skillId)
                .Distinct()
                .Count();
        }
    }
}
