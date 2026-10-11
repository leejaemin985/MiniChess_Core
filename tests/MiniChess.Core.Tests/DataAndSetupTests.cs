using MiniChess.Core.Actions;
using MiniChess.Core.Characters;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Presets;
using MiniChess.Core.Setup;
using MiniChess.Core.State;
using MiniChess.Core.Tests.Support;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class DataAndSetupTests
    {
        #region Roster / preset

        [Fact]
        public void Roster_HasNineCharacters_WithSpecRoles_AndNoStats()
        {
            List<CharacterDefinition> roster = PieceModules.CreateDefinitions();

            Assert.Equal(9, roster.Select(c => c.Id).Distinct().Count());
            Assert.Equal(4, roster.Count(c => c.Role == CharacterRole.Combat));
            Assert.Equal(2, roster.Count(c => c.Role == CharacterRole.Guardian));
            Assert.Equal(3, roster.Count(c => c.Role == CharacterRole.Control));
            Assert.All(roster, c => Assert.Equal(3, c.GetMissingFields().Count));
        }

        [Fact]
        public void QuickBattle_WithUnsetStats_ThrowsConfigErrorListingMissingFields()
        {
            var ex = Assert.Throws<GameConfigException>(() => QuickBattleFactory.Create(
                PrototypeTestPreset.CreateRules(),
                PrototypeTestPreset.CreateTestMap(),
                PieceModules.CreateDefinitions(),
                new[] { WarriorPiece.PieceId, ArcherPiece.PieceId }));

            Assert.Contains("WARRIOR.MaxHp 미설정", ex.Issues);
            Assert.Contains("ARCHER.AttackRange 미설정", ex.Issues);
        }

        [Fact]
        public void QuickBattle_WithUnknownCharacter_ThrowsConfigError()
        {
            var ex = Assert.Throws<GameConfigException>(() => QuickBattleFactory.Create(
                PrototypeTestPreset.CreateRules(),
                PrototypeTestPreset.CreateTestMap(),
                PrototypeTestPreset.CreateCharacters(),
                new[] { "NOPE" }));

            Assert.Contains("캐릭터 Id 'NOPE' 없음", ex.Issues);
        }

        [Fact]
        public void QuickBattle_WithPerTeamLineups_SpawnsEachTeamsOwnCharacters()
        {
            GameState state = QuickBattleFactory.Create(
                PrototypeTestPreset.CreateRules(),
                PrototypeTestPreset.CreateTestMap(),
                PrototypeTestPreset.CreateCharacters(),
                new[] { ChemistPiece.PieceId, FlamePiece.PieceId, MortarPiece.PieceId, SeamstressPiece.PieceId },
                new[] { WarriorPiece.PieceId, ArcherPiece.PieceId, ChainGuardPiece.PieceId, GardenerPiece.PieceId });

            Assert.Equal(new[] { "CHEMIST", "FLAME", "MORTAR", "SEAMSTRESS" }, state.GetPlayer(Team.Player1).Units.Select(u => u.Stats.Base.Id));
            Assert.Equal(new[] { "WARRIOR", "ARCHER", "CHAIN_GUARD", "GARDENER" }, state.GetPlayer(Team.Player2).Units.Select(u => u.Stats.Base.Id));
        }

        [Fact]
        public void QuickBattle_PerTeamLineups_ReportIssuesFromBothTeams()
        {
            var ex = Assert.Throws<GameConfigException>(() => QuickBattleFactory.Create(
                PrototypeTestPreset.CreateRules(),
                PrototypeTestPreset.CreateTestMap(),
                PrototypeTestPreset.CreateCharacters(),
                new[] { "A" },
                new[] { "B" }));

            Assert.Equal(new[] { "캐릭터 Id 'A' 없음", "캐릭터 Id 'B' 없음" }, ex.Issues);
        }

        [Fact]
        public void CreateDefault_StartsFourVersusFourBattle()
        {
            GameState state = QuickBattleFactory.CreateDefault();

            Assert.Equal(GamePhase.Battle, state.Phase);
            Assert.Equal(4, state.GetPlayer(Team.Player1).Units.Count);
            Assert.Equal(4, state.GetPlayer(Team.Player2).Units.Count);
            Assert.All(state.GetPlayer(Team.Player1).Units, u => Assert.True(u.IsPlaced));
        }

        #endregion

        #region Summon elimination policy

        private static (GameState State, Unit[] Units) SummonOnlyLeftAfterAttack(SummonEliminationPolicy policy)
        {
            var (state, u) = new TestGame()
                .WithRules(r => r.Match.SummonElimination = policy)
                .Place(Team.Player1, 0, 0, TestGame.Stats(attack: 5))
                .Place(Team.Player2, 0, 1, TestGame.Stats(hp: 5))
                .Place(Team.Player2, 6, 6)
                .Start();
            u[2].IsSummon = true;
            return (state, u);
        }

        // 명세 14: "분신만 남았을 때 전멸 판정" → 미확정. 별도 규칙 결정 전 자동 승패 판정 금지
        [Fact]
        public void SummonOnlyLeft_WithUndecidedPolicy_RaisesConfigError()
        {
            var (state, u) = SummonOnlyLeftAfterAttack(SummonEliminationPolicy.Undecided);

            Assert.Throws<GameConfigException>(() => new AttackAction(u[0], u[1]).Execute(state));
        }

        [Fact]
        public void SummonOnlyLeft_WithExcludePolicy_EndsGame()
        {
            var (state, u) = SummonOnlyLeftAfterAttack(SummonEliminationPolicy.ExcludeSummons);

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.Equal(Team.Player1, state.Winner);
        }

        [Fact]
        public void SummonOnlyLeft_WithIncludePolicy_ContinuesGame()
        {
            var (state, u) = SummonOnlyLeftAfterAttack(SummonEliminationPolicy.IncludeSummons);

            new AttackAction(u[0], u[1]).Execute(state);

            Assert.False(state.IsGameOver);
        }

        #endregion
    }
}
