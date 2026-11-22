using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Events
{
    /// <summary>칸 효과가 설치되었다. 값은 설치 직후 기준이다.</summary>
    public class CellEffectPlacedEvent : GameEvent
    {
        public CellEffectSnapshot Effect { get; }

        public CellEffectPlacedEvent(CellEffectSnapshot effect)
        {
            Effect = effect;
        }
    }

    public enum CellEffectRemoveReason
    {
        /// <summary>같은 레이어에 새 효과가 설치되어 덮어써짐.</summary>
        Replaced,

        /// <summary>수명 종료.</summary>
        Expired,

        /// <summary>발동 후 소모(일회용 덫 등).</summary>
        Consumed,

        /// <summary>공격으로 파괴(장애물).</summary>
        Destroyed,

        Removed,

        /// <summary>
        /// 설치자가 볼 수 없던 상대 효과(숨긴 덫) 위에 같은 레이어 효과를 설치해 해체함. 양 팀 모두에게 알린다.
        /// (사용자 확정: 어느 쪽 덫이 사라져도 위치는 드러나므로, 드러나는 것을 규칙으로 둔다)
        /// </summary>
        Disarmed,
    }

    /// <summary>유닛이 장애물을 기본 공격했다(UnitAttackedEvent 의 장애물판). 이어서 ObstacleDamagedEvent 가 기록된다.</summary>
    public class ObstacleHitEvent : GameEvent
    {
        public int AttackerId { get; }
        public Position Position { get; }

        public ObstacleHitEvent(int attackerId, Position position)
        {
            AttackerId = attackerId;
            Position = position;
        }
    }

    /// <summary>장애물이 피해를 받았다(UnitDamagedEvent 의 장애물판). 체력이 0 이면 이어서 파괴(제거) 이벤트가 기록된다.</summary>
    public class ObstacleDamagedEvent : GameEvent
    {
        /// <summary>피해를 준 유닛 Id. 출처가 유닛이 아니면(피로도 등) null.</summary>
        public int? SourceId { get; }
        public Position Position { get; }
        public DamageType Type { get; }

        /// <summary>요청된 피해량.</summary>
        public int RequestedAmount { get; }

        /// <summary>실제로 체력에서 깎인 양(남은 체력을 넘지 않음).</summary>
        public int AppliedAmount { get; }

        public int HpAfter { get; }
        public int MaxHp { get; }

        public ObstacleDamagedEvent(int? sourceId, Position position, DamageType type, int requestedAmount, int appliedAmount, int hpAfter, int maxHp)
        {
            SourceId = sourceId;
            Position = position;
            Type = type;
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            HpAfter = hpAfter;
            MaxHp = maxHp;
        }
    }

    /// <summary>칸 효과가 제거되었다. 한 칸에는 레이어마다 효과가 하나뿐이므로 (칸, 레이어)로 가리킨다.</summary>
    public class CellEffectRemovedEvent : GameEvent
    {
        public Position Position { get; }
        public CellEffectLayer Layer { get; }

        /// <summary>제거된 효과의 종류(CellEffectKinds).</summary>
        public string Kind { get; }

        /// <summary>제거된 효과를 설치한 팀. 팀이 없는 효과면 null.</summary>
        public Team? OwnerTeam { get; }

        public CellEffectRemoveReason Reason { get; }

        public CellEffectRemovedEvent(Position position, CellEffectLayer layer, string kind, Team? ownerTeam, CellEffectRemoveReason reason)
        {
            Position = position;
            Layer = layer;
            Kind = kind;
            OwnerTeam = ownerTeam;
            Reason = reason;
        }
    }

    /// <summary>칸 효과가 들어온 유닛에게 발동했다(덫 등). 이어서 발동 결과(소모로 인한 제거, 피해, 상태 순)가 기록된다.</summary>
    public class CellEffectTriggeredEvent : GameEvent
    {
        public Position Position { get; }
        public CellEffectLayer Layer { get; }

        /// <summary>발동한 효과의 종류(CellEffectKinds).</summary>
        public string Kind { get; }

        /// <summary>효과를 설치한 팀. 팀이 없는 효과면 null.</summary>
        public Team? OwnerTeam { get; }

        /// <summary>발동시킨 유닛.</summary>
        public int UnitId { get; }

        public CellEffectTriggeredEvent(Position position, CellEffectLayer layer, string kind, Team? ownerTeam, int unitId)
        {
            Position = position;
            Layer = layer;
            Kind = kind;
            OwnerTeam = ownerTeam;
            UnitId = unitId;
        }
    }
}
