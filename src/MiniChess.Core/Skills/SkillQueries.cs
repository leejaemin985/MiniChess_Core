using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills
{
    /// <summary>
    /// 표현 계층용 조회(스킬 버튼, 지정 가능 칸 하이라이트, 범위 미리보기). 상태를 바꾸지 않는다.
    /// </summary>
    internal static class SkillQueries
    {
        /// <summary>유닛이 가진 스킬(슬롯 순). 경기 스킬 목록에 정의가 없는 Id 는 Definition 이 null.</summary>
        public static List<(string Id, SkillDefinition Definition)> GetSkills(GameState state, Unit unit)
        {
            return unit.Stats.Base.SkillIds
                .Select(id => (id, state.Skills.Find(id)))
                .ToList();
        }

        /// <summary>지금 실제로 사용할 수 있는 (첫 번째) 지정 칸. 사용 불가 상태면 빈 목록.</summary>
        public static List<Position> GetValidTargets(GameState state, Unit caster, string skillId)
        {
            return GetValidTargets(state, caster, skillId, new List<Position>());
        }

        /// <summary>chosen 을 이미 지정했을 때 다음으로 지정할 수 있는 칸(다중 지정 스킬). 다 지정했으면 빈 목록.</summary>
        public static List<Position> GetValidTargets(GameState state, Unit caster, string skillId, IReadOnlyList<Position> chosen)
        {
            if (UseSkillAction.ValidateUsable(state, caster, skillId) != SkillFailReason.None)
                return new List<Position>();

            return state.Skills.Find(skillId).Targeting.GetCandidates(state, caster, chosen).Distinct().ToList();
        }

        /// <summary>사용 가능 여부와 상관없이 스킬의 첫 지정이 닿는 칸(사거리 미리보기). 스킬이 없으면 빈 목록.</summary>
        public static List<Position> GetReach(GameState state, Unit caster, string skillId)
        {
            SkillDefinition skill = state.Skills.Find(skillId);
            if (skill == null || !skill.IsConfigured)
                return new List<Position>();

            return skill.Targeting.GetReach(state, caster).Distinct().ToList();
        }

        /// <summary>
        /// 해당 칸을 지정했을 때 효과가 적용될 칸(범위 미리보기). 설정 오류가 있으면 빈 목록.
        /// 회전하는 범위는 회전 버튼을 누를 때마다 rotation 을 바꿔 다시 조회한다.
        /// </summary>
        public static IReadOnlyList<Position> GetAffectedCells(GameState state, Unit caster, string skillId, Position target, AreaRotation rotation = AreaRotation.R0)
        {
            SkillDefinition skill = state.Skills.Find(skillId);
            if (skill == null || !skill.IsConfigured)
                return new List<Position>();

            return skill.Area.GetCells(state, caster, target, rotation);
        }

        /// <summary>
        /// 방향을 골라 미는 스킬의 예상 도착 칸(밀리지 않으면 현재 칸).
        /// 지정이 다 끝나지 않았거나 유효하지 않거나, 미는 스킬이 아니면 null.
        /// </summary>
        public static Position? PredictPush(GameState state, Unit caster, string skillId, IReadOnlyList<Position> targets)
        {
            SkillDefinition skill = state.Skills.Find(skillId);
            if (skill == null || !skill.IsConfigured || !skill.Targeting.IsValid(state, caster, targets))
                return null;

            PushInDirectionEffect push = skill.Effects.OfType<PushInDirectionEffect>().FirstOrDefault();
            return push?.Predict(state, caster, targets);
        }

        /// <summary>스킬 범위를 플레이어가 회전할 수 있는지(회전 버튼 표시 판단용).</summary>
        public static bool CanRotate(GameState state, string skillId)
        {
            return state.Skills.Find(skillId)?.Area is PatternArea area && area.Rotate;
        }
    }
}
