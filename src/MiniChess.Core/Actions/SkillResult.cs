using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;

namespace MiniChess.Core.Actions
{
    public class SkillResult
    {
        public Unit Caster { get; }
        public SkillDefinition Skill { get; }

        /// <summary>지정한 칸 전부(지정 순).</summary>
        public IReadOnlyList<Position> Targets { get; }

        /// <summary>첫 번째 지정 칸.</summary>
        public Position Target => Targets[0];

        public AreaRotation Rotation { get; }
        public IReadOnlyList<Position> AffectedCells { get; }

        /// <summary>이 스킬로 발생한 이벤트(SkillUsedEvent 부터 발생 순).</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        public SkillResult(Unit caster, SkillDefinition skill, IReadOnlyList<Position> targets, AreaRotation rotation, IReadOnlyList<Position> affectedCells, IReadOnlyList<GameEvent> events)
        {
            Caster = caster;
            Skill = skill;
            Targets = targets;
            Rotation = rotation;
            AffectedCells = affectedCells;
            Events = events;
        }
    }
}
