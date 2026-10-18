using MiniChess.Core.Actions;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.Movement;
using MiniChess.Core.State;
using MiniChess.Core.Tests.Support;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class DamageAndEventTests
    {
        #region Damage pipeline

        [Fact]
        public void Attack_RecordsDamageEvent()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 3))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 10))
                .Start();

            AttackResult result = new AttackAction(u[0], u[1]).Execute(state);

            var damaged = Assert.Single(result.Events.OfType<UnitDamagedEvent>());
            Assert.Equal(u[0].Id, damaged.SourceId);
            Assert.Equal(u[1].Id, damaged.TargetId);
            Assert.Equal(DamageType.Direct, damaged.Type);
            Assert.Equal(3, damaged.RequestedAmount);
            Assert.Equal(3, damaged.AppliedAmount);
            Assert.Equal(7, damaged.HpAfter);
        }

        [Fact]
        public void Overkill_AppliedAmountIsRemainingHp()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 9))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 4))
                .Place(Team.Player2, 6, 6)
                .Start();

            AttackResult result = new AttackAction(u[0], u[1]).Execute(state);

            var damaged = Assert.Single(result.Events.OfType<UnitDamagedEvent>());
            Assert.Equal(9, damaged.RequestedAmount);
            Assert.Equal(4, damaged.AppliedAmount);
            Assert.Equal(4, result.Damage);
        }

        [Fact]
        public void Kill_RecordsDamageThenDeathThenGameEnd()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 5))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 5))
                .Start();

            AttackResult result = new AttackAction(u[0], u[1]).Execute(state);

            Assert.Collection(result.Events,
                e => Assert.IsType<UnitDamagedEvent>(e),
                e => Assert.Equal(new Position(0, 1), Assert.IsType<UnitDiedEvent>(e).Position),
                e => Assert.Equal(Team.Player1, Assert.IsType<GameEndedEvent>(e).Winner));
        }

        [Fact]
        public void Interceptors_RunInOrder_BeforeHpIsApplied()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 10))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Start();

            // 등록 순서와 무관하게 Order 순으로 적용: 절반(10→5) 후 -4(5→1).
            // 등록 순서대로 적용됐다면 (10-4)/2 = 3 이 되어 구분된다.
            state.AddDamageInterceptor(new FuncInterceptor(2, amount => amount - 4));
            state.AddDamageInterceptor(new FuncInterceptor(1, amount => amount / 2));

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(19, u[1].Stats.CurrentHp);
        }

        [Fact]
        public void Interceptor_CannotMakeDamageNegative()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 3))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 10))
                .Start();
            state.AddDamageInterceptor(new FuncInterceptor(0, amount => amount - 100));

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(10, u[1].Stats.CurrentHp);
        }

        [Fact]
        public void Damage_ToUnplacedUnit_IsIgnored()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 3))
                .Place(Team.Player2, 6, 6)
                .Start();
            DamageSystem.Apply(state, new DamageRequest(null, u[1], 5, DamageType.Direct));
            int eventCount = state.Events.Count;

            DamageOutcome outcome = DamageSystem.Apply(state, new DamageRequest(null, u[1], 5, DamageType.Direct));

            Assert.Equal(0, outcome.AppliedAmount);
            Assert.Equal(eventCount, state.Events.Count);
        }

        [Fact]
        public void Heal_IsClampedToMaxHp_AndRecorded()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0, TestGame.Stats(hp: 10)).Start();
            DamageSystem.Apply(state, new DamageRequest(null, u[0], 3, DamageType.Direct));

            int healed = DamageSystem.Heal(state, null, u[0], 5);

            Assert.Equal(3, healed);
            Assert.Equal(10, u[0].Stats.CurrentHp);
            Assert.Equal(3, state.Events.All.OfType<UnitHealedEvent>().Single().AppliedAmount);
        }

        #endregion

        #region Movement events

        [Fact]
        public void Move_RecordsOneEventPerCell()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Start();

            MovementResult result = new MoveAction(u[0], new Position(0, 3)).Execute(state);

            Assert.Equal(
                new[] { (0, 0, 0, 1), (0, 1, 0, 2), (0, 2, 0, 3) },
                result.Events.OfType<UnitMovedEvent>().Select(e => (e.From.X, e.From.Y, e.To.X, e.To.Y)));
        }

        [Fact]
        public void Move_IntoStoppingTrap_StopsAndChargesOnlyMovedCells()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Start();
            var trap = new TestCellEffect { StopOnEnter = true, Damage = 2 };
            CellEffectSystem.Place(state, new Position(0, 2), trap);

            MovementResult result = new MoveAction(u[0], new Position(0, 4)).Execute(state);

            Assert.Equal(new Position(0, 2), result.StoppedAt);
            Assert.True(result.WasInterrupted);
            Assert.Equal(2, state.GetPlayer(Team.Player1).Ap.Current); // 4 - 2칸
            Assert.Equal(1, trap.StopCount);
            Assert.Contains(result.Events, e => e is UnitDamagedEvent d && d.Type == DamageType.Area);
        }

        [Fact]
        public void Move_DyingOnTrap_ReportsDeathCellAsStoppedAt()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(hp: 2))
                .Place(Team.Player1, 6, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            CellEffectSystem.Place(state, new Position(0, 2), new TestCellEffect { Damage = 5 });

            MovementResult result = new MoveAction(u[0], new Position(0, 4)).Execute(state);

            Assert.False(u[0].IsAlive);
            Assert.Equal(new Position(0, 2), result.StoppedAt);
            Assert.Equal(new Position(0, 2), result.Events.OfType<UnitDiedEvent>().Single().Position);
        }

        #endregion

        #region Turn events

        [Fact]
        public void EndTurn_RecordsTurnEndedThenNextTurnStarted()
        {
            var (state, _) = new TestGame().Place(Team.Player1, 0, 0).Place(Team.Player2, 6, 6).Start();

            IReadOnlyList<GameEvent> events = new EndTurnAction(Team.Player1).Execute(state).Events;

            Assert.Collection(events,
                e => Assert.Equal((Team.Player1, 1), (((TurnEndedEvent)e).Team, ((TurnEndedEvent)e).TurnNumber)),
                e => Assert.Equal((Team.Player2, 2), (((TurnStartedEvent)e).Team, ((TurnStartedEvent)e).TurnNumber)));
        }

        [Fact]
        public void EventSequence_IsAssignedInOrder()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Place(Team.Player2, 6, 6).Start();
            new MoveAction(u[0], new Position(0, 2)).Execute(state);

            Assert.Equal(Enumerable.Range(0, state.Events.Count), state.Events.All.Select(e => e.Sequence));
        }

        #endregion

        private class FuncInterceptor : IDamageInterceptor
        {
            private readonly Func<int, int> _func;

            public FuncInterceptor(int order, Func<int, int> func)
            {
                Order = order;
                _func = func;
            }

            public int Order { get; }

            public int Intercept(GameState state, DamageRequest request, int currentAmount) => _func(currentAmount);
        }
    }
}
