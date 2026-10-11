using MiniChess.Core.Skills.Areas;

namespace MiniChess.Core.Characters.Shared
{
    /// <summary>
    /// 범위 스킬의 범위 정보. 기준점에 따라 지정 방식도 정해진다(SkillBuildUtil.AreaTargeting).
    ///   Target → 사거리 안의 칸 지정
    ///   Caster → 지정 없음(자기 자신)
    /// Rotate 면 플레이어가 사용 명령의 회전값(UseSkillAction.Rotation)으로 모양을 돌린다.
    /// </summary>
    public class AreaTuning
    {
        /// <summary>범위 모양 그리드(AreaShape 참고). null 이면 미설정.</summary>
        public string[] Shape { get; set; }

        public AreaAnchor Anchor { get; set; } = AreaAnchor.Target;

        /// <summary>플레이어가 기준점을 중심으로 모양을 회전할 수 있는지. 그린 모양이 회전 0 이다.</summary>
        public bool Rotate { get; set; }
    }
}
