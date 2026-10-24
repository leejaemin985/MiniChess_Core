using MiniChess.Core.Actions;
using MiniChess.Core.Capture;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Statuses.Library;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 점령(명세 3.5 + 사용자 확정 규칙):
    /// 자기 턴 시작에 자기 유닛이 점령 칸 위에 있으면 +1, 칸에서 빠지면 0, 2회 연속이면 점령 완료 후 칸 소멸.
    /// </summary>
    public class CaptureTests
    {
        private static readonly string[] CaptureMap =
        {
            ".......",
            ".......",
            ".......",
            "...C...",
            ".......",
            ".......",
            ".......",
        };

        private static readonly Position Tile = new(3, 3);

        private sealed class FixedRandom : IRandomSource
        {
            private readonly int _value;

            public FixedRandom(int value) => _value = value;

            public int Next(int maxExclusive) => _value;
        }

        /// <summary>P1 유닛 (3,2) — 한 칸 위가 점령 칸. P2 유닛은 멀리.</summary>
        private static TestGame CreateGame()
        {
            return new TestGame()
                .WithMap(CaptureMap)
                .Place(Team.Player1, 3, 2)
                .Place(Team.Player2, 0, 6);
        }

        /// <summary>현재 팀(P1)과 상대(P2) 턴을 차례로 끝내 다시 P1 턴 시작으로 돌아온다.</summary>
        private static void PassRound(GameState state)
        {
            new EndTurnAction(Team.Player1).Execute(state);
            new EndTurnAction(Team.Player2).Execute(state);
        }

        // 사용자 예시: 1턴에 올려둠 → 돌아오는 내 턴에 1 → 다음 내 턴에 2 → 점령 성공
        [Fact]
        public void Holding_CountsAtOwnTurnStart_AndCompletesAtRequired()
        {
            var (state, u) = CreateGame().Start();

            new MoveAction(u[0], Tile).Execute(state);
            Assert.Equal(0, state.Capture.GetProgress(Team.Player1));

            new EndTurnAction(Team.Player1).Execute(state);
            Assert.Equal(0, state.Capture.GetProgress(Team.Player1)); // 상대 턴 시작에는 세지 않는다

            new EndTurnAction(Team.Player2).Execute(state);
            Assert.Equal(1, state.Capture.GetProgress(Team.Player1));
            Assert.True(state.Capture.IsActive);

            PassRound(state);

            Assert.False(state.Capture.IsActive);
            Assert.Equal(Team.Player1, state.Capture.CapturedBy);
            Assert.False(state.Board.GetCell(Tile).IsCaptureTile);
            var completed = Assert.Single(state.Events.All.OfType<CaptureCompletedEvent>());
            Assert.Equal(Team.Player1, completed.Team);
        }

        [Fact]
        public void LeavingTile_ResetsProgress_EvenIfReturningInSameTurn()
        {
            var (state, u) = CreateGame().Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            Assert.Equal(1, state.Capture.GetProgress(Team.Player1));

            new MoveAction(u[0], new Position(3, 2)).Execute(state);
            Assert.Equal(0, state.Capture.GetProgress(Team.Player1));

            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);

            Assert.Equal(1, state.Capture.GetProgress(Team.Player1));
            Assert.True(state.Capture.IsActive);
        }

        [Fact]
        public void HolderDying_ResetsProgress()
        {
            var (state, u) = new TestGame()
                .WithMap(CaptureMap)
                .Place(Team.Player1, 3, 2, TestGame.Stats(hp: 2))
                .Place(Team.Player2, 4, 4)
                .Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            new EndTurnAction(Team.Player1).Execute(state);

            new AttackAction(u[1], u[0]).Execute(state);

            Assert.Equal(0, state.Capture.GetProgress(Team.Player1));
        }

        [Fact]
        public void Progress_IsTrackedPerTeam()
        {
            var (state, u) = CreateGame().Start();
            new EndTurnAction(Team.Player1).Execute(state);
            new MoveAction(u[1], new Position(0, 3)).Execute(state); // P2 는 아직 점령 칸에 없음
            new EndTurnAction(Team.Player2).Execute(state);

            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);

            Assert.Equal(1, state.Capture.GetProgress(Team.Player1));
            Assert.Equal(0, state.Capture.GetProgress(Team.Player2));
        }

        [Fact]
        public void AfterCapture_TileVanishes_NoFurtherProgress_AndTrapsAllowed()
        {
            var (state, u) = CreateGame().Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            PassRound(state);
            int eventsAfterCapture = state.Events.Count;

            PassRound(state);

            Assert.Empty(state.Events.Since(eventsAfterCapture).OfType<CaptureProgressChangedEvent>());
            Assert.Equal(0, state.Capture.GetProgress(Team.Player1));
            Assert.Equal(CellEffectPlaceFailReason.None, CellEffectSystem.CanPlace(state, Tile, CellEffectLayer.Trap));
        }

        [Fact]
        public void EmptyRewardPool_CompletesWithoutReward()
        {
            var (state, u) = CreateGame().Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            PassRound(state);

            Assert.Equal(Team.Player1, state.Capture.CapturedBy);
            Assert.Null(state.Capture.Reward);
        }

        [Fact]
        public void Reward_IsPickedFromPoolUsingStateRandom()
        {
            var shield = new ShieldReward(3);
            var burn = new BasicAttackStatusReward(StatusLibrary.Burn(1, 2));
            var (state, u) = CreateGame()
                .WithRules(r => r.Capture.RewardPool = new() { shield, burn })
                .Start();
            state.Random = new FixedRandom(1);

            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            PassRound(state);

            Assert.Same(burn, state.Capture.Reward);
            Assert.Equal(burn.Id, state.Events.All.OfType<CaptureCompletedEvent>().Single().RewardId);
        }

        [Fact]
        public void ShieldReward_ShieldsAllLivingTeamUnits_AndAbsorbsDamageFirst()
        {
            var (state, u) = new TestGame()
                .WithMap(CaptureMap)
                .WithRules(r => r.Capture.RewardPool = new() { new ShieldReward(3) })
                .Place(Team.Player1, 3, 2)
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 4, 4, TestGame.Stats(attack: 5))
                .Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            PassRound(state);

            Assert.Equal(3, u[0].Stats.Shield);
            Assert.Equal(3, u[1].Stats.Shield);
            Assert.Equal(0, u[2].Stats.Shield);

            new EndTurnAction(Team.Player1).Execute(state);
            AttackResult result = new AttackAction(u[2], u[0]).Execute(state);

            Assert.Equal(0, u[0].Stats.Shield);
            Assert.Equal(10 - 2, u[0].Stats.CurrentHp);
            var damaged = result.Events.OfType<UnitDamagedEvent>().Single();
            Assert.Equal(3, damaged.ShieldAbsorbed);
            Assert.Equal(2, damaged.AppliedAmount);
        }

        // 사용자 확정: 재생 보호막은 점령한 팀의 턴 시작마다 다시 Amount 로 채운다(쌓이지 않음). 상대 팀 턴에는 채우지 않는다
        [Fact]
        public void RegeneratingShieldReward_RefillsOnOwnerTurnStart_WithoutStacking()
        {
            var (state, u) = new TestGame()
                .WithMap(CaptureMap)
                .WithRules(r => r.Capture.RewardPool = new() { new ShieldReward(3, regenerate: true) })
                .Place(Team.Player1, 3, 2)
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 4, 4, TestGame.Stats(attack: 2))
                .Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            PassRound(state);
            Assert.Equal(3, u[0].Stats.Shield);

            new EndTurnAction(Team.Player1).Execute(state);
            new AttackAction(u[2], u[0]).Execute(state);
            Assert.Equal(1, u[0].Stats.Shield);
            Assert.Equal(0, u[2].Stats.Shield);

            new EndTurnAction(Team.Player2).Execute(state); // P1 턴 시작: 재생
            Assert.Equal(3, u[0].Stats.Shield);
            Assert.Equal(3, u[1].Stats.Shield);
            Assert.Equal(0, u[2].Stats.Shield);

            PassRound(state);
            Assert.Equal(3, u[0].Stats.Shield); // 쌓이지 않음
        }

        [Fact]
        public void BasicAttackStatusReward_AppliesStatusOnBasicAttack()
        {
            var (state, u) = new TestGame()
                .WithMap(CaptureMap)
                .WithRules(r => r.Capture.RewardPool = new() { new BasicAttackStatusReward(StatusLibrary.Burn(1, 2)) })
                .Place(Team.Player1, 3, 2)
                .Place(Team.Player2, 4, 4)
                .Start();
            new MoveAction(u[0], Tile).Execute(state);
            PassRound(state);
            PassRound(state);

            new AttackAction(u[0], u[1]).Execute(state);

            StatusEffect burn = u[1].FindStatus(StatusLibrary.BurnId);
            Assert.NotNull(burn);
            Assert.Same(u[0], burn.Source);
        }

        [Fact]
        public void BeforeCapture_BasicAttackAppliesNoStatus()
        {
            var (state, u) = new TestGame()
                .WithMap(CaptureMap)
                .WithRules(r => r.Capture.RewardPool = new() { new BasicAttackStatusReward(StatusLibrary.Burn(1, 2)) })
                .Place(Team.Player1, 3, 2)
                .Place(Team.Player2, 4, 3)
                .Start();

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Empty(u[1].Statuses);
        }

        [Fact]
        public void MapWithoutCaptureTile_IsInactive()
        {
            var (state, _) = new TestGame().Place(Team.Player1, 0, 0).Start();

            Assert.Null(state.Capture.Tile);
            Assert.False(state.Capture.IsActive);
        }

        [Fact]
        public void MapWithTwoCaptureTiles_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() =>
                new TestGame().WithMap("C.C", "...", "...").Place(Team.Player1, 1, 1).Start());
        }

        [Fact]
        public void StartCapture_CannotBeUsedAsStatusTiming()
        {
            Assert.Throws<ArgumentException>(() =>
                new StatusTiming(TurnStep.StartCapture, TimingOwner.TargetOwner, includeApplicationTurn: false));
        }
    }
}
