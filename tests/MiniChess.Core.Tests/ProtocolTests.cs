using MiniChess.Core.Actions;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Protocol;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>프로토콜: 명령 메시지 실행(팀은 서버가 정함, 오래된 턴 거절)과 메시지/이벤트 이름 등록.</summary>
    public class ProtocolTests
    {
        private static GameController NewGame()
        {
            return GameController.Create(new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
                Seed = 1,
            });
        }

        private static PlaceUnitsMessage Place(int y)
        {
            string[] lineup = { WarriorPiece.PieceId, ArcherPiece.PieceId, GardenerPiece.PieceId, ScythePiece.PieceId };
            return new PlaceUnitsMessage
            {
                Placements = lineup.Select((id, x) => new PlacementEntry { CharacterId = id, Position = new Position(x, y) }).ToList(),
            };
        }

        [Fact]
        public void Execute_UsesTeamGivenByCaller()
        {
            GameController game = NewGame();
            CommandExecutor.Execute(game, Team.Player1, Place(1));
            CommandExecutor.Execute(game, Team.Player2, Place(5));
            int player2Unit = game.State.GetPlayer(Team.Player2).Units[0].Id;

            // Player1 연결이 상대 유닛을 움직이려 하면 거절된다.
            CommandOutcome outcome = CommandExecutor.Execute(game, Team.Player1,
                new MoveMessage { UnitId = player2Unit, Destination = new Position(0, 4), TurnNumber = game.State.TurnNumber });

            Assert.False(outcome.Success);
            Assert.Equal(nameof(MoveFailReason.NotYourUnit), outcome.Reason);
            Assert.Empty(outcome.Events);
        }

        [Fact]
        public void Execute_RejectsStaleTurn_WithoutChangingState()
        {
            GameController game = NewGame();
            CommandExecutor.Execute(game, Team.Player1, Place(1));
            CommandExecutor.Execute(game, Team.Player2, Place(5));
            int eventsBefore = game.State.Events.Count;

            CommandOutcome outcome = CommandExecutor.Execute(game, game.State.CurrentTeam,
                new EndTurnMessage { TurnNumber = game.State.TurnNumber - 1 });

            Assert.Equal(CommandOutcome.StaleTurn, outcome.Reason);
            Assert.Equal(eventsBefore, game.State.Events.Count);
        }

        [Fact]
        public void Execute_MalformedMessage_IsInvalidRequest()
        {
            GameController game = NewGame();

            Assert.Equal(CommandOutcome.InvalidRequest, CommandExecutor.Execute(game, Team.Player1, null).Reason);
            Assert.Equal(CommandOutcome.InvalidRequest, CommandExecutor.Execute(game, Team.Player1, new PlaceUnitsMessage()).Reason);
            Assert.Equal(CommandOutcome.InvalidRequest, CommandExecutor.Execute(game, Team.Player1, new UseSkillMessage { SkillId = "X" }).Reason);
        }

        [Fact]
        public void Execute_Success_ReturnsEventsOfThatCommand()
        {
            GameController game = NewGame();

            CommandOutcome outcome = CommandExecutor.Execute(game, Team.Player1, Place(1));

            Assert.True(outcome.Success);
            Assert.IsType<UnitsPlacedEvent>(Assert.Single(outcome.Events));
        }

        [Fact]
        public void TypeRegistries_CoverEveryEventAndMessage()
        {
            Assert.Equal(typeof(UnitMovedEvent), EventTypes.Find(nameof(UnitMovedEvent)));
            Assert.Contains(typeof(GameEndedEvent), EventTypes.All);
            Assert.Null(EventTypes.Find("Nope"));

            Assert.Equal(typeof(MoveMessage), MessageTypes.Find("Move"));
            Assert.Equal(typeof(MatchStartedMessage), MessageTypes.Find("MatchStarted"));
            Assert.Equal(MessageTypes.Client.Count + MessageTypes.Server.Count,
                MessageTypes.Client.Concat(MessageTypes.Server).Select(MessageTypes.NameOf).Distinct().Count());
        }
    }
}
