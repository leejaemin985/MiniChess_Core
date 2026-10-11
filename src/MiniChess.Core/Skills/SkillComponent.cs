using System.Collections.Generic;

namespace MiniChess.Core.Skills
{
    /// <summary>
    /// 스킬을 구성하는 부품(지정 규칙/범위/효과/조건)의 공통 기반.
    /// 수치가 확정되지 않은(TBD) 필수 값은 null 로 두고, CollectConfigIssues 로 보고한다.
    /// </summary>
    public abstract class SkillComponent
    {
        /// <summary>비어 있는 필수 설정을 issues 에 추가한다. path 는 보고용 위치(예: "WARRIOR_SMASH.Effects[0]").</summary>
        public virtual void CollectConfigIssues(string path, List<string> issues) { }

        /// <summary>value 가 null 이면 "{path}.{field} 미설정" 을 issues 에 추가한다. 설정 누락 보고 형식의 단일 출처.</summary>
        internal static void Require(object value, string path, string field, List<string> issues)
        {
            if (value == null)
                issues.Add($"{path}.{field} 미설정");
        }
    }
}
