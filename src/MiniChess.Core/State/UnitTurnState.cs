using System.Collections.Generic;

namespace MiniChess.Core.State
{
    /// <summary>
    /// 유닛이 자기 소유자의 이번 턴에 한 행동 기록. 소유자의 턴 시작 시 초기화된다.
    /// 외부에서 받은 위치 변경(넉백/워프 등)은 이 상태를 바꾸지 않는다.
    /// </summary>
    public class UnitTurnState
    {
        /// <summary>이번 턴에 전투 행동(기본 공격 또는 액티브 스킬)을 사용했는지. 턴당 1회.</summary>
        public bool CombatActionUsed { get; private set; }

        /// <summary>자발적 이동(일반 이동 및 자신을 이동시키는 스킬)이 금지되었는지. 전투 행동 후 켜진다.</summary>
        public bool VoluntaryMoveLocked { get; private set; }

        /// <summary>이번 턴에 더 이상 어떤 행동도 할 수 없는지(예: 분신 소환 직후의 본체).</summary>
        public bool ActionsEnded { get; private set; }

        /// <summary>이번 턴에 일반 경로 이동(Path)으로 자발적으로 이동한 칸 수의 누적(이동 거리 제한 판정용).</summary>
        public int VoluntaryCellsMoved { get; private set; }

        /// <summary>이번 턴에 기본 공격(장애물 공격 포함)을 했는지.</summary>
        public bool BasicAttackUsed { get; private set; }

        private readonly HashSet<string> _usedSkillIds = new HashSet<string>();

        /// <summary>이번 턴에 사용한 스킬 Id.</summary>
        public IReadOnlyCollection<string> UsedSkillIds => _usedSkillIds;

        public bool HasUsedSkill(string skillId)
        {
            return _usedSkillIds.Contains(skillId);
        }

        internal void MarkBasicAttackUsed()
        {
            BasicAttackUsed = true;
        }

        internal void MarkSkillUsed(string skillId)
        {
            _usedSkillIds.Add(skillId);
        }

        internal void MarkCombatActionUsed()
        {
            CombatActionUsed = true;
            VoluntaryMoveLocked = true;
        }

        internal void EndActions()
        {
            ActionsEnded = true;
        }

        internal void AddVoluntaryCellsMoved(int cells)
        {
            VoluntaryCellsMoved += cells;
        }

        /// <summary>스냅샷 복원용.</summary>
        internal void Restore(
            bool combatActionUsed, bool voluntaryMoveLocked, bool actionsEnded, int voluntaryCellsMoved,
            bool basicAttackUsed, IEnumerable<string> usedSkillIds)
        {
            BasicAttackUsed = basicAttackUsed;
            _usedSkillIds.Clear();
            if (usedSkillIds != null)
                _usedSkillIds.UnionWith(usedSkillIds);
            CombatActionUsed = combatActionUsed;
            VoluntaryMoveLocked = voluntaryMoveLocked;
            ActionsEnded = actionsEnded;
            VoluntaryCellsMoved = voluntaryCellsMoved;
        }

        internal void Reset()
        {
            CombatActionUsed = false;
            VoluntaryMoveLocked = false;
            ActionsEnded = false;
            VoluntaryCellsMoved = 0;
            BasicAttackUsed = false;
            _usedSkillIds.Clear();
        }
    }
}
