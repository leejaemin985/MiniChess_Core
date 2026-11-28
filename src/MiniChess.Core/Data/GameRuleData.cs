using MiniChess.Core.Common;
using MiniChess.Core.Skills;

namespace MiniChess.Core.Data
{
    /// <summary>한 경기에 적용되는 규칙 묶음. 맵과 유닛 데이터는 별도로 관리한다.</summary>
    public class GameRuleData
    {
        public ApRuleData Ap { get; set; }
        public ActionCostData ActionCost { get; set; }
        public MatchRuleData Match { get; set; }
        public CaptureRuleData Capture { get; set; }

        /// <summary>토큰 규칙. null 이면 토큰이 나오지 않는다.</summary>
        public TokenRuleData Token { get; set; }

        /// <summary>피로도 규칙. null 이면 피로도 피해가 없다.</summary>
        public FatigueRuleData Fatigue { get; set; }

        /// <summary>개발용 테스트 규칙. null 이면 꺼짐(일반 규칙).</summary>
        public TestRuleData Test { get; set; }

        /// <summary>team 의 행동이 AP 를 쓰지 않는지(테스트 규칙).</summary>
        public bool IsApFree(Team team)
        {
            return Test != null && Test.FreeAp && Test.AppliesTo(team);
        }

        /// <summary>team 이 턴당 횟수 제한과 행동 종료 없이 행동하는지(테스트 규칙).</summary>
        public bool HasUnlimitedActions(Team team)
        {
            return Test != null && Test.UnlimitedActions && Test.AppliesTo(team);
        }

        /// <summary>team 이 장판 동시 설치 수 한도 없이 설치하는지(테스트 규칙).</summary>
        public bool HasUnlimitedZones(Team team)
        {
            return Test != null && Test.UnlimitedZones && Test.AppliesTo(team);
        }

        /// <summary>team 이 실제로 내는 기본 공격 AP.</summary>
        public int GetBasicAttackCost(Team team)
        {
            return IsApFree(team) ? 0 : ActionCost.BasicAttackCost;
        }

        /// <summary>team 이 실제로 내는 스킬 AP(비용 미설정이면 0).</summary>
        public int GetSkillCost(Team team, SkillDefinition skill)
        {
            return IsApFree(team) || skill?.ApCost == null ? 0 : skill.ApCost.Value;
        }
    }
}
