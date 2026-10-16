using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills
{
    /// <summary>시전자 기준으로 어떤 유닛을 대상으로 삼는지.</summary>
    public enum TargetFilter
    {
        Enemy,
        Ally,

        /// <summary>자신을 포함한 아군.</summary>
        AllyOrSelf,

        Self,

        /// <summary>자신을 포함한 모든 유닛.</summary>
        Any,
    }

    public static class TargetFilterExtensions
    {
        /// <summary>unit 이 caster 기준으로 조건에 맞는지. 대상 지정 불가 상태(봉인 등)의 유닛은 항상 제외한다.</summary>
        public static bool Matches(this TargetFilter filter, Unit caster, Unit unit)
        {
            if (unit == null || !unit.IsTargetable)
                return false;

            bool isSelf = ReferenceEquals(caster, unit);
            bool isAlly = unit.Team == caster.Team;

            switch (filter)
            {
                case TargetFilter.Enemy: return !isAlly;
                case TargetFilter.Ally: return isAlly && !isSelf;
                case TargetFilter.AllyOrSelf: return isAlly;
                case TargetFilter.Self: return isSelf;
                case TargetFilter.Any: return true;
                default: throw new ArgumentOutOfRangeException(nameof(filter), filter, null);
            }
        }

        /// <summary>칸들 위의 살아 있는 유닛 중 caster 기준으로 조건에 맞는 유닛(칸 순서). 범위 효과/대상 목록 공용.</summary>
        public static List<Unit> GetUnits(this TargetFilter filter, Board board, IEnumerable<Position> cells, Unit caster)
        {
            return cells
                .Select(board.GetLivingUnit)
                .Where(unit => filter.Matches(caster, unit))
                .ToList();
        }
    }
}
