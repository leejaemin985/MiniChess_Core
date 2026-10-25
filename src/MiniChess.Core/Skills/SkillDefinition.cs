using System;
using System.Collections.Generic;

namespace MiniChess.Core.Skills
{
    /// <summary>
    /// 스킬 정의(데이터). 부품의 조합으로 구성한다.
    ///   비용(ApCost) + 지정 규칙(Targeting) + 범위(Area) + 효과 목록(Effects) + 사용 조건(Conditions)
    ///   + 필수 수치(RequiredTuning)
    /// 수치가 TBD 인 항목은 null 로 두며, 설정이 비어 있는 스킬은 사용할 수 없다(SkillFailReason.ConfigMissing).
    /// </summary>
    public class SkillDefinition
    {
        public string Id { get; }

        /// <summary>임시 기능명.</summary>
        public string Name { get; }

        /// <summary>AP 비용. [TBD 가능]</summary>
        public int? ApCost { get; }

        public SkillTargeting Targeting { get; }
        public SkillArea Area { get; }
        public IReadOnlyList<SkillEffect> Effects { get; }
        public IReadOnlyList<SkillCondition> Conditions { get; }

        /// <summary>
        /// 부품에 드러나지 않는 수치(상태 지속, 덫 피해 등). 설정 누락 검사에만 쓰며, 하나라도 null 이면 사용할 수 없다.
        /// </summary>
        public IReadOnlyList<(string Name, object Value)> RequiredTuning { get; }

        /// <summary>사용 후 시전자의 이번 턴 행동을 모두 끝낸다(예: 분신 소환 후 본체 행동 불가).</summary>
        public bool EndsCasterActions { get; }

        public SkillDefinition(
            string id,
            string name,
            int? apCost,
            SkillTargeting targeting,
            SkillArea area,
            IReadOnlyList<SkillEffect> effects,
            IReadOnlyList<SkillCondition> conditions = null,
            IReadOnlyList<(string Name, object Value)> requiredTuning = null,
            bool endsCasterActions = false)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("스킬 Id 가 비어 있음", nameof(id));

            Id = id;
            Name = name;
            ApCost = apCost;
            Targeting = targeting ?? throw new ArgumentNullException(nameof(targeting));
            Area = area ?? throw new ArgumentNullException(nameof(area));
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Conditions = conditions ?? Array.Empty<SkillCondition>();
            RequiredTuning = requiredTuning ?? Array.Empty<(string, object)>();
            EndsCasterActions = endsCasterActions;
        }

        /// <summary>비어 있는 필수 설정 목록. 비어 있으면 사용 가능한 정의다.</summary>
        public List<string> GetConfigIssues()
        {
            var issues = new List<string>();

            SkillComponent.Require(ApCost, Id, nameof(ApCost), issues);

            Targeting.CollectConfigIssues($"{Id}.{nameof(Targeting)}", issues);
            Area.CollectConfigIssues($"{Id}.{nameof(Area)}", issues);

            SkillEffect.CollectAll(Effects, $"{Id}.{nameof(Effects)}", issues);

            for (int i = 0; i < Conditions.Count; i++)
                Conditions[i].CollectConfigIssues($"{Id}.{nameof(Conditions)}[{i}]", issues);

            foreach ((string name, object value) in RequiredTuning)
                SkillComponent.Require(value, Id, name, issues);

            return issues;
        }

        /// <summary>필수 설정이 모두 채워져 사용 가능한 정의인지.</summary>
        public bool IsConfigured => GetConfigIssues().Count == 0;
    }
}
