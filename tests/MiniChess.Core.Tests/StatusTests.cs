using MiniChess.Core.Actions;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Movement;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class StatusTests
    {
        private static readonly StatusTiming TargetEndTick = new(TurnStep.EndDurationTick, TimingOwner.TargetOwner, includeApplicationTurn: false);

        private static StatusDefinition Def(
            string id = "test",
            int duration = 1,
            StatusStackPolicy stack = StatusStackPolicy.Refresh,
            StatusTiming decrement = null,
            StatusTiming trigger = null,
            StatusBehavior behavior = null)
        {
            return new StatusDefinition(id, duration, stack, decrement ?? TargetEndTick, trigger, behavior);
        }

        private static (GameState State, Unit[] Units) TwoUnits()
        {
            // u[0] = Player1, u[1] = Player2. Player1 턴에서 시작.
            return new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(hp: 20))
                .Place(Team.Player2, 6, 6, TestGame.Stats(hp: 20))
                .Start();
        }

        private static void EndTurn(GameState state) => new EndTurnAction(state.CurrentTeam).Execute(state);

        #region Apply / stack policy

        [Fact]
        public void Apply_AddsStatusAndRecordsEvent()
        {
            var (state, u) = TwoUnits();

            StatusEffect status = StatusSystem.Apply(state, Def(duration: 2), u[1], u[0]);

            Assert.Same(status, Assert.Single(u[1].Statuses));
            Assert.Equal(2, status.Remaining);
            Assert.Equal(Team.Player1, status.SourceTeam);
            Assert.False(state.Events.All.OfType<StatusAppliedEvent>().Single().Refreshed);
        }

        [Fact]
        public void Refresh_ResetsRemaining_WithoutDuplicating()
        {
            var (state, u) = TwoUnits();
            StatusDefinition def = Def(duration: 2, decrement: new StatusTiming(TurnStep.EndDurationTick, TimingOwner.TargetOwner, true));
            StatusSystem.Apply(state, def, u[0], u[1]);
            EndTurn(state); // Player1 종료 → 2 → 1

            StatusSystem.Apply(state, def, u[0], u[1]);

            Assert.Equal(2, Assert.Single(u[0].Statuses).Remaining);
            Assert.True(state.Events.All.OfType<StatusAppliedEvent>().Last().Refreshed);
        }

        [Fact]
        public void Replace_RemovesOldAndAddsNew()
        {
            var (state, u) = TwoUnits();
            StatusDefinition def = Def(stack: StatusStackPolicy.Replace);
            StatusEffect first = StatusSystem.Apply(state, def, u[1], u[0]);

            StatusEffect second = StatusSystem.Apply(state, def, u[1], u[0]);

            Assert.NotSame(first, second);
            Assert.Same(second, Assert.Single(u[1].Statuses));
            Assert.Equal(StatusRemoveReason.Replaced, state.Events.All.OfType<StatusRemovedEvent>().Single().Reason);
        }

        [Fact]
        public void Ignore_KeepsExisting()
        {
            var (state, u) = TwoUnits();
            StatusDefinition def = Def(stack: StatusStackPolicy.Ignore);
            StatusEffect first = StatusSystem.Apply(state, def, u[1], u[0]);

            StatusEffect second = StatusSystem.Apply(state, def, u[1], u[0]);

            Assert.Null(second);
            Assert.Same(first, Assert.Single(u[1].Statuses));
        }

        #endregion

        #region Timing

        // 사슬 속박 등 "상대가 다음 행동 턴 한 번을 상태로 보내도록" 하는 정의 방식 검증.
        [Fact]
        public void TargetOwnerEndTick_LastsThroughTargetsNextTurn()
        {
            var (state, u) = TwoUnits();
            StatusSystem.Apply(state, Def(duration: 1), u[1], u[0]); // Player1 턴에 Player2 유닛에게

            EndTurn(state); // Player1 종료: 대상 소유자 턴이 아님
            Assert.Single(u[1].Statuses); // Player2 턴 동안 유지

            EndTurn(state); // Player2 종료: 1 → 0, 제거
            Assert.Empty(u[1].Statuses);
            Assert.Equal(StatusRemoveReason.Expired, state.Events.All.OfType<StatusRemovedEvent>().Single().Reason);
        }

        [Fact]
        public void ExcludedApplicationTurn_SkipsTheStepRemainingInTheSameTurn()
        {
            var (state, u) = TwoUnits();
            var sourceEnd = new StatusTiming(TurnStep.EndDurationTick, TimingOwner.SourceOwner, includeApplicationTurn: false);
            StatusSystem.Apply(state, Def(duration: 1, decrement: sourceEnd), u[1], u[0]);

            EndTurn(state); // Player1(부여한 턴) 종료: 건너뜀
            EndTurn(state); // Player2 종료: 기준 소유자 아님
            Assert.Single(u[1].Statuses);

            EndTurn(state); // Player1 다음 턴 종료: 소모
            Assert.Empty(u[1].Statuses);
        }

        [Fact]
        public void IncludedApplicationTurn_ConsumesInTheSameTurn()
        {
            var (state, u) = TwoUnits();
            var sourceEnd = new StatusTiming(TurnStep.EndDurationTick, TimingOwner.SourceOwner, includeApplicationTurn: true);
            StatusSystem.Apply(state, Def(duration: 1, decrement: sourceEnd), u[1], u[0]);

            EndTurn(state);

            Assert.Empty(u[1].Statuses);
        }

        // 명세 14: "압축열탄 화상: 대상 소유자 Turn End에 피해"
        [Fact]
        public void DoT_TriggersOnTargetOwnersTurnEnd_BeforeDurationTick()
        {
            var (state, u) = TwoUnits();
            var behavior = new TestStatusBehavior
            {
                Triggered = c => DamageSystem.Apply(c.State, new DamageRequest(c.Status.Source, c.Target, 2, DamageType.DoT)),
            };
            var trigger = new StatusTiming(TurnStep.EndDamageOverTime, TimingOwner.TargetOwner, includeApplicationTurn: false);
            StatusSystem.Apply(state, Def(duration: 1, trigger: trigger, behavior: behavior), u[1], u[0]);

            EndTurn(state); // Player1 종료: 피해 없음
            Assert.Equal(20, u[1].Stats.CurrentHp);

            EndTurn(state); // Player2 종료: DoT → 지속시간 감소 순
            Assert.Equal(18, u[1].Stats.CurrentHp);
            Assert.Equal(1, behavior.TriggerCount);
            Assert.Empty(u[1].Statuses);

            var damaged = state.Events.All.OfType<UnitDamagedEvent>().Single();
            var removed = state.Events.All.OfType<StatusRemovedEvent>().Single();
            Assert.Equal(DamageType.DoT, damaged.Type);
            Assert.True(damaged.Sequence < removed.Sequence);
        }

        [Fact]
        public void DoT_KillingLastUnit_EndsGameWithoutStartingNextTurn()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 6, 6, TestGame.Stats(hp: 1))
                .Start();
            var behavior = new TestStatusBehavior
            {
                Triggered = c => DamageSystem.Apply(c.State, new DamageRequest(null, c.Target, 5, DamageType.DoT)),
            };
            var trigger = new StatusTiming(TurnStep.EndDamageOverTime, TimingOwner.SourceOwner, includeApplicationTurn: true);
            StatusSystem.Apply(state, Def(trigger: trigger, behavior: behavior), u[1], u[0]);

            IReadOnlyList<GameEvent> events = new EndTurnAction(Team.Player1).Execute(state).Events;

            Assert.True(state.IsGameOver);
            Assert.Equal(Team.Player1, state.Winner);
            Assert.DoesNotContain(events, e => e is TurnEndedEvent || e is TurnStartedEvent);
        }

        #endregion

        #region Behavior hooks

        [Fact]
        public void MoveBlockingStatus_BlocksVoluntaryMove_ButPolicyCanAllowExternal()
        {
            var (state, u) = TwoUnits();
            var root = new TestStatusBehavior
            {
                // 자발적 이동만 막는 속박 정책 예시
                MoveCheck = (c, initiator, kind, cells) =>
                    MovementControl.IsVoluntary(c.Target, initiator) ? MoveBlockReason.Rooted : MoveBlockReason.None,
            };
            StatusSystem.Apply(state, Def(behavior: root), u[0], u[1]);

            Assert.Equal(MoveFailReason.Rooted, new MoveAction(u[0], new Position(0, 1)).Validate(state));
            Assert.Equal(MoveBlockReason.None, MovementControl.Check(state, u[0], u[1], MoveKind.Knockback, 1));
        }

        // 볼라 누적 제한안: 이동 명령을 나눠도 합산되어 우회 불가
        [Fact]
        public void DistanceLimitStatus_UsesAccumulatedCells()
        {
            var (state, u) = TwoUnits();
            var bola = new TestStatusBehavior
            {
                MoveCheck = (c, initiator, kind, cells) =>
                    MovementControl.IsVoluntary(c.Target, initiator) && c.Target.TurnState.VoluntaryCellsMoved + cells > 2
                        ? MoveBlockReason.DistanceLimited
                        : MoveBlockReason.None,
            };
            StatusSystem.Apply(state, Def(behavior: bola), u[0], u[1]);

            new MoveAction(u[0], new Position(0, 1)).Execute(state);
            Assert.Equal(MoveFailReason.None, new MoveAction(u[0], new Position(0, 2)).Validate(state));
            new MoveAction(u[0], new Position(0, 2)).Execute(state);

            Assert.Equal(MoveFailReason.DistanceLimited, new MoveAction(u[0], new Position(0, 3)).Validate(state));
        }

        [Fact]
        public void Interceptor_RegisteredWhileStatusActive()
        {
            var (state, u) = TwoUnits();
            var shield = new HalfDamageInterceptor();
            var behavior = new TestStatusBehavior
            {
                Applied = c => c.State.AddDamageInterceptor(shield),
                Removed = c => c.State.RemoveDamageInterceptor(shield),
            };
            StatusSystem.Apply(state, Def(duration: 1, behavior: behavior), u[0], u[0]);

            DamageSystem.Apply(state, new DamageRequest(null, u[0], 4, DamageType.Direct));
            Assert.Equal(18, u[0].Stats.CurrentHp);

            EndTurn(state);
            EndTurn(state);
            EndTurn(state); // Player1 의 다음 턴 종료에 만료 (부여 턴 제외)
            Assert.Empty(u[0].Statuses);

            DamageSystem.Apply(state, new DamageRequest(null, u[0], 4, DamageType.Direct));
            Assert.Equal(14, u[0].Stats.CurrentHp);
        }

        [Fact]
        public void Death_RemovesAllStatuses()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 3))
                .Place(Team.Player2, 6, 6)
                .Start();
            var behavior = new TestStatusBehavior();
            StatusSystem.Apply(state, Def(behavior: behavior), u[1], u[0]);

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Empty(u[1].Statuses);
            Assert.Equal(1, behavior.RemovedCount);
            Assert.Equal(StatusRemoveReason.TargetDied, state.Events.All.OfType<StatusRemovedEvent>().Single().Reason);
        }

        #endregion

        private class HalfDamageInterceptor : IDamageInterceptor
        {
            public int Order => 0;
            public int Intercept(GameState state, DamageRequest request, int currentAmount) => currentAmount / 2;
        }
    }
}
