using MiniChess.Core.Combat;
using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses
{
    /// <summary>상태효과 훅에 전달되는 정보.</summary>
    public class StatusContext
    {
        public GameState State { get; }
        public StatusEffect Status { get; }

        public Unit Target => Status.Target;

        public StatusContext(GameState state, StatusEffect status)
        {
            State = state;
            Status = status;
        }
    }

    /// <summary>
    /// 상태의 실제 동작. 필요한 훅만 재정의한다.
    /// 동작은 정의 단위로 공유되므로 상태 한 건에 묶인 값은 StatusEffect.BehaviorData 에 둔다.
    /// 피해 처리에 끼어드는 상태는 OnApplied 에서 IDamageInterceptor 를 등록해 BehaviorData 에 두고, OnRemoved 에서 해제한다.
    /// </summary>
    public abstract class StatusBehavior
    {
        public virtual void OnApplied(StatusContext context) { }

        public virtual void OnRemoved(StatusContext context) { }

        /// <summary>정의의 Trigger 시점마다 호출된다.</summary>
        public virtual void OnTrigger(StatusContext context) { }

        /// <summary>대상이 피해를 받은 뒤(보호막/HP 처리 후, 살아 있을 때만) 호출된다. 피해가 0 이어도 호출된다.</summary>
        public virtual void OnDamaged(StatusContext context, DamageRequest request) { }

        /// <summary>이 상태가 대상의 기본 공격을 막는지.</summary>
        public virtual bool BlocksBasicAttack(StatusContext context)
        {
            return false;
        }

        /// <summary>대상이 유닛을 기본 공격할 때 더할 피해. 0 보다 크면 그 기본 공격에서 상태가 소모된다(차징 등).</summary>
        public virtual int BasicAttackBonus(StatusContext context)
        {
            return 0;
        }

        /// <summary>이 상태가 대상의 이동을 막는지.</summary>
        /// <param name="initiator">이동을 일으킨 유닛. 대상 자신이면 자발적 이동.</param>
        /// <param name="cells">이번 이동의 칸 수.</param>
        public virtual MoveBlockReason CheckMove(StatusContext context, Unit initiator, MoveKind kind, int cells)
        {
            return MoveBlockReason.None;
        }
    }
}
