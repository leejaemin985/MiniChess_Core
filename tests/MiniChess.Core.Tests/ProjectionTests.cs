using System.Text.Json;
using System.Text.RegularExpressions;
using MiniChess.Core.Actions;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.Snapshots;
using MiniChess.Core.Statuses.Library;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 팀 시점 투영(숨김 규칙)과 스냅샷 복원.
    /// 숨김 규칙: 상대 덫은 밟을 때까지, 상대 포격 조준 칸은 착탄할 때까지 숨기고, 스킬을 썼다는 사실은 알린다.
    /// </summary>
    public class ProjectionTests
    {
        private static readonly string[] Lineup =
        {
            ChemistPiece.PieceId, MortarPiece.PieceId, WarriorPiece.PieceId, ArcherPiece.PieceId,
        };

        private static readonly Position TrapCell = new Position(1, 2);
        private static readonly Position StrikeTarget = new Position(1, 5);

        private static GameConfig Config()
        {
            return new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
                Seed = 7,
            };
        }

        private static GameController StartedGame()
        {
            GameController game = GameController.Create(Config());
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, Lineup.Select((id, x) => new UnitPlacement(id, new Position(x, 1))).ToList()));
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Lineup.Select((id, x) => new UnitPlacement(id, new Position(x, 5))).ToList()));
            return game;
        }

        private static int UnitAt(GameController game, Position position) => game.State.Board.GetCell(position).Occupant.Id;

        /// <summary>Player1 이 (1,2)에 덫을 놓고 (1,5)를 조준한 뒤의 경기와 두 명령의 이벤트.</summary>
        private static (GameController Game, IReadOnlyList<GameEvent> Trap, IReadOnlyList<GameEvent> Bombard) TrapAndBombard()
        {
            GameController game = StartedGame();

            var trap = game.UseSkill(new UseSkillRequest(Team.Player1, UnitAt(game, new Position(0, 1)), ChemistPiece.RootTrapId, TrapCell));
            Assert.True(trap.Success, trap.Reason.ToString());

            var bombard = game.UseSkill(new UseSkillRequest(Team.Player1, UnitAt(game, new Position(1, 1)), MortarPiece.BombardId, StrikeTarget));
            Assert.True(bombard.Success, bombard.Reason.ToString());

            return (game, trap.Events, bombard.Events);
        }

        #region Hidden from the opponent

        [Fact]
        public void EnemyTrap_IsHiddenFromOpponent_ButSkillUseIsVisible()
        {
            var (game, trapEvents, _) = TrapAndBombard();

            IReadOnlyList<GameEvent> mine = game.GetVisibleEvents(Team.Player1, trapEvents);
            IReadOnlyList<GameEvent> theirs = game.GetVisibleEvents(Team.Player2, trapEvents);

            Assert.Contains(mine, e => e is CellEffectPlacedEvent);
            Assert.Equal(TrapCell, Assert.IsType<SkillUsedEvent>(mine[0]).Targets.Single());

            Assert.DoesNotContain(theirs, e => e is CellEffectPlacedEvent);
            var used = Assert.IsType<SkillUsedEvent>(Assert.Single(theirs));
            Assert.Equal(ChemistPiece.RootTrapId, used.SkillId);
            Assert.Equal(mine[0].Sequence, used.Sequence);
            Assert.Empty(used.Targets);
            Assert.Empty(used.AffectedCells);

            Assert.Contains(game.GetSnapshot(Team.Player1).CellEffects, e => e.Kind == CellEffectKinds.RootTrap);
            Assert.DoesNotContain(game.GetSnapshot(Team.Player2).CellEffects, e => e.Kind == CellEffectKinds.RootTrap);
        }

        [Fact]
        public void EnemyStrike_IsHiddenFromOpponent_ButCasterStateIsVisible()
        {
            var (game, _, bombardEvents) = TrapAndBombard();

            IReadOnlyList<GameEvent> theirs = game.GetVisibleEvents(Team.Player2, bombardEvents);

            Assert.DoesNotContain(theirs, e => e is StrikeScheduledEvent);
            Assert.Empty(Assert.IsType<SkillUsedEvent>(theirs[0]).Targets);
            Assert.Contains(theirs, e => e is StatusAppliedEvent applied && applied.Status.StatusId == StatusLibrary.InstalledId);
            Assert.Contains(game.GetVisibleEvents(Team.Player1, bombardEvents), e => e is StrikeScheduledEvent);

            Assert.Single(game.GetSnapshot(Team.Player1).Strikes);
            Assert.Empty(game.GetSnapshot(Team.Player2).Strikes);
        }

        [Fact]
        public void EnemyTrap_IsRevealed_WhenSteppedOn()
        {
            var (game, _, _) = TrapAndBombard();
            game.EndTurn(new EndTurnRequest(Team.Player1));

            var move = game.Move(new MoveRequest(Team.Player2, UnitAt(game, new Position(1, 5)), TrapCell));
            Assert.True(move.Success, move.Reason.ToString());

            IReadOnlyList<GameEvent> theirs = game.GetVisibleEvents(Team.Player2, move.Events);
            var triggered = Assert.Single(theirs.OfType<CellEffectTriggeredEvent>());
            Assert.Equal(TrapCell, triggered.Position);
            Assert.Equal(Team.Player1, triggered.OwnerTeam);
            Assert.Contains(theirs, e => e is CellEffectRemovedEvent removed && removed.Reason == CellEffectRemoveReason.Consumed);
            Assert.Contains(theirs, e => e is StatusAppliedEvent applied && applied.Status.StatusId == StatusLibrary.RootId);
        }

        [Fact]
        public void EnemyStrike_IsRevealed_WhenItLands()
        {
            var (game, _, _) = TrapAndBombard();
            game.EndTurn(new EndTurnRequest(Team.Player1));

            var end = game.EndTurn(new EndTurnRequest(Team.Player2));

            var landed = Assert.Single(game.GetVisibleEvents(Team.Player2, end.Events).OfType<StrikeLandedEvent>());
            Assert.Contains(StrikeTarget, landed.Strike.Cells);
        }

        [Fact]
        public void OwnTrapExpiring_IsHiddenFromOpponent()
        {
            var removed = new CellEffectRemovedEvent(TrapCell, CellEffectLayer.Trap, CellEffectKinds.RootTrap, Team.Player1, CellEffectRemoveReason.Expired);
            GameController game = StartedGame();

            Assert.Empty(game.GetVisibleEvents(Team.Player2, new GameEvent[] { removed }));
            Assert.Single(game.GetVisibleEvents(Team.Player1, new GameEvent[] { removed }));
        }

        #endregion

        #region Restore

        private static readonly JsonSerializerOptions Json = new() { Converters = { new PositionConverter() } };

        private class PositionConverter : System.Text.Json.Serialization.JsonConverter<Position>
        {
            public override Position Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using JsonDocument document = JsonDocument.ParseValue(ref reader);
                return new Position(document.RootElement.GetProperty("X").GetInt32(), document.RootElement.GetProperty("Y").GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, Position value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("X", value.X);
                writer.WriteNumber("Y", value.Y);
                writer.WriteEndObject();
            }
        }

        /// <summary>복원한 경기에는 이벤트 기록이 없으므로 마지막 이벤트 순번은 비교하지 않는다.</summary>
        private static string Comparable(GameSnapshot snapshot)
        {
            return Regex.Replace(JsonSerializer.Serialize(snapshot, Json), "\"LastEventSequence\":-?\\d+", string.Empty);
        }

        /// <summary>서버가 보낸 것처럼 JSON 을 거친 스냅샷.</summary>
        private static GameSnapshot OverTheWire(GameSnapshot snapshot)
        {
            return JsonSerializer.Deserialize<GameSnapshot>(JsonSerializer.Serialize(snapshot, Json), Json);
        }

        /// <summary>덫 발동(속박), 설치(보호막), 포격 예약이 섞인 경기.</summary>
        private static GameController BusyGame()
        {
            var (game, _, _) = TrapAndBombard();
            game.EndTurn(new EndTurnRequest(Team.Player1));
            game.Move(new MoveRequest(Team.Player2, UnitAt(game, new Position(1, 5)), TrapCell));
            return game;
        }

        // 착탄 전에 시전자가 죽어도 포격은 남는데(사용자 확정), 스냅샷에는 살아 있는 유닛만 실린다. 시전자 없이 복원되어야 한다.
        [Fact]
        public void Restore_KeepsAStrikeWhoseCasterDiedBeforeItLanded()
        {
            var (game, _, _) = TrapAndBombard();
            var mortar = game.State.Board.GetCell(new Position(1, 1)).Occupant;
            Combat.DamageSystem.Apply(game.State, new Combat.DamageRequest(null, mortar, 100, Combat.DamageType.Direct));
            Assert.False(mortar.IsAlive);
            Assert.Single(game.State.ScheduledStrikes);

            GameSnapshot snapshot = game.GetSnapshot(Team.Player1);
            GameController restored = GameController.Restore(Config(), OverTheWire(snapshot));

            var strike = Assert.Single(restored.State.ScheduledStrikes);
            Assert.Null(strike.Source);
            Assert.Equal(mortar.Id, strike.SourceId);
            Assert.Equal(Team.Player1, strike.SourceTeam);
            Assert.Equal(Comparable(snapshot), Comparable(restored.GetSnapshot(Team.Player1)));
        }

        [Theory]
        [InlineData(Team.Player1)]
        [InlineData(Team.Player2)]
        public void Restore_FromSnapshot_ReproducesTheSameSnapshot(Team viewer)
        {
            GameController game = BusyGame();
            GameSnapshot snapshot = game.GetSnapshot(viewer);

            GameController restored = GameController.Restore(Config(), OverTheWire(snapshot));

            Assert.Equal(Comparable(snapshot), Comparable(restored.GetSnapshot(viewer)));
        }

        [Theory]
        [InlineData(Team.Player1)]
        [InlineData(Team.Player2)]
        public void Restore_AnswersQueriesLikeTheServer(Team viewer)
        {
            GameController game = BusyGame();
            GameController restored = GameController.Restore(Config(), OverTheWire(game.GetSnapshot(viewer)));

            foreach (int unitId in game.State.AllUnits().Where(unit => unit.IsOnBoard).Select(unit => unit.Id))
            {
                Assert.Equal(game.GetMoveDestinations(unitId), restored.GetMoveDestinations(unitId));
                Assert.Equal(game.GetAttackTargets(unitId).Select(unit => unit.Id), restored.GetAttackTargets(unitId).Select(unit => unit.Id));
                Assert.Equal(
                    game.GetSkills(unitId).Select(skill => (skill.Id, skill.Usable)),
                    restored.GetSkills(unitId).Select(skill => (skill.Id, skill.Usable)));
            }
        }

        [Fact]
        public void Restore_FromOpponentView_HasNoHiddenInformation()
        {
            var (game, _, _) = TrapAndBombard();

            GameController restored = GameController.Restore(Config(), OverTheWire(game.GetSnapshot(Team.Player2)));

            Assert.False(restored.State.Board.GetCell(TrapCell).HasEffect(CellEffectLayer.Trap));
            Assert.Empty(restored.State.ScheduledStrikes);
        }

        #endregion
    }
}
