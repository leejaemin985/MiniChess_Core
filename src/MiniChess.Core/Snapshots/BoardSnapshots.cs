using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.State;

namespace MiniChess.Core.Snapshots
{
    /// <summary>칸 하나의 고정 정보(지형, 점령 칸 여부). 경기 중 바뀌지 않는다.</summary>
    public class CellSnapshot
    {
        public Position Position { get; }
        public TerrainType Terrain { get; }
        public bool IsCaptureTile { get; }

        public CellSnapshot(Position position, TerrainType terrain, bool isCaptureTile)
        {
            Position = position;
            Terrain = terrain;
            IsCaptureTile = isCaptureTile;
        }
    }

    /// <summary>칸에 설치된 효과 하나의 값. 한 칸에는 레이어마다 하나뿐이므로 (칸, 레이어)로 가리킨다.</summary>
    public class CellEffectSnapshot
    {
        public Position Position { get; }
        public CellEffectLayer Layer { get; }

        /// <summary>효과 종류(CellEffectKinds). 표시 계층은 이 값으로 모양을 정한다.</summary>
        public string Kind { get; }

        /// <summary>설치한 팀. 팀이 없는 효과면 null.</summary>
        public Team? OwnerTeam { get; }

        /// <summary>설치한 스킬 Id. 스킬로 설치하지 않았으면 null.</summary>
        public string SkillId { get; }

        /// <summary>한 번의 시전으로 설치된 칸들이 공유하는 구역 번호. 구역이 없는 효과면 null.</summary>
        public int? ZoneId { get; }

        /// <summary>설치한 유닛 Id. 설치자가 없으면 null.</summary>
        public int? SourceId { get; }

        /// <summary>남은 수명(설치한 쪽 소유자의 턴 종료 횟수). 수명이 없으면 null.</summary>
        public int? Remaining { get; }

        /// <summary>장애물이 파괴까지 남은 피격 횟수. 장애물이 아니면 null.</summary>
        public int? HitsRemaining { get; }

        public CellEffectSnapshot(
            Position position, CellEffectLayer layer, string kind, Team? ownerTeam, string skillId,
            int? zoneId, int? sourceId, int? remaining, int? hitsRemaining)
        {
            ZoneId = zoneId;
            SourceId = sourceId;
            Position = position;
            Layer = layer;
            Kind = kind;
            OwnerTeam = ownerTeam;
            SkillId = skillId;
            Remaining = remaining;
            HitsRemaining = hitsRemaining;
        }
    }

    /// <summary>예약 포격 한 건의 값.</summary>
    public class StrikeSnapshot
    {
        /// <summary>경기 안에서 고유한 포격 번호.</summary>
        public int Id { get; }
        public string SkillId { get; }
        public int SourceId { get; }
        public Team SourceTeam { get; }
        public IReadOnlyList<Position> Cells { get; }
        public int Damage { get; }
        public int ScheduledTurnNumber { get; }

        public StrikeSnapshot(int id, string skillId, int sourceId, Team sourceTeam, IReadOnlyList<Position> cells, int damage, int scheduledTurnNumber)
        {
            Id = id;
            SkillId = skillId;
            SourceId = sourceId;
            SourceTeam = sourceTeam;
            Cells = cells;
            Damage = damage;
            ScheduledTurnNumber = scheduledTurnNumber;
        }
    }
}
