using System.Collections.Generic;
using MiniChess.Core.Data;
using MiniChess.Core.Movement;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Conditions;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;

namespace MiniChess.Core.Characters.Pieces
{
    /// <summary>
    /// 수리검 / 연속 베기 암살자(닌자). 낮은 체력의 근접 암살자.
    /// 스킬 1 수리검(멀리서 적 한 명), 스킬 2 연속 베기(적 사이를 순간이동하며 베고 마지막 대상 옆에 착지).
    /// </summary>
    public class AssassinPiece : PieceModule
    {
        public const string PieceId = "ASSASSIN";
        public const string ShurikenId = "ASSASSIN_SHURIKEN";
        public const string ChainSlashId = "ASSASSIN_CHAIN_SLASH";

        /// <summary>연속 베기로 고를 수 있는 최대 대상 수.</summary>
        public const int ChainSlashMaxTargets = 2;

        public override string Id => PieceId;
        public override string Name => "수리검 / 연속 베기 암살자";
        public override CharacterRole Role => CharacterRole.Combat;
        public override string Skill1Id => ShurikenId;
        public override string Skill2Id => ChainSlashId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Shuriken(tuning.Assassin.Shuriken);
            yield return ChainSlash(tuning.Assassin.ChainSlash);
        }

        /// <summary>수리검: 사거리 안의 적 한 명에게 피해를 준다. 벽은 막지 않는다.</summary>
        public static SkillDefinition Shuriken(ShurikenTuning t)
        {
            return new SkillDefinition(
                ShurikenId, "수리검", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[] { new DamageEffect(t.Damage) });
        }

        /// <summary>
        /// 연속 베기: 사거리 안의 적을 고르고, 그 적의 연쇄 거리 안에 다른 적이 있으면 그 적까지(최대 2명, 같은 적 제외) 고른 뒤,
        /// 마지막 대상의 상하좌우 칸에 착지 칸을 고른다. 차례로 베고(1명이면 SingleDamage, 2명이면 각자 MultiDamage) 착지 칸으로 순간이동한다.
        /// 속박 등 자발적 순간이동을 막는 상태면 쓸 수 없다. 착지할 칸이 없는 대상은 고를 수 없다.
        /// </summary>
        public static SkillDefinition ChainSlash(ChainSlashTuning t)
        {
            return new SkillDefinition(
                ChainSlashId, "연속 베기", t.ApCost,
                new ChainStrikeTargeting(t.Range, t.ChainRange, ChainSlashMaxTargets),
                PatternArea.SingleCell,
                new SkillEffect[] { new ChainStrikeEffect(t.SingleDamage, t.MultiDamage) },
                new SkillCondition[] { new CanMoveCondition(MoveKind.Warp) });
        }
    }

    public class AssassinTuning
    {
        public ShurikenTuning Shuriken { get; } = new ShurikenTuning();
        public ChainSlashTuning ChainSlash { get; } = new ChainSlashTuning();
    }

    /// <summary>수리검.</summary>
    public class ShurikenTuning
    {
        public int? ApCost { get; set; }

        /// <summary>대상을 고를 수 있는 거리(대각선 포함).</summary>
        public int? Range { get; set; }

        public int? Damage { get; set; }
    }

    /// <summary>연속 베기.</summary>
    public class ChainSlashTuning
    {
        public int? ApCost { get; set; }

        /// <summary>첫 대상을 고를 수 있는 시전자 기준 거리(대각선 포함).</summary>
        public int? Range { get; set; }

        /// <summary>첫 대상 기준으로 두 번째 대상을 고를 수 있는 거리(대각선 포함).</summary>
        public int? ChainRange { get; set; }

        /// <summary>한 명만 벨 때 피해.</summary>
        public int? SingleDamage { get; set; }

        /// <summary>두 명을 벨 때 각자 받는 피해.</summary>
        public int? MultiDamage { get; set; }
    }
}
