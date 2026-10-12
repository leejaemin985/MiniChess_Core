using System.Collections.Generic;
using MiniChess.Core.Characters.Shared;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 마법공학 박격포 여학생 (명세 5).
    /// 스킬 1 설치/해체, 스킬 2 지연 포격.
    /// 흐름: (해체 상태로 시작) 설치 → 조준 → 다음 턴 시작에 착탄 → 다시 조준 또는 해체.
    /// </summary>
    public class MortarPiece : PieceModule
    {
        public const string PieceId = "MORTAR";
        public const string InstallId = "MORTAR_INSTALL";
        public const string DelayedStrikeId = "MORTAR_DELAYED_STRIKE";

        public override string Id => PieceId;
        public override string Name => "마법공학 박격포 여학생";
        public override CharacterRole Role => CharacterRole.Control;
        public override string Skill1Id => InstallId;
        public override string Skill2Id => DelayedStrikeId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Install(tuning.Mortar.Install);
            yield return DelayedStrike(tuning.Mortar.DelayedStrike);
        }

        /// <summary>
        /// 설치/해체: 같은 버튼으로 설치 상태를 켜고 끈다(명세 5.2, 사용자 확정). 전투 행동이다.
        /// 설치 중: 자발적 이동/기본 공격 불가, 외부 강제 이동은 허용, 지연 포격 사용 가능.
        /// 설치하면 보호막(Id = 스킬 Id)을 받고(재설치 시 중첩 없이 재충전), 해체하면 그 보호막이 사라진다.
        /// </summary>
        public static SkillDefinition Install(InstallTuning t)
        {
            return new SkillDefinition(
                InstallId, "설치 / 해체", t.ApCost, SkillActionKind.Combat,
                new SelfTargeting(),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new ToggleStatusEffect(
                        StatusLibrary.Installed(),
                        onEnable: new SkillEffect[] { new ApplyShieldEffect(InstallId, t.ShieldAmount) },
                        onDisable: new SkillEffect[] { new RemoveShieldEffect(InstallId) }),
                });
        }

        /// <summary>
        /// 지연 포격: 시전 시 범위를 조준하고, 다음 내 턴 시작에 착탄해 범위 안 적에게 장판 피해(명세 5.3, 사용자 확정).
        /// 설치 상태에서만 쓸 수 있다. 발사는 행동을 소모하지 않으므로, 발사된 턴에는 다시 조준하거나 해체할 수 있다
        /// (설치 중이라 이동/기본 공격은 불가). 착탄 위치는 조준한 칸에 고정. 착탄 전에 시전자가 죽으면 예약이 취소된다.
        /// </summary>
        public static SkillDefinition DelayedStrike(DelayedStrikeTuning t)
        {
            return new SkillDefinition(
                DelayedStrikeId, "지연 포격", t.ApCost, SkillActionKind.Combat,
                SkillBuildUtil.AreaTargeting(t.Area, t.Range, CellRequirement.NotWall),
                SkillBuildUtil.Area(t.Area),
                new SkillEffect[] { new ScheduleStrikeEffect(t.Damage) },
                new SkillCondition[]
                {
                    new HasStatusCondition(StatusLibrary.InstalledId),
                    new MaxActiveStrikesCondition(DelayedStrikeId, t.MaxActive),
                });
        }
    }

    public class MortarTuning
    {
        public InstallTuning Install { get; } = new InstallTuning();
        public DelayedStrikeTuning DelayedStrike { get; } = new DelayedStrikeTuning();
    }

    /// <summary>설치/해체(명세 5.2). 설치와 해체의 AP 는 같다.</summary>
    public class InstallTuning
    {
        public int? ApCost { get; set; }

        /// <summary>설치 시 받는 보호막 양. 해체하면 사라진다.</summary>
        public int? ShieldAmount { get; set; }
    }

    /// <summary>지연 포격(명세 5.3).</summary>
    public class DelayedStrikeTuning
    {
        public int? ApCost { get; set; }

        /// <summary>조준 기준 칸을 지정할 수 있는 거리.</summary>
        public int? Range { get; set; }

        public AreaTuning Area { get; } = new AreaTuning();

        /// <summary>착탄 시 범위 안 적 각각에게 주는 장판 피해.</summary>
        public int? Damage { get; set; }

        /// <summary>시전자당 동시 예약 수.</summary>
        public int? MaxActive { get; set; }
    }
}
