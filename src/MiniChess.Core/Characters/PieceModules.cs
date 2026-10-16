using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;

namespace MiniChess.Core.Characters
{
    /// <summary>
    /// 캐릭터 목록(명세 v0.1 의 9개 + 추가 기획 암살자, 조각가). 새 캐릭터는 Pieces/ 에 파일을 만들고 여기에 한 줄 추가한다.
    /// </summary>
    public static class PieceModules
    {
        public static IReadOnlyList<PieceModule> All { get; } = new PieceModule[]
        {
            new ScythePiece(),
            new MortarPiece(),
            new GardenerPiece(),
            new WarriorPiece(),
            new ArcherPiece(),
            new ChemistPiece(),
            new FlamePiece(),
            new SeamstressPiece(),
            new ChainGuardPiece(),
            new AssassinPiece(),
            new SculptorPiece(),
        };

        /// <summary>수치가 비어 있는 모든 캐릭터 정의를 새로 만든다. 플레이 테스트용 수치는 Presets 에서 채운다.</summary>
        public static List<CharacterDefinition> CreateDefinitions()
        {
            return All.Select(piece => piece.CreateDefinition()).ToList();
        }

        /// <summary>모든 캐릭터의 구현된 스킬 정의 모음.</summary>
        public static SkillCatalog CreateSkillCatalog(SkillTuning tuning)
        {
            var catalog = new SkillCatalog();

            foreach (PieceModule piece in All)
            {
                foreach (SkillDefinition skill in piece.CreateSkills(tuning))
                    catalog.Add(skill);
            }

            return catalog;
        }
    }
}
