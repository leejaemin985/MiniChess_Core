using System.Collections.Generic;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 마법석 조각가 (기획 확정).
    /// 스킬 1 마법석 봉인, 스킬 2 자리 교정. 유닛의 전투 참여를 잠시 끊고 적의 위치를 조정하는 제어형.
    /// </summary>
    public class SculptorPiece : PieceModule
    {
        public const string PieceId = "SCULPTOR";
        public const string SealId = "SCULPTOR_SEAL";
        public const string CorrectionId = "SCULPTOR_CORRECTION";

        public override string Id => PieceId;
        public override string Name => "마법석 조각가";
        public override CharacterRole Role => CharacterRole.Control;
        public override string Skill1Id => SealId;
        public override string Skill2Id => CorrectionId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Seal(tuning.Sculptor.Seal);
            yield return Correction(tuning.Sculptor.Correction);
        }

        /// <summary>
        /// 마법석 봉인: 사거리 안의 유닛 하나(아군·적·자신)를 봉인한다.
        /// 봉인 중에는 행동할 수 없고 대상으로 지정되지 않으며 피해와 회복을 받지 않는다(StatusLibrary.Sealed).
        /// </summary>
        public static SkillDefinition Seal(SealTuning t)
        {
            return new SkillDefinition(
                SealId, "마법석 봉인", t.ApCost, SkillActionKind.Combat,
                new UnitTargeting(TargetFilter.Any, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[] { new ApplyStatusEffect(StatusLibrary.Sealed(), TargetFilter.Any) });
        }

        /// <summary>
        /// 자리 교정: 사거리 안의 적 하나를 지정하고, 상하좌우로 PushCells 칸 안의 칸 하나를 골라 그쪽으로 그 거리만큼 민다(강제 이동, 사용자 확정: 1~2칸 선택).
        /// 도중에 막히면 그 앞에서 멈춘다. 막힌 방향도 고를 수 있으며(밀기 시도) 그때는 밀리지 않고 피해만 받는다.
        /// </summary>
        public static SkillDefinition Correction(CorrectionTuning t)
        {
            return new SkillDefinition(
                CorrectionId, "자리 교정", t.ApCost, SkillActionKind.Combat,
                new PushDirectionTargeting(TargetFilter.Enemy, t.Range, t.PushCells),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new DamageEffect(t.Damage),
                    new PushInDirectionEffect(t.PushCells),
                });
        }
    }

    public class SculptorTuning
    {
        public SealTuning Seal { get; } = new SealTuning();
        public CorrectionTuning Correction { get; } = new CorrectionTuning();
    }

    /// <summary>마법석 봉인.</summary>
    public class SealTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상 유닛을 고를 수 있는 거리(대각선 포함).</summary>
        public int? Range { get; set; }
    }

    /// <summary>자리 교정.</summary>
    public class CorrectionTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상 적을 고를 수 있는 거리(대각선 포함).</summary>
        public int? Range { get; set; }

        public int? Damage { get; set; }

        /// <summary>고를 수 있는 최대 밀기 칸 수(1 ~ PushCells 중 선택).</summary>
        public int? PushCells { get; set; }
    }
}
