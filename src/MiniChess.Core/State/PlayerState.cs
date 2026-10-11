using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.State
{
    public class PlayerState
    {
        /// <summary>죽은 유닛도 포함한다(부활 대상 보존). 보드에서만 제거된다.</summary>
        private readonly List<Unit> _units = new List<Unit>();
        private readonly List<StatusDefinition> _basicAttackStatuses = new List<StatusDefinition>();

        public Team Team { get; }

        /// <summary>이 플레이어의 모든 유닛이 공유하는 AP.</summary>
        public ApPool Ap { get; }

        /// <summary>이 플레이어의 턴이 시작된 횟수. 첫 턴 판정(AP 회복 생략)에 사용.</summary>
        public int TurnsStarted { get; internal set; }

        /// <summary>Setup 단계에서 배치를 마쳤는지. 배치는 한 번만 할 수 있다.</summary>
        public bool HasPlaced { get; internal set; }

        public IReadOnlyList<Unit> Units => _units;
        public IEnumerable<Unit> AliveUnits => _units.Where(unit => unit.IsAlive);

        /// <summary>이 팀의 기본 공격이 적중하면 대상에게 거는 상태(점령 보상 등으로 얻는 팀 버프).</summary>
        public IReadOnlyList<StatusDefinition> BasicAttackStatuses => _basicAttackStatuses;

        /// <summary>
        /// 유닛이 1개 이상 있고 살아 있는 유닛이 없으면 전멸. 소환물을 셀지는 경기 규칙의 정책을 따른다.
        /// 정책이 Undecided 인데 소환물만 살아 남은 경우 판정할 수 없으므로 GameConfigException.
        /// </summary>
        public bool IsEliminated(SummonEliminationPolicy policy)
        {
            if (_units.Count == 0)
                return false;

            bool anyRegularAlive = AliveUnits.Any(unit => !unit.IsSummon);
            bool anySummonAlive = AliveUnits.Any(unit => unit.IsSummon);

            if (anyRegularAlive)
                return false;
            if (!anySummonAlive)
                return true;

            switch (policy)
            {
                case SummonEliminationPolicy.ExcludeSummons:
                    return true;
                case SummonEliminationPolicy.IncludeSummons:
                    return false;
                default:
                    throw new GameConfigException(new[]
                    {
                        $"{Team}: 소환물만 남았으나 MatchRuleData.SummonElimination 정책이 정해지지 않음",
                    });
            }
        }

        public PlayerState(Team team, ApPool ap)
        {
            Team = team;
            Ap = ap ?? throw new ArgumentNullException(nameof(ap));
        }

        internal void AddUnit(Unit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (unit.Team != Team)
                throw new ArgumentException($"유닛 {unit.Id} 의 팀({unit.Team})이 플레이어 팀({Team})과 다름", nameof(unit));

            _units.Add(unit);
        }

        internal void AddBasicAttackStatus(StatusDefinition status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));

            _basicAttackStatuses.Add(status);
        }
    }
}
