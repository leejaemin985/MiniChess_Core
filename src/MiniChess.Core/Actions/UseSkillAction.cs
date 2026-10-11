using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;

namespace MiniChess.Core.Actions
{
    /// <summary>
    /// 플레이어가 자기 유닛의 스킬을 지정한 칸(들)에 사용하는 명령.
    /// 처리 순서: 검사 → SkillUsedEvent → AP 차감 → 행동 상태 반영 → 범위 계산 → 효과 순서대로 실행 → (옵션) 행동 종료.
    /// </summary>
    public class UseSkillAction
    {
        public Unit Caster { get; }
        public string SkillId { get; }

        /// <summary>지정한 칸 전부(지정 순). 개수는 스킬의 Targeting.TargetCount 와 같아야 한다.</summary>
        public IReadOnlyList<Position> Targets { get; }

        /// <summary>첫 번째 지정 칸.</summary>
        public Position Target => Targets[0];

        /// <summary>범위 회전(시계 방향). 회전하는 범위의 스킬에만 의미가 있다.</summary>
        public AreaRotation Rotation { get; }

        public UseSkillAction(Unit caster, string skillId, Position target, AreaRotation rotation = AreaRotation.R0)
            : this(caster, skillId, new[] { target }, rotation)
        {
        }

        public UseSkillAction(Unit caster, string skillId, IReadOnlyList<Position> targets, AreaRotation rotation = AreaRotation.R0)
        {
            Caster = caster ?? throw new ArgumentNullException(nameof(caster));
            SkillId = skillId ?? throw new ArgumentNullException(nameof(skillId));
            Targets = targets ?? throw new ArgumentNullException(nameof(targets));
            if (targets.Count == 0) throw new ArgumentException("지정 칸이 비어 있음", nameof(targets));
            Rotation = rotation;
        }

        public SkillFailReason Validate(GameState state)
        {
            SkillFailReason reason = ValidateUsable(state, Caster, SkillId);
            if (reason != SkillFailReason.None) return reason;

            SkillDefinition skill = state.Skills.Find(SkillId);
            if (!skill.Targeting.IsValid(state, Caster, Targets)) return SkillFailReason.InvalidTarget;

            return SkillFailReason.None;
        }

        /// <summary>
        /// 지정 대상과 무관하게 지금 이 스킬을 쓸 수 있는지(스킬 버튼 활성화 판단용).
        /// None 이어도 지정 가능한 칸이 하나도 없을 수 있다(SkillQueries.GetValidTargets 로 확인).
        /// </summary>
        public static SkillFailReason ValidateUsable(GameState state, Unit caster, string skillId)
        {
            if (state.Phase != GamePhase.Battle) return SkillFailReason.NotBattlePhase;
            if (caster.Team != state.CurrentTeam) return SkillFailReason.NotYourTurn;
            if (!caster.IsOnBoard) return SkillFailReason.CasterNotOnBoard;

            SkillDefinition skill = caster.HasSkill(skillId) ? state.Skills.Find(skillId) : null;
            if (skill == null) return SkillFailReason.SkillNotOwned;
            if (!skill.IsConfigured) return SkillFailReason.ConfigMissing;

            SkillFailReason turnReason = CheckTurnState(caster, skill);
            if (turnReason != SkillFailReason.None) return turnReason;

            foreach (SkillCondition condition in skill.Conditions)
            {
                if (!condition.IsMet(state, caster))
                    return SkillFailReason.ConditionNotMet;
            }

            if (!state.GetPlayer(caster.Team).Ap.CanSpend(skill.ApCost.Value)) return SkillFailReason.NotEnoughAp;

            return SkillFailReason.None;
        }

        public SkillResult Execute(GameState state)
        {
            SkillFailReason reason = Validate(state);
            if (reason != SkillFailReason.None)
                throw new InvalidOperationException($"스킬 사용 불가({SkillId}): {reason}");

            SkillDefinition skill = state.Skills.Find(SkillId);
            int eventStart = state.Events.Count;

            IReadOnlyList<Position> affectedCells = skill.Area.GetCells(state, Caster, Target, Rotation);
            state.Events.Record(new SkillUsedEvent(Caster, skill, Targets, Rotation, affectedCells));

            state.GetPlayer(Caster.Team).Ap.TrySpend(skill.ApCost.Value);
            if (skill.ActionKind != SkillActionKind.Movement)
                Caster.TurnState.MarkCombatActionUsed();

            var context = new SkillContext(state, Caster, skill, Targets, affectedCells);
            foreach (SkillEffect effect in skill.Effects)
            {
                if (state.IsGameOver)
                    break;

                effect.Apply(context);
            }

            if (skill.EndsCasterActions)
                Caster.TurnState.EndActions();

            return new SkillResult(Caster, skill, Targets, Rotation, affectedCells, state.Events.Since(eventStart));
        }

        private static SkillFailReason CheckTurnState(Unit caster, SkillDefinition skill)
        {
            UnitTurnState turn = caster.TurnState;
            if (turn.ActionsEnded) return SkillFailReason.ActionsEnded;

            if (skill.ActionKind == SkillActionKind.Movement)
                return turn.VoluntaryMoveLocked ? SkillFailReason.MoveLocked : SkillFailReason.None;

            return turn.CombatActionUsed ? SkillFailReason.AlreadyActed : SkillFailReason.None;
        }
    }
}
