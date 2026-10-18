using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Data;

namespace MiniChess.Core.State
{
    /// <summary>
    /// 유닛의 스탯. 기본값(Base) 참조 + 게임 중 변하는 값(현재 HP 등)을 함께 관리한다.
    /// 외부에서는 Base 대신 이 클래스의 값을 읽는다(추후 버프/디버프가 여기서 반영됨).
    /// </summary>
    public class UnitStats
    {
        /// <summary>보유 중인 보호막(받은 순서). 흡수도 이 순서(먼저 받은 것부터)로 한다.</summary>
        private readonly List<ShieldInstance> _shields = new List<ShieldInstance>();

        /// <summary>기본 능력치(같은 종류 유닛끼리 공유).</summary>
        public IReadOnlyUnitBaseStats Base { get; }

        public int CurrentHp { get; private set; }

        public int MaxHp => Base.MaxHp;
        public int Attack => Base.Attack;
        public int AttackRange => Base.AttackRange;

        /// <summary>보유 중인 보호막(받은 순서). Id 가 다르면 함께 존재하고, 같은 Id 는 중첩되지 않는다.</summary>
        public IReadOnlyList<ShieldInstance> Shields => _shields;

        /// <summary>남은 보호막의 합. HP 보다 먼저 피해를 흡수하며, 소모될 때까지 유지된다.</summary>
        public int Shield => _shields.Sum(shield => shield.Amount);

        public bool IsAlive => CurrentHp > 0;

        public UnitStats(IReadOnlyUnitBaseStats baseStats)
        {
            Base = baseStats ?? throw new ArgumentNullException(nameof(baseStats));
            CurrentHp = MaxHp;
        }

        /// <summary>피해를 적용한다. HP 는 0 아래로 내려가지 않는다.</summary>
        internal void ApplyDamage(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            CurrentHp = Math.Max(0, CurrentHp - amount);
        }

        /// <summary>스냅샷 복원용: 현재 HP 를 그대로 맞춘다.</summary>
        internal void RestoreHp(int hp)
        {
            CurrentHp = Math.Max(0, Math.Min(MaxHp, hp));
        }

        public ShieldInstance FindShield(string shieldId)
        {
            return _shields.FirstOrDefault(shield => shield.Id == shieldId);
        }

        /// <summary>
        /// 보호막을 부여한다. 같은 Id 가 이미 있으면 중첩하지 않고 새 양으로 교체(재충전)한다.
        /// 교체된 보호막은 새로 받은 것으로 보고 흡수 순서의 맨 뒤로 간다.
        /// </summary>
        internal ShieldInstance SetShield(string shieldId, int amount, Unit source)
        {
            if (string.IsNullOrEmpty(shieldId)) throw new ArgumentException("보호막 Id 가 비어 있음", nameof(shieldId));
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

            _shields.RemoveAll(shield => shield.Id == shieldId);

            var instance = new ShieldInstance(shieldId, amount, source);
            _shields.Add(instance);
            return instance;
        }

        /// <summary>해당 Id 의 보호막을 제거한다. 제거한 보호막, 없으면 null.</summary>
        internal ShieldInstance RemoveShield(string shieldId)
        {
            ShieldInstance shield = FindShield(shieldId);
            if (shield != null)
                _shields.Remove(shield);

            return shield;
        }

        /// <summary>
        /// 피해를 보호막으로 흡수한다(먼저 받은 것부터). 모두 흡수된 보호막은 제거한다.
        /// 흡수에 쓰인 보호막과 각각 흡수한 양을 반환한다.
        /// </summary>
        internal List<(ShieldInstance Shield, int Absorbed)> AbsorbWithShields(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            var used = new List<(ShieldInstance, int)>();

            foreach (ShieldInstance shield in _shields.ToList())
            {
                if (amount == 0)
                    break;

                int absorbed = Math.Min(shield.Amount, amount);
                shield.Amount -= absorbed;
                amount -= absorbed;
                used.Add((shield, absorbed));

                if (shield.Amount == 0)
                    _shields.Remove(shield);
            }

            return used;
        }

        /// <summary>회복을 적용한다. HP 는 MaxHp 를 넘지 않는다.</summary>
        internal void Heal(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
        }
    }
}
