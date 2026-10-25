using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.State;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>기존(v0.7 프로토타입) 이동/공격/턴 동작을 고정하는 테스트. 리팩터링 중 회귀 확인용.</summary>
    public class BaselineRuleTests
    {
        #region Move

        [Fact]
        public void Move_StraightLine_SpendsApPerCell()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Start();

            new MoveAction(u[0], new Position(0, 3)).Execute(state);

            Assert.Equal(new Position(0, 3), u[0].Position);
            Assert.Equal(1, state.GetPlayer(Team.Player1).Ap.Current);
        }

        [Fact]
        public void Move_Diagonal_IsRejected()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Start();

            Assert.Equal(MoveFailReason.NotStraightLine, new MoveAction(u[0], new Position(1, 1)).Validate(state));
        }

        [Fact]
        public void Move_ThroughWall_IsRejected()
        {
            var (state, u) = new TestGame()
                .WithMap(".......", ".......", ".......", ".......", ".......", "#......", ".......")
                .Place(Team.Player1, 0, 0)
                .Start();

            Assert.Equal(MoveFailReason.PathBlocked, new MoveAction(u[0], new Position(0, 2)).Validate(state));
        }

        [Fact]
        public void Move_ThroughUnit_IsRejected()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 0, 1)
                .Start();

            Assert.Equal(MoveFailReason.PathBlocked, new MoveAction(u[0], new Position(0, 2)).Validate(state));
        }

        [Fact]
        public void Move_BeyondAp_IsRejected()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Start();

            Assert.Equal(MoveFailReason.NotEnoughAp, new MoveAction(u[0], new Position(0, 5)).Validate(state));
        }

        #endregion

        #region Attack

        [Fact]
        public void MoveThenAttack_IsAllowed()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 3)
                .Start();

            new MoveAction(u[0], new Position(0, 2)).Execute(state);

            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void AttackThenMove_IsRejected()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1)
                .Start();

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(MoveFailReason.AlreadyActed, new MoveAction(u[0], new Position(1, 0)).Validate(state));
        }

        [Fact]
        public void Attack_SecondTimeSameTurn_IsRejected()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Start();

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(AttackFailReason.AlreadyActed, new AttackAction(u[0], u[1]).Validate(state));
        }

        // TurnActionRuleData 를 모두 끄면 AP 만 남는다: 공격 → 이동 → 공격 반복, AP 가 떨어지면 거부
        [Fact]
        public void TurnActionLimitsOff_AttackMoveAttack_OnlyApLimits()
        {
            var (state, u) = new TestGame()
                .WithRules(r => r.TurnAction = new TurnActionRuleData { CombatOncePerTurn = false, LockMoveAfterCombat = false })
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 1, 1, TestGame.Stats(hp: 20))
                .Start();

            new AttackAction(u[0], u[1]).Execute(state);
            Assert.Equal(MoveFailReason.None, new MoveAction(u[0], new Position(1, 0)).Validate(state));
            new MoveAction(u[0], new Position(1, 0)).Execute(state);
            Assert.Equal(AttackFailReason.NotEnoughAp, new AttackAction(u[0], u[1]).Validate(state));

            state.GetPlayer(Team.Player1).Ap.Restore(state.GetPlayer(Team.Player1).Ap.Max);
            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        // 한 번씩 규칙: 기본 공격과 각 스킬은 턴에 한 번씩, 서로 다른 것은 함께(스킬 A + 스킬 B + 기본 공격), 다음 턴에 초기화
        [Fact]
        public void EachActionOncePerTurn_SameActionTwice_IsRejected_DifferentActionsAllowed()
        {
            SkillDefinition Poke(string id) => new SkillDefinition(
                id, id, 1, SkillActionKind.Combat,
                new CellTargeting(1, CellRequirement.Empty, RangeShape.Orthogonal), PatternArea.SingleCell,
                Array.Empty<SkillEffect>());
            var (state, u) = new TestGame()
                .WithRules(r => r.TurnAction = new TurnActionRuleData { CombatOncePerTurn = false, LockMoveAfterCombat = false, EachActionOncePerTurn = true })
                .WithRules(r => r.Ap = new ApRuleData { MaxAp = 12, StartAp = 12, TurnRecoveryAp = 12, CarryOver = true })
                .WithSkills(Poke("A"), Poke("B"))
                .Place(Team.Player1, 0, 0, TestGame.Stats(skills: new[] { "A", "B" }))
                .Place(Team.Player2, 1, 1, TestGame.Stats(hp: 30))
                .Start();

            new UseSkillAction(u[0], "A", new Position(0, 1)).Execute(state);
            Assert.Equal(SkillFailReason.AlreadyUsedThisTurn, new UseSkillAction(u[0], "A", new Position(0, 1)).Validate(state));
            Assert.Equal(SkillFailReason.None, new UseSkillAction(u[0], "B", new Position(0, 1)).Validate(state));
            new UseSkillAction(u[0], "B", new Position(0, 1)).Execute(state);

            new AttackAction(u[0], u[1]).Execute(state);
            Assert.Equal(AttackFailReason.AlreadyUsedThisTurn, new AttackAction(u[0], u[1]).Validate(state));
            Assert.Equal(MoveFailReason.None, new MoveAction(u[0], new Position(1, 0)).Validate(state));
            Assert.False(TurnActionRules.CanStillFight(state, u[0]));

            new EndTurnAction(state.CurrentTeam).Execute(state);
            new EndTurnAction(state.CurrentTeam).Execute(state);
            Assert.Equal(SkillFailReason.None, new UseSkillAction(u[0], "A", new Position(0, 1)).Validate(state));
            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        // 행동 종료 스킬(분신 소환 등)은 행동 제한을 꺼도 이후 행동을 막는다
        [Fact]
        public void TurnActionLimitsOff_EndsCasterActions_StillBlocks()
        {
            const string summon = "SUMMON_LIKE";
            var summonSkill = new SkillDefinition(
                summon, "summon-like", 1, SkillActionKind.Combat,
                new CellTargeting(1, CellRequirement.Empty, RangeShape.Orthogonal), PatternArea.SingleCell,
                Array.Empty<SkillEffect>(), endsCasterActions: true);
            var (state, u) = new TestGame()
                .WithRules(r => r.TurnAction = new TurnActionRuleData { CombatOncePerTurn = false, LockMoveAfterCombat = false })
                .WithSkills(summonSkill)
                .Place(Team.Player1, 0, 0, TestGame.Stats(skills: summon))
                .Place(Team.Player2, 1, 1)
                .Start();

            new UseSkillAction(u[0], summon, new Position(0, 1)).Execute(state);

            Assert.Equal(AttackFailReason.ActionsEnded, new AttackAction(u[0], u[1]).Validate(state));
            Assert.Equal(MoveFailReason.ActionsEnded, new MoveAction(u[0], new Position(1, 0)).Validate(state));
        }

        [Fact]
        public void Attack_DiagonalWithinChebyshevRange_IsAllowed()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 1, 1)
                .Start();

            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void Attack_OverWall_IsAllowed()
        {
            var (state, u) = new TestGame()
                .WithMap(".......", ".......", ".......", ".......", ".......", "#......", ".......")
                .Place(Team.Player1, 0, 0, TestGame.Stats(range: 2))
                .Place(Team.Player2, 0, 2)
                .Start();

            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void Attack_OutOfRange_IsRejected()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 2)
                .Start();

            Assert.Equal(AttackFailReason.OutOfRange, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void Attack_DealsDamageAndSpendsAp()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 3))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 10))
                .Start();

            AttackResult result = new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(3, result.Damage);
            Assert.Equal(7, u[1].Stats.CurrentHp);
            Assert.Equal(2, state.GetPlayer(Team.Player1).Ap.Current);
        }

        [Fact]
        public void Attack_KillingLastEnemy_EndsGame()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 5))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 5))
                .Start();

            AttackResult result = new AttackAction(u[0], u[1]).Execute(state);

            Assert.True(result.TargetDied);
            Assert.False(u[1].IsPlaced);
            Assert.True(state.IsGameOver);
            Assert.Equal(Team.Player1, state.Winner);
        }

        #endregion

        #region Turn

        [Fact]
        public void FirstTurn_UsesStartApOnly_ThenRecoversFromSecondTurn()
        {
            var (state, _) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            Assert.Equal(4, state.GetPlayer(Team.Player1).Ap.Current);

            new EndTurnAction(Team.Player1).Execute(state);
            Assert.Equal(4, state.GetPlayer(Team.Player2).Ap.Current);

            new EndTurnAction(Team.Player2).Execute(state);
            Assert.Equal(6, state.GetPlayer(Team.Player1).Ap.Current); // 4 + 4, 최대 6
        }

        [Fact]
        public void EndTurn_ByWrongTeam_IsRejected()
        {
            var (state, _) = new TestGame().Place(Team.Player1, 0, 0).Place(Team.Player2, 6, 6).Start();

            Assert.Equal(EndTurnFailReason.NotYourTurn, new EndTurnAction(Team.Player2).Validate(state));
        }

        [Fact]
        public void NextOwnTurn_AllowsActingAgain()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Start();

            new AttackAction(u[0], u[1]).Execute(state);
            new EndTurnAction(Team.Player1).Execute(state);
            new EndTurnAction(Team.Player2).Execute(state);

            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        #endregion
    }
}
