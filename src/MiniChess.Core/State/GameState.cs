using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Effects.Scheduled;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;

namespace MiniChess.Core.State
{
    /// <summary>한 경기의 전체 상태. 단계/턴 진행 등 동작은 별도 시스템이 담당한다.</summary>
    public class GameState
    {
        private readonly Dictionary<Team, PlayerState> _players;
        private readonly List<IDamageInterceptor> _damageInterceptors = new List<IDamageInterceptor>();
        private readonly List<ScheduledStrike> _scheduledStrikes = new List<ScheduledStrike>();
        private int _nextUnitId = 1;

        public GameRuleData Rules { get; }
        public Board Board { get; }

        /// <summary>이 경기에서 쓰는 스킬 정의.</summary>
        public SkillCatalog Skills { get; }

        /// <summary>배치할 때 고를 수 있는 캐릭터 정의.</summary>
        public CharacterCatalog Characters { get; }

        /// <summary>경기 중 발생한 상태 변화 기록.</summary>
        public GameEventLog Events { get; } = new GameEventLog();

        /// <summary>피해 적용 전에 끼어드는 규칙들(호위, 보호막 등). DamageSystem 이 Order 순으로 사용한다.</summary>
        public IReadOnlyList<IDamageInterceptor> DamageInterceptors => _damageInterceptors;

        /// <summary>조준되어 착탄을 기다리는 예약 포격(조준 순). 변경은 ScheduledStrikeSystem 을 통해서만 한다.</summary>
        public IReadOnlyList<ScheduledStrike> ScheduledStrikes => _scheduledStrikes;

        /// <summary>점령 목표 상태.</summary>
        public CaptureState Capture { get; }

        /// <summary>이 경기의 무작위 시드. 로그에 남겨두면 같은 경기를 재현할 수 있다.</summary>
        public int Seed { get; }

        /// <summary>규칙이 쓰는 무작위 값(점령 보상 선택 등). Seed 로 만들며, 테스트에서 고정값으로 바꿀 수 있다.</summary>
        public IRandomSource Random { get; internal set; }

        public GamePhase Phase { get; internal set; }

        /// <summary>현재 턴을 진행 중인 팀. Battle 단계에서만 의미가 있다.</summary>
        public Team CurrentTeam { get; internal set; }

        /// <summary>
        /// 경기 전체 턴 번호. Battle 시작 시 1 이 되며 한 플레이어의 턴이 끝날 때마다 1 증가.
        /// Battle 이전에는 0.
        /// </summary>
        public int TurnNumber { get; internal set; }

        /// <summary>승리한 팀. 경기가 끝나지 않았으면 null.</summary>
        public Team? Winner { get; internal set; }

        /// <summary>경기가 끝난 사유. 끝나지 않았으면 null.</summary>
        public GameEndReason? EndReason { get; internal set; }

        public bool IsGameOver => Phase == GamePhase.Ended;
        public PlayerState CurrentPlayer => GetPlayer(CurrentTeam);

        /// <summary>유닛 없이 Setup 단계로 만든다. 외부에서는 GameController.Create 를 쓴다.</summary>
        internal GameState(GameRuleData rules, MapData map, SkillCatalog skills = null, CharacterCatalog characters = null, int seed = 0)
        {
            Seed = seed;
            Random = new SystemRandomSource(seed);
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Board = new Board(map);
            Skills = skills ?? SkillCatalog.Empty;
            Characters = characters ?? CharacterCatalog.Empty;
            Capture = CreateCapture(Board, rules.Capture);

            _players = new Dictionary<Team, PlayerState>
            {
                { Team.Player1, CreatePlayer(Team.Player1, rules.Ap) },
                { Team.Player2, CreatePlayer(Team.Player2, rules.Ap) },
            };

            Phase = GamePhase.Setup;
            CurrentTeam = rules.Match.FirstTeam;
            TurnNumber = 0;
        }

        public PlayerState GetPlayer(Team team)
        {
            return _players[team];
        }

        /// <summary>양 팀의 모든 유닛(죽은 유닛, 보드 밖 유닛 포함). Player1 → Player2 순.</summary>
        public IEnumerable<Unit> AllUnits()
        {
            return GetPlayer(Team.Player1).Units.Concat(GetPlayer(Team.Player2).Units);
        }

        /// <summary>Id 로 유닛을 찾는다(죽은 유닛, 소환물 포함). 없으면 null.</summary>
        public Unit FindUnit(int unitId)
        {
            return AllUnits().FirstOrDefault(unit => unit.Id == unitId);
        }

        /// <summary>새 유닛을 만들어 해당 팀 플레이어에 추가한다. 보드 배치는 별도로 한다.</summary>
        internal Unit CreateUnit(Team team, IReadOnlyUnitBaseStats baseStats)
        {
            var unit = new Unit(_nextUnitId++, team, baseStats);
            GetPlayer(team).AddUnit(unit);
            return unit;
        }

        internal void AddDamageInterceptor(IDamageInterceptor interceptor)
        {
            if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));

            _damageInterceptors.Add(interceptor);
        }

        private int _nextStrikeId;
        private int _nextZoneId;

        internal void AddScheduledStrike(ScheduledStrike strike)
        {
            if (strike == null) throw new ArgumentNullException(nameof(strike));

            strike.Id = _nextStrikeId++;
            _scheduledStrikes.Add(strike);
        }

        /// <summary>새 구역(FieldZone)에 붙일 번호.</summary>
        internal int NextZoneId()
        {
            return _nextZoneId++;
        }

        /// <summary>스냅샷 복원용: Id 가 이미 정해진 유닛을 그대로 넣는다.</summary>
        internal void RestoreUnit(Unit unit)
        {
            GetPlayer(unit.Team).AddUnit(unit);
            _nextUnitId = Math.Max(_nextUnitId, unit.Id + 1);
        }

        /// <summary>스냅샷 복원용: Id 가 이미 정해진 예약 포격을 그대로 넣는다.</summary>
        internal void RestoreScheduledStrike(ScheduledStrike strike)
        {
            _scheduledStrikes.Add(strike);
            _nextStrikeId = Math.Max(_nextStrikeId, strike.Id + 1);
        }

        /// <summary>스냅샷 복원용: 다음 구역 번호가 이미 쓴 번호와 겹치지 않게 한다.</summary>
        internal void ReserveZoneId(int zoneId)
        {
            _nextZoneId = Math.Max(_nextZoneId, zoneId + 1);
        }

        internal bool RemoveScheduledStrike(ScheduledStrike strike)
        {
            return _scheduledStrikes.Remove(strike);
        }

        internal bool RemoveDamageInterceptor(IDamageInterceptor interceptor)
        {
            return _damageInterceptors.Remove(interceptor);
        }

        /// <summary>맵의 점령 칸(최대 1칸)으로 점령 상태를 만든다. 규칙이 없으면 비활성.</summary>
        private static CaptureState CreateCapture(Board board, CaptureRuleData rule)
        {
            List<Position> tiles = board.AllCells().Where(cell => cell.IsCaptureTile).Select(cell => cell.Position).ToList();
            if (tiles.Count > 1)
                throw new InvalidOperationException($"점령 칸은 맵에 최대 1칸: {string.Join(", ", tiles)}");

            Position? tile = tiles.Count == 1 ? tiles[0] : null;

            return rule == null
                ? new CaptureState(tile, 0, active: false)
                : new CaptureState(tile, rule.RequiredTurnStartCount, rule.ActiveFromStart);
        }

        /// <summary>게임 시작 시 AP 는 StartAp 만 적용한다. 회복은 각 플레이어의 다음 턴부터.</summary>
        private static PlayerState CreatePlayer(Team team, ApRuleData apRule)
        {
            var ap = new ApPool(apRule.MaxAp, apRule.StartAp);
            return new PlayerState(team, ap);
        }
    }
}
