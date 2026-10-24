using System.Collections.Generic;
using MiniChess.Core.Characters.Shared;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Movement;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 공간 재봉사 (명세 11).
    /// 스킬 1 실 관문(아군 순간이동), 스킬 2 장애물 생성.
    /// </summary>
    public class SeamstressPiece : PieceModule
    {
        public const string PieceId = "SEAMSTRESS";
        public const string WarpId = "SEAMSTRESS_WARP";
        public const string ObstacleId = "SEAMSTRESS_OBSTACLE";

        public override string Id => PieceId;
        public override string Name => "공간 재봉사";
        public override CharacterRole Role => CharacterRole.Control;
        public override string Skill1Id => WarpId;
        public override string Skill2Id => ObstacleId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Warp(tuning.Seamstress.Warp);
            yield return CreateObstacle(tuning.Seamstress.Obstacle);
        }

        /// <summary>
        /// 장애물 생성: 사거리 안의 빈 칸 1개에 장애물을 세운다(명세 11.3, 사용자 확정).
        /// 이동만 막고 사거리는 막지 않는다. 양 팀 모두 기본 공격(AttackObstacleAction) Hits 회로 부술 수 있고,
        /// 스킬 피해로는 부서지지 않는다. 점령 칸에는 세울 수 없다.
        /// </summary>
        public static SkillDefinition CreateObstacle(ObstacleTuning t)
        {
            int? hits = t.Hits;

            return new SkillDefinition(
                ObstacleId, "장애물 생성", t.ApCost, SkillActionKind.Combat,
                new CellTargeting(t.Range, CellRequirement.Empty, placeableLayer: CellEffectLayer.Obstacle),
                PatternArea.SingleCell,
                new SkillEffect[]
                {
                    new PlaceZoneEffect(t.Lifetime, (zone, _) => new Obstacle(zone, hits.Value)),
                },
                new SkillCondition[]
                {
                    new MaxActiveZonesCondition(ObstacleId, t.MaxActive),
                },
                requiredTuning: SkillBuildUtil.Require((nameof(t.Hits), t.Hits)));
        }

        /// <summary>
        /// 실 관문: 마법실로 관문을 열어 사거리 안의 아군 하나(자신 포함)를 사거리 안의 빈 칸으로 순간이동시킨다(사용자 확정, 워프/위치 교환 대체).
        /// 다른 아군은 외부 이동이라 이미 공격했어도 옮길 수 있고 전투 행동은 회복되지 않으며, 속박 등은 외부 이동 정책을 따른다.
        /// 자신을 옮기면 자발적 이동이라 속박 중에는 지정할 수 없다. 도착 칸의 칸 효과는 발동한다.
        /// </summary>
        public static SkillDefinition Warp(WarpTuning t)
        {
            return new SkillDefinition(
                WarpId, "실 관문", t.ApCost, SkillActionKind.Combat,
                new UnitToCellTargeting(TargetFilter.AllyOrSelf, t.Range, MoveKind.Warp),
                PatternArea.SingleCell,
                new SkillEffect[] { new WarpUnitEffect() });
        }
    }

    public class SeamstressTuning
    {
        public WarpTuning Warp { get; } = new WarpTuning();
        public ObstacleTuning Obstacle { get; } = new ObstacleTuning();
    }

    /// <summary>장애물 생성(명세 11.3).</summary>
    public class ObstacleTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }

        /// <summary>파괴까지 필요한 기본 공격 피격 횟수(피해량 무관).</summary>
        public int? Hits { get; set; }

        /// <summary>수명(설치한 쪽 소유자 턴 종료 횟수).</summary>
        public int? Lifetime { get; set; }

        /// <summary>시전자당 동시 장애물 수.</summary>
        public int? MaxActive { get; set; }
    }

    /// <summary>실 관문(명세 11.2 워프/위치 교환을 사용자 확정으로 대체).</summary>
    public class WarpTuning
    {
        public int? ApCost { get; set; }

        /// <summary>옮길 아군과 도착 칸 각각이 시전자로부터 떨어질 수 있는 최대 거리.</summary>
        public int? Range { get; set; }
    }
}
