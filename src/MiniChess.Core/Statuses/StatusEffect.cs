using MiniChess.Core.Common;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Statuses
{
    /// <summary>유닛에 실제로 걸린 상태 한 건. 유닛별 진행 값(남은 횟수 등)은 여기에 둔다.</summary>
    public class StatusEffect
    {
        public StatusDefinition Definition { get; }
        public Unit Target { get; }

        /// <summary>건 유닛. 맵 효과처럼 유닛이 아닌 출처면 null.</summary>
        public Unit Source { get; private set; }

        /// <summary>건 쪽의 팀. Source 가 없어도 SourceOwner 시점 판정에 쓴다.</summary>
        public Team SourceTeam { get; private set; }

        public int Remaining { get; internal set; }

        /// <summary>걸리거나 갱신된 경기 턴 번호.</summary>
        public int AppliedTurnNumber { get; private set; }

        /// <summary>
        /// 동작(StatusBehavior)이 이 상태 한 건에 묶어 두는 값(등록한 가로채기 등).
        /// 동작은 정의 단위로 공유되므로 건별 값은 여기에 둔다. 동작 외에는 쓰지 않는다.
        /// </summary>
        public object BehaviorData { get; set; }

        internal StatusEffect(StatusDefinition definition, Unit target, Unit source, Team sourceTeam, int turnNumber)
        {
            Definition = definition;
            Target = target;
            Reapply(source, sourceTeam, turnNumber);
        }

        /// <summary>처음 걸린 상태로 되돌린다(Refresh).</summary>
        internal void Reapply(Unit source, Team sourceTeam, int turnNumber)
        {
            Source = source;
            SourceTeam = sourceTeam;
            Remaining = Definition.Duration;
            AppliedTurnNumber = turnNumber;
        }

        /// <summary>지금 턴 단계가 timing 에 해당하는지.</summary>
        internal bool Matches(StatusTiming timing, TurnContext context)
        {
            if (timing == null || timing.Step != context.Step)
                return false;

            Team ownerTeam = timing.Owner == TimingOwner.TargetOwner ? Target.Team : SourceTeam;
            if (ownerTeam != context.ActiveTeam)
                return false;

            return timing.IncludeApplicationTurn || context.State.TurnNumber != AppliedTurnNumber;
        }
    }
}
