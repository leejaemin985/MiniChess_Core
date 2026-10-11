using MiniChess.Core.Actions;
using MiniChess.Core.Characters;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.State;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>배치 단계(사용자 확정: 팀 순서 없음, 다시 배치 불가, 팀 안에서 같은 캐릭터 불가, 두 팀 배치 후 바로 Battle).</summary>
    public class PlacementTests
    {
        private static readonly string[] Lineup =
        {
            WarriorPiece.PieceId, ArcherPiece.PieceId, GardenerPiece.PieceId, ScythePiece.PieceId,
        };

        private static GameState NewSetupGame(List<CharacterDefinition> characters = null)
        {
            return new GameState(
                PrototypeTestPreset.CreateRules(),
                PrototypeTestPreset.CreateTestMap(),
                PrototypeTestPreset.CreateSkills(),
                new CharacterCatalog(characters ?? PrototypeTestPreset.CreateCharacters()));
        }

        /// <summary>테스트 맵 시작 위치: Player1 은 아래 두 줄(y 0~1), Player2 는 위 두 줄(y 5~6).</summary>
        private static List<UnitPlacement> Row(int y, params string[] ids)
        {
            return ids.Select((id, x) => new UnitPlacement(id, new Position(x, y))).ToList();
        }

        private static PlaceFailReason Check(GameState state, Team team, List<UnitPlacement> placements) =>
            new PlaceUnitsAction(team, placements).Validate(state);

        [Fact]
        public void NewGame_StartsInSetupWithoutUnits()
        {
            GameState state = NewSetupGame();

            Assert.Equal(GamePhase.Setup, state.Phase);
            Assert.Empty(state.AllUnits());
        }

        [Fact]
        public void BothTeamsPlaced_InAnyOrder_StartsBattleWithFirstTeam()
        {
            GameState state = NewSetupGame();

            PlaceUnitsResult second = new PlaceUnitsAction(Team.Player2, Row(6, Lineup)).Execute(state);
            Assert.False(second.BattleStarted);
            Assert.Equal(GamePhase.Setup, state.Phase);

            PlaceUnitsResult first = new PlaceUnitsAction(Team.Player1, Row(0, Lineup)).Execute(state);

            Assert.True(first.BattleStarted);
            Assert.Equal(GamePhase.Battle, state.Phase);
            Assert.Equal(Team.Player1, state.CurrentTeam);
            Assert.Equal(new Position(2, 0), first.Units[2].Position);
            Assert.Equal(GardenerPiece.PieceId, first.Units[2].Stats.Base.Id);
            Assert.Contains(first.Events, e => e is UnitsPlacedEvent placed && placed.Team == Team.Player1);
            Assert.Contains(first.Events, e => e is TurnStartedEvent);
        }

        [Fact]
        public void PlacedTeam_CannotPlaceAgain()
        {
            GameState state = NewSetupGame();
            new PlaceUnitsAction(Team.Player1, Row(0, Lineup)).Execute(state);

            Assert.Equal(PlaceFailReason.AlreadyPlaced, Check(state, Team.Player1, Row(1, Lineup)));
        }

        [Fact]
        public void AfterBattleStarts_PlacementIsRejected()
        {
            GameState state = NewSetupGame();
            new PlaceUnitsAction(Team.Player1, Row(0, Lineup)).Execute(state);
            new PlaceUnitsAction(Team.Player2, Row(6, Lineup)).Execute(state);

            Assert.Equal(PlaceFailReason.NotSetupPhase, Check(state, Team.Player1, Row(1, Lineup)));
        }

        [Fact]
        public void InvalidRequests_AreRejectedWithReason()
        {
            GameState state = NewSetupGame();

            Assert.Equal(PlaceFailReason.WrongUnitCount, Check(state, Team.Player1, Row(0, WarriorPiece.PieceId)));
            Assert.Equal(PlaceFailReason.UnknownCharacter, Check(state, Team.Player1, Row(0, "NOPE", ArcherPiece.PieceId, GardenerPiece.PieceId, ScythePiece.PieceId)));
            Assert.Equal(PlaceFailReason.DuplicateCharacter, Check(state, Team.Player1, Row(0, WarriorPiece.PieceId, WarriorPiece.PieceId, GardenerPiece.PieceId, ScythePiece.PieceId)));
            Assert.Equal(PlaceFailReason.NotSpawnCell, Check(state, Team.Player1, Row(2, Lineup)));  // 빈 칸
            Assert.Equal(PlaceFailReason.NotSpawnCell, Check(state, Team.Player1, Row(6, Lineup)));  // 상대 시작 위치

            List<UnitPlacement> samePosition = Row(0, Lineup);
            samePosition[1] = new UnitPlacement(ArcherPiece.PieceId, new Position(0, 0));
            Assert.Equal(PlaceFailReason.DuplicatePosition, Check(state, Team.Player1, samePosition));

            Assert.Empty(state.AllUnits());
        }

        [Fact]
        public void UnconfiguredCharacter_IsRejected()
        {
            GameState state = NewSetupGame(PieceModules.CreateDefinitions());

            Assert.Equal(PlaceFailReason.CharacterNotConfigured, Check(state, Team.Player1, Row(0, Lineup)));
        }

        [Fact]
        public void SameCharacter_IsAllowedAcrossTeams()
        {
            GameState state = NewSetupGame();
            new PlaceUnitsAction(Team.Player1, Row(0, Lineup)).Execute(state);

            Assert.Equal(PlaceFailReason.None, Check(state, Team.Player2, Row(6, Lineup)));
        }
    }
}
