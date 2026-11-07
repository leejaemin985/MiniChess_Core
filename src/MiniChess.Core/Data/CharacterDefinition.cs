using System;
using System.Collections.Generic;
using System.Linq;

namespace MiniChess.Core.Data
{
    /// <summary>
    /// 캐릭터 정의. 수치가 확정되지 않은(TBD) 항목은 null 로 둔다.
    /// 경기에 쓰려면 ToBaseStats() 로 모든 필수 수치가 채워졌는지 확인한다.
    /// </summary>
    public class CharacterDefinition
    {
        public string Id { get; }

        /// <summary>임시 기능명(고유 이름 아님).</summary>
        public string Name { get; }

        public CharacterRole Role { get; }

        public int? MaxHp { get; set; }

        /// <summary>기본 공격 피해량.</summary>
        public int? Attack { get; set; }

        /// <summary>기본 공격 사거리(칸, 체비셰프 거리).</summary>
        public int? AttackRange { get; set; }

        /// <summary>이동 AP 배율(기본 1). 이동 비용 = 칸 수 × 칸당 AP × 이 값.</summary>
        public int MoveCostMultiplier { get; set; } = 1;

        /// <summary>액티브 스킬 슬롯 2개의 스킬 Id. 아직 정의하지 않은 슬롯은 null.</summary>
        public string[] SkillSlots { get; } = new string[SkillSlotCount];

        public const int SkillSlotCount = 2;

        /// <summary>스킬 슬롯이 아닌 캐릭터 고유 기능의 스킬 Id(예: 낫의 분신 교환). 표현 계층은 슬롯과 따로 보여준다.</summary>
        public List<string> ExtraSkillIds { get; } = new List<string>();

        public CharacterDefinition(string id, string name, CharacterRole role)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("캐릭터 Id 가 비어 있음", nameof(id));

            Id = id;
            Name = name;
            Role = role;
        }

        /// <summary>비어 있는 필수 수치 목록.</summary>
        public List<string> GetMissingFields()
        {
            var missing = new List<string>();
            if (MaxHp == null) missing.Add($"{Id}.{nameof(MaxHp)}");
            if (Attack == null) missing.Add($"{Id}.{nameof(Attack)}");
            if (AttackRange == null) missing.Add($"{Id}.{nameof(AttackRange)}");
            return missing;
        }

        /// <summary>경기에서 쓰는 기본 능력치로 변환한다. 필수 수치가 비어 있으면 GameConfigException.</summary>
        public UnitBaseStats ToBaseStats()
        {
            List<string> missing = GetMissingFields();
            if (missing.Count > 0)
                throw new GameConfigException(missing);

            return new UnitBaseStats
            {
                Id = Id,
                Name = Name,
                MaxHp = MaxHp.Value,
                Attack = Attack.Value,
                AttackRange = AttackRange.Value,
                MoveCostMultiplier = MoveCostMultiplier,
                SkillIds = SkillSlots.Where(id => id != null).Concat(ExtraSkillIds).ToArray(),
            };
        }

        public CharacterDefinition Clone()
        {
            var clone = new CharacterDefinition(Id, Name, Role)
            {
                MaxHp = MaxHp,
                Attack = Attack,
                AttackRange = AttackRange,
                MoveCostMultiplier = MoveCostMultiplier,
            };
            Array.Copy(SkillSlots, clone.SkillSlots, SkillSlotCount);
            clone.ExtraSkillIds.AddRange(ExtraSkillIds);
            return clone;
        }
    }
}
