using MiniChess.Core.Movement;
using MiniChess.Core.State;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 설치: 자발적 이동과 기본 공격을 막는다. 외부 강제 이동(워프 교환 등)은 막지 않는다.
    /// 설치형 캐릭터 공용이며, 보호막 같은 부가 효과는 설치/해체 스킬이 따로 처리한다.
    /// </summary>
    public class InstalledBehavior : StatusBehavior
    {
        public override bool BlocksBasicAttack(StatusContext context)
        {
            return true;
        }

        public override MoveBlockReason CheckMove(StatusContext context, Unit initiator, MoveKind kind, int cells)
        {
            return MovementControl.IsVoluntary(context.Target, initiator) ? MoveBlockReason.Installed : MoveBlockReason.None;
        }
    }
}
