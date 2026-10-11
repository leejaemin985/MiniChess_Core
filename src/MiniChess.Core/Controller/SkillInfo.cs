using MiniChess.Core.Actions;
using MiniChess.Core.Skills;

namespace MiniChess.Core.Controller
{
    /// <summary>유닛이 가진 스킬 하나의 표시용 정보(스킬 버튼).</summary>
    public class SkillInfo
    {
        public string Id { get; }

        /// <summary>경기 스킬 목록의 정의. 정의가 없는 Id 면 null.</summary>
        public SkillDefinition Definition { get; }

        /// <summary>스킬 슬롯이 아닌 캐릭터 고유 기능(예: 낫의 분신 교환)인지. 버튼을 슬롯과 따로 보여준다.</summary>
        public bool IsExtra { get; }

        /// <summary>지정 대상과 무관하게 지금 쓸 수 있는지. None 이면 사용 가능.</summary>
        public SkillFailReason Usable { get; }

        /// <summary>범위를 플레이어가 회전할 수 있는지(회전 버튼 표시).</summary>
        public bool CanRotate { get; }

        public SkillInfo(string id, SkillDefinition definition, bool isExtra, SkillFailReason usable, bool canRotate)
        {
            Id = id;
            Definition = definition;
            IsExtra = isExtra;
            Usable = usable;
            CanRotate = canRotate;
        }
    }
}
