using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;

namespace MiniChess.Core.Data
{
    /// <summary>
    /// 개발용 테스트 규칙(이펙트/스킬 확인용). 로컬 경기에서만 쓴다. 온라인 경기 규칙(OnlineMatch)에는 넣지 않는다.
    /// Teams 에 든 팀에만 적용한다(봇 팀을 빼 두면 봇은 일반 규칙으로 둔다).
    /// </summary>
    public class TestRuleData
    {
        /// <summary>이동/기본 공격/스킬이 AP 를 쓰지 않는다.</summary>
        public bool FreeAp { get; set; }

        /// <summary>기본 공격과 스킬의 턴당 1회 제한, 행동 종료(행동이 끝나는 스킬)를 무시한다.</summary>
        public bool UnlimitedActions { get; set; }

        /// <summary>적용할 팀. null 이면 모든 팀.</summary>
        public IReadOnlyCollection<Team> Teams { get; set; }

        public bool AppliesTo(Team team)
        {
            return Teams == null || Teams.Contains(team);
        }
    }
}
