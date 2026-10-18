using MiniChess.Core.Common;

namespace MiniChess.Core.Events
{
    /// <summary>팀의 점령 진행도가 바뀌었다(증가 또는 초기화).</summary>
    public class CaptureProgressChangedEvent : GameEvent
    {
        public Team Team { get; }
        public int Progress { get; }
        public int Required { get; }

        public CaptureProgressChangedEvent(Team team, int progress, int required)
        {
            Team = team;
            Progress = progress;
            Required = required;
        }
    }

    /// <summary>점령이 완료되었다. 점령 칸은 소멸하고 이 경기에서 더 이상 점령은 없다.</summary>
    public class CaptureCompletedEvent : GameEvent
    {
        public Team Team { get; }

        /// <summary>지급된 보상 Id(CaptureReward.Id). 보상 풀이 비어 있으면 null.</summary>
        public string RewardId { get; }

        public CaptureCompletedEvent(Team team, string rewardId)
        {
            Team = team;
            RewardId = rewardId;
        }
    }
}
