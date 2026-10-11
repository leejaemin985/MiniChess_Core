using MiniChess.Core.Actions;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>컨트롤러(단일 진입점)만으로 경기를 진행한다. 프리셋 규칙/맵/캐릭터/스킬 사용.</summary>
    public class ControllerTests
    {
        private static readonly string[] Lineup =
        {
            WarriorPiece.PieceId, ArcherPiece.PieceId, GardenerPiece.PieceId, ScythePiece.PieceId,
        };

        private static GameController NewGame()
        {
            return GameController.Create(new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
            });
        }

        private static List<UnitPlacement> Row(int y)
        {
            return Lineup.Select((id, x) => new UnitPlacement(id, new Position(x, y))).ToList();
        }

        /// <summary>Player1: (0..3, 1), Player2: (0..3, 5). 각 팀 유닛 Id 는 [전사, 궁수, 정원사, 낫] 순.</summary>
        private static (GameController Game, int[] P1, int[] P2) StartedGame()
        {
            GameController game = NewGame();
            int[] p1 = game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, Row(1))).Result.Units.Select(u => u.Id).ToArray();
            int[] p2 = game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Row(5))).Result.Units.Select(u => u.Id).ToArray();
            return (game, p1, p2);
        }

        private static int Ap(GameController game, Team team) => game.State.GetPlayer(team).Ap.Current;

        [Fact]
        public void Create_StartsInSetup_AndRejectsBattleCommands()
        {
            GameController game = NewGame();

            Assert.Equal(GamePhase.Setup, game.State.Phase);
            Assert.Equal(14, game.GetPlacementCells(Team.Player1).Count);
            Assert.Equal(9, game.GetCharacters().Count);
            Assert.Equal(EndTurnFailReason.NotBattlePhase, game.EndTurn(new EndTurnRequest(Team.Player1)).Reason);
        }

        [Fact]
        public void Placement_ThroughController_StartsBattle()
        {
            GameController game = NewGame();

            var first = game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Row(5)));
            var again = game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, Row(6)));
            var second = game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, Row(1)));

            Assert.True(first.Success);
            Assert.False(again.Success);
            Assert.Equal(PlaceFailReason.AlreadyPlaced, again.Reason);
            Assert.Null(again.Result);
            Assert.Empty(again.Events);
            Assert.True(second.Result.BattleStarted);
            Assert.Equal(GamePhase.Battle, game.State.Phase);
        }

        [Fact]
        public void Requests_ForUnknownOrOtherTeamsUnit_AreRejected_WithoutChangingState()
        {
            var (game, p1, p2) = StartedGame();
            int apBefore = Ap(game, Team.Player1);
            int eventsBefore = game.State.Events.Count;

            Assert.Equal(MoveFailReason.UnknownUnit, game.Move(new MoveRequest(Team.Player1, 999, new Position(0, 2))).Reason);
            Assert.Equal(MoveFailReason.NotYourUnit, game.Move(new MoveRequest(Team.Player1, p2[0], new Position(0, 4))).Reason);
            Assert.Equal(AttackFailReason.NotYourUnit, game.Attack(new AttackRequest(Team.Player1, p2[1], p1[1])).Reason);
            Assert.Equal(SkillFailReason.NotYourUnit, game.UseSkill(new UseSkillRequest(Team.Player2, p1[3], ScythePiece.SlashId, new Position(3, 3))).Reason);
            Assert.Equal(EndTurnFailReason.NotYourTurn, game.EndTurn(new EndTurnRequest(Team.Player2)).Reason);

            Assert.Equal(apBefore, Ap(game, Team.Player1));
            Assert.Equal(eventsBefore, game.State.Events.Count);
        }

        [Fact]
        public void OneTurn_MoveAttackSkillEndTurn_ThroughRequestsOnly()
        {
            var (game, p1, p2) = StartedGame();
            int archer = p1[1];

            // 이동: 궁수 (1,1) → (1,2)
            Assert.Contains(new Position(1, 2), game.GetMoveDestinations(archer));
            var move = game.Move(new MoveRequest(Team.Player1, archer, new Position(1, 2)));
            Assert.True(move.Success);
            Assert.Contains(move.Events, e => e is UnitMovedEvent);

            // 공격: 사거리 3 안의 적 궁수 (1,5)
            Unit enemyArcher = game.State.FindUnit(p2[1]);
            Assert.Contains(enemyArcher, game.GetAttackTargets(archer));
            var attack = game.Attack(new AttackRequest(Team.Player1, archer, enemyArcher.Id));
            Assert.True(attack.Success);
            Assert.Equal(6, enemyArcher.Stats.CurrentHp);

            // 스킬: 낫 일자 베기, 시계 90도 회전
            int scythe = p1[3];
            Assert.Contains(new Position(3, 3), game.GetSkillTargets(scythe, ScythePiece.SlashId));
            IReadOnlyList<Position> preview = game.GetSkillArea(scythe, ScythePiece.SlashId, new Position(3, 3), AreaRotation.R90);
            var skill = game.UseSkill(new UseSkillRequest(Team.Player1, scythe, ScythePiece.SlashId, new Position(3, 3), AreaRotation.R90));
            Assert.True(skill.Success);
            Assert.Equal(preview, skill.Result.AffectedCells);
            Assert.Equal(8 - 1 - 2 - 3, Ap(game, Team.Player1));

            // 턴 종료
            var end = game.EndTurn(new EndTurnRequest(Team.Player1));
            Assert.True(end.Success);
            Assert.Equal(Team.Player2, end.Result.NextTeam);
            Assert.Contains(end.Events, e => e is TurnStartedEvent started && started.Team == Team.Player2);
        }

        [Fact]
        public void Queries_DescribeWhatTheUnitCanDoNow()
        {
            var (game, p1, p2) = StartedGame();

            // 전사 (0,1): 위 (0,2) 까지(벽 (0,3)), 아래 (0,0), 오른쪽은 아군
            Assert.Equal(new[] { new Position(0, 0), new Position(0, 2) }.ToHashSet(), game.GetMoveDestinations(p1[0]).ToHashSet());
            Assert.Empty(game.GetMoveDestinations(p2[0])); // 상대 턴의 유닛
            Assert.Empty(game.GetAttackTargets(p1[0]));    // 사거리 안에 적 없음

            List<SkillInfo> skills = game.GetSkills(p1[3]);
            Assert.Equal(new[] { ScythePiece.SlashId, ScythePiece.ShadowCloneId, ScythePiece.CloneSwapId }, skills.Select(s => s.Id));
            Assert.True(skills.Single(s => s.Id == ScythePiece.CloneSwapId).IsExtra);
            Assert.True(skills.Single(s => s.Id == ScythePiece.SlashId).CanRotate);
            Assert.Equal(SkillFailReason.None, skills.Single(s => s.Id == ScythePiece.SlashId).Usable);
            Assert.Empty(game.GetSkillTargets(p1[3], ScythePiece.CloneSwapId)); // 분신이 없어 지정할 칸 없음
        }
    }
}
