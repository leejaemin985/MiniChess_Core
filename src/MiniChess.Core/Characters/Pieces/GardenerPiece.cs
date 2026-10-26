using System.Collections.Generic;
using MiniChess.Core.Characters.Shared;
using MiniChess.Core.Data;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 원예부 선배 수호자 (명세 6).
    /// 스킬 1 회복 초원, 스킬 2 단일 보호막.
    /// </summary>
    public class GardenerPiece : PieceModule
    {
        public const string PieceId = "GARDENER";
        public const string HealingMeadowId = "GARDENER_HEALING_MEADOW";
        public const string SingleShieldId = "GARDENER_SINGLE_SHIELD";

        public override string Id => PieceId;
        public override string Name => "원예부 선배 수호자";
        public override CharacterRole Role => CharacterRole.Guardian;
        public override string Skill1Id => HealingMeadowId;
        public override string Skill2Id => SingleShieldId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return HealingMeadow(tuning.Gardener.HealingMeadow);
            yield return SingleShield(tuning.Gardener.SingleShield);
        }

        /// <summary>
        /// 회복 초원: 고정 회복 장판. 장판 위 아군을 매 턴 시작에 회복(명세 6.2). 자기 칸 중심 설치 허용.
        /// 시전자당 동시 구역 수는 MaxActive 까지(사용자 확정: 1, 다른 장판과 같다).
        /// </summary>
        public static SkillDefinition HealingMeadow(HealingMeadowTuning t)
        {
            int? heal = t.Heal;

            return new SkillDefinition(
                HealingMeadowId, "회복 초원", t.ApCost,
                SkillBuildUtil.AreaTargeting(t.Area, t.Range, CellRequirement.NotWall, includeCasterCell: true),
                SkillBuildUtil.Area(t.Area),
                new SkillEffect[]
                {
                    new PlaceZoneEffect(t.Lifetime, (zone, _) => new HealField(zone, heal.Value)),
                },
                new SkillCondition[]
                {
                    new MaxActiveZonesCondition(HealingMeadowId, t.MaxActive),
                },
                requiredTuning: SkillBuildUtil.Require((nameof(t.Heal), t.Heal)));
        }

        /// <summary>
        /// 단일 보호막: 아군 1명(자신 포함)에게 보호막을 준다(명세 6.3, 사용자 확정: 보호막안 채택, 자신 대상 허용).
        /// 보호막 Id 는 스킬 Id 와 같다. 같은 대상에 다시 쓰면 중첩 없이 재충전되고, 소멸 기한은 없다.
        /// </summary>
        public static SkillDefinition SingleShield(SingleShieldTuning t)
        {
            return new SkillDefinition(
                SingleShieldId, "단일 보호막", t.ApCost,
                new UnitTargeting(TargetFilter.AllyOrSelf, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[] { new ApplyShieldEffect(SingleShieldId, t.Amount, TargetFilter.AllyOrSelf) });
        }
    }

    public class GardenerTuning
    {
        public HealingMeadowTuning HealingMeadow { get; } = new HealingMeadowTuning();
        public SingleShieldTuning SingleShield { get; } = new SingleShieldTuning();
    }

    /// <summary>단일 보호막(명세 6.3).</summary>
    public class SingleShieldTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상 아군을 지정할 수 있는 거리.</summary>
        public int? Range { get; set; }

        /// <summary>보호막 양.</summary>
        public int? Amount { get; set; }
    }

    /// <summary>회복 초원(명세 6.2).</summary>
    public class HealingMeadowTuning
    {
        public int? ApCost { get; set; }

        /// <summary>장판 중심을 지정할 수 있는 거리.</summary>
        public int? Range { get; set; }

        /// <summary>장판 범위. [설계안] 3×3.</summary>
        public AreaTuning Area { get; } = new AreaTuning();

        /// <summary>턴 시작마다 회복량.</summary>
        public int? Heal { get; set; }

        /// <summary>장판 수명(설치한 쪽 소유자 턴 종료 횟수).</summary>
        public int? Lifetime { get; set; }

        /// <summary>시전자당 동시 구역 수.</summary>
        public int? MaxActive { get; set; }
    }
}
