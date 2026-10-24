using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Capture
{
    /// <summary>
    /// 점령 완료 시 점령한 팀에 주는 보상(명세 3.5 "작은 보상 풀의 무작위 팀 버프").
    /// 경기 규칙(CaptureRuleData.RewardPool)에 데이터로 넣고, CaptureSystem 이 하나를 골라 지급한다.
    /// </summary>
    public abstract class CaptureReward
    {
        /// <summary>보상 종류 식별자(표시/기록용).</summary>
        public abstract string Id { get; }

        /// <summary>점령한 팀에 보상을 적용한다.</summary>
        internal abstract void Grant(GameState state, Team team);

        /// <summary>점령한 팀의 턴 시작마다(점령을 완료한 턴 다음부터) 호출된다. 지속 보상(재생 보호막 등)이 쓴다.</summary>
        internal virtual void OnOwnerTurnStart(GameState state, Team team) { }
    }
}
