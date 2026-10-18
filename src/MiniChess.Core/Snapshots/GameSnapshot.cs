using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Snapshots
{
    /// <summary>
    /// 경기 전체의 특정 시점 값(한 팀 시점으로 거른 것). 접속 직후 화면을 그리거나, 이벤트 재생 후 상태를 맞출 때 쓴다.
    /// GameController.GetSnapshot 으로 만든다.
    /// </summary>
    public class GameSnapshot
    {
        /// <summary>이 스냅샷을 본 팀.</summary>
        public Team Viewer { get; }

        public GamePhase Phase { get; }
        public Team CurrentTeam { get; }
        public int TurnNumber { get; }

        /// <summary>승리 팀. 진행 중이거나 무승부면 null.</summary>
        public Team? Winner { get; }

        /// <summary>이 스냅샷에 반영된 마지막 이벤트 순번. 이벤트가 없으면 -1.</summary>
        public int LastEventSequence { get; }

        public int BoardWidth { get; }
        public int BoardHeight { get; }
        public IReadOnlyList<CellSnapshot> Cells { get; }

        public IReadOnlyList<PlayerSnapshot> Players { get; }

        /// <summary>보드 위에 있거나 배치 전인 살아 있는 유닛. 죽은 유닛은 넣지 않는다.</summary>
        public IReadOnlyList<UnitSnapshot> Units { get; }

        public IReadOnlyList<CellEffectSnapshot> CellEffects { get; }
        public IReadOnlyList<StrikeSnapshot> Strikes { get; }
        public CaptureSnapshot Capture { get; }

        public GameSnapshot(
            Team viewer, GamePhase phase, Team currentTeam, int turnNumber, Team? winner, int lastEventSequence,
            int boardWidth, int boardHeight, IReadOnlyList<CellSnapshot> cells, IReadOnlyList<PlayerSnapshot> players,
            IReadOnlyList<UnitSnapshot> units, IReadOnlyList<CellEffectSnapshot> cellEffects, IReadOnlyList<StrikeSnapshot> strikes,
            CaptureSnapshot capture)
        {
            Viewer = viewer;
            Phase = phase;
            CurrentTeam = currentTeam;
            TurnNumber = turnNumber;
            Winner = winner;
            LastEventSequence = lastEventSequence;
            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            Cells = cells;
            Players = players;
            Units = units;
            CellEffects = cellEffects;
            Strikes = strikes;
            Capture = capture;
        }
    }

    public class PlayerSnapshot
    {
        public Team Team { get; }
        public int Ap { get; }
        public int MaxAp { get; }
        public bool HasPlaced { get; }

        /// <summary>이 팀이 배치할 수 있는 칸(시작 위치).</summary>
        public IReadOnlyList<Position> SpawnCells { get; }

        public PlayerSnapshot(Team team, int ap, int maxAp, bool hasPlaced, IReadOnlyList<Position> spawnCells)
        {
            SpawnCells = spawnCells;
            Team = team;
            Ap = ap;
            MaxAp = maxAp;
            HasPlaced = hasPlaced;
        }
    }

    public class CaptureSnapshot
    {
        /// <summary>점령 칸. 맵에 없으면 null.</summary>
        public Position? Tile { get; }
        public int Required { get; }
        public bool IsActive { get; }
        public Team? CapturedBy { get; }

        /// <summary>지급된 보상 Id. 아직 점령 전이거나 보상이 없으면 null.</summary>
        public string RewardId { get; }

        public int Player1Progress { get; }
        public int Player2Progress { get; }

        public CaptureSnapshot(Position? tile, int required, bool isActive, Team? capturedBy, string rewardId, int player1Progress, int player2Progress)
        {
            Tile = tile;
            Required = required;
            IsActive = isActive;
            CapturedBy = capturedBy;
            RewardId = rewardId;
            Player1Progress = player1Progress;
            Player2Progress = player2Progress;
        }
    }
}
