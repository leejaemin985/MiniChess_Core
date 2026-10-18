using System.Collections.Generic;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 대검 전사 (명세 7).
    /// 스킬 1 강타, 스킬 2 돌진.
    /// </summary>
    public class WarriorPiece : PieceModule
    {
        public const string PieceId = "WARRIOR";
        public const string SmashId = "WARRIOR_SMASH";
        public const string DashId = "WARRIOR_DASH";

        public override string Id => PieceId;
        public override string Name => "대검 전사";
        public override CharacterRole Role => CharacterRole.Combat;
        public override string Skill1Id => SmashId;
        public override string Skill2Id => DashId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Smash(tuning.Warrior.Smash);
            yield return Dash(tuning.Warrior.Dash);
        }

        /// <summary>
        /// 강타: 인접한 적에게 높은 피해를 주는 단일 공격. 부가 효과 없음(명세 7.2).
        /// [가정] "인접"은 사거리 1(체비셰프, 대각선 포함).
        /// </summary>
        public static SkillDefinition Smash(SmashTuning t)
        {
            return new SkillDefinition(
                SmashId, "강타", t.ApCost, SkillActionKind.Combat,
                new UnitTargeting(TargetFilter.Enemy, 1),
                PatternArea.SingleCell,
                new SkillEffect[] { new DamageEffect(t.Damage) });
        }

        /// <summary>
        /// 돌진: 제어기의 멈춤을 무시하는 직선 이동 후, 전방 1칸의 적을 공격(명세 7.3, 사용자 확정).
        /// 이동 제약 상태(속박, 이동 거리 제한 등)는 무시하고 쓸 수 있다(사용자 확정). 행동 자체를 막는 상태(봉인, 기절)에서만
        /// 쓸 수 없으며, 이는 스킬 공통 검사(StatusBehavior.BlocksSkillUse)가 막는다.
        /// 전투 행동이므로 기본 공격과 같은 턴에 함께 쓸 수 없다.
        /// </summary>
        public static SkillDefinition Dash(DashTuning t)
        {
            return new SkillDefinition(
                DashId, "돌진", t.ApCost, SkillActionKind.Combat,
                new StraightMoveTargeting(t.Range),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new DashEffect(),
                    new FrontStrikeEffect(t.Damage),
                });
        }
    }

    public class WarriorTuning
    {
        public SmashTuning Smash { get; } = new SmashTuning();
        public DashTuning Dash { get; } = new DashTuning();
    }

    /// <summary>강타(명세 7.2).</summary>
    public class SmashTuning
    {
        public int? ApCost { get; set; }
        public int? Damage { get; set; }
    }

    /// <summary>돌진(명세 7.3).</summary>
    public class DashTuning
    {
        public int? ApCost { get; set; }

        /// <summary>최대 이동 칸 수(상하좌우 직선).</summary>
        public int? Range { get; set; }

        /// <summary>도착 후 전방 1칸 적에게 주는 피해.</summary>
        public int? Damage { get; set; }
    }
}
