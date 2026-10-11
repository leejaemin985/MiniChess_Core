using System.Linq;
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
    /// </summary>
    public class GuardBehavior : StatusBehavior
    {
        /// <summary>피해 시점에 호위자와 보호 대상 사이 최대 거리(체비셰프).</summary>
        public int MaxDistance { get; }

        public GuardBehavior(int maxDistance)
        {
            MaxDistance = maxDistance;
        }

        public override void OnApplied(StatusContext context)
        {
            StatusEffect status = context.Status;
            GameState state = context.State;

            foreach (StatusEffect other in state.AllUnits().SelectMany(u => u.Statuses).ToList())
            {
                if (other != status && other.Definition.Id == status.Definition.Id && other.Source == status.Source)
                    StatusSystem.Remove(state, other, StatusRemoveReason.Replaced);
            }

            var interceptor = new GuardInterceptor(status, MaxDistance);
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

        // [정책] 다른 가로채기(피해 감소 등)보다 먼저: 대상이 바뀌면 이후 단계는 호위자 기준으로 다시 처리된다.
        public int Order => 0;

        public GuardInterceptor(StatusEffect status, int maxDistance)
        {
            Status = status;
            MaxDistance = maxDistance;
        }

        public int Intercept(GameState state, DamageRequest request, int currentAmount)
        {
            Unit guardian = Status.Source;
            Unit protectedUnit = Status.Target;

            // 직접 피해만 대신 받는다. 전가 피해(Redirected)는 다시 전가하지 않아 순환이 생기지 않는다.
            if (request.Target != protectedUnit || request.Type != DamageType.Direct || currentAmount <= 0)
                return currentAmount;
            if (guardian == null || !guardian.IsOnBoard)
                return currentAmount;
            if (RangeCalculator.GetDistance(guardian.Position.Value, protectedUnit.Position.Value) > MaxDistance)
                return currentAmount;

            DamageSystem.Apply(state, new DamageRequest(request.Source, guardian, currentAmount, DamageType.Redirected));
            return 0;
        }
    }
}
