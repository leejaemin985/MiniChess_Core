using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Skills.Areas;

namespace MiniChess.Core.Events
{
    /// <summary>스킬 시전 시작. 사용 명령의 입력(지정 칸 전부, 회전)과 계산된 범위를 담는다. 이 이벤트 뒤에 스킬 효과로 인한 이벤트들이 이어진다.</summary>
    public class SkillUsedEvent : GameEvent
    {
        public int CasterId { get; }
        public string SkillId { get; }

        /// <summary>지정한 칸 전부(지정 순).</summary>
        public IReadOnlyList<Position> Targets { get; }

        /// <summary>범위 회전(시계 방향). 회전하는 범위의 스킬에만 의미가 있다.</summary>
        public AreaRotation Rotation { get; }

        public IReadOnlyList<Position> AffectedCells { get; }

        public SkillUsedEvent(int casterId, string skillId, IReadOnlyList<Position> targets, AreaRotation rotation, IReadOnlyList<Position> affectedCells)
        {
            CasterId = casterId;
            SkillId = skillId;
            Targets = targets;
            Rotation = rotation;
            AffectedCells = affectedCells;
        }
    }
}
