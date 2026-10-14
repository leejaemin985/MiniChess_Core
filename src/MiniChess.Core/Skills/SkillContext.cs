using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Skills
{
    /// <summary>스킬 실행 중 효과들에 전달되는 정보.</summary>
    public class SkillContext
    {
        public GameState State { get; }
        public Unit Caster { get; }
        public SkillDefinition Skill { get; }

        /// <summary>시전 시작 시점의 시전자 위치. 이동하는 효과 뒤에도 원래 위치와 진행 방향을 알 수 있다.</summary>
        public Position CasterOrigin { get; }

        /// <summary>플레이어가 지정한 칸 전부(지정 순).</summary>
        public IReadOnlyList<Position> Targets { get; }

        /// <summary>첫 번째 지정 칸. 범위의 기준이 된다.</summary>
        public Position Target => Targets[0];

        /// <summary>범위 계산 결과(효과가 적용되는 칸). 시전 시점에 한 번 계산한다.</summary>
        public IReadOnlyList<Position> AffectedCells { get; }

        public SkillContext(GameState state, Unit caster, SkillDefinition skill, IReadOnlyList<Position> targets, IReadOnlyList<Position> affectedCells)
        {
            State = state;
            Caster = caster;
            Skill = skill;
            CasterOrigin = caster.Position.Value;
            Targets = targets;
            AffectedCells = affectedCells;
        }

        /// <summary>지정 칸과 범위를 시전자 칸으로 바꾼 정보. 시전자 자신에게 거는 부가 효과(설치 보호막 등)에 쓴다.</summary>
        public SkillContext ForCaster()
        {
            Position self = Caster.Position.Value;
            return new SkillContext(State, Caster, Skill, new[] { self }, new[] { self });
        }
    }
}
