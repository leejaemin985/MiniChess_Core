using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Data;
using MiniChess.Core.Presets;
using MiniChess.Core.Skills;

namespace MiniChess.Core.Setup
{
    /// <summary>
    /// 양 팀 배치를 자동으로 마치고 바로 Battle 을 시작하는 테스트용 팩토리.
    /// 각 팀의 유닛을 시작 위치 후보 칸에 앞에서부터 순서대로 컨트롤러 배치 요청으로 놓으므로 배치 규칙을 그대로 따른다.
    /// </summary>
    public static class QuickBattleFactory
    {
        /// <param name="characters">사용 가능한 캐릭터 정의.</param>
        /// <param name="lineup">양 팀이 공통으로 쓰는 캐릭터 Id 구성.</param>
        /// <param name="skills">경기에서 쓰는 스킬 정의. null 이면 스킬 없음.</param>
        /// <exception cref="GameConfigException">구성에 쓰인 캐릭터의 필수 수치가 비어 있거나 Id 가 없음.</exception>
        public static GameController Create(
            GameRuleData rules,
            MapData map,
            IReadOnlyList<CharacterDefinition> characters,
            IReadOnlyList<string> lineup,
            SkillCatalog skills = null)
        {
            return Create(rules, map, characters, lineup, lineup, skills);
        }

        /// <summary>팀별로 다른 구성으로 생성한다.</summary>
        /// <exception cref="GameConfigException">구성에 쓰인 캐릭터의 필수 수치가 비어 있거나 Id 가 없음(양 팀 문제를 모아 보고).</exception>
        public static GameController Create(
            GameRuleData rules,
            MapData map,
            IReadOnlyList<CharacterDefinition> characters,
            IReadOnlyList<string> player1Lineup,
            IReadOnlyList<string> player2Lineup,
            SkillCatalog skills = null)
        {
            var issues = new List<string>();
            CollectIssues(characters, player1Lineup, issues);
            CollectIssues(characters, player2Lineup, issues);
            if (issues.Count > 0)
                throw new GameConfigException(issues.Distinct().ToList());

            GameController game = GameController.Create(new GameConfig { Rules = rules, Map = map, Characters = characters, Skills = skills });

            PlaceTeam(game, Team.Player1, player1Lineup);
            PlaceTeam(game, Team.Player2, player2Lineup);
            return game;
        }

        /// <summary>플레이 테스트 프리셋(PrototypeTestPreset)으로 생성한다.</summary>
        public static GameController CreateDefault()
        {
            return Create(
                PrototypeTestPreset.CreateRules(),
                PrototypeTestPreset.CreateTestMap(),
                PrototypeTestPreset.CreateCharacters(),
                PrototypeTestPreset.CreateStartingLineup(),
                PrototypeTestPreset.CreateSkills());
        }

        /// <summary>구성의 캐릭터가 모두 있고 필수 수치가 채워졌는지 확인한다. 문제는 issues 에 모은다.</summary>
        private static void CollectIssues(IReadOnlyList<CharacterDefinition> characters, IReadOnlyList<string> lineup, List<string> issues)
        {
            foreach (string id in lineup)
            {
                CharacterDefinition definition = characters.FirstOrDefault(c => c.Id == id);
                if (definition == null)
                    issues.Add($"캐릭터 Id '{id}' 없음");
                else
                    issues.AddRange(definition.GetMissingFields().Select(field => $"{field} 미설정"));
            }
        }

        /// <summary>시작 위치 후보 칸 앞에서부터 차례로 배치 요청을 보낸다. 배치 규칙(인원, 중복 등)을 어기면 예외.</summary>
        private static void PlaceTeam(GameController game, Team team, IReadOnlyList<string> lineup)
        {
            IReadOnlyList<Position> spawnPositions = game.GetPlacementCells(team);
            if (spawnPositions.Count < lineup.Count)
                throw new InvalidOperationException($"{team} 시작 위치 {spawnPositions.Count}칸 < 유닛 {lineup.Count}개");

            List<UnitPlacement> placements = lineup.Select((id, i) => new UnitPlacement(id, spawnPositions[i])).ToList();
            var response = game.PlaceUnits(new PlaceUnitsRequest(team, placements));
            if (!response.Success)
                throw new InvalidOperationException($"{team} 배치 실패: {response.Reason}");
        }
    }
}
