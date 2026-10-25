using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Effects.Scheduled
{
    /// <summary>
    /// 예약 포격 한 건. 시전자 소유자의 다음 턴 시작(예약 효과 단계)에 범위 칸의 적에게 피해를 준다.
    /// 칸 효과(장판)와 별개로 관리하므로 같은 칸의 장판을 덮어쓰지 않는다.
    /// </summary>
    public class ScheduledStrike
    {
        /// <summary>경기 안에서 고유한 번호. 예약될 때 정해진다.</summary>
        public int Id { get; internal set; } = -1;

        public string SkillId { get; }

        /// <summary>조준한 유닛. 착탄 전에 죽어도 남는다(스냅샷으로 복원한 경기에서는 죽은 시전자면 null).</summary>
        public Unit Source { get; }

        /// <summary>시전자 Id 와 팀. Source 가 없어도(복원한 경기) 착탄 순서/숨김 판정/표시에 쓴다.</summary>
        public int SourceId { get; }
        public Team SourceTeam { get; }

        /// <summary>조준한 범위 칸. 조준 시점에 고정된다.</summary>
        public IReadOnlyList<Position> Cells { get; }

        public int Damage { get; }

        /// <summary>조준한 경기 턴 번호. 같은 턴에는 발사하지 않는다.</summary>
        public int ScheduledTurnNumber { get; }

        public ScheduledStrike(string skillId, Unit source, IReadOnlyList<Position> cells, int damage, int scheduledTurnNumber)
            : this(skillId, source, source.Id, source.Team, cells, damage, scheduledTurnNumber)
        {
        }

        /// <summary>스냅샷 복원/경기 복사용: 시전자가 이미 죽어 스냅샷에 없으면 source 가 null 이다.</summary>
        internal ScheduledStrike(string skillId, Unit source, int sourceId, Team sourceTeam, IReadOnlyList<Position> cells, int damage, int scheduledTurnNumber)
        {
            SkillId = skillId;
            Source = source;
            SourceId = sourceId;
            SourceTeam = sourceTeam;
            Cells = cells;
            Damage = damage;
            ScheduledTurnNumber = scheduledTurnNumber;
        }
    }
}
