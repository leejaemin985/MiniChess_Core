using System.Linq;
using MiniChess.Core.Data;
using MiniChess.Core.State;

namespace MiniChess.Core.Turns
{
    /// <summary>
    /// 유닛의 이번 턴 행동 기록(UnitTurnState)과 경기 규칙(TurnActionRuleData)을 합쳐 행동 잠금을 판정한다.
    /// 행동 기록은 규칙과 상관없이 남기고, 잠글지는 여기서 규칙을 보고 정한다.
    /// </summary>
    public static class TurnActionRules
    {
        /// <summary>이미 쓴 전투 행동 때문에 전투 행동(기본 공격/전투 스킬)을 더 할 수 없는지. 행동 종료(ActionsEnded)는 보지 않는다.</summary>
        public static bool IsCombatLocked(GameState state, Unit unit)
        {
            return unit.TurnState.CombatActionUsed && Rule(state).CombatOncePerTurn;
        }

        /// <summary>이미 쓴 전투 행동 때문에 자발적 이동을 할 수 없는지. 행동 종료(ActionsEnded)는 보지 않는다.</summary>
        public static bool IsVoluntaryMoveLocked(GameState state, Unit unit)
        {
            return unit.TurnState.VoluntaryMoveLocked && Rule(state).LockMoveAfterCombat;
        }

        /// <summary>한 번씩 규칙에서 이번 턴의 기본 공격을 이미 썼는지.</summary>
        public static bool IsBasicAttackUsedUp(GameState state, Unit unit)
        {
            return unit.TurnState.BasicAttackUsed && Rule(state).EachActionOncePerTurn;
        }

        /// <summary>한 번씩 규칙에서 이번 턴에 이 스킬을 이미 썼는지.</summary>
        public static bool IsSkillUsedUp(GameState state, Unit unit, string skillId)
        {
            return unit.TurnState.HasUsedSkill(skillId) && Rule(state).EachActionOncePerTurn;
        }

        /// <summary>
        /// 이번 턴에 아직 전투 행동을 할 수 있는지(행동 종료와 전투 잠금 모두 아님, AP/상태효과/대상 제외).
        /// 한 번씩 규칙이면 기본 공격이나 아직 안 쓴 스킬이 하나라도 남아 있어야 한다.
        /// </summary>
        public static bool CanStillFight(GameState state, Unit unit)
        {
            if (unit.TurnState.ActionsEnded || IsCombatLocked(state, unit))
                return false;

            if (!Rule(state).EachActionOncePerTurn)
                return true;

            return !unit.TurnState.BasicAttackUsed || unit.Stats.Base.SkillIds.Any(id => !unit.TurnState.HasUsedSkill(id));
        }

        private static TurnActionRuleData Rule(GameState state)
        {
            return state.Rules.TurnAction ?? Default;
        }

        private static readonly TurnActionRuleData Default = new TurnActionRuleData();
    }
}
