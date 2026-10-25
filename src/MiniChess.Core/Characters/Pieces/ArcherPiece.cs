using System.Collections.Generic;
using MiniChess.Core.Characters.Shared;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 궁수 / 볼라 사냥꾼 (명세 8).
    /// 스킬 1 장거리 조준, 스킬 2 볼라 투척.
    /// </summary>
    public class ArcherPiece : PieceModule
    {
        public const string PieceId = "ARCHER";
        public const string AimedShotId = "ARCHER_AIMED_SHOT";
        public const string BolaId = "ARCHER_BOLA";

        public override string Id => PieceId;
        public override string Name => "궁수 / 볼라 사냥꾼";
        public override CharacterRole Role => CharacterRole.Combat;
        public override string Skill1Id => AimedShotId;
        public override string Skill2Id => BolaId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return AimedShot(tuning.Archer.AimedShot);
            yield return Bola(tuning.Archer.Bola);
        }

        /// <summary>장거리 조준: 장거리 단일 공격. 조준 준비 턴/명중률 없음(명세 8.2).</summary>
        public static SkillDefinition AimedShot(AimedShotTuning t)
        {
            return new SkillDefinition(
                AimedShotId, "장거리 조준", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[] { new DamageEffect(t.Damage) });
        }

        /// <summary>
        /// 볼라 투척: 적 단일 대상 공격/디버프. 대상의 다음 행동 턴 동안 자발적 이동을 누적 MaxCells 칸으로 제한한다(명세 8.3).
        /// 속박(이동 완전 불가)과는 다른 상태다. 외부 강제 이동은 제한하지 않는다.
        /// </summary>
        public static SkillDefinition Bola(BolaTuning t)
        {
            var effects = new List<SkillEffect>();
            SkillBuildUtil.AddOptionalDamage(effects, t.Damage);
            effects.Add(new ApplyStatusEffect(StatusLibrary.DistanceLimit(t.MaxCells, t.TargetTurns)));

            return new SkillDefinition(
                BolaId, "볼라 투척", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                effects,
                requiredTuning: SkillBuildUtil.Require(
                    (nameof(t.MaxCells), t.MaxCells),
                    (nameof(t.TargetTurns), t.TargetTurns)));
        }
    }

    public class ArcherTuning
    {
        public AimedShotTuning AimedShot { get; } = new AimedShotTuning();
        public BolaTuning Bola { get; } = new BolaTuning();
    }

    /// <summary>장거리 조준(명세 8.2).</summary>
    public class AimedShotTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }
        public int? Damage { get; set; }
    }

    /// <summary>볼라 투척(명세 8.3).</summary>
    public class BolaTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }

        /// <summary>명중 피해. 0 이면 없음.</summary>
        public int? Damage { get; set; }

        /// <summary>제한 중 한 턴에 자발적으로 이동할 수 있는 최대 칸 수(누적).</summary>
        public int? MaxCells { get; set; }

        /// <summary>제한 기간(대상 소유자 턴 종료 횟수).</summary>
        public int? TargetTurns { get; set; }
    }
}
