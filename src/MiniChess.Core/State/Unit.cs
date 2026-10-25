using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.State
{
    public class Unit
    {
        private readonly List<StatusEffect> _statuses = new List<StatusEffect>();
        private readonly List<StatusDefinition> _basicAttackStatuses = new List<StatusDefinition>();

        public int Id { get; }
        public Team Team { get; }
        public UnitStats Stats { get; }

        /// <summary>보드에 배치되지 않았으면 null. 변경은 Board 를 통해서만 한다.</summary>
        public Position? Position { get; internal set; }

        /// <summary>자기 소유자의 이번 턴 행동 기록(쓴 기본 공격/스킬, 행동 종료 등).</summary>
        public UnitTurnState TurnState { get; } = new UnitTurnState();

        /// <summary>소환물(분신 등)인지. 점령/전멸 판정에서 다르게 취급된다.</summary>
        public bool IsSummon { get; internal set; }

        /// <summary>이 소환물을 소환한 유닛. 소환물이 아니면 null.</summary>
        public Unit SummonOwner { get; internal set; }

        /// <summary>이 유닛의 기본 공격이 적중하면 대상에게 거는 상태(분신의 저주 표식 등). 팀 버프와 별개.</summary>
        public IReadOnlyList<StatusDefinition> BasicAttackStatuses => _basicAttackStatuses;

        public bool IsAlive => Stats.IsAlive;
        public bool IsPlaced => Position.HasValue;

        /// <summary>보드 위에 살아 있는지. 행동/피해/상태 부여 대상 판정의 기본.</summary>
        public bool IsOnBoard => IsAlive && IsPlaced;

        public Unit(int id, Team team, IReadOnlyUnitBaseStats baseStats)
        {
            Id = id;
            Team = team;
            Stats = new UnitStats(baseStats);
        }

        /// <summary>
        /// 이번 턴에 아직 쓰지 않은 기본 공격이나 스킬이 남아 있는지(행동 종료면 false). AP, 상태효과, 대상 유무는 보지 않는다.
        /// </summary>
        public bool HasCombatActionLeft =>
            !TurnState.ActionsEnded && (!TurnState.BasicAttackUsed || Stats.Base.SkillIds.Any(id => !TurnState.HasUsedSkill(id)));

        public bool HasSkill(string skillId)
        {
            return skillId != null && Stats.Base.SkillIds.Contains(skillId);
        }

        /// <summary>걸려 있는 상태효과(부여 순). 변경은 StatusSystem 을 통해서만 한다.</summary>
        public IReadOnlyList<StatusEffect> Statuses => _statuses;

        public StatusEffect FindStatus(string definitionId)
        {
            return _statuses.FirstOrDefault(status => status.Definition.Id == definitionId);
        }

        /// <summary>공격·스킬·범위 효과의 대상이 될 수 있는지(봉인 등 대상 지정 불가 상태가 없는지).</summary>
        public bool IsTargetable => !_statuses.Any(status => status.Definition.Behavior?.MakesUntargetable == true);

        public bool HasStatus(StatusEffect status)
        {
            return _statuses.Contains(status);
        }

        internal void AddStatus(StatusEffect status)
        {
            _statuses.Add(status);
        }

        internal bool RemoveStatus(StatusEffect status)
        {
            return _statuses.Remove(status);
        }

        internal void AddBasicAttackStatus(StatusDefinition status)
        {
            _basicAttackStatuses.Add(status);
        }

        /// <summary>
        /// 경기 복사(GameState.Fork) 1단계: 다른 객체를 가리키지 않는 값(HP, 위치, 턴 행동 기록 등)만 복사한 유닛.
        /// 소환자, 보호막, 상태는 모든 유닛을 만든 뒤 ForkLinks 로 잇는다.
        /// </summary>
        internal Unit Fork()
        {
            var copy = new Unit(Id, Team, Stats.Base)
            {
                IsSummon = IsSummon,
                Position = Position,
            };
            copy.Stats.RestoreHp(Stats.CurrentHp);
            copy.TurnState.Restore(TurnState.ActionsEnded, TurnState.VoluntaryCellsMoved, TurnState.BasicAttackUsed, TurnState.UsedSkillIds);
            copy._basicAttackStatuses.AddRange(_basicAttackStatuses);
            return copy;
        }

        /// <summary>경기 복사 2단계: 원본(original)이 가리키는 유닛/상태를 복사본으로 바꿔 잇는다.</summary>
        internal void ForkLinks(Unit original, ForkMap map)
        {
            SummonOwner = map.Unit(original.SummonOwner);

            foreach (ShieldInstance shield in original.Stats.Shields)
            {
                if (shield.Amount > 0)
                    Stats.SetShield(shield.Id, shield.Amount, map.Unit(shield.Source));
            }

            foreach (StatusEffect status in original._statuses)
            {
                StatusEffect copy = status.Fork(map);
                map.Add(status, copy);
                _statuses.Add(copy);
            }
        }

        /// <summary>소유 플레이어의 턴 시작 시 호출한다.</summary>
        internal void ResetTurnState()
        {
            TurnState.Reset();
        }
    }
}
