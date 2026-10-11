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
        public Position Target { get; }
        public AreaRotation Rotation { get; }
        public IReadOnlyList<Position> AffectedCells { get; }

        /// <summary>이 스킬로 발생한 이벤트(SkillUsedEvent 부터 발생 순).</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        public SkillResult(Unit caster, SkillDefinition skill, Position target, AreaRotation rotation, IReadOnlyList<Position> affectedCells, IReadOnlyList<GameEvent> events)
        {
            Caster = caster;
            Skill = skill;
            Target = target;
            Rotation = rotation;
            AffectedCells = affectedCells;
            Events = events;
        }
    }
}
