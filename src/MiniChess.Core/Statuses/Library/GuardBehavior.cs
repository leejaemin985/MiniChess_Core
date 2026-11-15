using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 호위: 상태를 받은 유닛(보호 대상)의 직접 피해를 상태를 건 유닛(호위자)이 대신 받는다.
    /// 걸릴 때 피해 가로채기를 등록해 상태(BehaviorData)에 묶어 두고, 풀릴 때 해제한다. 호위자는 한 명만 보호하므로
    /// 같은 호위자가 다른 대상에게 건 호위는 새로 걸 때 해제한다.
    /// 호위자는 대신 받는 피해를 DamageReduction 만큼 덜 받는다(0 까지).
    /// </summary>
    public class GuardBehavior : StatusBehavior
    {
        public override System.Collections.Generic.IReadOnlyDictionary<string, int> Parameters =>
            new System.Collections.Generic.Dictionary<string, int>
            {
                [nameof(MaxDistance)] = MaxDistance,
                [nameof(DamageReduction)] = DamageReduction,
            };

        /// <summary>피해 시점에 호위자와 보호 대상 사이 최대 거리(체비셰프).</summary>
        public int MaxDistance { get; }

        /// <summary>호위자가 대신 받는 피해에서 빼는 양.</summary>
        public int DamageReduction { get; }

        public GuardBehavior(int maxDistance, int damageReduction = 0)
        {
            if (damageReduction < 0) throw new System.ArgumentOutOfRangeException(nameof(damageReduction));

            MaxDistance = maxDistance;
            DamageReduction = damageReduction;
        }

        public override void OnApplied(StatusContext context)
        {
            StatusEffect status = context.Status;
            GameState state = context.State;

            foreach (StatusEffect other in StatusSystem.FindBySource(state, status.Source))
            {
                if (other != status && other.Definition.Id == status.Definition.Id)
                    StatusSystem.Remove(state, other, StatusRemoveReason.Replaced);
            }

            var interceptor = new GuardInterceptor(status, MaxDistance, DamageReduction);
            status.BehaviorData = interceptor;
            state.AddDamageInterceptor(interceptor);
        }

        public override void OnRemoved(StatusContext context)
        {
            if (context.Status.BehaviorData is GuardInterceptor interceptor)
                context.State.RemoveDamageInterceptor(interceptor);
        }
    }

    /// <summary>호위 상태 하나에 대응하는 피해 가로채기.</summary>
    internal class GuardInterceptor : IDamageInterceptor
    {
        public StatusEffect Status { get; }
        public int MaxDistance { get; }
        public int DamageReduction { get; }

        // [정책] 다른 가로채기(피해 감소 등)보다 먼저: 대상이 바뀌면 이후 단계는 호위자 기준으로 다시 처리된다.
        public int Order => 0;

        public GuardInterceptor(StatusEffect status, int maxDistance, int damageReduction)
        {
            Status = status;
            MaxDistance = maxDistance;
            DamageReduction = damageReduction;
        }

        public int Intercept(GameState state, DamageRequest request, int currentAmount)
        {
            Unit guardian = Status.Source;
            Unit protectedUnit = Status.Target;

            // 직접 피해만 대신 받는다. 전가 피해(Redirected)는 다시 전가하지 않아 순환이 생기지 않는다.
            if (request.Target != protectedUnit || request.Type != DamageType.Direct || currentAmount <= 0)
                return currentAmount;
            // 체력 변화가 막힌 호위자(봉인 등)는 호위하지 못한다. 대신 받으면 피해가 사라지기 때문이다.
            if (guardian == null || !guardian.IsOnBoard || StatusSystem.BlocksHpChange(state, guardian))
                return currentAmount;
            if (RangeCalculator.GetDistance(guardian.Position.Value, protectedUnit.Position.Value) > MaxDistance)
                return currentAmount;

            // [정책] 감소 후 0 이어도 대신 받은 것으로 처리한다(보호 대상은 0, 호위자에게도 0 피해 이벤트가 남아 호위 반응이 보인다).
            int redirected = System.Math.Max(0, currentAmount - DamageReduction);
            DamageSystem.Apply(state, new DamageRequest(request.Source, guardian, redirected, DamageType.Redirected));
            return 0;
        }
    }
}
