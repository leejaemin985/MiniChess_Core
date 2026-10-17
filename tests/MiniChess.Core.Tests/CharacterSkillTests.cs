using MiniChess.Core.Actions;
using MiniChess.Core.Capture;
using MiniChess.Core.Characters;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Events;
using MiniChess.Core.Movement;
using MiniChess.Core.Presets;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;
using MiniChess.Core.Statuses.Library;
using MiniChess.Core.Tests.Support;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 구현된 캐릭터 스킬 동작 검증. 수치는 PrototypeTestPreset 의 임시값을 사용한다(규칙은 TestGame 의 AP 6/4/4).
    /// </summary>
    public class CharacterSkillTests
    {
        private static SkillDefinition[] PresetSkills() =>
            PieceModules.CreateSkillCatalog(PrototypeTestPreset.CreateSkillTuning()).All.ToArray();

        private static TestGame Game() => new TestGame().WithSkills(PresetSkills());

        private static UnitBaseStats With(string skill, int hp = 10) => TestGame.Stats(skill, hp: hp, skills: skill);

        private static UnitBaseStats With(string skill1, string skill2) => TestGame.Stats("dual", skills: new[] { skill1, skill2 });

        private static void EndTurn(GameState state) => new EndTurnAction(state.CurrentTeam).Execute(state);

        private static SkillResult Use(GameState state, Unit caster, string skill, int x, int y) =>
            new UseSkillAction(caster, skill, new Position(x, y)).Execute(state);

        private static SkillFailReason Check(GameState state, Unit caster, string skill, int x, int y) =>
            new UseSkillAction(caster, skill, new Position(x, y)).Validate(state);

        #region Config

        [Fact]
        public void PresetTuning_MakesAllImplementedSkillsUsable()
        {
            SkillDefinition[] skills = PresetSkills();

            Assert.Equal(24, skills.Length); // 슬롯 22 + 고유 기능 2(분신 교환, 해체)
            Assert.All(skills, s => Assert.Empty(s.GetConfigIssues()));
        }

        [Fact]
        public void EmptyTuning_ReportsEveryMissingValueByName()
        {
            SkillDefinition[] skills = PieceModules.CreateSkillCatalog(new SkillTuning()).All.ToArray();

            Assert.All(skills, s => Assert.NotEmpty(s.GetConfigIssues()));

            List<string> trap = skills.Single(s => s.Id == ChemistPiece.RootTrapId).GetConfigIssues();
            Assert.Contains("CHEMIST_ROOT_TRAP.ApCost 미설정", trap);
            Assert.Contains("CHEMIST_ROOT_TRAP.SingleUse 미설정", trap);
            Assert.Contains("CHEMIST_ROOT_TRAP.Root.BlocksExternalMoves 미설정", trap);
        }

        [Fact]
        public void RosterSkillSlots_ReferToImplementedSkills()
        {
            var ids = PresetSkills().Select(s => s.Id).ToHashSet();
            var slotted = PieceModules.CreateDefinitions().SelectMany(c => c.SkillSlots).Where(id => id != null).ToList();

            Assert.Equal(22, slotted.Count);
            Assert.All(slotted, id => Assert.Contains(id, ids));
        }

        #endregion

        #region Single target

        [Fact]
        public void WarriorSmash_HitsAdjacentEnemyOnly()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 2, 2, With(WarriorPiece.SmashId))
                .Place(Team.Player2, 3, 3)
                .Place(Team.Player2, 4, 2)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], WarriorPiece.SmashId, 4, 2));

            Use(state, u[0], WarriorPiece.SmashId, 3, 3);

            Assert.Equal(5, u[1].Stats.CurrentHp);
        }

        [Fact]
        public void ArcherAimedShot_ReachesItsRange()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ArcherPiece.AimedShotId))
                .Place(Team.Player2, 5, 5)
                .Place(Team.Player2, 6, 6)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ArcherPiece.AimedShotId, 6, 6));

            Use(state, u[0], ArcherPiece.AimedShotId, 5, 5);

            Assert.Equal(7, u[1].Stats.CurrentHp);
        }

        // 명세 14: "압축열탄 화상" → 대상 소유자 Turn End 에 피해
        [Fact]
        public void FlameCompressedShell_ImpactThenBurnOnTargetOwnersTurnEnds()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(FlamePiece.CompressedShellId))
                .Place(Team.Player2, 2, 2)
                .Start();

            Use(state, u[0], FlamePiece.CompressedShellId, 2, 2);
            Assert.Equal(9, u[1].Stats.CurrentHp);

            EndTurn(state); // P1 종료: 화상 없음
            Assert.Equal(9, u[1].Stats.CurrentHp);

            EndTurn(state); // P2 종료: 화상 1회
            Assert.Equal(8, u[1].Stats.CurrentHp);

            EndTurn(state);
            EndTurn(state); // P2 종료: 화상 2회째, 만료
            Assert.Equal(7, u[1].Stats.CurrentHp);
            Assert.Empty(u[1].Statuses);
        }

        [Fact]
        public void FlameCompressedShell_ReapplyRefreshesInsteadOfStacking()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(FlamePiece.CompressedShellId))
                .Place(Team.Player1, 1, 0, With(FlamePiece.CompressedShellId))
                .Place(Team.Player2, 2, 2, TestGame.Stats(hp: 20))
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();

            Use(state, u[0], FlamePiece.CompressedShellId, 2, 2);
            Use(state, u[1], FlamePiece.CompressedShellId, 2, 2);

            Assert.Single(u[2].Statuses);
            EndTurn(state);
            EndTurn(state);
            Assert.Equal(17, u[2].Stats.CurrentHp); // 즉시 1 + 1, 화상 1 (중첩 없음)
        }

        [Fact]
        public void ChainBind_RootsTargetForItsNextTurn_ButAllowsAttack()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChainGuardPiece.ChainBindId))
                .Place(Team.Player2, 1, 1, TestGame.Stats(hp: 20))
                .Start();

            Use(state, u[0], ChainGuardPiece.ChainBindId, 1, 1);
            EndTurn(state);

            Assert.Equal(MoveFailReason.Rooted, new MoveAction(u[1], new Position(1, 2)).Validate(state));
            Assert.Equal(AttackFailReason.None, new AttackAction(u[1], u[0]).Validate(state));

            EndTurn(state);
            EndTurn(state);
            Assert.Equal(MoveFailReason.None, new MoveAction(u[1], new Position(1, 2)).Validate(state));
        }

        #endregion

        #region Root trap

        [Fact]
        public void RootTrap_StopsEnemyMove_DamagesRoots_AndIsConsumed()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChemistPiece.RootTrapId))
                .Place(Team.Player2, 0, 4)
                .Start();

            Use(state, u[0], ChemistPiece.RootTrapId, 0, 2);
            EndTurn(state);

            MovementResult move = new MoveAction(u[1], new Position(0, 1)).Execute(state);

            Assert.Equal(new Position(0, 2), move.StoppedAt);
            Assert.Equal(9, u[1].Stats.CurrentHp);
            Assert.NotNull(u[1].FindStatus(StatusLibrary.RootId));
            Assert.Null(state.Board.GetCell(new Position(0, 2)).GetEffect(CellEffectLayer.Trap));
            Assert.Equal(MoveFailReason.Rooted, new MoveAction(u[1], new Position(1, 2)).Validate(state));
        }

        // 자기 턴에 덫을 밟으면 남은 턴 + 다음 자기 턴까지 속박 (부여 턴 종료는 세지 않음)
        [Fact]
        public void RootTrap_TriggeredOnOwnTurn_RootsThroughNextOwnTurn()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChemistPiece.RootTrapId))
                .Place(Team.Player2, 0, 4)
                .Start();
            Use(state, u[0], ChemistPiece.RootTrapId, 0, 2);
            EndTurn(state);
            new MoveAction(u[1], new Position(0, 1)).Execute(state);

            EndTurn(state); // P2 종료 (부여 턴, 세지 않음)
            EndTurn(state); // P1 종료
            Assert.Equal(MoveFailReason.Rooted, new MoveAction(u[1], new Position(1, 2)).Validate(state));

            EndTurn(state); // P2 종료: 만료
            EndTurn(state);
            Assert.Equal(MoveFailReason.None, new MoveAction(u[1], new Position(1, 2)).Validate(state));
        }

        [Fact]
        public void RootTrap_IsNotTriggeredByAllies()
        {
            var (state, u) = Game()
                .WithRules(r => r.Ap.StartAp = 6)
                .Place(Team.Player1, 0, 0, With(ChemistPiece.RootTrapId))
                .Place(Team.Player1, 1, 4)
                .Start();
            Use(state, u[0], ChemistPiece.RootTrapId, 1, 2);

            MovementResult move = new MoveAction(u[1], new Position(1, 1)).Execute(state);

            Assert.Equal(new Position(1, 1), move.StoppedAt);
            Assert.NotNull(state.Board.GetCell(new Position(1, 2)).GetEffect(CellEffectLayer.Trap));
        }

        // 명세 14: "점령 타일에 덫 생성" → 거부 (지정 단계에서 제외)
        [Fact]
        public void RootTrap_CannotTargetCaptureTile()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", ".......", ".......", "C......", ".......", ".......")
                .Place(Team.Player1, 0, 0, With(ChemistPiece.RootTrapId))
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ChemistPiece.RootTrapId, 0, 2));
            Assert.DoesNotContain(new Position(0, 2), SkillQueries.GetValidTargets(state, u[0], ChemistPiece.RootTrapId));
        }

        [Fact]
        public void RootTrap_RespectsMaxActive_AndExpiresAfterLifetime()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 3, With(ChemistPiece.RootTrapId))
                .Place(Team.Player2, 6, 6)
                .Start();

            Use(state, u[0], ChemistPiece.RootTrapId, 3, 5); // 턴 1
            EndTurn(state); EndTurn(state);
            Use(state, u[0], ChemistPiece.RootTrapId, 3, 1); // 턴 3
            EndTurn(state); EndTurn(state);

            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], ChemistPiece.RootTrapId, 5, 3)); // 턴 5: 최대 2개

            // 첫 덫(수명 3): 턴 3, 5, 7 의 P1 종료에 감소 → 턴 7 종료에 만료
            EndTurn(state); EndTurn(state);
            Assert.NotNull(state.Board.GetCell(new Position(3, 5)).GetEffect(CellEffectLayer.Trap));
            EndTurn(state);
            Assert.Null(state.Board.GetCell(new Position(3, 5)).GetEffect(CellEffectLayer.Trap));
            Assert.Contains(state.Events.All, e => e is CellEffectRemovedEvent r && r.Reason == CellEffectRemoveReason.Expired);
        }

        #endregion

        #region Fields

        // 사용자 확정: 독 장판은 피해가 없고, 설치 순간 범위 안의 적을 중독시킨다. 중독은 화상과 같은 시점에 1 × 2턴
        [Fact]
        public void PoisonGas_PoisonsEnemiesInAreaOnPlacement_ZoneItselfDealsNoDamage()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChemistPiece.PoisonGasId))
                .Place(Team.Player2, 3, 3)
                .Start();

            SkillResult result = Use(state, u[0], ChemistPiece.PoisonGasId, 2, 2);
            Assert.Equal(
                new[] { new Position(2, 2), new Position(3, 2), new Position(2, 3), new Position(3, 3) }.ToHashSet(),
                result.AffectedCells.ToHashSet());
            Assert.NotNull(u[1].FindStatus(StatusLibrary.PoisonId));

            EndTurn(state); // P1 종료: 적 소유자 턴 아님
            Assert.Equal(10, u[1].Stats.CurrentHp);
            EndTurn(state); // P2 종료: 중독 1
            Assert.Equal(9, u[1].Stats.CurrentHp);
            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], ChemistPiece.PoisonGasId, 0, 2));
            EndTurn(state); // P1 종료: 장판 수명 2 → 1
            EndTurn(state); // P2 종료: 중독 2(마지막). 장판 위에 머물러도 추가 효과 없음
            Assert.Equal(8, u[1].Stats.CurrentHp);
            Assert.Null(u[1].FindStatus(StatusLibrary.PoisonId));
            EndTurn(state); // P1 종료: 장판 수명 0, 전체 제거
            Assert.Null(state.Board.GetCell(new Position(2, 2)).GetEffect(CellEffectLayer.AreaEffect));
            EndTurn(state);
            Assert.Equal(8, u[1].Stats.CurrentHp);
            Assert.Equal(SkillFailReason.None, Check(state, u[0], ChemistPiece.PoisonGasId, 0, 2));
        }

        // 사용자 확정: 독 장판을 지나가기만 해도 중독. 걸린 그 턴은 세지 않는다(화상과 동일)
        [Fact]
        public void PoisonGas_EnemyPassingThroughGetsPoisoned_AllyDoesNot()
        {
            var (state, u) = Game()
                .WithRules(r => r.Ap.StartAp = 6)
                .Place(Team.Player1, 0, 0, With(ChemistPiece.PoisonGasId))
                .Place(Team.Player1, 3, 4)
                .Place(Team.Player2, 2, 1)
                .Start();
            Use(state, u[0], ChemistPiece.PoisonGasId, 2, 2);
            new MoveAction(u[1], new Position(3, 1)).Execute(state);
            EndTurn(state);

            new MoveAction(u[2], new Position(2, 4)).Execute(state); // (2,2), (2,3) 통과
            EndTurn(state); // 걸린 턴의 종료: 피해 없음

            Assert.Null(u[1].FindStatus(StatusLibrary.PoisonId));
            Assert.NotNull(u[2].FindStatus(StatusLibrary.PoisonId));
            Assert.Equal(10, u[2].Stats.CurrentHp);

            EndTurn(state);
            EndTurn(state);
            Assert.Equal(9, u[2].Stats.CurrentHp);
        }

        // 사용자 확정: 화염지대는 십자 5칸. 설치 즉시 피해 후 장판 피해, 상태이상 없음
        [Fact]
        public void FlameZone_HitsOnPlacement_ThenDamagesEnemiesInCrossOnTheirTurnEnd()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(FlamePiece.FlameZoneId))
                .Place(Team.Player2, 3, 4) // 범위 안 (십자 끝)
                .Place(Team.Player2, 4, 4) // 범위 밖 (대각선)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], FlamePiece.FlameZoneId, 4, 4)); // 사거리 3 초과

            SkillResult result = Use(state, u[0], FlamePiece.FlameZoneId, 3, 3);
            Assert.Equal(5, result.AffectedCells.Count);
            Assert.Equal(8, u[1].Stats.CurrentHp); // 설치 즉시 2

            EndTurn(state); // P1 종료: 적 소유자 턴 아님
            Assert.Equal(8, u[1].Stats.CurrentHp);
            EndTurn(state); // P2 종료: 장판 3
            Assert.Equal(5, u[1].Stats.CurrentHp);
            Assert.Equal(10, u[2].Stats.CurrentHp);
            Assert.Empty(u[1].Statuses);

            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], FlamePiece.FlameZoneId, 1, 1));
        }

        // 명세 14: "턴 시작 회복 초원" → 지정한 Turn Start 에만 회복
        [Fact]
        public void HealingMeadow_HealsAlliesAtTheirTurnStart()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 1, 1, With(GardenerPiece.HealingMeadowId))
                .Place(Team.Player1, 2, 2)
                .Place(Team.Player2, 0, 0, TestGame.Stats(hp: 10))
                .Start();
            DamageSystem.Apply(state, new DamageRequest(null, u[1], 5, DamageType.Direct));
            DamageSystem.Apply(state, new DamageRequest(null, u[2], 5, DamageType.Direct));

            Use(state, u[0], GardenerPiece.HealingMeadowId, 1, 1); // 자기 칸 중심 3×3
            EndTurn(state); // P1 종료: 회복 없음
            Assert.Equal(5, u[1].Stats.CurrentHp);

            EndTurn(state); // P2 종료 → P1 시작: 아군 회복, 적은 회복 안 됨
            Assert.Equal(6, u[1].Stats.CurrentHp);
            Assert.Equal(5, u[2].Stats.CurrentHp);
        }

        // 명세 14: "3×3 회복지대 일부에 독가스 설치" → 겹치는 타일의 회복지대가 교체됨
        [Fact]
        public void PoisonGas_OverwritesOverlappingMeadowTiles_OthersRemain()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 1, 1, With(GardenerPiece.HealingMeadowId))
                .Place(Team.Player1, 5, 5, With(ChemistPiece.PoisonGasId))
                .Place(Team.Player2, 6, 0)
                .Start();
            Use(state, u[0], GardenerPiece.HealingMeadowId, 1, 1); // (0..2, 0..2)
            EndTurn(state); EndTurn(state);

            Use(state, u[1], ChemistPiece.PoisonGasId, 2, 2); // (2..3, 2..3)

            Assert.IsType<PoisonField>(state.Board.GetCell(new Position(2, 2)).GetEffect(CellEffectLayer.AreaEffect));
            Assert.IsType<HealField>(state.Board.GetCell(new Position(0, 0)).GetEffect(CellEffectLayer.AreaEffect));
            Assert.IsType<HealField>(state.Board.GetCell(new Position(2, 1)).GetEffect(CellEffectLayer.AreaEffect));
        }

        #endregion

        #region Shield

        [Fact]
        public void SingleShield_ShieldsAlly_SpendsAp_AndUsesCombatAction()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 1, 1, With(GardenerPiece.SingleShieldId))
                .Place(Team.Player1, 3, 3)
                .Place(Team.Player2, 6, 6)
                .Start();

            Use(state, u[0], GardenerPiece.SingleShieldId, 3, 3);

            Assert.Equal(3, u[1].Stats.FindShield(GardenerPiece.SingleShieldId).Amount);
            Assert.Same(u[0], u[1].Stats.FindShield(GardenerPiece.SingleShieldId).Source);
            Assert.Equal(1, state.GetPlayer(Team.Player1).Ap.Current);
            Assert.True(u[0].TurnState.CombatActionUsed);
        }

        [Fact]
        public void SingleShield_CanTargetSelf_ButNotEnemy()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 1, 1, With(GardenerPiece.SingleShieldId))
                .Place(Team.Player2, 2, 2)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], GardenerPiece.SingleShieldId, 2, 2));

            Use(state, u[0], GardenerPiece.SingleShieldId, 1, 1);
            Assert.Equal(3, u[0].Stats.Shield);
        }

        [Fact]
        public void SingleShield_RecastOnSameTarget_RechargesWithoutStacking()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 1, 1, With(GardenerPiece.SingleShieldId))
                .Place(Team.Player2, 6, 6)
                .Start();
            Use(state, u[0], GardenerPiece.SingleShieldId, 1, 1);
            DamageSystem.Apply(state, new DamageRequest(null, u[0], 2, DamageType.Direct)); // 3 → 1
            EndTurn(state); EndTurn(state);

            Use(state, u[0], GardenerPiece.SingleShieldId, 1, 1);

            Assert.Equal(3, u[0].Stats.Shield);
            Assert.Single(u[0].Stats.Shields);
        }

        [Fact]
        public void SingleShield_AddsToShieldWithDifferentId()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 1, 1, With(GardenerPiece.SingleShieldId))
                .Place(Team.Player2, 6, 6)
                .Start();
            DamageSystem.AddShield(state, u[0], ShieldReward.ShieldId, 3, source: null);

            Use(state, u[0], GardenerPiece.SingleShieldId, 1, 1);

            Assert.Equal(6, u[0].Stats.Shield);
            Assert.Equal(new[] { ShieldReward.ShieldId, GardenerPiece.SingleShieldId }, u[0].Stats.Shields.Select(s => s.Id));
        }

        #endregion

        #region Bola

        // 명세 14: "볼라 대상이 이동 명령을 나누어 사용" → 누적 합산, 명령 분할로 우회 불가
        [Fact]
        public void Bola_Damages_AndLimitsAccumulatedVoluntaryMovesOnTargetsNextTurn()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ArcherPiece.BolaId))
                .Place(Team.Player2, 2, 2, TestGame.Stats(hp: 10))
                .Start();

            Use(state, u[0], ArcherPiece.BolaId, 2, 2);
            Assert.Equal(9, u[1].Stats.CurrentHp);
            EndTurn(state);

            Assert.Equal(MoveFailReason.DistanceLimited, new MoveAction(u[1], new Position(2, 5)).Validate(state));
            new MoveAction(u[1], new Position(2, 3)).Execute(state);
            new MoveAction(u[1], new Position(2, 4)).Execute(state);
            Assert.Equal(MoveFailReason.DistanceLimited, new MoveAction(u[1], new Position(2, 5)).Validate(state));
        }

        [Fact]
        public void Bola_DoesNotLimitExternalMoves()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ArcherPiece.BolaId))
                .Place(Team.Player2, 2, 2)
                .Start();
            Use(state, u[0], ArcherPiece.BolaId, 2, 2);

            MovementResult result = MovementResolver.Resolve(
                state, u[1], u[0], MoveKind.Knockback, new[] { new Position(3, 2), new Position(4, 2), new Position(5, 2) });

            Assert.Equal(3, result.CellsMoved);
        }

        [Fact]
        public void Bola_ExpiresAfterTargetsNextTurn()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ArcherPiece.BolaId))
                .Place(Team.Player2, 2, 2)
                .Start();
            Use(state, u[0], ArcherPiece.BolaId, 2, 2);
            EndTurn(state); // P1 종료 → P2 턴: 제한 중
            Assert.NotNull(u[1].FindStatus(StatusLibrary.DistanceLimitId));

            EndTurn(state); // P2 종료: 1 감소 → 해제
            EndTurn(state); // P1 종료 → P2 턴

            Assert.Null(u[1].FindStatus(StatusLibrary.DistanceLimitId));
            Assert.Equal(MoveFailReason.None, new MoveAction(u[1], new Position(2, 5)).Validate(state));
        }

        [Fact]
        public void Bola_Reapply_RefreshesInsteadOfStacking()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ArcherPiece.BolaId))
                .Place(Team.Player1, 0, 1, With(ArcherPiece.BolaId))
                .Place(Team.Player2, 2, 2, TestGame.Stats(hp: 10))
                .Start();
            state.GetPlayer(Team.Player1).Ap.Recover(6);

            Use(state, u[0], ArcherPiece.BolaId, 2, 2);
            Use(state, u[1], ArcherPiece.BolaId, 2, 2);

            Assert.Single(u[2].Statuses, s => s.Definition.Id == StatusLibrary.DistanceLimitId);
            Assert.Same(u[1], u[2].FindStatus(StatusLibrary.DistanceLimitId).Source);
        }

        #endregion

        #region Dash

        [Fact]
        public void Dash_MovesStraight_ThenHitsOnlyTheEnemyDirectlyAhead()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(WarriorPiece.DashId))
                .Place(Team.Player2, 0, 4) // 전방
                .Place(Team.Player2, 1, 4) // 대각선 앞
                .Start();

            Use(state, u[0], WarriorPiece.DashId, 0, 3);

            Assert.Equal(new Position(0, 3), u[0].Position);
            Assert.Equal(7, u[1].Stats.CurrentHp);
            Assert.Equal(10, u[2].Stats.CurrentHp);
            Assert.Equal(AttackFailReason.AlreadyActed, new AttackAction(u[0], u[1]).Validate(state));
        }

        [Fact]
        public void Dash_WithoutEnemyAhead_JustMoves()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(WarriorPiece.DashId))
                .Place(Team.Player2, 6, 6)
                .Start();

            SkillResult result = Use(state, u[0], WarriorPiece.DashId, 0, 2);

            Assert.Equal(new Position(0, 2), u[0].Position);
            Assert.DoesNotContain(result.Events, e => e is UnitDamagedEvent);
        }

        [Fact]
        public void Dash_TargetsOnlyStraightClearCellsInRange()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(WarriorPiece.DashId))
                .Place(Team.Player1, 0, 2)
                .Place(Team.Player2, 6, 6)
                .Start();

            Assert.Equal(SkillFailReason.None, Check(state, u[0], WarriorPiece.DashId, 0, 1));
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], WarriorPiece.DashId, 0, 3)); // 유닛에 막힘
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], WarriorPiece.DashId, 1, 1)); // 대각선
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], WarriorPiece.DashId, 4, 0)); // 사거리 초과
        }

        // 사용자 확정: 돌진 중 제어기에 걸려도 피해/상태는 받되 도착 지점까지 간다
        [Fact]
        public void Dash_ThroughRootTrap_TakesDamageAndRoot_ButReachesDestination()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(WarriorPiece.DashId))
                .Place(Team.Player2, 2, 2, With(ChemistPiece.RootTrapId))
                .Place(Team.Player2, 0, 4)
                .Start();
            EndTurn(state);
            Use(state, u[1], ChemistPiece.RootTrapId, 0, 2);
            EndTurn(state);

            Use(state, u[0], WarriorPiece.DashId, 0, 3);

            Assert.Equal(new Position(0, 3), u[0].Position);
            Assert.Equal(9, u[0].Stats.CurrentHp);
            Assert.NotNull(u[0].FindStatus(StatusLibrary.RootId));
            Assert.Equal(7, u[2].Stats.CurrentHp);
        }

        [Fact]
        public void Dash_DyingOnTheWay_StopsThere_WithoutAttacking()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(WarriorPiece.DashId, hp: 1))
                .Place(Team.Player1, 6, 0)
                .Place(Team.Player2, 2, 2, With(ChemistPiece.RootTrapId))
                .Place(Team.Player2, 0, 4)
                .Start();
            EndTurn(state);
            Use(state, u[2], ChemistPiece.RootTrapId, 0, 2);
            EndTurn(state);

            Use(state, u[0], WarriorPiece.DashId, 0, 3);

            Assert.False(u[0].IsAlive);
            Assert.Equal(10, u[3].Stats.CurrentHp);
        }

        // 사용자 확정: 이동 불가 상태면 시전 불가, 이동 거리 제한은 넘어서 사용 가능
        [Fact]
        public void Dash_BlockedByRoot_ButIgnoresDistanceLimit()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(WarriorPiece.DashId))
                .Place(Team.Player2, 6, 6)
                .Start();

            Statuses.StatusSystem.Apply(state, StatusLibrary.DistanceLimit(maxCells: 1, targetTurns: 1), u[0], u[1]);
            Assert.Equal(SkillFailReason.None, Check(state, u[0], WarriorPiece.DashId, 0, 3));

            Statuses.StatusSystem.Apply(state, StatusLibrary.Root(targetTurns: 1, blocksExternalMoves: false), u[0], u[1]);
            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], WarriorPiece.DashId, 0, 3));
        }

        #endregion

        #region Guard

        private static readonly Statuses.StatusDefinition GuardStatus = StatusLibrary.Guard(maxDistance: 2);

        private static void Hit(GameState state, Unit target, int amount, DamageType type = DamageType.Direct) =>
            DamageSystem.Apply(state, new DamageRequest(null, target, amount, type));

        // 사용자 확정: 비율 분담 없이 대상이 받을 직접 피해를 호위자가 전부 대신 받는다
        [Fact]
        public void Guard_GuardianTakesAllDirectDamageInsteadOfAlly()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChainGuardPiece.GuardId))
                .Place(Team.Player1, 1, 1)
                .Place(Team.Player2, 2, 2)
                .Start();

            Use(state, u[0], ChainGuardPiece.GuardId, 1, 1);
            EndTurn(state);
            new AttackAction(u[2], u[1]).Execute(state);

            Assert.Equal(10, u[1].Stats.CurrentHp);
            Assert.Equal(7, u[0].Stats.CurrentHp);
            Assert.Contains(state.Events.Since(0), e => e is UnitDamagedEvent d && d.Target == u[0] && d.Type == DamageType.Redirected);
        }

        [Fact]
        public void Guard_CannotTargetSelfOrEnemy()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChainGuardPiece.GuardId))
                .Place(Team.Player2, 1, 1)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ChainGuardPiece.GuardId, 0, 0));
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ChainGuardPiece.GuardId, 1, 1));
        }

        [Fact]
        public void Guard_OnlyRedirectsWhileWithinMaxDistanceAtHitTime()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            Statuses.StatusSystem.Apply(state, GuardStatus, u[1], u[0]);

            new MoveAction(u[1], new Position(3, 0)).Execute(state); // 거리 3
            Hit(state, u[1], 2);
            Assert.Equal(8, u[1].Stats.CurrentHp);

            new MoveAction(u[1], new Position(2, 0)).Execute(state); // 다시 거리 2
            Hit(state, u[1], 2);
            Assert.Equal(8, u[1].Stats.CurrentHp);
            Assert.Equal(8, u[0].Stats.CurrentHp);
        }

        [Theory]
        [InlineData(DamageType.DoT)]
        [InlineData(DamageType.Area)]
        public void Guard_DoesNotRedirectNonDirectDamage(DamageType type)
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            Statuses.StatusSystem.Apply(state, GuardStatus, u[1], u[0]);

            Hit(state, u[1], 2, type);

            Assert.Equal(8, u[1].Stats.CurrentHp);
            Assert.Equal(10, u[0].Stats.CurrentHp);
        }

        [Fact]
        public void Guard_LastsThroughEnemyTurn_AndEndsAtGuardiansNextTurnStart()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(ChainGuardPiece.GuardId))
                .Place(Team.Player1, 1, 1)
                .Place(Team.Player2, 6, 6)
                .Start();

            Use(state, u[0], ChainGuardPiece.GuardId, 1, 1);
            EndTurn(state); // P2 턴: 유지
            Assert.NotNull(u[1].FindStatus(StatusLibrary.GuardId));

            EndTurn(state); // P1 턴 시작: 해제
            Assert.Null(u[1].FindStatus(StatusLibrary.GuardId));
            Assert.Empty(state.DamageInterceptors);
        }

        [Fact]
        public void Guard_EndsImmediatelyWhenGuardianDies()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, TestGame.Stats(hp: 2))
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player1, 6, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            Statuses.StatusSystem.Apply(state, GuardStatus, u[1], u[0]);

            Hit(state, u[1], 5); // 호위자가 대신 받고 사망

            Assert.False(u[0].IsAlive);
            Assert.Equal(10, u[1].Stats.CurrentHp);
            Assert.Null(u[1].FindStatus(StatusLibrary.GuardId));
            Assert.Empty(state.DamageInterceptors);

            Hit(state, u[1], 2);
            Assert.Equal(8, u[1].Stats.CurrentHp);
        }

        // 사용자 확정: 호위자당 1명, 대상당 1명
        [Fact]
        public void Guard_OneTargetPerGuardian_AndOneGuardianPerTarget()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            Statuses.StatusSystem.Apply(state, GuardStatus, u[1], u[0]);
            Statuses.StatusSystem.Apply(state, GuardStatus, u[2], u[0]); // 같은 호위자가 다른 대상에게
            Assert.Null(u[1].FindStatus(StatusLibrary.GuardId));
            Assert.Single(state.DamageInterceptors);

            Statuses.StatusSystem.Apply(state, GuardStatus, u[2], u[1]); // 다른 호위자가 같은 대상에게
            Assert.Same(u[1], u[2].FindStatus(StatusLibrary.GuardId).Source);
            Assert.Single(state.DamageInterceptors);
        }

        // 명세 12.2 [설계안]: 전가된 피해는 다시 다른 호위에 전가하지 않는다
        [Fact]
        public void Guard_RedirectedDamageIsNotRedirectedAgain()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            Statuses.StatusSystem.Apply(state, GuardStatus, u[1], u[0]); // u0 가 u1 호위
            Statuses.StatusSystem.Apply(state, GuardStatus, u[0], u[2]); // u2 가 u0 호위

            Hit(state, u[1], 3);

            Assert.Equal(10, u[1].Stats.CurrentHp);
            Assert.Equal(7, u[0].Stats.CurrentHp);
            Assert.Equal(10, u[2].Stats.CurrentHp);
        }

        #endregion

        #region Warp

        private static UseSkillAction Warp(Unit caster, params (int X, int Y)[] targets) =>
            new UseSkillAction(caster, SeamstressPiece.WarpId, targets.Select(t => new Position(t.X, t.Y)).ToList());

        [Fact]
        public void Warp_SwapsTwoAlliesInRange()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 1, 1)
                .Place(Team.Player1, 3, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            SkillResult result = Warp(u[0], (1, 1), (3, 0)).Execute(state);

            Assert.Equal(new Position(3, 0), u[1].Position);
            Assert.Equal(new Position(1, 1), u[2].Position);
            Assert.Equal(new Position(0, 0), u[0].Position);
            Assert.Equal(2, result.Events.OfType<UnitMovedEvent>().Count(e => e.Kind == MoveKind.Swap));

            // 지정 칸 전부가 결과와 시전 이벤트에 남는다
            var targets = new[] { new Position(1, 1), new Position(3, 0) };
            Assert.Equal(targets, result.Targets);
            Assert.Equal(targets, result.Events.OfType<SkillUsedEvent>().Single().Targets);
        }

        [Fact]
        public void Warp_CanSwapCasterItself()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 2, 2)
                .Place(Team.Player2, 6, 6)
                .Start();

            Warp(u[0], (0, 0), (2, 2)).Execute(state);

            Assert.Equal(new Position(2, 2), u[0].Position);
            Assert.Equal(new Position(0, 0), u[1].Position);
        }

        [Fact]
        public void Warp_RejectsEnemyOutOfRangeDuplicateOrWrongCount()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player1, 4, 0)
                .Place(Team.Player2, 2, 0)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Warp(u[0], (1, 0), (2, 0)).Validate(state)); // 적
            Assert.Equal(SkillFailReason.InvalidTarget, Warp(u[0], (1, 0), (4, 0)).Validate(state)); // 사거리 밖
            Assert.Equal(SkillFailReason.InvalidTarget, Warp(u[0], (1, 0), (1, 0)).Validate(state)); // 같은 유닛
            Assert.Equal(SkillFailReason.InvalidTarget, Warp(u[0], (1, 0)).Validate(state));         // 하나만
            Assert.Equal(SkillFailReason.None, Warp(u[0], (1, 0), (0, 0)).Validate(state));
        }

        [Fact]
        public void Warp_StepwiseCandidates_ExcludeAlreadyChosen()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            var first = SkillQueries.GetValidTargets(state, u[0], SeamstressPiece.WarpId);
            var second = SkillQueries.GetValidTargets(state, u[0], SeamstressPiece.WarpId, new[] { new Position(1, 0) });
            var done = SkillQueries.GetValidTargets(state, u[0], SeamstressPiece.WarpId, new[] { new Position(1, 0), new Position(0, 0) });

            Assert.Equal(new[] { new Position(0, 0), new Position(1, 0) }.ToHashSet(), first.ToHashSet());
            Assert.Equal(new[] { new Position(0, 0) }, second);
            Assert.Empty(done);
        }

        // 명세 11.2: 이미 공격한 아군도 옮길 수 있고(이동 잠금 예외), 전투 행동은 회복되지 않는다
        [Fact]
        public void Warp_MovesAllyThatAlreadyAttacked_WithoutRestoringItsCombatAction()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 1, 1)
                .Place(Team.Player2, 2, 2)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            new AttackAction(u[1], u[2]).Execute(state);

            Warp(u[0], (1, 1), (0, 0)).Execute(state);

            Assert.Equal(new Position(0, 0), u[1].Position);
            Assert.Equal(AttackFailReason.AlreadyActed, new AttackAction(u[1], u[2]).Validate(state));
        }

        [Fact]
        public void Warp_TriggersCellEffectsAtBothDestinations()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            var atCaster = new TestCellEffect();
            var atAlly = new TestCellEffect();
            CellEffectSystem.Place(state, new Position(0, 0), atCaster);
            CellEffectSystem.Place(state, new Position(2, 0), atAlly);

            Warp(u[0], (0, 0), (2, 0)).Execute(state);

            Assert.Equal((1, 1), (atCaster.EnterCount, atCaster.StopCount));
            Assert.Equal((1, 1), (atAlly.EnterCount, atAlly.StopCount));
        }

        [Fact]
        public void Warp_RootedCasterCannotSwapItself_ButRootedAllyCanBeMoved()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            Statuses.StatusDefinition root = StatusLibrary.Root(targetTurns: 1, blocksExternalMoves: false);
            Statuses.StatusSystem.Apply(state, root, u[0], u[3]);
            Statuses.StatusSystem.Apply(state, root, u[1], u[3]);

            Assert.Equal(SkillFailReason.InvalidTarget, Warp(u[0], (0, 0), (2, 0)).Validate(state));
            Assert.Equal(SkillFailReason.None, Warp(u[0], (1, 0), (2, 0)).Validate(state));
        }

        #endregion

        #region Slash

        // 사용자 확정: 사거리 안(대각선 포함) 선택 칸 기준 가로 4칸, 플레이어가 시계 방향으로 회전, 범위 안 적 전원, 벽에 막히지 않음
        [Fact]
        public void Slash_HitsEveryEnemyInHorizontal4CellsAtSelectedCell_ThroughWalls()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", ".......", ".......", ".......", "...#...", ".......")
                .Place(Team.Player1, 3, 0, With(ScythePiece.SlashId))
                .Place(Team.Player2, 2, 2) // 범위 안
                .Place(Team.Player2, 5, 2) // 범위 안 (끝 칸)
                .Place(Team.Player2, 6, 2) // 범위 밖
                .Place(Team.Player1, 4, 2) // 범위 안 아군
                .Start();

            SkillResult result = Use(state, u[0], ScythePiece.SlashId, 3, 2);

            Assert.Equal(
                new[] { new Position(2, 2), new Position(3, 2), new Position(4, 2), new Position(5, 2) }.ToHashSet(),
                result.AffectedCells.ToHashSet());
            Assert.Equal(new[] { 7, 7, 10, 10 }, u.Skip(1).Select(unit => unit.Stats.CurrentHp));
        }

        [Fact]
        public void Slash_AllowsDiagonalCellsInRange_AndRotatesByPlayerChoice()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 3, With(ScythePiece.SlashId))
                .Place(Team.Player2, 5, 3) // 회전 범위 안
                .Place(Team.Player2, 6, 5) // 회전 전 범위에만 있음
                .Start();

            Assert.Equal(SkillFailReason.None, Check(state, u[0], ScythePiece.SlashId, 5, 5));          // 대각선
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ScythePiece.SlashId, 6, 3)); // 사거리 초과
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ScythePiece.SlashId, 3, 3)); // 자기 칸

            // (5,5) 기준 시계 90도 → 세로 (5,6)~(5,3)
            SkillResult result = new UseSkillAction(u[0], ScythePiece.SlashId, new Position(5, 5), AreaRotation.R90).Execute(state);

            Assert.Equal(AreaRotation.R90, result.Rotation);
            Assert.Equal(7, u[1].Stats.CurrentHp);
            Assert.Equal(10, u[2].Stats.CurrentHp);
        }

        #endregion

        #region Mortar - Bombard

        private static UnitBaseStats Mortar() =>
            TestGame.Stats("mortar", skills: new[] { MortarPiece.BombardId, MortarPiece.ShockShellId, MortarPiece.UninstallId });

        /// <summary>박격포를 설치 상태로 만든다(보호막 없이 상태만).</summary>
        private static void InstallMortar(GameState state, Unit mortar) =>
            Statuses.StatusSystem.Apply(state, StatusLibrary.Installed(), mortar, mortar);

        // 사용자 확정: 해체 상태에서 포격하면 설치 + 보호막, 다음 내 턴 시작에 착탄
        [Fact]
        public void Bombard_WhenNotInstalled_InstallsWithShield_AndLandsAtNextTurnStart()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Mortar())
                .Place(Team.Player2, 3, 3)
                .Place(Team.Player2, 3, 4) // 범위 안 (십자 위)
                .Place(Team.Player2, 4, 4) // 범위 밖 (대각선)
                .Place(Team.Player1, 2, 3) // 범위 안 아군
                .Start();

            Use(state, u[0], MortarPiece.BombardId, 3, 3);

            Assert.NotNull(u[0].FindStatus(StatusLibrary.InstalledId));
            Assert.Equal(2, u[0].Stats.Shield);
            Assert.Equal(10, u[1].Stats.CurrentHp);
            Assert.Single(state.ScheduledStrikes);

            EndTurn(state); // P2 턴: 아직 착탄 안 함
            Assert.Equal(10, u[1].Stats.CurrentHp);

            EndTurn(state); // P1 턴 시작: 착탄
            Assert.Equal(4, u[1].Stats.CurrentHp);
            Assert.Equal(4, u[2].Stats.CurrentHp);
            Assert.Equal(10, u[3].Stats.CurrentHp);
            Assert.Equal(10, u[4].Stats.CurrentHp);
            Assert.Empty(state.ScheduledStrikes);
            Assert.Contains(state.Events.Since(0), e => e is UnitDamagedEvent d && d.Target == u[1] && d.Type == DamageType.Area);
        }

        // 사용자 확정: 착탄한 턴에는 다시 포격 또는 해체만 남는다. 다시 포격해도 보호막은 재충전되지 않는다
        [Fact]
        public void Bombard_LandingTurn_LeavesOnlyBombardOrUninstall()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Mortar())
                .Place(Team.Player2, 1, 1)
                .Start();
            Use(state, u[0], MortarPiece.BombardId, 4, 4);
            EndTurn(state);
            Hit(state, u[0], 1); // 보호막 2 → 1
            EndTurn(state); // 착탄

            Assert.False(u[0].TurnState.CombatActionUsed); // 착탄은 행동이 아니다
            Assert.Equal(SkillFailReason.None, Check(state, u[0], MortarPiece.BombardId, 4, 4));
            Assert.Equal(SkillFailReason.None, Check(state, u[0], MortarPiece.UninstallId, 0, 0));
            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], MortarPiece.ShockShellId, 1, 1));
            Assert.Equal(MoveFailReason.Installed, new MoveAction(u[0], new Position(0, 1)).Validate(state));
            Assert.Equal(AttackFailReason.BlockedByStatus, new AttackAction(u[0], u[1]).Validate(state));

            Use(state, u[0], MortarPiece.BombardId, 4, 4);
            Assert.Equal(1, u[0].Stats.Shield);
        }

        // 사용자 확정: 착탄 위치는 조준한 칸에 고정, 예상하고 움직이면 피할 수 있다
        [Fact]
        public void Bombard_EnemyMovingOutOfAimedArea_Dodges()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Mortar())
                .Place(Team.Player2, 3, 3)
                .Start();

            Use(state, u[0], MortarPiece.BombardId, 3, 3);
            EndTurn(state);
            new MoveAction(u[1], new Position(3, 6)).Execute(state);
            EndTurn(state);

            Assert.Equal(10, u[1].Stats.CurrentHp);
        }

        // 사용자 확정: 착탄 전에 시전자가 죽으면 예약 취소
        [Fact]
        public void Bombard_IsCancelledWhenCasterDies()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Mortar())
                .Place(Team.Player1, 6, 0)
                .Place(Team.Player2, 3, 3)
                .Start();

            Use(state, u[0], MortarPiece.BombardId, 3, 3);
            EndTurn(state);
            DamageSystem.Apply(state, new DamageRequest(u[2], u[0], 99, DamageType.Direct));

            Assert.Empty(state.ScheduledStrikes);
            Assert.Contains(state.Events.Since(0), e => e is StrikeCancelledEvent);

            EndTurn(state);
            Assert.Equal(10, u[2].Stats.CurrentHp);
        }

        [Fact]
        public void Bombard_AllowsOneActiveStrikePerCaster()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Mortar())
                .Place(Team.Player2, 6, 6)
                .Start();
            var condition = new Skills.Conditions.MaxActiveStrikesCondition(MortarPiece.BombardId, 1);

            Assert.True(condition.IsMet(state, u[0]));
            Use(state, u[0], MortarPiece.BombardId, 3, 3);
            Assert.False(condition.IsMet(state, u[0]));
        }

        #endregion

        #region Mortar - Uninstall

        // 사용자 확정: 해체는 설치 중에만 가능한 전투 행동. 설치와 보호막이 사라지고, 다음 턴부터 이동/충격탄 가능
        [Fact]
        public void Uninstall_RemovesInstallAndShield_AsCombatAction()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Mortar())
                .Place(Team.Player2, 1, 2)
                .Start();
            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], MortarPiece.UninstallId, 0, 0));

            Use(state, u[0], MortarPiece.BombardId, 4, 4);
            EndTurn(state);
            EndTurn(state);

            Use(state, u[0], MortarPiece.UninstallId, 0, 0);
            Assert.Null(u[0].FindStatus(StatusLibrary.InstalledId));
            Assert.Equal(0, u[0].Stats.Shield);
            Assert.Equal(SkillFailReason.AlreadyActed, Check(state, u[0], MortarPiece.ShockShellId, 1, 2));
            Assert.Equal(MoveFailReason.AlreadyActed, new MoveAction(u[0], new Position(0, 1)).Validate(state));

            EndTurn(state);
            EndTurn(state);
            Assert.Equal(SkillFailReason.None, Check(state, u[0], MortarPiece.ShockShellId, 1, 2));
            Assert.Equal(MoveFailReason.None, new MoveAction(u[0], new Position(0, 1)).Validate(state));
        }

        // 사용자 확정: 설치 중에도 외부 강제 이동(워프 교환 등)은 허용
        [Fact]
        public void Installed_MortarCanStillBeSwappedByAlly()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.WarpId))
                .Place(Team.Player1, 1, 0, Mortar())
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();
            InstallMortar(state, u[1]);

            Warp(u[0], (1, 0), (2, 0)).Execute(state);

            Assert.Equal(new Position(2, 0), u[1].Position);
        }

        #endregion

        #region Mortar - Shock shell / Knockback

        // 사용자 확정(코어 규칙): 세로 차이 >= 가로 차이면 상하, 가로 차이가 더 크면 좌우. 미는 쪽에서 멀어지는 방향
        [Theory]
        [InlineData(5, 4, 1, 0)]   // 가로 2 > 세로 1 → 오른쪽
        [InlineData(4, 5, 0, 1)]   // 세로 2 > 가로 1 → 위
        [InlineData(5, 5, 0, 1)]   // 같으면 상하 → 위
        [InlineData(1, 1, 0, -1)]  // 같으면 상하 → 아래
        [InlineData(0, 4, -1, 0)]  // 가로 3 > 세로 1 → 왼쪽
        public void Knockback_Direction_FollowsAxisWithLargerGap_TiesGoVertical(int targetX, int targetY, int expectedX, int expectedY)
        {
            Assert.Equal((expectedX, expectedY), Knockback.GetDirection(new Position(3, 3), new Position(targetX, targetY)));
        }

        [Fact]
        public void ShockShell_DamagesAndPushesEnemyAway()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 1, Mortar())
                .Place(Team.Player2, 4, 2)
                .Start();

            SkillResult result = Use(state, u[0], MortarPiece.ShockShellId, 4, 2);

            Assert.Equal(9, u[1].Stats.CurrentHp);
            Assert.Equal(new Position(4, 4), u[1].Position); // 가로 1 = 세로 1 → 위로 2칸
            Assert.All(result.Events.OfType<UnitMovedEvent>(), e => Assert.Equal(MoveKind.Knockback, e.Kind));
        }

        // 사용자 확정: 벽/유닛/장애물/보드 끝에 막히면 그 앞에서 멈추고 추가 피해 없음
        [Fact]
        public void ShockShell_StopsBeforeBlocker_WithoutExtraDamage()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", "...#...", ".......", ".......", ".......", ".......")
                .WithRules(r => r.Ap.StartAp = 6)
                .Place(Team.Player1, 3, 1, Mortar())
                .Place(Team.Player2, 3, 2)
                .Place(Team.Player1, 0, 4, Mortar())
                .Place(Team.Player2, 0, 5)
                .Start();

            Use(state, u[0], MortarPiece.ShockShellId, 3, 2); // 벽 (3,4) 앞
            Use(state, u[2], MortarPiece.ShockShellId, 0, 5); // 보드 끝 (0,6)

            Assert.Equal(new Position(3, 3), u[1].Position);
            Assert.Equal(new Position(0, 6), u[3].Position);
            Assert.Equal(9, u[1].Stats.CurrentHp);
            Assert.Equal(9, u[3].Stats.CurrentHp);
        }

        #endregion

        #region Obstacle

        // 명세 11.3: 장애물은 이동을 막지만 공격/스킬 사거리는 막지 않는다
        [Fact]
        public void Obstacle_BlocksMovementAndDash_ButNotAttackRange()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.ObstacleId))
                .Place(Team.Player2, 0, 3, TestGame.Stats(range: 3))
                .Place(Team.Player2, 0, 5, With(WarriorPiece.DashId))
                .Start();

            Use(state, u[0], SeamstressPiece.ObstacleId, 0, 2);
            EndTurn(state);

            Assert.True(state.Board.GetCell(new Position(0, 2)).HasObstacle);
            Assert.Equal(MoveFailReason.PathBlocked, new MoveAction(u[1], new Position(0, 1)).Validate(state));
            Assert.Equal(AttackFailReason.None, new AttackAction(u[1], u[0]).Validate(state));

            new MoveAction(u[1], new Position(1, 3)).Execute(state); // 돌진 경로 비우기
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[2], WarriorPiece.DashId, 0, 2)); // 장애물 칸
            Assert.Equal(SkillFailReason.None, Check(state, u[2], WarriorPiece.DashId, 0, 3));
        }

        // 사용자 확정: 양 팀 모두 기본 공격 1회로 부술 수 있고, AP 와 전투 행동을 쓴다
        [Fact]
        public void Obstacle_IsDestroyedByOneBasicAttack_FromEitherTeam()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.ObstacleId))
                .Place(Team.Player1, 1, 1)
                .Place(Team.Player2, 1, 3)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], SeamstressPiece.ObstacleId, 0, 2);
            Assert.Equal(AttackFailReason.None, new AttackObstacleAction(u[1], new Position(0, 2)).Validate(state)); // 아군

            EndTurn(state);
            int apBefore = state.CurrentPlayer.Ap.Current;
            AttackObstacleResult result = new AttackObstacleAction(u[2], new Position(0, 2)).Execute(state); // 적

            Assert.True(result.Destroyed);
            Assert.False(state.Board.GetCell(new Position(0, 2)).HasObstacle);
            Assert.Equal(apBefore - 2, state.CurrentPlayer.Ap.Current);
            Assert.Equal(AttackFailReason.AlreadyActed, new AttackAction(u[2], u[1]).Validate(state));
        }

        [Fact]
        public void Obstacle_AttackRequiresRangeAndAnObstacle()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.ObstacleId))
                .Place(Team.Player1, 6, 0)
                .Place(Team.Player2, 6, 6)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], SeamstressPiece.ObstacleId, 0, 2);

            Assert.Equal(AttackFailReason.OutOfRange, new AttackObstacleAction(u[1], new Position(0, 2)).Validate(state));
            Assert.Equal(AttackFailReason.InvalidTarget, new AttackObstacleAction(u[1], new Position(5, 0)).Validate(state));
        }

        // 명세 11.3: 점령 칸에는 생성 불가. 유닛이 있는 칸에도 불가
        [Fact]
        public void Obstacle_CannotBePlacedOnCaptureTileOrOccupiedCell()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", ".......", "...C...", ".......", ".......", ".......")
                .Place(Team.Player1, 3, 1, With(SeamstressPiece.ObstacleId))
                .Place(Team.Player2, 3, 2)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], SeamstressPiece.ObstacleId, 3, 3)); // 점령 칸
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], SeamstressPiece.ObstacleId, 3, 2)); // 유닛
            Assert.Equal(SkillFailReason.None, Check(state, u[0], SeamstressPiece.ObstacleId, 2, 2));
        }

        [Fact]
        public void Obstacle_AllowsOneActive_AndExpiresAfterLifetime()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, With(SeamstressPiece.ObstacleId))
                .Place(Team.Player2, 6, 6)
                .Start();
            Use(state, u[0], SeamstressPiece.ObstacleId, 0, 2);
            EndTurn(state); // P1 종료: 설치 턴은 세지 않음
            EndTurn(state);

            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], SeamstressPiece.ObstacleId, 2, 0));

            EndTurn(state); // P1 종료: 수명 2 → 1
            EndTurn(state);
            EndTurn(state); // P1 종료: 수명 0, 제거
            Assert.False(state.Board.GetCell(new Position(0, 2)).HasObstacle);
        }

        #endregion

        #region Shadow clone

        private static UnitBaseStats Scythe(int range = 1) =>
            TestGame.Stats("scythe", range: range, skills: new[] { ScythePiece.SlashId, ScythePiece.ShadowCloneId });

        private static Unit CloneOf(GameState state, Unit owner) =>
            state.GetPlayer(owner.Team).Units.Single(unit => unit.SummonOwner == owner && unit.IsAlive);

        // 명세 4.3 + 사용자 확정: 소환한 턴에 본체는 행동 종료, 분신은 그 턴부터 팀 AP 로 이동/공격
        [Fact]
        public void ShadowClone_SummonsAdjacent_EndsOwnerActions_CloneActsSameTurnWithTeamAp()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe())
                .Place(Team.Player2, 3, 3)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();

            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            Unit clone = CloneOf(state, u[0]);

            Assert.True(clone.IsSummon);
            Assert.Equal(new Position(3, 1), clone.Position);
            Assert.Equal(1, clone.Stats.CurrentHp);
            Assert.Equal(MoveFailReason.ActionsEnded, new MoveAction(u[0], new Position(2, 0)).Validate(state));

            new MoveAction(clone, new Position(3, 2)).Execute(state);
            AttackResult attack = new AttackAction(clone, u[1]).Execute(state);

            Assert.Equal(0, attack.Damage);
            Assert.NotNull(u[1].FindStatus(StatusLibrary.CurseMarkId));
            Assert.Equal(0, state.CurrentPlayer.Ap.Current); // 6 - 소환 3 - 이동 1 - 공격 2
        }

        [Fact]
        public void ShadowClone_TargetsEmptyNeighborsIncludingDiagonals()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe())
                .Place(Team.Player1, 2, 0)
                .Place(Team.Player2, 6, 6)
                .Start();

            Assert.Equal(SkillFailReason.None, Check(state, u[0], ScythePiece.ShadowCloneId, 4, 1));          // 대각선
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ScythePiece.ShadowCloneId, 3, 2)); // 2칸
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ScythePiece.ShadowCloneId, 2, 0)); // 유닛
            Assert.Equal(SkillFailReason.None, Check(state, u[0], ScythePiece.ShadowCloneId, 4, 0));
        }

        [Fact]
        public void ShadowClone_OnlyOneAlive_CanResummonAfterItDies()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe())
                .Place(Team.Player2, 6, 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            EndTurn(state);
            EndTurn(state);

            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], ScythePiece.ShadowCloneId, 4, 0));

            DamageSystem.Apply(state, new DamageRequest(null, CloneOf(state, u[0]), 1, DamageType.Direct));
            Assert.Equal(SkillFailReason.None, Check(state, u[0], ScythePiece.ShadowCloneId, 4, 0));
        }

        // 사용자 확정: 본체가 죽으면 분신도 사라진다. 분신이 죽어도 이미 건 표식은 유지(명세 4.5)
        [Fact]
        public void ShadowClone_DiesWithOwner_ButMarkSurvivesCloneDeath()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe())
                .Place(Team.Player1, 6, 0)
                .Place(Team.Player2, 3, 2)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            Unit clone = CloneOf(state, u[0]);
            new AttackAction(clone, u[2]).Execute(state);

            DamageSystem.Apply(state, new DamageRequest(null, u[0], 99, DamageType.Direct));

            Assert.False(clone.IsAlive);
            Assert.False(clone.IsPlaced);
            Assert.NotNull(u[2].FindStatus(StatusLibrary.CurseMarkId));
        }

        // 사용자 확정: 분신은 점령을 진행하지 못한다
        [Fact]
        public void ShadowClone_OnCaptureTile_DoesNotProgressCapture()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", ".......", "...C...", ".......", ".......", ".......")
                .Place(Team.Player1, 3, 2, Scythe())
                .Place(Team.Player2, 6, 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 3); // 점령 칸에 소환
            EndTurn(state);
            EndTurn(state);

            Assert.Equal(0, state.Capture.GetProgress(Team.Player1));
        }

        #endregion

        #region Curse mark

        // 명세 4.5 설계안 + 사용자 확정: 본체의 유효 타격 시 표식 소모, 추가 피해 1회
        [Fact]
        public void CurseMark_OwnerBasicAttack_ConsumesMarkAndAddsBonus()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe(range: 2))
                .Place(Team.Player2, 3, 2)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            new AttackAction(CloneOf(state, u[0]), u[1]).Execute(state);
            EndTurn(state);
            EndTurn(state);

            AttackResult result = new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(5, u[1].Stats.CurrentHp); // 기본 3 + 표식 2
            Assert.Null(u[1].FindStatus(StatusLibrary.CurseMarkId));
            Assert.Contains(result.Events, e => e is UnitDamagedEvent d && d.Type == DamageType.MarkBonus);
        }

        [Fact]
        public void CurseMark_SlashFromOwner_AlsoConsumesMark()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe())
                .Place(Team.Player2, 3, 2)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 4, 0);
            Unit clone = CloneOf(state, u[0]);
            new MoveAction(clone, new Position(4, 1)).Execute(state);
            EndTurn(state);
            EndTurn(state);
            new AttackAction(clone, u[1]).Execute(state);

            Use(state, u[0], ScythePiece.SlashId, 3, 2);

            Assert.Equal(5, u[1].Stats.CurrentHp); // 베기 3 + 표식 2
            Assert.Null(u[1].FindStatus(StatusLibrary.CurseMarkId));
        }

        [Fact]
        public void CurseMark_OtherAttackers_DoNotTriggerIt()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, Scythe())
                .Place(Team.Player1, 2, 2)
                .Place(Team.Player2, 3, 2)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            new AttackAction(CloneOf(state, u[0]), u[2]).Execute(state);
            state.GetPlayer(Team.Player1).Ap.Recover(6);

            new AttackAction(u[1], u[2]).Execute(state);

            Assert.Equal(7, u[2].Stats.CurrentHp);
            Assert.NotNull(u[2].FindStatus(StatusLibrary.CurseMarkId));
        }

        #endregion

        #region Clone swap

        private static UnitBaseStats ScytheWithSwap() =>
            TestGame.Stats("scythe", skills: new[] { ScythePiece.SlashId, ScythePiece.ShadowCloneId, ScythePiece.CloneSwapId });

        [Fact]
        public void CloneSwap_IsAnExtraAbility_NotASkillSlot()
        {
            CharacterDefinition scythe = PieceModules.CreateDefinitions().Single(c => c.Id == ScythePiece.PieceId);

            Assert.DoesNotContain(ScythePiece.CloneSwapId, scythe.SkillSlots);
            Assert.Contains(ScythePiece.CloneSwapId, scythe.ExtraSkillIds);
        }

        // 명세 4.4 + 사용자 확정: AP 1, 거리 무제한, 교환 후 본체 공격 가능
        [Fact]
        public void CloneSwap_SwapsWithFarClone_ThenOwnerCanStillAttack()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, ScytheWithSwap())
                .Place(Team.Player2, 3, 5)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            Unit clone = CloneOf(state, u[0]);
            new MoveAction(clone, new Position(3, 4)).Execute(state);
            EndTurn(state);
            EndTurn(state);
            int apBefore = state.CurrentPlayer.Ap.Current;

            Use(state, u[0], ScythePiece.CloneSwapId, 3, 4);

            Assert.Equal(new Position(3, 4), u[0].Position);
            Assert.Equal(new Position(3, 0), clone.Position);
            Assert.Equal(apBefore - 1, state.CurrentPlayer.Ap.Current);
            Assert.Equal(AttackFailReason.None, new AttackAction(u[0], u[1]).Validate(state));
        }

        // 명세 4.4: 공격/스킬 후에는 교환 불가, 소환 직후 행동 불능을 교환으로 우회 불가
        [Fact]
        public void CloneSwap_NotOnSummonTurn_NorAfterAttacking()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, ScytheWithSwap())
                .Place(Team.Player2, 4, 1)
                .WithRules(r => r.Ap.StartAp = 6)
                .Start();
            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            Assert.Equal(SkillFailReason.ActionsEnded, Check(state, u[0], ScythePiece.CloneSwapId, 3, 1));

            EndTurn(state);
            EndTurn(state);
            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(SkillFailReason.MoveLocked, Check(state, u[0], ScythePiece.CloneSwapId, 3, 1));
        }

        [Fact]
        public void CloneSwap_RequiresLivingClone_AndUnrootedOwner()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 0, ScytheWithSwap())
                .Place(Team.Player2, 6, 6)
                .Start();
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], ScythePiece.CloneSwapId, 3, 1)); // 분신 없음

            Use(state, u[0], ScythePiece.ShadowCloneId, 3, 1);
            EndTurn(state);
            EndTurn(state);
            Statuses.StatusSystem.Apply(state, StatusLibrary.Root(targetTurns: 1, blocksExternalMoves: false), u[0], u[1]);

            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], ScythePiece.CloneSwapId, 3, 1));
        }

        #endregion

        #region Assassin

        private static UnitBaseStats Assassin() =>
            TestGame.Stats("assassin", hp: 9, attack: 4, range: 1, skills: new[] { AssassinPiece.LeapId, AssassinPiece.ChargeId });

        // 사용자 확정: 사거리 4 안의 빈 칸으로 순간이동(벽/유닛을 넘음), 이동 계열이라 도약 후 기본 공격 가능
        [Fact]
        public void Leap_TeleportsOverWallsToEmptyCellInRange_ThenCanAttack()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", ".......", ".......", ".......", "#######", ".......")
                .WithRules(r => r.Ap.StartAp = 6)
                .Place(Team.Player1, 0, 0, Assassin())
                .Place(Team.Player2, 2, 4)
                .Start();

            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], AssassinPiece.LeapId, 2, 4)); // 유닛 있는 칸
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], AssassinPiece.LeapId, 1, 1)); // 벽
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[0], AssassinPiece.LeapId, 0, 5)); // 사거리 초과

            SkillResult result = Use(state, u[0], AssassinPiece.LeapId, 2, 3); // 벽 줄 너머

            Assert.Equal(new Position(2, 3), u[0].Position);
            Assert.Equal(MoveKind.Warp, result.Events.OfType<UnitMovedEvent>().Single().Kind);
            Assert.False(u[0].TurnState.CombatActionUsed);

            new AttackAction(u[0], u[1]).Execute(state);
            Assert.Equal(6, u[1].Stats.CurrentHp);
        }

        [Fact]
        public void Leap_IsBlockedWhenRooted_OrAfterCombatAction()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, Assassin())
                .Place(Team.Player1, 6, 0, Assassin())
                .Place(Team.Player2, 6, 1)
                .Start();
            Statuses.StatusSystem.Apply(state, StatusLibrary.Root(1, false), u[0], u[2]);

            Assert.Equal(SkillFailReason.ConditionNotMet, Check(state, u[0], AssassinPiece.LeapId, 2, 2));

            new AttackAction(u[1], u[2]).Execute(state);
            Assert.Equal(SkillFailReason.MoveLocked, Check(state, u[1], AssassinPiece.LeapId, 4, 2));
        }

        // 사용자 확정: 차징 후 다음 내 턴의 기본 공격에 추가 피해. 보호막/호위 없는 궁수(체력 8)는 한 번에 쓰러진다
        [Fact]
        public void Charge_NextTurnBasicAttackDealsBonus_DownsUnprotectedArcher()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 3, Assassin())
                .Place(Team.Player2, 3, 4, TestGame.Stats(hp: 8))
                .Place(Team.Player2, 6, 6)
                .Start();

            Use(state, u[0], AssassinPiece.ChargeId, 3, 3);
            Assert.NotNull(u[0].FindStatus(StatusLibrary.ChargedId));
            Assert.Equal(AttackFailReason.AlreadyActed, new AttackAction(u[0], u[1]).Validate(state)); // 차징은 전투 행동

            EndTurn(state);
            EndTurn(state);
            AttackResult attack = new AttackAction(u[0], u[1]).Execute(state);

            Assert.True(attack.TargetDied);
            Assert.Equal(8, attack.Damage);
            Assert.Null(u[0].FindStatus(StatusLibrary.ChargedId));
        }

        // 사용자 확정: 차징은 다음 내 턴 끝까지 유지, 첫 기본 공격에 소모
        [Fact]
        public void Charge_IsConsumedByFirstAttack_OrExpiresAtEndOfNextOwnTurn()
        {
            var (state, u) = Game()
                .WithRules(r => r.Ap.StartAp = 6)
                .Place(Team.Player1, 3, 3, Assassin())
                .Place(Team.Player1, 0, 0, Assassin())
                .Place(Team.Player2, 3, 4, TestGame.Stats(hp: 20))
                .Start();
            Use(state, u[0], AssassinPiece.ChargeId, 3, 3);
            Use(state, u[1], AssassinPiece.ChargeId, 0, 0);
            EndTurn(state);
            EndTurn(state);

            Assert.Equal(8, new AttackAction(u[0], u[2]).Execute(state).Damage);
            Assert.Null(u[0].FindStatus(StatusLibrary.ChargedId));
            Assert.NotNull(u[1].FindStatus(StatusLibrary.ChargedId)); // 공격하지 않은 쪽은 이번 턴 동안 유지

            EndTurn(state);
            Assert.Null(u[1].FindStatus(StatusLibrary.ChargedId));
        }

        #endregion

        #region Sculptor

        private static UnitBaseStats Sculptor() =>
            TestGame.Stats("sculptor", skills: new[] { SculptorPiece.SealId, SculptorPiece.CorrectionId });

        private static void Seal(GameState state, Unit target, Unit source) =>
            Statuses.StatusSystem.Apply(state, StatusLibrary.Sealed(), target, source);

        private static UseSkillAction Correction(Unit caster, Position target, Position direction) =>
            new UseSkillAction(caster, SculptorPiece.CorrectionId, new[] { target, direction });

        // 기획 확정: 봉인 중 이동/기본 공격/스킬 불가, 아군·적 모두 대상으로 지정 불가
        [Fact]
        public void Seal_BlocksActions_AndMakesUntargetable()
        {
            var (state, u) = Game()
                .WithRules(r => { r.Ap.MaxAp = 12; r.Ap.StartAp = 12; })
                .Place(Team.Player1, 3, 3, Sculptor())
                .Place(Team.Player1, 2, 4, Sculptor())
                .Place(Team.Player2, 3, 4, Sculptor())
                .Start();

            Use(state, u[0], SculptorPiece.SealId, 3, 4);

            Assert.NotNull(u[2].FindStatus(StatusLibrary.SealedId));
            Assert.Equal(AttackFailReason.InvalidTarget, new AttackAction(u[1], u[2]).Validate(state));
            Assert.Equal(SkillFailReason.InvalidTarget, Check(state, u[1], SculptorPiece.SealId, 3, 4));
            Assert.Equal(SkillFailReason.InvalidTarget, Correction(u[1], new Position(3, 4), new Position(4, 4)).Validate(state));

            EndTurn(state);

            Assert.Equal(MoveFailReason.Sealed, new MoveAction(u[2], new Position(3, 5)).Validate(state));
            Assert.Equal(AttackFailReason.BlockedByStatus, new AttackAction(u[2], u[0]).Validate(state));
            Assert.Equal(SkillFailReason.BlockedByStatus, Check(state, u[2], SculptorPiece.SealId, 3, 3));
        }

        // 기획 확정: 장판/지속 피해를 포함한 모든 피해 무효. 회복도 받지 않는다(사용자 확정)
        [Fact]
        public void Seal_BlocksAllDamageAndHealing()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 3, Sculptor())
                .Place(Team.Player2, 3, 4, TestGame.Stats(hp: 10))
                .Start();
            DamageSystem.Apply(state, new DamageRequest(u[0], u[1], 3, DamageType.Direct));
            Statuses.StatusSystem.Apply(state, StatusLibrary.Burn(damagePerTrigger: 2, triggers: 2), u[1], u[0]);
            Seal(state, u[1], u[0]);

            DamageSystem.Apply(state, new DamageRequest(u[0], u[1], 5, DamageType.Direct));
            DamageSystem.Heal(state, u[0], u[1], 3);
            EndTurn(state);
            EndTurn(state); // 대상(P2) 턴 종료: 화상 발동은 무효, 봉인 해제

            Assert.Equal(7, u[1].Stats.CurrentHp);
            Assert.Null(u[1].FindStatus(StatusLibrary.SealedId));
            Assert.NotNull(u[1].FindStatus(StatusLibrary.BurnId)); // 다른 상태의 지속시간은 정상 감소
        }

        // 기획 제안: 대상 소유자의 다음 턴 종료에 해제. 자신/아군에게 걸면 상대 턴을 지나 내 다음 턴 끝까지
        [Fact]
        public void Seal_ReleasesAtEndOfTargetOwnersNextTurn()
        {
            var (state, u) = Game()
                .WithRules(r => { r.Ap.MaxAp = 12; r.Ap.StartAp = 12; })
                .Place(Team.Player1, 3, 3, Sculptor())
                .Place(Team.Player1, 0, 0, Sculptor())
                .Place(Team.Player2, 3, 4)
                .Start();
            Use(state, u[0], SculptorPiece.SealId, 3, 4);
            Use(state, u[1], SculptorPiece.SealId, 0, 0);

            EndTurn(state);
            Assert.NotNull(u[2].FindStatus(StatusLibrary.SealedId));
            EndTurn(state);
            Assert.Null(u[2].FindStatus(StatusLibrary.SealedId));
            Assert.NotNull(u[1].FindStatus(StatusLibrary.SealedId));
            EndTurn(state);
            Assert.Null(u[1].FindStatus(StatusLibrary.SealedId));
        }

        // 사용자 확정: 봉인된 호위자는 호위하지 못한다(피해는 원래 대상이 받음)
        [Fact]
        public void Seal_SealedGuardianDoesNotGuard()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 3, TestGame.Stats(hp: 10))
                .Place(Team.Player1, 3, 2, TestGame.Stats(hp: 10))
                .Place(Team.Player2, 3, 4, TestGame.Stats(attack: 3))
                .Start();
            Statuses.StatusSystem.Apply(state, StatusLibrary.Guard(maxDistance: 2), u[0], u[1]);
            Seal(state, u[1], u[2]);

            DamageSystem.Apply(state, new DamageRequest(u[2], u[0], 3, DamageType.Direct));

            Assert.Equal(7, u[0].Stats.CurrentHp);
            Assert.Equal(10, u[1].Stats.CurrentHp);
        }

        // 사용자 확정: 점령 칸 위에서 봉인되면 점령하지 않은 것으로 보고 진행도가 0 이 된다
        [Fact]
        public void Seal_OnCaptureTile_ResetsCaptureProgress()
        {
            var (state, u) = Game()
                .WithMap(".......", ".......", ".......", "...C...", ".......", ".......", ".......")
                .Place(Team.Player1, 3, 0, Sculptor())
                .Place(Team.Player2, 3, 3)
                .Start();
            EndTurn(state);
            Assert.Equal(1, state.Capture.GetProgress(Team.Player2));
            EndTurn(state);

            Use(state, u[0], SculptorPiece.SealId, 3, 3);
            EndTurn(state);

            Assert.Equal(0, state.Capture.GetProgress(Team.Player2));
            Assert.True(state.Capture.IsActive);
        }

        // 기획 확정: 고른 방향으로 1칸 강제 이동. 결과 예측 지원
        [Fact]
        public void Correction_PushesEnemyOneCellInChosenDirection_WithPreview()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 3, 3, Sculptor())
                .Place(Team.Player2, 3, 5, TestGame.Stats(hp: 10))
                .Start();
            Position target = new Position(3, 5), right = new Position(4, 5);

            Assert.Equal(4, SkillQueries.GetValidTargets(state, u[0], SculptorPiece.CorrectionId, new[] { target }).Count);
            Assert.Equal(right, SkillQueries.PredictPush(state, u[0], SculptorPiece.CorrectionId, new[] { target, right }));

            SkillResult result = Correction(u[0], target, right).Execute(state);

            Assert.Equal(right, u[1].Position);
            Assert.Equal(9, u[1].Stats.CurrentHp);
            Assert.Equal(MoveKind.Knockback, result.Events.OfType<UnitMovedEvent>().Single().Kind);
        }

        // 사용자 확정: 막힌 방향도 고를 수 있다(밀기 시도). 밀리지 않고 피해만, 다른 방향으로 바꾸지 않음
        [Fact]
        public void Correction_TowardBlockedCell_OnlyDamages()
        {
            var (state, u) = Game()
                .WithMap(".......", "..#....", ".......", ".......", ".......", ".......", ".......")
                .Place(Team.Player1, 3, 3, Sculptor())
                .Place(Team.Player2, 3, 5, TestGame.Stats(hp: 10))
                .Start();
            Position target = new Position(3, 5), wall = new Position(2, 5);
            int apBefore = state.GetPlayer(Team.Player1).Ap.Current;

            Assert.Equal(target, SkillQueries.PredictPush(state, u[0], SculptorPiece.CorrectionId, new[] { target, wall }));
            SkillResult result = Correction(u[0], target, wall).Execute(state);

            Assert.Equal(target, u[1].Position);
            Assert.Equal(9, u[1].Stats.CurrentHp);
            Assert.Empty(result.Events.OfType<UnitMovedEvent>());
            Assert.True(state.GetPlayer(Team.Player1).Ap.Current < apBefore);
        }

        #endregion
    }
}
