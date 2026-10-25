using System.Collections.Generic;
using MiniChess.Core.Characters.Shared;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Statuses;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 화학공학 덫 전문가 (명세 9).
    /// 스킬 1 속박 덫, 스킬 2 독가스 설치.
    /// </summary>
    public class ChemistPiece : PieceModule
    {
        public const string PieceId = "CHEMIST";
        public const string RootTrapId = "CHEMIST_ROOT_TRAP";
        public const string PoisonGasId = "CHEMIST_POISON_GAS";

        public override string Id => PieceId;
        public override string Name => "화학공학 덫 전문가";
        public override CharacterRole Role => CharacterRole.Control;
        public override string Skill1Id => RootTrapId;
        public override string Skill2Id => PoisonGasId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return RootTrap(tuning.Chemist.RootTrap, tuning.Root);
            yield return PoisonGas(tuning.Chemist.PoisonGas);
        }

        /// <summary>
        /// 속박 덫: 빈 칸에 덫 설치. 적이 밟으면 피해 + 속박, 진행 중 이동 정지(명세 9.2 [설계안]).
        /// 점령 칸 설치 불가는 CellEffectSystem 이 적용한다.
        /// </summary>
        public static SkillDefinition RootTrap(RootTrapTuning t, RootPolicyTuning root)
        {
            StatusDefinition rootStatus = StatusLibrary.Root(t.RootTurns, root.BlocksExternalMoves);
            int? damage = t.Damage;
            bool? singleUse = t.SingleUse;

            return new SkillDefinition(
                RootTrapId, "속박 덫", t.ApCost,
                new CellTargeting(t.Range, CellRequirement.Empty, placeableLayer: CellEffectLayer.Trap),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new PlaceZoneEffect(t.Lifetime, (zone, _) => new Effects.Zones.RootTrap(zone, damage.Value, rootStatus, singleUse.Value)),
                },
                new SkillCondition[]
                {
                    new MaxActiveZonesCondition(RootTrapId, t.MaxActive),
                },
                requiredTuning: SkillBuildUtil.Require(
                    (nameof(t.Damage), t.Damage),
                    (nameof(t.RootTurns), t.RootTurns),
                    (nameof(t.SingleUse), t.SingleUse),
                    ("Root." + nameof(root.BlocksExternalMoves), root.BlocksExternalMoves)));
        }

        /// <summary>
        /// 독가스 설치: 독 장판 구역(사용자 확정). 장판 피해는 없고, 적이 들어오면(지나가기만 해도) 중독에 걸린다.
        /// 설치 순간 범위 안에 있던 적도 바로 중독된다. 중독은 대상 소유자 턴 종료마다 약한 지속 피해.
        /// </summary>
        public static SkillDefinition PoisonGas(PoisonGasTuning t)
        {
            StatusDefinition poison = StatusLibrary.Poison(t.PoisonDamage, t.PoisonTurns);

            return new SkillDefinition(
                PoisonGasId, "독가스 설치", t.ApCost,
                SkillBuildUtil.AreaTargeting(t.Area, t.Range, CellRequirement.NotWall),
                SkillBuildUtil.Area(t.Area),
                new SkillEffect[]
                {
                    new PlaceZoneEffect(t.Lifetime, (zone, _) => new PoisonField(zone, poison)),
                    new ApplyStatusEffect(poison),
                },
                new SkillCondition[]
                {
                    new MaxActiveZonesCondition(PoisonGasId, t.MaxActive),
                });
        }
    }

    public class ChemistTuning
    {
        public RootTrapTuning RootTrap { get; } = new RootTrapTuning();
        public PoisonGasTuning PoisonGas { get; } = new PoisonGasTuning();
    }

    /// <summary>속박 덫(명세 9.2). 속박의 외부 이동 정책은 SkillTuning.Root 를 따른다.</summary>
    public class RootTrapTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }

        /// <summary>덫 피해. 0 이면 없음.</summary>
        public int? Damage { get; set; }

        /// <summary>속박 기간(대상 소유자 턴 종료 횟수).</summary>
        public int? RootTurns { get; set; }

        /// <summary>시전자당 동시에 유지할 수 있는 덫 수.</summary>
        public int? MaxActive { get; set; }

        /// <summary>덫 수명(설치한 쪽 소유자 턴 종료 횟수).</summary>
        public int? Lifetime { get; set; }

        /// <summary>발동 후 사라지는지.</summary>
        public bool? SingleUse { get; set; }
    }

    /// <summary>독가스 설치(명세 9.3).</summary>
    public class PoisonGasTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }

        /// <summary>구역 범위. [설계안] 2×2.</summary>
        public AreaTuning Area { get; } = new AreaTuning();

        /// <summary>중독 1회 피해.</summary>
        public int? PoisonDamage { get; set; }

        /// <summary>중독 발동 횟수(대상 소유자 턴 종료 횟수).</summary>
        public int? PoisonTurns { get; set; }

        /// <summary>구역 수명(설치한 쪽 소유자 턴 종료 횟수).</summary>
        public int? Lifetime { get; set; }

        /// <summary>시전자당 동시 구역 수. [설계안] 1.</summary>
        public int? MaxActive { get; set; }
    }
}
