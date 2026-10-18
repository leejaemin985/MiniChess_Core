using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Events
{
    /// <summary>유닛이 한 칸 이동했다. 여러 칸 이동은 칸마다 기록된다(워프는 한 번).</summary>
    public class UnitMovedEvent : GameEvent
    {
        public int UnitId { get; }
        public Position From { get; }
        public Position To { get; }
        public MoveKind Kind { get; }

        /// <summary>이동을 일으킨 유닛 Id. 출처가 유닛이 아니면 null.</summary>
        public int? InitiatorId { get; }

        /// <summary>자기 의지로 한 이동인지(일반 이동, 돌진 등). 넉백/당기기/교환당함은 false.</summary>
        public bool IsVoluntary { get; }

        public UnitMovedEvent(int unitId, Position from, Position to, MoveKind kind, int? initiatorId, bool isVoluntary)
        {
            UnitId = unitId;
            From = from;
            To = to;
            Kind = kind;
            InitiatorId = initiatorId;
            IsVoluntary = isVoluntary;
        }
    }

    /// <summary>유닛이 다른 유닛을 기본 공격했다. 이어서 피해 이벤트가 기록된다(장애물 공격은 ObstacleHitEvent).</summary>
    public class UnitAttackedEvent : GameEvent
    {
        public int AttackerId { get; }
        public int TargetId { get; }

        public UnitAttackedEvent(int attackerId, int targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }
    }

    /// <summary>유닛이 피해를 받았다.</summary>
    public class UnitDamagedEvent : GameEvent
    {
        /// <summary>피해를 준 유닛 Id. 출처가 유닛이 아니면 null.</summary>
        public int? SourceId { get; }
        public int TargetId { get; }
        public DamageType Type { get; }

        /// <summary>요청된 피해량(가로채기 전).</summary>
        public int RequestedAmount { get; }

        /// <summary>실제로 HP 에서 깎인 양.</summary>
        public int AppliedAmount { get; }

        public int HpAfter { get; }

        /// <summary>보호막이 흡수한 양.</summary>
        public int ShieldAbsorbed { get; }

        public UnitDamagedEvent(
            int? sourceId, int targetId, DamageType type, int requestedAmount, int appliedAmount, int hpAfter, int shieldAbsorbed)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Type = type;
            RequestedAmount = requestedAmount;
            AppliedAmount = appliedAmount;
            HpAfter = hpAfter;
            ShieldAbsorbed = shieldAbsorbed;
        }
    }

    /// <summary>유닛이 회복했다.</summary>
    public class UnitHealedEvent : GameEvent
    {
        public int? SourceId { get; }
        public int TargetId { get; }
        public int AppliedAmount { get; }
        public int HpAfter { get; }

        public UnitHealedEvent(int? sourceId, int targetId, int appliedAmount, int hpAfter)
        {
            SourceId = sourceId;
            TargetId = targetId;
            AppliedAmount = appliedAmount;
            HpAfter = hpAfter;
        }
    }

    /// <summary>소환물이 생성되어 보드에 놓였다.</summary>
    public class UnitSummonedEvent : GameEvent
    {
        public int OwnerId { get; }

        /// <summary>놓인 직후의 소환물.</summary>
        public UnitSnapshot Summon { get; }

        public UnitSummonedEvent(int ownerId, UnitSnapshot summon)
        {
            OwnerId = ownerId;
            Summon = summon;
        }
    }

    /// <summary>유닛이 사망해 보드에서 제거되었다.</summary>
    public class UnitDiedEvent : GameEvent
    {
        public int UnitId { get; }

        /// <summary>사망한 칸.</summary>
        public Position Position { get; }

        public UnitDiedEvent(int unitId, Position position)
        {
            UnitId = unitId;
            Position = position;
        }
    }

    /// <summary>유닛의 보호막 하나(Id 기준)의 양이 바뀌었다(부여/재충전 또는 피해 흡수). 0 이면 그 보호막은 사라졌다.</summary>
    public class UnitShieldChangedEvent : GameEvent
    {
        public int UnitId { get; }

        /// <summary>바뀐 보호막의 Id.</summary>
        public string ShieldId { get; }

        /// <summary>이 Id 보호막의 바뀐 뒤 양.</summary>
        public int AmountAfter { get; }

        /// <summary>유닛의 모든 보호막 합(바뀐 뒤).</summary>
        public int ShieldAfter { get; }

        public UnitShieldChangedEvent(int unitId, string shieldId, int amountAfter, int shieldAfter)
        {
            UnitId = unitId;
            ShieldId = shieldId;
            AmountAfter = amountAfter;
            ShieldAfter = shieldAfter;
        }
    }
}
