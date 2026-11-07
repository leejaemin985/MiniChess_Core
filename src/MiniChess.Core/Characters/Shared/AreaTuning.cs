using MiniChess.Core.Skills.Areas;

namespace MiniChess.Core.Characters.Shared
{
    /// <summary>
    /// 범위 스킬의 범위 정보. 기준점에 따라 지정 방식도 정해진다(SkillBuildUtil.AreaTargeting).
    ///   Target → 사거리 안의 칸 지정
    ///   Caster → 지정 없음(자기 자신)
    /// 모양은 항상 사용 명령의 회전값(UseSkillAction.Rotation)으로 돌아간다. 그린 모양이 회전 0 이다.
    /// </summary>
    public class AreaTuning
    {
        /// <summary>범위 모양 그리드(AreaShape 참고). null 이면 미설정.</summary>
        public string[] Shape { get; set; }

        public AreaAnchor Anchor { get; set; } = AreaAnchor.Target;
    }
}
