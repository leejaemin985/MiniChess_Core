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
    }
}
