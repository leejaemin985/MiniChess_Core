using System.Collections.Generic;
using MiniChess.Core.Data;
using MiniChess.Core.Movement;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 도약 / 차징 암살자 (사용자 확정).
    /// 스킬 1 도약, 스킬 2 차징. 낮은 체력, 높은 기본 피해.
    /// 흐름: 차징 → 다음 턴 도약으로 접근 → 강화된 기본 공격으로 마무리.
    /// </summary>
    public class AssassinPiece : PieceModule
    {
        public const string PieceId = "ASSASSIN";
        public const string LeapId = "ASSASSIN_LEAP";
        public const string ChargeId = "ASSASSIN_CHARGE";

        public override string Id => PieceId;
        public override string Name => "도약 / 차징 암살자";
        public override CharacterRole Role => CharacterRole.Combat;
        public override string Skill1Id => LeapId;
        public override string Skill2Id => ChargeId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Leap(tuning.Assassin.Leap);
            yield return Charge(tuning.Assassin.Charge);
        }

        /// <summary>
        /// 도약: 사거리 안(대각선 포함)의 빈 칸으로 순간이동한다. 벽/유닛을 넘어간다.
        /// 속박 등 자발적 이동을 막는 상태면 불가. 도착 칸의 덫은 발동한다.
        /// </summary>
        public static SkillDefinition Leap(LeapTuning t)
        {
            return new SkillDefinition(
                LeapId, "도약", t.ApCost,
                new CellTargeting(t.Range, CellRequirement.Empty),
                PatternArea.SingleCell,
                new SkillEffect[] { new TeleportEffect() },
                new SkillCondition[] { new CanMoveCondition(MoveKind.Warp) });
        }

        /// <summary>
        /// 차징: 자신에게 차징 상태를 건다. 다음 내 턴 끝까지 첫 기본 공격에 추가 피해를 주고 소모된다(스킬 피해에는 미적용).
        /// </summary>
        public static SkillDefinition Charge(ChargeTuning t)
        {
            return new SkillDefinition(
                ChargeId, "차징", t.ApCost,
                new SelfTargeting(),
                PatternArea.SingleCell,
                new SkillEffect[] { new ApplyStatusEffect(StatusLibrary.Charged(t.BonusDamage), TargetFilter.Self) });
        }
    }

    public class AssassinTuning
    {
        public LeapTuning Leap { get; } = new LeapTuning();
        public ChargeTuning Charge { get; } = new ChargeTuning();
    }

    /// <summary>도약.</summary>
    public class LeapTuning
    {
        public int? ApCost { get; set; }

        /// <summary>도착 칸을 고를 수 있는 거리(대각선 포함).</summary>
        public int? Range { get; set; }
    }

    /// <summary>차징.</summary>
    public class ChargeTuning
    {
        public int? ApCost { get; set; }

        /// <summary>다음 기본 공격에 더하는 피해.</summary>
        public int? BonusDamage { get; set; }
    }
}
