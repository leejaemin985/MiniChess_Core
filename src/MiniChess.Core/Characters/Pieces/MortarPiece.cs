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
    /// 마법공학 박격포 여학생 (명세 5, 사용자 확정).
    /// 스킬 1 포격, 스킬 2 충격탄, 고유 기능 해체.
    /// 흐름: (해체 상태로 시작) 포격 = 설치 + 조준 → 다음 내 턴 시작에 착탄 → 다시 포격 또는 해체 → 해체 상태에서 충격탄.
    /// </summary>
    public class MortarPiece : PieceModule
    {
        public const string PieceId = "MORTAR";
        public const string BombardId = "MORTAR_BOMBARD";
        public const string ShockShellId = "MORTAR_SHOCK_SHELL";
        public const string UninstallId = "MORTAR_UNINSTALL";

        /// <summary>설치 중에 유지되는 보호막의 Id. 포격으로 설치할 때 받고 해체할 때 사라진다.</summary>
        public const string InstallShieldId = "MORTAR_INSTALL_SHIELD";

        public override string Id => PieceId;
        public override string Name => "마법공학 박격포 여학생";
        public override CharacterRole Role => CharacterRole.Control;
        public override string Skill1Id => BombardId;
        public override string Skill2Id => ShockShellId;
        public override IReadOnlyList<string> ExtraSkillIds => new[] { UninstallId };

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Bombard(tuning.Mortar.Bombard);
            yield return ShockShell(tuning.Mortar.ShockShell);
            yield return Uninstall(tuning.Mortar.Uninstall);
        }

        /// <summary>
        /// 포격: 범위를 조준하고 다음 내 턴 시작에 착탄해 범위 안 적에게 장판 피해.
        /// 설치 상태가 아니면 이때 설치되며 보호막을 받는다(이미 설치 중이면 보호막은 다시 받지 않는다).
        /// 설치 중에는 자발적 이동/기본 공격이 막히므로, 착탄한 턴에는 다시 포격하거나 해체만 할 수 있다.
        /// 착탄 위치는 조준한 칸에 고정. 착탄 전에 시전자가 죽으면 예약이 취소된다.
        /// </summary>
        public static SkillDefinition Bombard(BombardTuning t)
        {
            return new SkillDefinition(
                BombardId, "포격", t.ApCost,
                SkillBuildUtil.AreaTargeting(t.Area, t.Range, CellRequirement.NotWall),
                SkillBuildUtil.Area(t.Area),
                new SkillEffect[]
                {
                    new ApplySelfStatusEffect(
                        StatusLibrary.Installed(),
                        onApplied: new SkillEffect[] { new ApplyShieldEffect(InstallShieldId, t.InstallShieldAmount) }),
                    new ScheduleStrikeEffect(t.Damage),
                },
                new SkillCondition[] { new MaxActiveStrikesCondition(BombardId, t.MaxActive) });
        }

        /// <summary>
        /// 해체: 설치 상태를 풀고 설치 보호막을 제거한다. 설치 중에만 보이는 고유 기능이다.
        /// </summary>
        public static SkillDefinition Uninstall(UninstallTuning t)
        {
            return new SkillDefinition(
                UninstallId, "해체", t.ApCost,
                new SelfTargeting(),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new RemoveSelfStatusEffect(
                        StatusLibrary.InstalledId,
                        onRemoved: new SkillEffect[] { new RemoveShieldEffect(InstallShieldId) }),
                },
                new SkillCondition[] { new HasStatusCondition(StatusLibrary.InstalledId) });
        }

        /// <summary>
        /// 충격탄: 사거리 안의 적 1명에게 피해를 주고 밀어낸 뒤(코어 밀어내기 규칙) 이동 거리 제한을 건다(사용자 확정). 설치 중에는 쓸 수 없다.
        /// 막혀서 덜 밀리거나 못 밀려도 제한은 걸린다. 피해로 죽거나 밀리는 도중 죽으면 제한은 걸리지 않는다.
        /// </summary>
        public static SkillDefinition ShockShell(ShockShellTuning t)
        {
            return new SkillDefinition(
                ShockShellId, "충격탄", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new DamageEffect(t.Damage),
                    new KnockbackEffect(t.PushCells, afterPush: new SkillEffect[]
                    {
                        new ApplyStatusEffect(StatusLibrary.DistanceLimit(t.LimitCells, t.LimitTurns)),
                    }),
                },
                new SkillCondition[] { new LacksStatusCondition(StatusLibrary.InstalledId) },
                requiredTuning: SkillBuildUtil.Require(
                    (nameof(t.LimitCells), t.LimitCells),
                    (nameof(t.LimitTurns), t.LimitTurns)));
        }
    }

    public class MortarTuning
    {
        public BombardTuning Bombard { get; } = new BombardTuning();
        public ShockShellTuning ShockShell { get; } = new ShockShellTuning();
        public UninstallTuning Uninstall { get; } = new UninstallTuning();
    }

    /// <summary>포격(명세 5.3).</summary>
    public class BombardTuning
    {
        public int? ApCost { get; set; }

        /// <summary>조준 기준 칸을 지정할 수 있는 거리.</summary>
        public int? Range { get; set; }

        public AreaTuning Area { get; } = new AreaTuning();

        /// <summary>착탄 시 범위 안 적 각각에게 주는 장판 피해.</summary>
        public int? Damage { get; set; }

        /// <summary>시전자당 동시 예약 수.</summary>
        public int? MaxActive { get; set; }

        /// <summary>포격으로 설치될 때 받는 보호막 양. 해체하면 사라진다.</summary>
        public int? InstallShieldAmount { get; set; }
    }

    /// <summary>해체(명세 5.2).</summary>
    public class UninstallTuning
    {
        public int? ApCost { get; set; }
    }

    /// <summary>충격탄.</summary>
    public class ShockShellTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상 적을 지정할 수 있는 거리.</summary>
        public int? Range { get; set; }

        /// <summary>대상에게 주는 피해.</summary>
        public int? Damage { get; set; }

        /// <summary>밀어내는 칸 수.</summary>
        public int? PushCells { get; set; }

        /// <summary>밀어낸 뒤 거는 이동 거리 제한의 누적 최대 칸 수.</summary>
        public int? LimitCells { get; set; }

        /// <summary>이동 거리 제한 지속(대상 소유자 턴 종료 횟수).</summary>
        public int? LimitTurns { get; set; }
    }
}
