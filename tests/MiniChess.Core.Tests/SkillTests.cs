using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class SkillTests
    {
        private const string Strike = "STRIKE";

        private static readonly PatternArea Square3x3 =
            new PatternArea(AreaShape.Parse(new[] { "###", "#@#", "###" }), AreaAnchor.Target);

        /// <summary>사거리 1 적 단일 대상, 피해 5, AP 3 의 전투 스킬.</summary>
        private static SkillDefinition StrikeSkill(int? apCost = 3, int? damage = 5, int? range = 1)
        {
            return new SkillDefinition(
                Strike, "test strike", apCost, SkillActionKind.Combat,
                new UnitTargeting(TargetFilter.Enemy, range),
                PatternArea.SingleCell,
                new SkillEffect[] { new DamageEffect(damage) });
        }

        private static UnitBaseStats Caster(params string[] skills) => TestGame.Stats("caster", hp: 10, skills: skills);

        #region Basic use

        [Fact]
        public void CombatSkill_DealsDamage_SpendsAp_AndUsesCombatAction()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill())
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 10))
                .Start();

            SkillResult result = new UseSkillAction(u[0], Strike, new Position(0, 1)).Execute(state);

            Assert.Equal(5, u[1].Stats.CurrentHp);
            Assert.Equal(1, state.GetPlayer(Team.Player1).Ap.Current);
            Assert.True(u[0].TurnState.CombatActionUsed);
            Assert.IsType<SkillUsedEvent>(result.Events[0]);
            Assert.Contains(result.Events, e => e is UnitDamagedEvent d && d.Source == u[0]);
        }

        [Fact]
        public void Skill_NotOwnedByCaster_IsRejected()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill())
                .Place(Team.Player1, 0, 0, Caster())
                .Place(Team.Player2, 0, 1)
                .Start();

            Assert.Equal(SkillFailReason.SkillNotOwned, new UseSkillAction(u[0], Strike, new Position(0, 1)).Validate(state));
        }

        [Fact]
        public void Skill_OwnedButNotInCatalog_IsRejected()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1)
                .Start();

            Assert.Equal(SkillFailReason.SkillNotOwned, new UseSkillAction(u[0], Strike, new Position(0, 1)).Validate(state));
        }

        [Fact]
        public void Skill_WithUnsetValues_IsRejectedAsConfigMissing_AndReportsPaths()
        {
            SkillDefinition skill = StrikeSkill(apCost: null, damage: null, range: null);
            var (state, u) = new TestGame()
                .WithSkills(skill)
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1)
                .Start();

            Assert.Equal(SkillFailReason.ConfigMissing, new UseSkillAction(u[0], Strike, new Position(0, 1)).Validate(state));
            Assert.Equal(
                new[] { "STRIKE.ApCost 미설정", "STRIKE.Targeting.Range 미설정", "STRIKE.Effects[0].Amount 미설정" },
                skill.GetConfigIssues());
        }

        [Theory]
        [InlineData(0, 2)] // 사거리 밖
        [InlineData(1, 0)] // 아군
        [InlineData(1, 1)] // 빈 칸
        public void Skill_InvalidTarget_IsRejected(int x, int y)
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill())
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player2, 0, 2)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, new UseSkillAction(u[0], Strike, new Position(x, y)).Validate(state));
        }

        [Fact]
        public void Skill_NotEnoughAp_IsRejected()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill(apCost: 5))
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1)
                .Start();

            Assert.Equal(SkillFailReason.NotEnoughAp, new UseSkillAction(u[0], Strike, new Position(0, 1)).Validate(state));
        }

        // 명세 14: "벽 너머 사거리 내 대상 공격" → 허용 (스킬도 동일)
        [Fact]
        public void Skill_TargetBehindWall_IsAllowed()
        {
            var (state, u) = new TestGame()
                .WithMap(".......", ".......", ".......", ".......", ".......", "#......", ".......")
                .WithSkills(StrikeSkill(range: 2))
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 2)
                .Start();

            Assert.Equal(SkillFailReason.None, new UseSkillAction(u[0], Strike, new Position(0, 2)).Validate(state));
        }

        #endregion

        #region Combat action rule (명세 3.2)

        [Fact]
        public void MoveThenSkill_IsAllowed()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill(apCost: 2))
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 3)
                .Start();

            new MoveAction(u[0], new Position(0, 2)).Execute(state);

            Assert.Equal(SkillFailReason.None, new UseSkillAction(u[0], Strike, new Position(0, 3)).Validate(state));
        }

        [Fact]
        public void SkillThenMove_IsRejected()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill(apCost: 1))
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Start();

            new UseSkillAction(u[0], Strike, new Position(0, 1)).Execute(state);

            Assert.Equal(MoveFailReason.AlreadyActed, new MoveAction(u[0], new Position(1, 0)).Validate(state));
        }

        [Fact]
        public void AttackThenSkill_AndSkillThenAttack_AreRejected()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill(apCost: 1))
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player1, 6, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Place(Team.Player2, 6, 1, TestGame.Stats(hp: 20))
                .Start();

            new AttackAction(u[0], u[2]).Execute(state);
            Assert.Equal(SkillFailReason.AlreadyActed, new UseSkillAction(u[0], Strike, new Position(0, 1)).Validate(state));

            new UseSkillAction(u[1], Strike, new Position(6, 1)).Execute(state);
            Assert.Equal(AttackFailReason.AlreadyActed, new AttackAction(u[1], u[3]).Validate(state));
        }

        [Fact]
        public void MovementSkill_DoesNotUseCombatAction_AndIsBlockedAfterCombat()
        {
            const string swap = "SWAP_LIKE";
            var movementSkill = new SkillDefinition(
                swap, "movement-like", 1, SkillActionKind.Movement,
                new SelfTargeting(), PatternArea.SingleCell, Array.Empty<SkillEffect>());
            var (state, u) = new TestGame()
                .WithSkills(movementSkill)
                .Place(Team.Player1, 0, 0, Caster(swap))
                .Place(Team.Player1, 6, 0, Caster(swap))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 20))
                .Place(Team.Player2, 6, 1, TestGame.Stats(hp: 20))
                .Start();

            // 이동 계열 사용 후 공격 가능 (낫: 분신 교환 후 공격 허용)
            new UseSkillAction(u[0], swap, new Position(0, 0)).Execute(state);
            Assert.False(u[0].TurnState.CombatActionUsed);
            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[2]).Validate(state));

            // 공격 후 이동 계열 사용 불가 (낫: 본체 공격 후 분신 교환 거부)
            new AttackAction(u[1], u[3]).Execute(state);
            Assert.Equal(SkillFailReason.MoveLocked, new UseSkillAction(u[1], swap, new Position(6, 0)).Validate(state));
        }

        // 명세 14: "낫이 분신 소환 후 본체 이동/공격/교환" → 모두 거부
        [Fact]
        public void EndsCasterActions_BlocksEverythingElseThisTurn()
        {
            const string summon = "SUMMON_LIKE";
            var summonSkill = new SkillDefinition(
                summon, "summon-like", 1, SkillActionKind.Combat,
                new CellTargeting(1, CellRequirement.Empty, RangeShape.Orthogonal), PatternArea.SingleCell,
                Array.Empty<SkillEffect>(), endsCasterActions: true);
            var (state, u) = new TestGame()
                .WithSkills(summonSkill)
                .Place(Team.Player1, 0, 0, Caster(summon))
                .Place(Team.Player2, 1, 1)
                .Start();

            new UseSkillAction(u[0], summon, new Position(0, 1)).Execute(state);

            Assert.Equal(MoveFailReason.ActionsEnded, new MoveAction(u[0], new Position(1, 0)).Validate(state));
            Assert.Equal(AttackFailReason.ActionsEnded, new AttackAction(u[0], u[1]).Validate(state));
            Assert.Equal(SkillFailReason.ActionsEnded, new UseSkillAction(u[0], summon, new Position(1, 0)).Validate(state));
        }

        [Fact]
        public void UnmetCondition_IsRejected()
        {
            const string conditional = "CONDITIONAL";
            var skill = new SkillDefinition(
                conditional, "conditional", 1, SkillActionKind.Combat,
                new SelfTargeting(), PatternArea.SingleCell, Array.Empty<SkillEffect>(),
                new SkillCondition[] { new NeverCondition() });
            var (state, u) = new TestGame().WithSkills(skill).Place(Team.Player1, 0, 0, Caster(conditional)).Start();

            Assert.Equal(SkillFailReason.ConditionNotMet, new UseSkillAction(u[0], conditional, new Position(0, 0)).Validate(state));
        }

        #endregion

        #region Targeting / area / effects

        [Fact]
        public void OrthogonalAdjacentEmptyCellTargeting_ExcludesDiagonalsWallsAndUnits()
        {
            const string summon = "SUMMON_LIKE";
            var skill = new SkillDefinition(
                summon, "summon-like", 1, SkillActionKind.Combat,
                new CellTargeting(1, CellRequirement.Empty, RangeShape.Orthogonal), PatternArea.SingleCell,
                Array.Empty<SkillEffect>());
            var (state, u) = new TestGame()
                .WithMap(".......", ".......", ".......", ".......", "...#...", ".......", ".......")
                .WithSkills(skill)
                .Place(Team.Player1, 3, 3, Caster(summon))
                .Place(Team.Player2, 4, 3)
                .Start();

            List<Position> targets = SkillQueries.GetValidTargets(state, u[0], summon);

            Assert.Equal(new[] { new Position(2, 3), new Position(3, 4) }, targets.OrderBy(p => p.X).ThenBy(p => p.Y));
        }

        [Fact]
        public void PatternAreaDamage_HitsOnlyEnemiesInArea()
        {
            const string blast = "BLAST";
            var skill = new SkillDefinition(
                blast, "blast", 2, SkillActionKind.Combat,
                new CellTargeting(3, CellRequirement.NotWall), Square3x3,
                new SkillEffect[] { new DamageEffect(2) });
            var (state, u) = new TestGame()
                .WithSkills(skill)
                .Place(Team.Player1, 0, 0, Caster(blast))
                .Place(Team.Player2, 2, 2, TestGame.Stats(hp: 10)) // 중심
                .Place(Team.Player2, 3, 3, TestGame.Stats(hp: 10)) // 범위 안 (대각)
                .Place(Team.Player2, 4, 2, TestGame.Stats(hp: 10)) // 범위 밖
                .Place(Team.Player1, 1, 2, TestGame.Stats(hp: 10)) // 범위 안 아군
                .Start();

            SkillResult result = new UseSkillAction(u[0], blast, new Position(2, 2)).Execute(state);

            Assert.Equal(9, result.AffectedCells.Count);
            Assert.Equal(new[] { 10, 8, 8, 10, 10 }, u.Select(unit => unit.Stats.CurrentHp));
        }

        [Fact]
        public void ApplyStatusEffect_AppliesStatusFromCaster()
        {
            const string root = "ROOT";
            var status = new StatusDefinition("rooted", 1, StatusStackPolicy.Refresh,
                new StatusTiming(TurnStep.EndDurationTick, TimingOwner.TargetOwner, false));
            var skill = new SkillDefinition(
                root, "root", 1, SkillActionKind.Combat,
                new UnitTargeting(TargetFilter.Enemy, 2), PatternArea.SingleCell,
                new SkillEffect[] { new ApplyStatusEffect(status) });
            var (state, u) = new TestGame()
                .WithSkills(skill)
                .Place(Team.Player1, 0, 0, Caster(root))
                .Place(Team.Player2, 2, 2)
                .Start();

            new UseSkillAction(u[0], root, new Position(2, 2)).Execute(state);

            StatusEffect applied = Assert.Single(u[1].Statuses);
            Assert.Same(u[0], applied.Source);
        }

        [Fact]
        public void PlaceZoneEffect_PlacesPerCell_AndSkipsDisallowedCells()
        {
            const string traps = "TRAPS";
            StatusDefinition root = Statuses.Library.StatusLibrary.Root(targetTurns: 1, blocksExternalMoves: false);
            var skill = new SkillDefinition(
                traps, "traps", 1, SkillActionKind.Combat,
                new CellTargeting(3, CellRequirement.NotWall), Square3x3,
                new SkillEffect[] { new PlaceZoneEffect(2, (zone, p) => new RootTrap(zone, 0, root, singleUse: true)) });
            var (state, u) = new TestGame()
                .WithMap(".......", ".......", ".......", "...C...", ".......", ".......", ".......")
                .WithSkills(skill)
                .Place(Team.Player1, 0, 0, Caster(traps))
                .Start();

            new UseSkillAction(u[0], traps, new Position(3, 2)).Execute(state);

            int placed = state.Events.All.OfType<CellEffectPlacedEvent>().Count();
            Assert.Equal(8, placed); // 3×3 중 점령 칸 (3,3) 제외
            Assert.Null(state.Board.GetCell(new Position(3, 3)).GetEffect(CellEffectLayer.Trap));
        }

        #endregion

        #region Queries

        [Fact]
        public void Queries_ReturnNoTargets_WhenSkillIsUnusable()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill(apCost: 5))
                .Place(Team.Player1, 0, 0, Caster(Strike))
                .Place(Team.Player2, 0, 1)
                .Start();

            Assert.Empty(SkillQueries.GetValidTargets(state, u[0], Strike));
            Assert.Equal(SkillFailReason.NotEnoughAp, UseSkillAction.ValidateUsable(state, u[0], Strike));
        }

        [Fact]
        public void Queries_ListOwnedSkills_InSlotOrder()
        {
            var (state, u) = new TestGame()
                .WithSkills(StrikeSkill())
                .Place(Team.Player1, 0, 0, Caster(Strike, "UNDEFINED"))
                .Start();

            var skills = SkillQueries.GetSkills(state, u[0]);

            Assert.Equal(new[] { Strike, "UNDEFINED" }, skills.Select(s => s.Id));
            Assert.NotNull(skills[0].Definition);
            Assert.Null(skills[1].Definition);
        }

        #endregion

        private class NeverCondition : SkillCondition
        {
            public override bool IsMet(GameState state, Unit caster) => false;
        }
    }
}
