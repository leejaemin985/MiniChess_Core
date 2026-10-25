using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.Movement;
using MiniChess.Core.State;
using MiniChess.Core.Tests.Support;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class TurnStateAndMovementTests
    {
        [Fact]
        public void Attack_MarksBasicAttackUsed_AndMoveStaysAllowed()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Start();

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.True(u[0].TurnState.BasicAttackUsed);
            Assert.Equal(MoveFailReason.None, new MoveAction(u[0], new Position(1, 0)).Validate(state));
        }

        // 명세 14: "A 공격 후 B가 A를 워프" → 외부 이동은 허용, A의 기본 공격 기록은 회복되지 않음
        [Theory]
        [InlineData(MoveKind.Warp)]
        [InlineData(MoveKind.Knockback)]
        [InlineData(MoveKind.Pull)]
        public void ExternalMove_AfterAttack_IsAllowed_AndDoesNotRestoreBasicAttack(MoveKind kind)
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Place(Team.Player1, 3, 3)
                .Start();
            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(MoveBlockReason.None, MovementControl.Check(state, u[0], u[2], kind, 1));

            MovementResolver.Resolve(state, u[0], u[2], kind, new[] { new Position(1, 0) });

            Assert.Equal(new Position(1, 0), u[0].Position);
            Assert.True(u[0].TurnState.BasicAttackUsed);
            Assert.Equal(AttackFailReason.AlreadyUsedThisTurn, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void VoluntaryWarpOfSelf_AfterAttack_IsAllowed()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Start();
            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(MoveBlockReason.None, MovementControl.Check(state, u[0], u[0], MoveKind.Warp, 1));
        }

        [Fact]
        public void ActionsEnded_BlocksMoveAndAttack()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1)
                .Start();

            u[0].TurnState.EndActions();

            Assert.Equal(MoveFailReason.ActionsEnded, new MoveAction(u[0], new Position(1, 0)).Validate(state));
            Assert.Equal(AttackFailReason.ActionsEnded, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void ActionsEnded_DoesNotBlockExternalMove()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 5, 5)
                .Start();
            u[0].TurnState.EndActions();

            Assert.Equal(MoveBlockReason.None, MovementControl.Check(state, u[0], u[1], MoveKind.Knockback, 1));
        }

        [Fact]
        public void VoluntaryCellsMoved_AccumulatesAcrossCommands_AndResetsOnOwnTurn()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            new MoveAction(u[0], new Position(0, 1)).Execute(state);
            new MoveAction(u[0], new Position(0, 3)).Execute(state);
            Assert.Equal(3, u[0].TurnState.VoluntaryCellsMoved);

            new EndTurnAction(Team.Player1).Execute(state);
            Assert.Equal(3, u[0].TurnState.VoluntaryCellsMoved); // 상대 턴 시작에는 초기화되지 않음

            new EndTurnAction(Team.Player2).Execute(state);
            Assert.Equal(0, u[0].TurnState.VoluntaryCellsMoved);
        }

        [Fact]
        public void ExternalMove_DoesNotCountAsVoluntaryCells()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            MovementResolver.Resolve(state, u[0], u[1], MoveKind.Knockback, new[] { new Position(0, 1), new Position(0, 2) });

            Assert.Equal(0, u[0].TurnState.VoluntaryCellsMoved);
        }

        [Fact]
        public void MoveEvent_CarriesKindAndInitiator()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            MovementResult self = new MoveAction(u[0], new Position(0, 1)).Execute(state);
            int start = state.Events.Count;
            MovementResolver.Resolve(state, u[0], u[1], MoveKind.Knockback, new[] { new Position(0, 2) });

            UnitMovedEvent selfMove = self.Events.OfType<UnitMovedEvent>().Single();
            UnitMovedEvent pushed = state.Events.Since(start).OfType<UnitMovedEvent>().Single();
            Assert.Equal((MoveKind.Path, true), (selfMove.Kind, selfMove.IsVoluntary));
            Assert.Equal((MoveKind.Knockback, false), (pushed.Kind, pushed.IsVoluntary));
        }

        [Fact]
        public void IgnoreStopRequests_TriggersEffectButContinues()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            var trap = new TestCellEffect { StopOnEnter = true, Damage = 1 };
            CellEffectSystem.Place(state, new Position(0, 1), trap);

            MovementResult result = MovementResolver.Resolve(
                state, u[0], u[0], MoveKind.Dash, new[] { new Position(0, 1), new Position(0, 2) }, ignoreStopRequests: true);

            Assert.Equal(new Position(0, 2), result.StoppedAt);
            Assert.False(result.WasInterrupted);
            Assert.Equal(1, trap.EnterCount);
            Assert.Equal(9, u[0].Stats.CurrentHp);
        }
    }
}
