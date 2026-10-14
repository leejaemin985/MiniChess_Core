using System.Collections.Generic;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;

namespace MiniChess.Core.Controller
{
    /// <summary>경기 생성에 필요한 데이터 묶음.</summary>
    public class GameConfig
    {
        public GameRuleData Rules { get; set; }
        public MapData Map { get; set; }

        /// <summary>배치할 때 고를 수 있는 캐릭터.</summary>
        public IReadOnlyList<CharacterDefinition> Characters { get; set; }

        /// <summary>캐릭터들이 쓰는 스킬 정의.</summary>
        public SkillCatalog Skills { get; set; }

        /// <summary>
        /// 무작위 값(점령 보상 등)의 시드. 비우면 생성 시각으로 정한다.
        /// 실제 쓴 값은 GameState.Seed 에 남으며, 같은 시드와 같은 요청 순서면 같은 경기가 재현된다.
        /// </summary>
        public int? Seed { get; set; }
    }
}
