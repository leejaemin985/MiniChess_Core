using System;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Statuses.Library;
using MiniChess.Core.Summons;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using MiniChess.Protocol;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class FatigueTests
    {
        private static TestGame Game(int hp1 = 100, int hp2 = 100) => new TestGame()
            .WithRules(rules =>
            {
                rules.Fatigue = new FatigueRuleData();
                rules.Match.SummonElimination = SummonEliminationPolicy.ExcludeSummons;
            })
            .Place(Team.Player1, 0, 0, TestGame.Stats(hp: hp1))
            .Place(Team.Player2, 6, 6, TestGame.Stats(hp: hp2));

        private static EndTurnResult End(GameState state) => new EndTurnAction(state.CurrentTeam).Execute(state);

        private static void AdvanceTo(GameState state, int turn)
        {
            while (state.TurnNumber < turn)
                End(state);
        }

        [Theory]
        [InlineData(Team.Player1)]
        [InlineData(Team.Player2)]
        public void BothTurnsOfEachRound_HitBothTeams_WithTheSameIncreasingDamage(Team first)
        {
            var (state, units) = Game().WithRules(rules => rules.Match.FirstTeam = first).Start();
            AdvanceTo(state, 15);
            Assert.All(units, unit => Assert.Equal(100, unit.Stats.CurrentHp));
            Assert.DoesNotContain(state.Events.All.OfType<UnitDamagedEvent>(), e => e.Type == DamageType.Fatigue);

            int hp = 100;
            foreach (int damage in new[] { 1, 1, 2, 2, 3, 3 })
            {
                EndTurnResult result = End(state);
                hp -= damage;
                Assert.All(units, unit => Assert.Equal(hp, unit.Stats.CurrentHp));
                var events = result.Events.OfType<UnitDamagedEvent>().ToArray();
                Assert.Equal(2, events.Length);
                Assert.All(events, e =>
                {
                    Assert.Equal(DamageType.Fatigue, e.Type);
                    Assert.Null(e.SourceId);
                    Assert.Equal(damage, e.RequestedAmount);
                });
            }
        }

        [Fact]
        public void NullRule_DisablesFatigue()
        {
            var (state, units) = Game().WithRules(rules => rules.Fatigue = null).Start();
            AdvanceTo(state, 25);
            Assert.All(units, unit => Assert.Equal(100, unit.Stats.CurrentHp));
            Assert.Empty(state.Events.All.OfType<UnitDamagedEvent>());
        }

        [Fact]
        public void CustomRule_ChangesStartDamageAndIncrease()
        {
            var (state, units) = Game().WithRules(rules => rules.Fatigue = new FatigueRuleData
            {
                StartRound = 2, InitialDamage = 3, DamageIncreasePerRound = 2,
            }).Start();
            AdvanceTo(state, 3);
            End(state); // 2 라운드 선공 3
            End(state); // 2 라운드 후공 3
            End(state); // 3 라운드 선공 5
            Assert.All(units, unit => Assert.Equal(89, unit.Stats.CurrentHp));
        }

        [Fact]
        public void ShieldsAbsorbFatigue_AndGuardDoesNotRedirectIt()
        {
            var (state, units) = Game().Place(Team.Player1, 1, 0).Start();
            AdvanceTo(state, 15);
            DamageSystem.AddShield(state, units[0], "test", 2, units[0]);
            StatusSystem.Apply(state, StatusLibrary.Guard(3), units[0], units[2]);

            EndTurnResult result = End(state);

            Assert.Equal(100, units[0].Stats.CurrentHp);
            Assert.Equal(1, units[0].Stats.Shield);
            Assert.Equal(9, units[2].Stats.CurrentHp);
            Assert.Equal(99, units[1].Stats.CurrentHp);
            var hit = result.Events.OfType<UnitDamagedEvent>().Single(e => e.TargetId == units[0].Id);
            Assert.Equal(1, hit.ShieldAbsorbed);
            Assert.DoesNotContain(result.Events.OfType<UnitDamagedEvent>(), e => e.Type == DamageType.Redirected);
        }

        [Fact]
        public void SealedUnit_IsImmuneThroughItsFinalTurn_ThenTakesFatigue()
        {
            var (state, units) = Game().Start();
            AdvanceTo(state, 13);
            StatusSystem.Apply(state, StatusLibrary.Sealed(), units[0], units[1]);
            AdvanceTo(state, 15);

            End(state);
            Assert.Equal(100, units[0].Stats.CurrentHp);
            Assert.DoesNotContain(units[0].Statuses, s => s.Definition.Id == StatusLibrary.SealedId);
            End(state);
            Assert.Equal(99, units[0].Stats.CurrentHp);
        }

        [Theory]
        [InlineData(1, 2, Team.Player2)]
        [InlineData(2, 1, Team.Player1)]
        public void Elimination_WaitsUntilBothTeamsHaveTakenDamage(int hp1, int hp2, Team winner)
        {
            var (state, units) = Game(hp1, hp2).Start();
            AdvanceTo(state, 15);
            EndTurnResult result = End(state);

            Assert.True(state.IsGameOver);
            Assert.Equal(winner, state.Winner);
            Assert.Equal(2, result.Events.OfType<UnitDamagedEvent>().Count());
            Assert.Equal(1, units.Single(u => u.Team == winner).Stats.CurrentHp);
            Assert.Single(result.Events.OfType<GameEndedEvent>());
            Assert.DoesNotContain(result.Events, e => e is TurnStartedEvent);
        }

        [Fact]
        public void BothTeamsEliminated_IsDraw_WithAllDeathsBeforeGameEnd()
        {
            var (state, units) = Game(1, 1).Start();
            AdvanceTo(state, 15);
            EndTurnResult result = End(state);

            Assert.True(state.IsGameOver);
            Assert.Null(state.Winner);
            Assert.All(units, u => Assert.False(u.IsOnBoard));
            Assert.Equal(2, result.Events.OfType<UnitDiedEvent>().Count());
            Assert.IsType<GameEndedEvent>(result.Events.Last());
            Assert.Single(result.Events.OfType<GameEndedEvent>());
        }

        [Fact]
        public void SummonsTakeFatigue_AndOwnerDeathDoesNotEndBatchEarly()
        {
            var (state, units) = Game(2, 2).Start();
            Unit summon = SummonSystem.Summon(state, units[0], TestGame.Stats(hp: 5), new Position(1, 0), Array.Empty<StatusDefinition>());
            AdvanceTo(state, 15);
            End(state);
            Assert.Equal(4, summon.Stats.CurrentHp);
            End(state);
            Assert.False(summon.IsOnBoard);
            Assert.True(state.IsGameOver);
            Assert.Null(state.Winner);
            Assert.Equal(0, state.DamageBatchDepth);
        }

        [Fact]
        public void Fork_PredictsFatigueWithoutChangingOriginal()
        {
            var (state, units) = Game().Start();
            AdvanceTo(state, 17);
            GameController fork = new GameController(state).Fork(Team.Player1);
            Assert.True(fork.EndTurn(new EndTurnRequest(fork.State.CurrentTeam)).Success);
            Assert.Equal(96, fork.State.FindUnit(units[0].Id).Stats.CurrentHp);
            Assert.Equal(98, units[0].Stats.CurrentHp);
            Assert.Equal(17, state.TurnNumber);
        }

        [Fact]
        public void OnlineMessages_RoundTripFatigueEventsAndUpdatedSnapshots()
        {
            GameController game = OnlineMatch.Create(42);
            foreach (Team team in new[] { Team.Player1, Team.Player2 })
                Assert.True(game.PlaceUnits(OnlineMatch.RandomPlacement(game, team, new Random((int)team + 42))).Success);
            while (game.State.TurnNumber < 15)
                Assert.True(game.EndTurn(new EndTurnRequest(game.State.CurrentTeam)).Success);
            var result = game.EndTurn(new EndTurnRequest(game.State.CurrentTeam));
            var update = new StateUpdateMessage
            {
                Events = result.Events.Select(ProtocolJson.Wrap).ToList(),
                Snapshot = game.GetSnapshot(Team.Player1),
            };
            var decoded = Assert.IsType<StateUpdateMessage>(ProtocolJson.DecodeServer(ProtocolJson.Encode(update)));
            Assert.Equal(8, decoded.Events.Select(ProtocolJson.ReadEvent).OfType<UnitDamagedEvent>().Count(e => e.Type == DamageType.Fatigue));
            GameController view = OnlineMatch.Restore(OnlineMatch.CreateConfig(), decoded.Snapshot);
            foreach (Unit unit in game.State.AllUnits())
                Assert.Equal(unit.Stats.CurrentHp, view.State.FindUnit(unit.Id).Stats.CurrentHp);
            Assert.NotNull(PrototypeTestPreset.CreateRules().Fatigue);
        }
    }
}
