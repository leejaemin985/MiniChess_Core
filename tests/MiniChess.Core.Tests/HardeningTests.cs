using System.Reflection;
using MiniChess.Core.Actions;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Snapshots;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Statuses.Library;
using MiniChess.Protocol;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 2차 검수 대응: 네트워크 입력 방어, 공용 JSON 형식, 배치 숨김, 무효 처리, 복원 경기 읽기 전용, 상태 정의 복원 일치.
    /// </summary>
    public class HardeningTests
    {
        private static readonly string[] Lineup =
        {
            ChemistPiece.PieceId, MortarPiece.PieceId, WarriorPiece.PieceId, ArcherPiece.PieceId,
        };

        private static GameController NewGame() => OnlineMatch.Create(seed: 3);

        private static List<UnitPlacement> Row(int y) => Lineup.Select((id, x) => new UnitPlacement(id, new Position(x, y))).ToList();

        private static GameController StartedGame()
        {
            GameController game = NewGame();
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, Row(1)));
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Row(5)));
            return game;
        }

        #region Malformed network input

        [Theory]
        [InlineData("not json")]
        [InlineData("{}")]
        [InlineData("{\"Type\":\"Move\"}")]
        [InlineData("{\"Type\":\"Nope\",\"Payload\":{}}")]
        [InlineData("{\"Type\":\"Move\",\"Payload\":{\"Destination\":{}}}")]
        [InlineData("{\"Type\":\"Move\",\"Payload\":{\"Destination\":{\"X\":\"a\",\"Y\":1}}}")]
        [InlineData("{\"Type\":\"Move\",\"Payload\":{\"UnitId\":99999999999}}")]
        [InlineData("{\"Type\":\"UseSkill\",\"Payload\":{\"Rotation\":7}}")]
        [InlineData("{\"Type\":\"UseSkill\",\"Payload\":{\"Rotation\":\"R7\"}}")]
        [InlineData("{\"Type\":\"UseSkill\",\"Payload\":{\"Targets\":[{}]}}")]
        [InlineData("{\"Type\":\"MatchStarted\",\"Payload\":{}}")]
        public void MalformedClientMessage_DecodesToNull_WithoutThrowing(string json)
        {
            Assert.Null(ProtocolJson.DecodeClient(json));
        }

        [Fact]
        public void UndefinedRotation_IsInvalidRequest()
        {
            GameController game = StartedGame();
            int chemist = game.State.Board.GetCell(new Position(0, 1)).Occupant.Id;

            var response = game.UseSkill(new UseSkillRequest(Team.Player1, chemist, ChemistPiece.RootTrapId, new Position(1, 2), (AreaRotation)7));

            Assert.Equal(SkillFailReason.InvalidRequest, response.Reason);
        }

        #endregion

        #region Shared JSON format

        [Fact]
        public void EveryEventAndTheSnapshot_RoundTripThroughProtocolJson()
        {
            // 덫/포격/속박/설치 보호막이 섞인 경기의 모든 이벤트와 스냅샷.
            GameController game = StartedGame();
            Unit chemist = game.State.Board.GetCell(new Position(0, 1)).Occupant;
            Unit mortar = game.State.Board.GetCell(new Position(1, 1)).Occupant;
            game.UseSkill(new UseSkillRequest(Team.Player1, chemist.Id, ChemistPiece.RootTrapId, new Position(1, 2)));
            game.UseSkill(new UseSkillRequest(Team.Player1, mortar.Id, MortarPiece.BombardId, new Position(1, 5)));
            game.EndTurn(new EndTurnRequest(Team.Player1));
            game.Move(new MoveRequest(Team.Player2, game.State.Board.GetCell(new Position(1, 5)).Occupant.Id, new Position(1, 2)));
            game.EndTurn(new EndTurnRequest(Team.Player2));

            var update = new StateUpdateMessage
            {
                Events = game.State.Events.All.Select(ProtocolJson.Wrap).ToList(),
                Snapshot = game.GetSnapshot(Team.Player1),
                TurnTimeLeftMs = 1000,
            };

            string json = ProtocolJson.Encode(update);
            var back = Assert.IsType<StateUpdateMessage>(ProtocolJson.DecodeServer(json));

            Assert.Equal(json, ProtocolJson.Encode(new StateUpdateMessage
            {
                Events = back.Events.Select(envelope => ProtocolJson.Wrap(WithSequence(ProtocolJson.ReadEvent(envelope), envelope.Sequence))).ToList(),
                Snapshot = back.Snapshot,
                TurnTimeLeftMs = back.TurnTimeLeftMs,
            }));
            Assert.DoesNotContain("\\u", json); // 한글이 \uXXXX 로 바뀌지 않는다.
        }

        private static GameEvent WithSequence(GameEvent gameEvent, int sequence)
        {
            typeof(GameEvent).GetProperty(nameof(GameEvent.Sequence))!.SetValue(gameEvent, sequence);
            return gameEvent;
        }

        [Fact]
        public void EnumsAreWrittenAsNames()
        {
            string json = ProtocolJson.Encode(new UseSkillMessage { SkillId = "X", Targets = new List<Position>(), Rotation = AreaRotation.R90 });

            Assert.Contains("\"Rotation\":\"R90\"", json);
        }

        [Fact]
        public void ConfigHash_IsStable_AndChangesWithBalance()
        {
            string hash = OnlineMatch.ConfigHash();

            Assert.Equal(hash, OnlineMatch.ConfigHash());

            var tuning = PrototypeTestPreset.CreateSkillTuning();
            string before = ProtocolJson.Fingerprint(tuning);
            tuning.Mortar.Bombard.Damage++;
            Assert.NotEqual(before, ProtocolJson.Fingerprint(tuning));
        }

        #endregion

        #region Placement hidden until battle

        [Fact]
        public void OpponentPlacement_IsHidden_UntilBothTeamsConfirm()
        {
            GameController game = NewGame();

            var first = game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, Row(1)));

            Assert.Empty(game.GetSnapshot(Team.Player2).Units);
            Assert.True(game.GetSnapshot(Team.Player2).Players.Single(p => p.Team == Team.Player1).HasPlaced);
            Assert.Equal(4, game.GetSnapshot(Team.Player1).Units.Count);
            var placed = Assert.IsType<UnitsPlacedEvent>(Assert.Single(game.GetVisibleEvents(Team.Player2, first.Events)));
            Assert.Equal(Team.Player1, placed.Team);
            Assert.Empty(placed.Units);
            Assert.Equal(4, Assert.IsType<UnitsPlacedEvent>(game.GetVisibleEvents(Team.Player1, first.Events)[0]).Units.Count);

            var second = game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Row(5)));

            Assert.Equal(8, game.GetSnapshot(Team.Player2).Units.Count);
            Assert.Equal(8, game.GetSnapshot(Team.Player1).Units.Count);
            Assert.Equal(4, game.GetVisibleEvents(Team.Player1, second.Events).OfType<UnitsPlacedEvent>().Single().Units.Count);
        }

        #endregion

        #region Abort and read-only restore

        [Fact]
        public void Abort_EndsWithoutWinner_AndOnlyOnce()
        {
            GameController game = StartedGame();

            var ended = Assert.IsType<GameEndedEvent>(Assert.Single(game.Abort()));

            Assert.Null(ended.Winner);
            Assert.Equal(GameEndReason.Aborted, ended.Reason);
            Assert.Equal(GameEndReason.Aborted, game.GetSnapshot(Team.Player1).EndReason);
            Assert.Empty(game.Abort());
        }

        [Fact]
        public void RestoredGame_RejectsCommands_ButAnswersQueries()
        {
            GameController game = StartedGame();
            GameController restored = OnlineMatch.Restore(OnlineMatch.CreateConfig(), game.GetSnapshot(Team.Player1));
            int unitId = game.State.GetPlayer(Team.Player1).Units[0].Id;

            Assert.True(restored.IsReadOnly);
            Assert.NotEmpty(restored.GetMoveDestinations(unitId));
            Assert.Throws<InvalidOperationException>(() => restored.EndTurn(new EndTurnRequest(Team.Player1)));
            Assert.Throws<InvalidOperationException>(() => restored.Abort());
            Assert.False(game.IsReadOnly);
        }

        #endregion

        #region Status definitions survive restore

        /// <summary>StatusLibrary 의 모든 공개 정의(테스트 값). 새 정의를 추가하면 여기에도 추가해야 아래 검사가 통과한다.</summary>
        private static readonly Dictionary<string, StatusDefinition> LibraryDefinitions = new()
        {
            [nameof(StatusLibrary.Root)] = StatusLibrary.Root(2, true),
            [nameof(StatusLibrary.Burn)] = StatusLibrary.Burn(3, 4),
            [nameof(StatusLibrary.Poison)] = StatusLibrary.Poison(5, 6),
            [nameof(StatusLibrary.DistanceLimit)] = StatusLibrary.DistanceLimit(2, 3),
            [nameof(StatusLibrary.Guard)] = StatusLibrary.Guard(2),
            [nameof(StatusLibrary.Installed)] = StatusLibrary.Installed(),
            [nameof(StatusLibrary.CurseMark)] = StatusLibrary.CurseMark(4),
            [nameof(StatusLibrary.Charged)] = StatusLibrary.Charged(5),
            [nameof(StatusLibrary.Sealed)] = StatusLibrary.Sealed(),
            [nameof(StatusLibrary.Stunned)] = StatusLibrary.Stunned(2),
        };

        [Fact]
        public void EveryLibraryFactory_IsCoveredByThisTest()
        {
            IEnumerable<string> factories = typeof(StatusLibrary).GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(method => method.ReturnType == typeof(StatusDefinition) && method.Name != nameof(StatusLibrary.Restore))
                .Select(method => method.Name);

            Assert.Equal(factories.OrderBy(name => name), LibraryDefinitions.Keys.OrderBy(name => name));
        }

        [Fact]
        public void EveryLibraryDefinition_RestoresToTheSameDefinition()
        {
            foreach ((string factory, StatusDefinition original) in LibraryDefinitions)
            {
                StatusDefinition restored = StatusLibrary.Restore(original.Id, original.Duration, original.Behavior?.Parameters);

                Assert.True(restored != null, factory);
                Assert.Equal(original.Behavior?.GetType(), restored.Behavior?.GetType());
                Assert.Equal(ProtocolJson.Fingerprint(original), ProtocolJson.Fingerprint(restored));
            }
        }

        /// <summary>생성자 인자가 있는 동작은 그 값을 Parameters 로 내보내야 복원할 수 있다.</summary>
        [Fact]
        public void BehaviorsWithConstructorArguments_OverrideParameters()
        {
            IEnumerable<Type> behaviors = typeof(StatusBehavior).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(StatusBehavior).IsAssignableFrom(type))
                .Where(type => type.GetConstructors().Any(constructor => constructor.GetParameters().Length > 0));

            foreach (Type behavior in behaviors)
            {
                PropertyInfo parameters = behavior.GetProperty(nameof(StatusBehavior.Parameters))!;
                Assert.True(parameters.DeclaringType == behavior, $"{behavior.Name} 이 Parameters 를 재정의하지 않음");
            }
        }

        #endregion
    }
}
