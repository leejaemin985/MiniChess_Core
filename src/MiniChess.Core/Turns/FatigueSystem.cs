using System;
using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Data;
using MiniChess.Core.State;

namespace MiniChess.Core.Turns
{
    /// <summary>매 턴 종료에 양 팀의 보드 위 유닛(소환물 포함)에게 같은 라운드의 피로도 피해를 준다.</summary>
    public static class FatigueSystem
    {
        /// <summary>8 라운드(전체 턴 15·16) 1, 9 라운드(17·18) 2, ... 시작 전/비활성은 0.</summary>
        public static int GetDamage(FatigueRuleData rule, int turnNumber)
        {
            if (rule == null || rule.StartRound <= 0 || turnNumber <= 0)
                return 0;

            int round = (turnNumber - 1) / 2 + 1;
            if (round < rule.StartRound)
                return 0;

            long damage = rule.InitialDamage + (long)(round - rule.StartRound) * rule.DamageIncreasePerRound;
            return (int)Math.Min(int.MaxValue, Math.Max(0L, damage));
        }

        internal static void OnTurnEnd(GameState state)
        {
            int damage = GetDamage(state.Rules.Fatigue, state.TurnNumber);
            if (damage <= 0 || state.IsGameOver)
                return;

            // 죽음/소환물 정리로 대상 목록이 바뀌어도 이번 피해 대상은 고정한다.
            DamageRequest[] requests = state.AllUnits().Where(unit => unit.IsOnBoard)
                .Select(unit => new DamageRequest(null, unit, damage, DamageType.Fatigue)).ToArray();
            DamageSystem.ApplyBatch(state, requests);
        }
    }
}
