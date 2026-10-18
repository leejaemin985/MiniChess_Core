using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Combat
{
    /// <summary>
    /// 모든 피해/회복의 단일 처리 경로.
    /// 처리 순서: 가로채기(Order 순) → 보호막 흡수 → HP 적용 → 이벤트 기록 → 사망·승패 판정 → (생존 시) 상태효과 OnDamaged.
    /// </summary>
    public static class DamageSystem
    {
        /// <summary>
        /// 피해를 적용한다. 보드 위에 살아 있는 대상만 처리하며, 그 외에는 아무것도 하지 않는다.
        /// 체력 변화를 막는 상태(봉인 등)의 대상은 가로채기 전에 무효 처리한다(호위로 넘어가지도 않는다).
        /// </summary>
        internal static DamageOutcome Apply(GameState state, DamageRequest request)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (request == null) throw new ArgumentNullException(nameof(request));

            Unit target = request.Target;
            if (state.IsGameOver || !target.IsOnBoard || StatusSystem.BlocksHpChange(state, target))
                return DamageOutcome.None;

            int amount = request.Amount;
            foreach (IDamageInterceptor interceptor in state.DamageInterceptors.OrderBy(i => i.Order))
                amount = Math.Max(0, interceptor.Intercept(state, request, amount));

            // 가로채기(호위 대신 맞기 등)로 경기가 끝났으면 남은 피해는 적용하지 않는다.
            if (state.IsGameOver)
                return DamageOutcome.None;

            // [가정] 보호막은 피해 종류와 무관하게, 가로채기(호위 등)가 끝난 뒤의 피해를 흡수한다.
            int shieldBefore = target.Stats.Shield;
            List<(ShieldInstance Shield, int Absorbed)> usedShields = target.Stats.AbsorbWithShields(amount);
            int absorbed = usedShields.Sum(used => used.Absorbed);
            amount -= absorbed;

            int hpBefore = target.Stats.CurrentHp;
            target.Stats.ApplyDamage(amount);
            int applied = hpBefore - target.Stats.CurrentHp;

            state.Events.Record(new UnitDamagedEvent(
                request.Source?.Id, target.Id, request.Type, request.Amount, applied, target.Stats.CurrentHp, absorbed));

            int shieldAfter = shieldBefore;
            foreach ((ShieldInstance shield, int used) in usedShields)
            {
                shieldAfter -= used;
                state.Events.Record(new UnitShieldChangedEvent(target.Id, shield.Id, shield.Amount, shieldAfter));
            }

            bool killed = DeathSystem.HandleIfDead(state, target);
            if (!killed && !state.IsGameOver)
                StatusSystem.NotifyDamaged(state, target, request);

            return new DamageOutcome(applied, killed);
        }

        /// <summary>
        /// 보호막을 부여한다. 같은 Id 의 보호막이 있으면 중첩하지 않고 새 양으로 재충전한다.
        /// 보드 위에 살아 있는 대상만 처리한다.
        /// </summary>
        /// <param name="source">보호막을 준 유닛. 유닛이 아닌 출처면 null.</param>
        internal static void AddShield(GameState state, Unit target, string shieldId, int amount, Unit source)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));

            if (!target.IsOnBoard || amount <= 0)
                return;

            ShieldInstance shield = target.Stats.SetShield(shieldId, amount, source);
            state.Events.Record(new UnitShieldChangedEvent(target.Id, shield.Id, shield.Amount, target.Stats.Shield));
        }

        /// <summary>해당 Id 의 보호막을 남은 양과 무관하게 제거한다(설치 해제 등). 없으면 아무것도 하지 않는다.</summary>
        internal static void RemoveShield(GameState state, Unit target, string shieldId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));

            ShieldInstance removed = target.Stats.RemoveShield(shieldId);
            if (removed != null)
                state.Events.Record(new UnitShieldChangedEvent(target.Id, shieldId, 0, target.Stats.Shield));
        }

        /// <summary>회복을 적용한다. 보드 위에 살아 있고 체력 변화를 막는 상태가 없는 대상만 처리한다.</summary>
        internal static int Heal(GameState state, Unit source, Unit target, int amount)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));

            if (!target.IsOnBoard || StatusSystem.BlocksHpChange(state, target))
                return 0;

            int hpBefore = target.Stats.CurrentHp;
            target.Stats.Heal(amount);
            int applied = target.Stats.CurrentHp - hpBefore;

            state.Events.Record(new UnitHealedEvent(source?.Id, target.Id, applied, target.Stats.CurrentHp));
            return applied;
        }
    }

    public readonly struct DamageOutcome
    {
        public static readonly DamageOutcome None = new DamageOutcome(0, false);

        /// <summary>실제로 HP 에서 깎인 양.</summary>
        public int AppliedAmount { get; }

        /// <summary>이 피해로 대상이 사망했는지.</summary>
        public bool Killed { get; }

        public DamageOutcome(int appliedAmount, bool killed)
        {
            AppliedAmount = appliedAmount;
            Killed = killed;
        }
    }
}
