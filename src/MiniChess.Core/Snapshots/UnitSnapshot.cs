using System.Collections.Generic;
using MiniChess.Core.Common;

namespace MiniChess.Core.Snapshots
{
    /// <summary>
    /// 유닛 한 명의 특정 시점 값. 살아 있는 객체를 가리키지 않으므로 직렬화/전송/보관해도 나중에 바뀌지 않는다.
    /// </summary>
    public class UnitSnapshot
    {
        public int Id { get; }
        public Team Team { get; }

        /// <summary>캐릭터 Id(소환물은 소환물 Id).</summary>
        public string CharacterId { get; }
        public string Name { get; }

        /// <summary>보드 위 칸. 배치 전이거나 사망해 제거되었으면 null.</summary>
        public Position? Position { get; }

        public int Hp { get; }
        public int MaxHp { get; }
        public int Attack { get; }
        public int AttackRange { get; }

        public bool IsSummon { get; }

        /// <summary>소환자 Id. 소환물이 아니면 null.</summary>
        public int? SummonOwnerId { get; }

        public IReadOnlyList<string> SkillIds { get; }
        public IReadOnlyList<ShieldSnapshot> Shields { get; }

        /// <summary>걸린 상태(부여 순).</summary>
        public IReadOnlyList<StatusSnapshot> Statuses { get; }

        /// <summary>이번 턴에 전투 행동(기본 공격/전투 스킬)을 했는지.</summary>
        public bool CombatActionUsed { get; }

        /// <summary>이번 턴에 더 이상 행동할 수 없는지.</summary>
        public bool ActionsEnded { get; }

        /// <summary>이번 턴에 자발적 이동이 금지되었는지(전투 행동 후).</summary>
        public bool VoluntaryMoveLocked { get; }

        /// <summary>이번 턴에 일반 이동으로 움직인 칸 수의 누적(이동 거리 제한 판정용).</summary>
        public int VoluntaryCellsMoved { get; }

        public UnitSnapshot(
            int id, Team team, string characterId, string name, Position? position,
            int hp, int maxHp, int attack, int attackRange, bool isSummon, int? summonOwnerId,
            IReadOnlyList<string> skillIds, IReadOnlyList<ShieldSnapshot> shields, IReadOnlyList<StatusSnapshot> statuses,
            bool combatActionUsed, bool actionsEnded, bool voluntaryMoveLocked, int voluntaryCellsMoved)
        {
            VoluntaryMoveLocked = voluntaryMoveLocked;
            VoluntaryCellsMoved = voluntaryCellsMoved;
            Id = id;
            Team = team;
            CharacterId = characterId;
            Name = name;
            Position = position;
            Hp = hp;
            MaxHp = maxHp;
            Attack = attack;
            AttackRange = attackRange;
            IsSummon = isSummon;
            SummonOwnerId = summonOwnerId;
            SkillIds = skillIds;
            Shields = shields;
            Statuses = statuses;
            CombatActionUsed = combatActionUsed;
            ActionsEnded = actionsEnded;
        }
    }

    /// <summary>
    /// 유닛에 걸린 상태 한 건의 값. 출처별 상태는 한 유닛에 같은 Id 가 여러 건일 수 있으므로 (유닛 Id, 상태 Id, 건 유닛 Id, 원인)으로 가리킨다.
    /// </summary>
    public class StatusSnapshot
    {
        public string StatusId { get; }

        /// <summary>건 원인(스킬 Id 등, StatusEffect.OriginId). 모르면 null.</summary>
        public string OriginId { get; }

        /// <summary>남은 횟수. 영구 상태면 의미 없음.</summary>
        public int Remaining { get; }
        public bool IsPermanent { get; }

        /// <summary>건 유닛 Id. 유닛이 아닌 출처면 null.</summary>
        public int? SourceId { get; }
        public Team SourceTeam { get; }

        /// <summary>정의의 지속 횟수(걸리거나 갱신될 때 Remaining 의 시작값).</summary>
        public int Duration { get; }

        /// <summary>정의의 동작 수치(StatusBehavior.Parameters). 스냅샷에서 상태를 복원할 때 쓴다.</summary>
        public IReadOnlyDictionary<string, int> Parameters { get; }

        public StatusSnapshot(
            string statusId, int remaining, bool isPermanent, int? sourceId, Team sourceTeam, int duration, IReadOnlyDictionary<string, int> parameters,
            string originId = null)
        {
            OriginId = originId;
            Duration = duration;
            Parameters = parameters;
            StatusId = statusId;
            Remaining = remaining;
            IsPermanent = isPermanent;
            SourceId = sourceId;
            SourceTeam = sourceTeam;
        }
    }

    /// <summary>유닛의 보호막 하나(Id 별)의 값.</summary>
    public class ShieldSnapshot
    {
        public string ShieldId { get; }
        public int Amount { get; }

        public ShieldSnapshot(string shieldId, int amount)
        {
            ShieldId = shieldId;
            Amount = amount;
        }
    }
}
