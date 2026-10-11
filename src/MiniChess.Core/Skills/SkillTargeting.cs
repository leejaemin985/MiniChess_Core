using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills
{
    /// <summary>
    /// 플레이어가 무엇을 지정할 수 있는지. 지정 하나는 칸(Position)으로 표현하며,
    /// TargetCount 개를 순서대로 지정한다(대부분 1개). 설정 오류가 없는 상태에서만 호출된다.
    /// </summary>
    public abstract class SkillTargeting : SkillComponent
    {
        /// <summary>지정해야 하는 칸 수.</summary>
        public virtual int TargetCount => 1;

        /// <summary>첫 번째로 지정할 수 있는 칸 전부.</summary>
        public abstract IEnumerable<Position> GetCandidates(GameState state, Unit caster);

        /// <summary>chosen 을 이미 지정했을 때 다음으로 지정할 수 있는 칸. 다 지정했으면 빈 목록.</summary>
        public virtual IEnumerable<Position> GetCandidates(GameState state, Unit caster, IReadOnlyList<Position> chosen)
        {
            return chosen.Count == 0 ? GetCandidates(state, caster) : Enumerable.Empty<Position>();
        }

        public bool IsValid(GameState state, Unit caster, Position target)
        {
            return IsValid(state, caster, new[] { target });
        }

        /// <summary>지정 개수가 맞고, 각 지정이 앞선 지정 기준으로 유효한지.</summary>
        public bool IsValid(GameState state, Unit caster, IReadOnlyList<Position> targets)
        {
            if (targets == null || targets.Count != TargetCount)
                return false;

            for (int i = 0; i < targets.Count; i++)
            {
                IReadOnlyList<Position> chosen = targets.Take(i).ToList();
                if (!GetCandidates(state, caster, chosen).Contains(targets[i]))
                    return false;
            }

            return true;
        }
    }
}
