using System.Collections.Generic;
using MiniChess.Core.Characters.Shared;
using MiniChess.Core.Data;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 소형 중화기 화염 딜러 (명세 10).
    /// 스킬 1 화염지대, 스킬 2 압축열탄.
    /// </summary>
    public class FlamePiece : PieceModule
    {
        public const string PieceId = "FLAME";
        public const string FlameZoneId = "FLAME_FLAME_ZONE";
        public const string CompressedShellId = "FLAME_COMPRESSED_SHELL";

        public override string Id => PieceId;
        public override string Name => "소형 중화기 화염 딜러";
        public override CharacterRole Role => CharacterRole.Combat;
        public override string Skill1Id => FlameZoneId;
        public override string Skill2Id => CompressedShellId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return FlameZone(tuning.Flame.FlameZone);
            yield return CompressedShell(tuning.Flame.CompressedShell);
        }

        /// <summary>
        /// 화염지대: 설치 즉시 범위 안 적에게 피해를 주고 피해 장판 구역을 깐다(사용자 확정).
        /// 장판 위 적에게는 그 적 소유자 Turn End 에 피해(사용자 확정: 턴 종료만, 화상 없음).
        /// 다른 장판과 겹치면 교체되고, 점령 칸에도 설치할 수 있다(명세 10.2).
        /// </summary>
        public static SkillDefinition FlameZone(FlameZoneTuning t)
        {
            int? damage = t.Damage;

            var effects = new List<SkillEffect>();
            SkillBuildUtil.AddOptionalDamage(effects, t.ImpactDamage);
            effects.Add(new PlaceZoneEffect(t.Lifetime, (zone, _) => new DamageField(zone, damage.Value)));

            return new SkillDefinition(
                FlameZoneId, "화염지대", t.ApCost,
                SkillBuildUtil.AreaTargeting(t.Area, t.Range, CellRequirement.NotWall),
                SkillBuildUtil.Area(t.Area),
                effects,
                new SkillCondition[]
                {
                    new MaxActiveZonesCondition(FlameZoneId, t.MaxActive),
                },
                requiredTuning: SkillBuildUtil.Require((nameof(t.Damage), t.Damage)));
        }

        /// <summary>압축열탄: 단일 대상 화상. 화상은 대상 소유자 Turn End 피해, 스택 대신 갱신(명세 10.3).</summary>
        public static SkillDefinition CompressedShell(CompressedShellTuning t)
        {
            var effects = new List<SkillEffect>();
            SkillBuildUtil.AddOptionalDamage(effects, t.ImpactDamage);
            effects.Add(new ApplyStatusEffect(StatusLibrary.Burn(t.BurnDamage, t.BurnTriggers)));

            return new SkillDefinition(
                CompressedShellId, "압축열탄", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                effects,
                requiredTuning: SkillBuildUtil.Require(
                    (nameof(t.BurnDamage), t.BurnDamage),
                    (nameof(t.BurnTriggers), t.BurnTriggers)));
        }
    }

    public class FlameTuning
    {
        public FlameZoneTuning FlameZone { get; } = new FlameZoneTuning();
        public CompressedShellTuning CompressedShell { get; } = new CompressedShellTuning();
    }

    /// <summary>화염지대(명세 10.2).</summary>
    public class FlameZoneTuning
    {
        public int? ApCost { get; set; }

        /// <summary>선택 칸 기준 범위일 때 설치 사거리.</summary>
        public int? Range { get; set; }

        public AreaTuning Area { get; } = new AreaTuning();

        /// <summary>설치 즉시 범위 안 적에게 주는 피해. 0 이면 없음.</summary>
        public int? ImpactDamage { get; set; }

        /// <summary>적 소유자 턴 종료마다 주는 장판 피해.</summary>
        public int? Damage { get; set; }

        /// <summary>구역 수명(설치한 쪽 소유자 턴 종료 횟수).</summary>
        public int? Lifetime { get; set; }

        /// <summary>시전자당 동시 구역 수.</summary>
        public int? MaxActive { get; set; }
    }

    /// <summary>압축열탄(명세 10.3).</summary>
    public class CompressedShellTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }

        /// <summary>명중 즉시 피해. 0 이면 없음.</summary>
        public int? ImpactDamage { get; set; }

        /// <summary>화상 1회 피해.</summary>
        public int? BurnDamage { get; set; }

        /// <summary>화상 발동 횟수(대상 소유자 턴 종료 횟수).</summary>
        public int? BurnTriggers { get; set; }
    }
}
