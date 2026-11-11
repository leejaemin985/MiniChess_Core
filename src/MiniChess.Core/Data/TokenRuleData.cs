using System.Collections.Generic;
using MiniChess.Core.Tokens;

namespace MiniChess.Core.Data
{
    /// <summary>
    /// 토큰 규칙. 판정 방식은 TokenSystem 참고.
    /// 라운드는 양 팀이 한 번씩 턴을 진행한 묶음이다(전체 턴 1·2 가 1 라운드, 3·4 가 2 라운드).
    /// StartRound 부터 PeriodRounds 마다 그 라운드의 양 팀 턴 시작에 각자 토큰을 하나씩 만들고, 종류는 Sequence 를 차례로 돈다.
    /// 예) StartRound 3, PeriodRounds 2, Sequence [Heal, Ap] → 3 라운드 힐, 5 라운드 AP, 7 라운드 힐 ...
    /// </summary>
    public class TokenRuleData
    {
        /// <summary>처음 토큰이 나오는 라운드(1부터).</summary>
        public int StartRound { get; set; }

        /// <summary>토큰이 나오는 라운드 간격. 1 이면 매 라운드.</summary>
        public int PeriodRounds { get; set; }

        /// <summary>나오는 순서대로의 토큰 종류. 끝까지 쓰면 처음부터 다시 돈다. 비어 있으면 토큰이 나오지 않는다.</summary>
        public List<TokenKind> Sequence { get; set; } = new List<TokenKind>();

        /// <summary>AP 토큰을 먹으면 그 팀이 얻는 AP(최대치 초과분은 버린다).</summary>
        public int ApAmount { get; set; }

        /// <summary>힐 토큰을 먹은 유닛이 회복하는 체력(최대 체력 초과분은 버린다).</summary>
        public int HealAmount { get; set; }
    }
}
