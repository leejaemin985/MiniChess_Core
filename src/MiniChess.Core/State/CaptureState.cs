using System.Collections.Generic;
using MiniChess.Core.Capture;
using MiniChess.Core.Common;

namespace MiniChess.Core.State
{
    /// <summary>
    /// 경기의 점령 목표 상태. 점령 칸은 맵에 최대 1칸이다.
    /// 변경은 CaptureSystem 을 통해서만 한다.
    /// </summary>
    public class CaptureState
    {
        private readonly Dictionary<Team, int> _progress = new Dictionary<Team, int>
        {
            { Team.Player1, 0 },
            { Team.Player2, 0 },
        };

        /// <summary>점령 칸 위치. 맵에 점령 칸이 없으면 null.</summary>
        public Position? Tile { get; }

        /// <summary>점령 완료에 필요한 연속 판정 횟수.</summary>
        public int Required { get; }

        /// <summary>지금 점령을 진행할 수 있는지. 점령이 완료되면 false 가 되고 다시 켜지지 않는다.</summary>
        public bool IsActive { get; internal set; }

        /// <summary>점령한 팀. 아직 점령되지 않았으면 null.</summary>
        public Team? CapturedBy { get; internal set; }

        /// <summary>점령으로 받은 보상. 점령 전이거나 보상 풀이 비어 있었으면 null.</summary>
        public CaptureReward Reward { get; internal set; }

        public CaptureState(Position? tile, int required, bool active)
        {
            Tile = tile;
            Required = required;
            IsActive = tile.HasValue && active;
        }

        /// <summary>팀의 연속 점령 판정 횟수.</summary>
        public int GetProgress(Team team)
        {
            return _progress[team];
        }

        internal void SetProgress(Team team, int value)
        {
            _progress[team] = value;
        }

        /// <summary>경기 복사(GameState.Fork)용.</summary>
        internal CaptureState Fork()
        {
            var copy = new CaptureState(Tile, Required, active: false)
            {
                IsActive = IsActive,
                CapturedBy = CapturedBy,
                Reward = Reward,
            };
            foreach (KeyValuePair<Team, int> progress in _progress)
                copy._progress[progress.Key] = progress.Value;
            return copy;
        }
    }
}
