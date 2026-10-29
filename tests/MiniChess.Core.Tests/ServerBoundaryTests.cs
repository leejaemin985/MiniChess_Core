using System.Text.Json;
using System.Text.RegularExpressions;
using MiniChess.Core.Actions;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.Snapshots;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 서버 경계: 잘못된 요청 거절, 이벤트/스냅샷이 값이라 직렬화되고 나중에 바뀌지 않음, 같은 시드+같은 명령이면 같은 결과.
    /// </summary>
    public class ServerBoundaryTests
    {
        // Position 은 읽기 전용 구조체라 변환기가 필요하다(서버/클라도 각자 직렬화 설정에 같은 변환기를 둔다).
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

        private static readonly string[] Lineup =
        {
            WarriorPiece.PieceId, ArcherPiece.PieceId, GardenerPiece.PieceId, ScythePiece.PieceId,
        };

        private static GameController NewGame(int seed = 7)
        {
            return GameController.Create(new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
                Seed = seed,
            });
        }

        private static List<UnitPlacement> Row(int y)
        {
            return Lineup.Select((id, x) => new UnitPlacement(id, new Position(x, y))).ToList();
        }

        private static GameController StartedGame(int seed = 7)
        {
            GameController game = NewGame(seed);
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, Row(1)));
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Row(5)));
            return game;
        }

        #region Malformed requests

        [Fact]
        public void NullOrUndefinedTeamRequests_FailWithInvalidRequest_WithoutChangingState()
        {
            GameController game = StartedGame();
            int unitId = game.State.GetPlayer(Team.Player1).Units[0].Id;
            int eventsBefore = game.State.Events.Count;
            const Team bogus = (Team)5;

            Assert.Equal(PlaceFailReason.InvalidRequest, game.PlaceUnits(null).Reason);
            Assert.Equal(PlaceFailReason.InvalidRequest, game.PlaceUnits(new PlaceUnitsRequest(bogus, Row(1))).Reason);
            Assert.Equal(MoveFailReason.InvalidRequest, game.Move(null).Reason);
            Assert.Equal(MoveFailReason.InvalidRequest, game.Move(new MoveRequest(bogus, unitId, new Position(0, 2))).Reason);
            Assert.Equal(AttackFailReason.InvalidRequest, game.Attack(new AttackRequest(bogus, unitId, unitId)).Reason);
            Assert.Equal(AttackFailReason.InvalidRequest, game.AttackObstacle(new AttackObstacleRequest(bogus, unitId, new Position(0, 2))).Reason);
            Assert.Equal(SkillFailReason.InvalidRequest, game.UseSkill(new UseSkillRequest(bogus, unitId, "X", new Position(0, 2))).Reason);
            Assert.Equal(EndTurnFailReason.InvalidRequest, game.EndTurn(null).Reason);
            Assert.Equal(EndTurnFailReason.InvalidRequest, game.EndTurn(new EndTurnRequest(bogus)).Reason);

            Assert.Equal(eventsBefore, game.State.Events.Count);
        }

        // 이동이 막혔는지는 None 여부로 정하고 사유는 따로 옮긴다. 새 차단 이유를 추가하면 이 테스트가 사유 연결을 요구한다.
        [Fact]
        public void EveryMoveBlockReason_MapsToItsOwnFailReason_AndUnknownStaysBlocked()
        {
            Assert.Equal(MoveFailReason.None, MoveAction.ToFailReason(Movement.MoveBlockReason.None));
            foreach (Movement.MoveBlockReason block in Enum.GetValues(typeof(Movement.MoveBlockReason)))
            {
                if (block == Movement.MoveBlockReason.None)
                    continue;

                MoveFailReason reason = MoveAction.ToFailReason(block);
                Assert.True(reason != MoveFailReason.None && reason != MoveFailReason.Unknown, $"{block} 의 실패 사유가 없음");
            }

            Assert.Equal(MoveFailReason.Unknown, MoveAction.ToFailReason((Movement.MoveBlockReason)99));
        }

        public static IEnumerable<object[]> OffBoardPositions()
        {
            yield return new object[] { 0, int.MaxValue };
            yield return new object[] { 0, int.MinValue };
            yield return new object[] { int.MaxValue, 1 };
            yield return new object[] { int.MinValue, 1 };
            yield return new object[] { -1, 1 };
            yield return new object[] { 0, -1 };
            yield return new object[] { 0, 2_000_000_000 };
            yield return new object[] { int.MaxValue, int.MinValue };
        }

        // 서버는 코어 예외를 경기 무효로 처리하므로, 클라이언트가 보낸 어떤 좌표로도 명령이 예외를 던지면 안 된다(실패로 돌려준다).
        [Theory]
        [MemberData(nameof(OffBoardPositions))]
        public void OffBoardPositions_FailWithoutThrowing_AndWithoutChangingState(int x, int y)
        {
            var position = new Position(x, y);

            GameController setup = NewGame();
            Assert.Equal(PlaceFailReason.NotSpawnCell, setup.PlaceUnits(new PlaceUnitsRequest(Team.Player1,
                Lineup.Select((id, i) => new UnitPlacement(id, i == 0 ? position : new Position(i, 1))).ToList())).Reason);

            GameController game = StartedGame();
            int eventsBefore = game.State.Events.Count;
            Unit unit = game.State.GetPlayer(Team.Player1).Units[0];

            Assert.Equal(MoveFailReason.PathBlocked, game.Move(new MoveRequest(Team.Player1, unit.Id, position)).Reason);
            Assert.Equal(AttackFailReason.InvalidTarget, game.AttackObstacle(new AttackObstacleRequest(Team.Player1, unit.Id, position)).Reason);
            foreach (string skillId in unit.Stats.Base.SkillIds)
                Assert.False(game.UseSkill(new UseSkillRequest(Team.Player1, unit.Id, skillId, position)).Success);

            Assert.Empty(game.State.Board.GetStraightPath(unit.Position.Value, position));
            Assert.Equal(eventsBefore, game.State.Events.Count);
        }

        #endregion

        #region Disconnect

        [Fact]
        public void EndByDisconnect_OpponentWins_AndOnlyOnce()
        {
            GameController game = StartedGame();

            IReadOnlyList<GameEvent> events = game.EndByDisconnect(Team.Player1);
            IReadOnlyList<GameEvent> again = game.EndByDisconnect(Team.Player2);

            var ended = Assert.IsType<GameEndedEvent>(Assert.Single(events));
            Assert.Equal(Team.Player2, ended.Winner);
            Assert.Equal(GameEndReason.Disconnect, ended.Reason);
            Assert.True(game.State.IsGameOver);
            Assert.Empty(again);
        }

        [Fact]
        public void EndByDisconnect_DuringSetup_EndsWithoutAWinner()
        {
            GameController game = NewGame();

            IReadOnlyList<GameEvent> events = game.EndByDisconnect(Team.Player2);

            var ended = Assert.IsType<GameEndedEvent>(Assert.Single(events));
            Assert.Null(ended.Winner);
            Assert.Equal(GameEndReason.Disconnect, ended.Reason);
            Assert.True(game.State.IsGameOver);
        }

        #endregion

        #region Value events

        // 상태를 다시 걸어도(갱신) 앞서 기록된 이벤트의 값은 그대로다(리플레이/관전 전제)
        [Fact]
        public void RecordedStatusEvent_DoesNotChange_WhenStatusIsRefreshedLater()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            var timing = new StatusTiming(TurnStep.EndDurationTick, TimingOwner.TargetOwner, includeApplicationTurn: true);
            var definition = new StatusDefinition("test", 3, StatusStackPolicy.Refresh, timing);

            StatusSystem.Apply(state, definition, u[2], u[0]);
            new EndTurnAction(Team.Player1).Execute(state);
            new EndTurnAction(Team.Player2).Execute(state); // 남은 횟수 감소
            StatusSystem.Apply(state, definition, u[2], u[1]); // 다른 유닛이 갱신

            var applied = state.Events.All.OfType<StatusAppliedEvent>().ToList();
            Assert.Equal(u[0].Id, applied[0].Status.SourceId);
            Assert.Equal(3, applied[0].Status.Remaining);
            Assert.Equal(u[1].Id, applied[1].Status.SourceId);
        }

        [Fact]
        public void AllEventsAndSnapshot_RoundTripThroughJson()
        {
            GameController game = PlayScripted(seed: 11, maxTurns: 12);
            Assert.NotEmpty(game.State.Events.All);

            foreach (GameEvent gameEvent in game.State.Events.All)
            {
                string json = JsonSerializer.Serialize(gameEvent, gameEvent.GetType(), Json);
                object back = JsonSerializer.Deserialize(json, gameEvent.GetType(), Json);
                string again = JsonSerializer.Serialize(back, gameEvent.GetType(), Json);

                // 순번은 로그가 정하므로(전송은 메시지 봉투가 담당) 비교에서 뺀다.
                Assert.Equal(WithoutSequence(json), WithoutSequence(again));
            }

            GameSnapshot snapshot = game.GetSnapshot(Team.Player1);
            string snapshotJson = JsonSerializer.Serialize(snapshot, Json);
            string snapshotAgain = JsonSerializer.Serialize(JsonSerializer.Deserialize<GameSnapshot>(snapshotJson, Json), Json);
            Assert.Equal(snapshotJson, snapshotAgain);
        }

        private static string WithoutSequence(string json) => Regex.Replace(json, "\"Sequence\":-?\\d+,?", string.Empty);

        #endregion

        #region Snapshot

        [Fact]
        public void Snapshot_MatchesLiveState()
        {
            GameController game = PlayScripted(seed: 3, maxTurns: 6);
            GameState state = game.State;

            GameSnapshot snapshot = game.GetSnapshot(Team.Player2);

            Assert.Equal(Team.Player2, snapshot.Viewer);
            Assert.Equal(state.TurnNumber, snapshot.TurnNumber);
            Assert.Equal(state.CurrentTeam, snapshot.CurrentTeam);
            Assert.Equal(state.Events.Count - 1, snapshot.LastEventSequence);
            Assert.Equal(state.Board.Width * state.Board.Height, snapshot.Cells.Count);
            foreach (Unit unit in state.AllUnits().Where(unit => unit.IsAlive))
            {
                UnitSnapshot copy = snapshot.Units.Single(u => u.Id == unit.Id);
                Assert.Equal(unit.Position, copy.Position);
                Assert.Equal(unit.Stats.CurrentHp, copy.Hp);
                Assert.Equal(unit.Statuses.Count, copy.Statuses.Count);
            }
        }

        #endregion

        #region Replay

        // 같은 시드 + 같은 명령 순서 → 이벤트 로그 전체가 같다(크래시 복구/재현의 전제)
        [Fact]
        public void SameSeedAndCommands_ProduceIdenticalEventLog()
        {
            string first = Serialize(PlayScripted(seed: 42, maxTurns: 20).State.Events.All);
            string second = Serialize(PlayScripted(seed: 42, maxTurns: 20).State.Events.All);

            Assert.Equal(first, second);
        }

        private static string Serialize(IEnumerable<GameEvent> events)
        {
            return string.Join("\n", events.Select(e => e.GetType().Name + JsonSerializer.Serialize(e, e.GetType(), Json)));
        }

        /// <summary>
        /// 컨트롤러 조회만으로 정해지는 단순한 진행: 유닛마다 쓸 수 있는 첫 스킬 → 첫 공격 대상 → 첫 이동 칸, 그다음 턴 종료.
        /// </summary>
        private static GameController PlayScripted(int seed, int maxTurns)
        {
            GameController game = StartedGame(seed);

            for (int turn = 0; turn < maxTurns && !game.State.IsGameOver; turn++)
            {
                Team team = game.State.CurrentTeam;
                foreach (int unitId in game.State.GetPlayer(team).AliveUnits.Select(u => u.Id).ToList())
                {
                    if (game.State.IsGameOver)
                        break;

                    TryFirstSkill(game, team, unitId);
                    List<Unit> targets = game.GetAttackTargets(unitId);
                    if (targets.Count > 0)
                        game.Attack(new AttackRequest(team, unitId, targets[0].Id));

                    List<Position> moves = game.GetMoveDestinations(unitId);
                    if (moves.Count > 0)
                        game.Move(new MoveRequest(team, unitId, moves[0]));
                }

                game.EndTurn(new EndTurnRequest(team));
            }

            return game;
        }

        private static void TryFirstSkill(GameController game, Team team, int unitId)
        {
            foreach (SkillInfo skill in game.GetSkills(unitId).Where(s => s.Usable == SkillFailReason.None))
            {
                var chosen = new List<Position>();
                for (int i = 0; i < skill.Definition.Targeting.TargetCount; i++)
                {
                    List<Position> candidates = game.GetSkillTargets(unitId, skill.Id, chosen);
                    if (candidates.Count == 0)
                        break;
                    chosen.Add(candidates[0]);
                }

                if (chosen.Count == skill.Definition.Targeting.TargetCount &&
                    game.UseSkill(new UseSkillRequest(team, unitId, skill.Id, chosen)).Success)
                    return;
            }
        }

        #endregion
    }
}
