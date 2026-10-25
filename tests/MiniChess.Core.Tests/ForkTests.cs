using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Presets;
using MiniChess.Core.Setup;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// GameController.Fork: 복사본은 원본과 같은 명령에 같은 결과를 내고(빠뜨린 필드가 있으면 이후 스냅샷이 갈라진다),
    /// 복사본에 한 명령은 원본을 바꾸지 않으며, viewer 를 주면 그 팀에게 숨기는 정보가 빠진다.
    /// </summary>
    public class ForkTests
    {
        private sealed class FixedRandom : IRandomSource
        {
            public int Next(int maxExclusive) => 0;
        }

        /// <summary>시드마다 다른 캐릭터 조합(모든 캐릭터가 돌아가며 나온다)의 프리셋 경기. 무작위 값은 고정한다.</summary>
        private static GameController CreateGame(int seed)
        {
            GameRuleData rules = PrototypeTestPreset.CreateRules();
            List<CharacterDefinition> characters = PrototypeTestPreset.CreateCharacters();
            int count = rules.Match.UnitsPerTeam;
            List<string> Lineup(int offset) => Enumerable.Range(0, count).Select(i => characters[(offset + i) % characters.Count].Id).ToList();

            GameController game = QuickBattleFactory.Create(
                rules, PrototypeTestPreset.CreateTestMap(), characters, Lineup(seed * count), Lineup(seed * count + count), PrototypeTestPreset.CreateSkills());
            game.State.Random = new FixedRandom();
            return game;
        }

        /// <summary>어느 경기에든 같은 요청으로 다시 낼 수 있는 행동 하나.</summary>
        private sealed record Step(string Kind, Team Team, int UnitId = 0, Position Cell = default, int TargetId = 0,
            string SkillId = null, IReadOnlyList<Position> Targets = null, AreaRotation Rotation = AreaRotation.R0)
        {
            public bool Apply(GameController game)
            {
                switch (Kind)
                {
                    case "Move": return game.Move(new MoveRequest(Team, UnitId, Cell)).Success;
                    case "Attack": return game.Attack(new AttackRequest(Team, UnitId, TargetId)).Success;
                    case "Obstacle": return game.AttackObstacle(new AttackObstacleRequest(Team, UnitId, Cell)).Success;
                    case "Skill": return game.UseSkill(new UseSkillRequest(Team, UnitId, SkillId, Targets, Rotation)).Success;
                    default: return game.EndTurn(new EndTurnRequest(Team)).Success;
                }
            }
        }

        /// <summary>지금 할 수 있는 행동 중 하나를 무작위로 고른다(가끔은 턴 종료).</summary>
        private static Step RandomStep(GameController game, Random random)
        {
            Team team = game.State.CurrentTeam;
            var steps = new List<Step>();

            foreach (Unit unit in game.State.GetPlayer(team).AliveUnits.Where(unit => unit.IsOnBoard))
            {
                steps.AddRange(game.GetMoveDestinations(unit.Id).Select(cell => new Step("Move", team, unit.Id, Cell: cell)));
                steps.AddRange(game.GetAttackTargets(unit.Id).Select(target => new Step("Attack", team, unit.Id, TargetId: target.Id)));
                steps.AddRange(game.GetAttackableObstacles(unit.Id).Select(cell => new Step("Obstacle", team, unit.Id, Cell: cell)));

                foreach (SkillInfo skill in game.GetSkills(unit.Id).Where(skill => skill.Usable == SkillFailReason.None && skill.Definition != null))
                {
                    var targets = new List<Position>();
                    while (targets.Count < skill.Definition.Targeting.TargetCount)
                    {
                        List<Position> next = game.GetSkillTargets(unit.Id, skill.Id, targets);
                        if (next.Count == 0)
                            break;
                        targets.Add(next[random.Next(next.Count)]);
                    }

                    if (targets.Count == skill.Definition.Targeting.TargetCount)
                    {
                        var rotation = skill.CanRotate ? (AreaRotation)random.Next(4) : AreaRotation.R0;
                        steps.Add(new Step("Skill", team, unit.Id, SkillId: skill.Id, Targets: targets, Rotation: rotation));
                    }
                }
            }

            // 공격/스킬을 더 자주 골라 상태이상, 장판, 호위, 포격이 쌓이게 한다.
            List<Step> actions = steps.Where(step => step.Kind != "Move").ToList();
            if (steps.Count == 0 || random.NextDouble() < 0.12)
                return new Step("EndTurn", team);
            if (actions.Count > 0 && random.NextDouble() < 0.6)
                return actions[random.Next(actions.Count)];
            return steps[random.Next(steps.Count)];
        }

        /// <summary>양 팀 시점 스냅샷(이벤트 번호 제외). 복사본은 이벤트 기록이 비어 시작하므로 번호만 다르다.</summary>
        private static string Dump(GameController game)
        {
            var dump = new JObject();
            foreach (Team team in new[] { Team.Player1, Team.Player2 })
            {
                JObject snapshot = JObject.FromObject(game.GetSnapshot(team));
                snapshot.Remove("LastEventSequence");
                dump[team.ToString()] = snapshot;
            }

            return dump.ToString(Formatting.None);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        public void Fork_gives_the_same_results_as_the_original_for_the_rest_of_the_match(int seed)
        {
            GameController game = CreateGame(seed);
            var random = new Random(seed);
            var forks = new List<(GameController Game, int ForkedAt)>();

            for (int stepIndex = 0; stepIndex < 600 && !game.State.IsGameOver; stepIndex++)
            {
                // 경기 중 여러 시점에서 복사해, 그때부터 끝까지 원본과 같은 행동을 시킨다.
                if (stepIndex % 15 == 0)
                {
                    GameController fork = game.Fork();
                    fork.State.Random = new FixedRandom();
                    Assert.Equal(Dump(game), Dump(fork));
                    forks.Add((fork, stepIndex));
                }

                Step step = RandomStep(game, random);
                bool applied = step.Apply(game);
                Assert.True(applied, $"원본에서 거부된 행동: {step}");

                string expected = Dump(game);
                foreach ((GameController fork, int forkedAt) in forks)
                {
                    Assert.True(step.Apply(fork), $"{forkedAt} 번째에 복사한 경기에서 거부된 행동(원본은 성공): {step}");
                    Assert.Equal(expected, Dump(fork));
                }
            }
        }

        [Fact]
        public void Commands_on_a_fork_do_not_change_the_original()
        {
            GameController game = CreateGame(3);
            var random = new Random(3);
            for (int i = 0; i < 40 && !game.State.IsGameOver; i++)
                RandomStep(game, random).Apply(game);
            string before = Dump(game);

            GameController fork = game.Fork();
            for (int i = 0; i < 80 && !fork.State.IsGameOver; i++)
                RandomStep(fork, random).Apply(fork);

            Assert.NotEqual(before, Dump(fork));
            Assert.Equal(before, Dump(game));
        }

        // 숨김 규칙(GameVisibility): 상대가 설치한 덫과 상대 예약 포격은 viewer 시점 복사본에 없다. 자기 것은 남는다.
        [Fact]
        public void Fork_for_a_viewer_leaves_out_information_hidden_from_that_viewer()
        {
            GameController game = FindGameWithTrapAndStrike();
            Assert.NotNull(game);

            foreach (Team viewer in new[] { Team.Player1, Team.Player2 })
            {
                Team enemy = viewer == Team.Player1 ? Team.Player2 : Team.Player1;
                GameController fork = game.Fork(viewer);

                Assert.False(HasTrapOf(fork, enemy));
                Assert.Equal(HasTrapOf(game, viewer), HasTrapOf(fork, viewer));
                Assert.DoesNotContain(fork.State.ScheduledStrikes, strike => strike.Source.Team == enemy);
                Assert.Equal(game.State.ScheduledStrikes.Count(strike => strike.Source.Team == viewer), fork.State.ScheduledStrikes.Count);
            }
        }

        /// <summary>무작위 진행 중에 덫과 예약 포격이 함께 보드에 있는 순간의 경기(시드를 바꿔 가며 찾는다).</summary>
        private static GameController FindGameWithTrapAndStrike()
        {
            for (int seed = 1; seed <= 30; seed++)
            {
                GameController game = CreateGame(seed);
                var random = new Random(seed);
                for (int i = 0; i < 600 && !game.State.IsGameOver; i++)
                {
                    bool trap = HasTrapOf(game, Team.Player1) || HasTrapOf(game, Team.Player2);
                    if (trap && game.State.ScheduledStrikes.Count > 0)
                        return game;
                    RandomStep(game, random).Apply(game);
                }
            }

            return null;
        }

        private static bool HasTrapOf(GameController game, Team owner)
        {
            return game.State.Board.AllCells().Any(cell =>
                cell.GetEffect(CellEffectLayer.Trap) is Effects.Zones.ZoneCellEffect trap && trap.Zone.OwnerTeam == owner);
        }

        [Fact]
        public void Restored_game_cannot_be_forked()
        {
            GameController game = CreateGame(1);
            GameController restored = GameController.Restore(new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
            }, game.GetSnapshot(Team.Player1));

            Assert.Throws<InvalidOperationException>(() => restored.Fork());
        }
    }
}
