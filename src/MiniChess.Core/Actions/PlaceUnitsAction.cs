using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Turns;
using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Actions
{
    /// <summary>배치 한 건: 어떤 캐릭터를 어느 칸에 둘지.</summary>
    public readonly struct UnitPlacement
    {
        public string CharacterId { get; }
        public Position Position { get; }

        public UnitPlacement(string characterId, Position position)
        {
            CharacterId = characterId;
            Position = position;
        }
    }

    /// <summary>
    /// 한 팀의 배치를 한 번에 확정하는 명령(사용자 확정: 팀 순서 없음, 다시 배치 불가, 팀 안에서 같은 캐릭터 불가).
    /// 두 팀이 모두 배치하면 바로 Battle 을 시작한다.
    /// </summary>
    internal class PlaceUnitsAction
    {
        public Team Team { get; }
        public IReadOnlyList<UnitPlacement> Placements { get; }

        public PlaceUnitsAction(Team team, IReadOnlyList<UnitPlacement> placements)
        {
            Team = team;
            Placements = placements ?? throw new ArgumentNullException(nameof(placements));
        }

        public PlaceFailReason Validate(GameState state)
        {
            if (state.Phase != GamePhase.Setup) return PlaceFailReason.NotSetupPhase;
            if (state.GetPlayer(Team).HasPlaced) return PlaceFailReason.AlreadyPlaced;
            if (Placements.Count != state.Rules.Match.UnitsPerTeam) return PlaceFailReason.WrongUnitCount;

            foreach (UnitPlacement placement in Placements)
            {
                CharacterDefinition character = state.Characters.Find(placement.CharacterId);
                if (character == null) return PlaceFailReason.UnknownCharacter;
                if (character.GetMissingFields().Count > 0) return PlaceFailReason.CharacterNotConfigured;
            }

            if (Placements.Select(p => p.CharacterId).Distinct().Count() != Placements.Count)
                return PlaceFailReason.DuplicateCharacter;

            IReadOnlyList<Position> spawnCells = state.Board.GetSpawnPositions(Team);
            if (Placements.Any(p => !spawnCells.Contains(p.Position)))
                return PlaceFailReason.NotSpawnCell;
            if (Placements.Select(p => p.Position).Distinct().Count() != Placements.Count)
                return PlaceFailReason.DuplicatePosition;

            return PlaceFailReason.None;
        }

        /// <summary>유닛을 만들어 놓고, 상대도 배치를 마쳤으면 Battle 을 시작한다.</summary>
        public PlaceUnitsResult Execute(GameState state)
        {
            PlaceFailReason reason = Validate(state);
            if (reason != PlaceFailReason.None)
                throw new InvalidOperationException($"배치 불가({Team}): {reason}");

            int eventStart = state.Events.Count;

            var units = new List<Unit>();
            foreach (UnitPlacement placement in Placements)
            {
                Unit unit = state.CreateUnit(Team, state.Characters.Find(placement.CharacterId).ToBaseStats());
                state.Board.TrySpawn(unit, placement.Position);
                units.Add(unit);
            }

            state.GetPlayer(Team).HasPlaced = true;
            state.Events.Record(new UnitsPlacedEvent(Team, units.Select(SnapshotBuilder.Unit).ToList()));

            bool battleStarted = state.GetPlayer(TurnSystem.GetOpponent(Team)).HasPlaced;
            if (battleStarted)
                TurnSystem.StartBattle(state);

            return new PlaceUnitsResult(Team, units, battleStarted, state.Events.Since(eventStart));
        }
    }

    public class PlaceUnitsResult
    {
        public Team Team { get; }

        /// <summary>배치된 유닛(요청 순).</summary>
        public IReadOnlyList<Unit> Units { get; }

        /// <summary>이 배치로 두 팀 배치가 끝나 Battle 이 시작되었는지.</summary>
        public bool BattleStarted { get; }

        /// <summary>이 배치로 발생한 이벤트(Battle 시작 시 첫 턴 시작 처리 포함).</summary>
        public IReadOnlyList<GameEvent> Events { get; }

        public PlaceUnitsResult(Team team, IReadOnlyList<Unit> units, bool battleStarted, IReadOnlyList<GameEvent> events)
        {
            Team = team;
            Units = units;
            BattleStarted = battleStarted;
            Events = events;
        }
    }
}
