using System.Collections.Generic;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;

namespace MiniChess.Core.Characters.Shared
{
    /// <summary>캐릭터 스킬 정의에서 공통으로 쓰는 도우미.</summary>
    public static class SkillBuildUtil
    {
        /// <summary>피해 수치가 미설정이면 누락으로 보고되도록 넣고, 0 이면 피해 없음으로 생략한다.</summary>
        public static void AddOptionalDamage(List<SkillEffect> effects, int? damage)
        {
            if (damage != 0)
                effects.Add(new DamageEffect(damage));
        }

        /// <summary>SkillDefinition 의 requiredTuning 인자로 넘길 필수 수치 목록. (이름, 값) 순.</summary>
        public static (string Name, object Value)[] Require(params (string Name, object Value)[] values)
        {
            return values;
        }

        /// <summary>범위 정보로 범위 부품을 만든다.</summary>
        public static PatternArea Area(AreaTuning area)
        {
            return new PatternArea(AreaShape.Parse(area.Shape), area.Anchor, area.Rotate);
        }

        /// <summary>
        /// 범위 정보의 기준점에 맞는 지정 부품. range/requirement/includeCasterCell 은 선택 칸 기준일 때만 쓴다.
        /// 회전은 지정과 별개로 사용 명령의 회전값으로 받는다.
        /// </summary>
        public static SkillTargeting AreaTargeting(AreaTuning area, int? range, CellRequirement requirement, bool includeCasterCell = false)
        {
            if (area.Anchor == AreaAnchor.Target)
                return new CellTargeting(range, requirement, RangeShape.Square, includeCasterCell: includeCasterCell);

            return new SelfTargeting();
        }
    }
}
