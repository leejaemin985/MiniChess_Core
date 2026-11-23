using System.Collections.Generic;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 이단심판관 (사용자 기획).
    /// 스킬 1 정화, 스킬 2 심판. 아군의 나쁜 상태이상을 풀고, 가까운 적을 밀어내는 수호형.
    /// </summary>
    public class InquisitorPiece : PieceModule
    {
        public const string PieceId = "INQUISITOR";
        public const string PurifyId = "INQUISITOR_PURIFY";
        public const string JudgmentId = "INQUISITOR_JUDGMENT";

        public override string Id => PieceId;
        public override string Name => "이단심판관";
        public override CharacterRole Role => CharacterRole.Guardian;
        public override string Skill1Id => PurifyId;
        public override string Skill2Id => JudgmentId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Purify(tuning.Inquisitor.Purify);
            yield return Judgment(tuning.Inquisitor.Judgment);
        }

        /// <summary>
        /// 정화: 사거리 안의 아군 하나(자신 포함)에게 걸린 나쁜 상태이상(StatusDefinition.IsHarmful)을 모두 지운다.
        /// 나쁜 상태가 없는 아군에게도 쓸 수 있다(아무 일도 일어나지 않음).
        /// </summary>
        public static SkillDefinition Purify(PurifyTuning t)
        {
            return new SkillDefinition(
                PurifyId, "정화", t.ApCost,
                new UnitTargeting(TargetFilter.AllyOrSelf, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[] { new CleanseEffect(TargetFilter.AllyOrSelf) });
        }

        /// <summary>
        /// 심판: 사거리 안의 적 하나에게 피해를 주고, 시전자 반대쪽으로 밀어낸다(코어 밀어내기 규칙).
        /// 피해로 죽으면 밀기는 일어나지 않는다. (사용자 확정: 기절 제거)
        /// </summary>
        public static SkillDefinition Judgment(JudgmentTuning t)
        {
            return new SkillDefinition(
                JudgmentId, "심판", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new DamageEffect(t.Damage),
                    new KnockbackEffect(t.PushCells),
                });
        }
    }

    public class InquisitorTuning
    {
        public PurifyTuning Purify { get; } = new PurifyTuning();
        public JudgmentTuning Judgment { get; } = new JudgmentTuning();
    }

    /// <summary>정화.</summary>
    public class PurifyTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상 아군을 고를 수 있는 거리(대각선 포함).</summary>
        public int? Range { get; set; }
    }

    /// <summary>심판.</summary>
    public class JudgmentTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상 적을 고를 수 있는 거리(대각선 포함).</summary>
        public int? Range { get; set; }

        /// <summary>피해량.</summary>
        public int? Damage { get; set; }

        /// <summary>밀어내는 칸 수.</summary>
        public int? PushCells { get; set; }
    }
}
