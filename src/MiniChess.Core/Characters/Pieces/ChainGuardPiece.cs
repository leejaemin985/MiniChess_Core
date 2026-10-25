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
    /// 사슬 수호기사 (명세 12).
    /// 스킬 1 호위, 스킬 2 사슬 속박.
    /// </summary>
    public class ChainGuardPiece : PieceModule
    {
        public const string PieceId = "CHAIN_GUARD";
        public const string GuardId = "CHAIN_GUARD_GUARD";
        public const string ChainBindId = "CHAIN_GUARD_CHAIN_BIND";

        public override string Id => PieceId;
        public override string Name => "사슬 수호기사";
        public override CharacterRole Role => CharacterRole.Guardian;
        public override string Skill1Id => GuardId;
        public override string Skill2Id => ChainBindId;

        public override IEnumerable<SkillDefinition> CreateSkills(SkillTuning tuning)
        {
            yield return Guard(tuning.ChainGuard.Guard);
            yield return ChainBind(tuning.ChainGuard.ChainBind, tuning.Root);
        }

        /// <summary>
        /// 호위: 아군 1명(자신 제외)의 직접 피해를 대신 받는다(명세 12.2, 사용자 확정: 비율 분담 없이 전부 대신 받음).
        /// 피해 시점에 MaxDistance 이내일 때만, 다음 내 턴 시작까지. 상세 규칙은 StatusLibrary.Guard.
        /// </summary>
        public static SkillDefinition Guard(GuardTuning t)
        {
            return new SkillDefinition(
                GuardId, "호위", t.ApCost,
                new UnitTargeting(TargetFilter.Ally, t.Range),
                PatternArea.SingleCell,
                new SkillEffect[] { new ApplyStatusEffect(StatusLibrary.Guard(t.MaxDistance), TargetFilter.Ally) },
                requiredTuning: SkillBuildUtil.Require((nameof(t.MaxDistance), t.MaxDistance)));
        }

        /// <summary>
        /// 사슬 속박: 적 단일 대상에게 한 턴 속박(자발적 이동 불가, 공격 가능). 덫과 같은 속박 개념(명세 12.3).
        /// </summary>
        public static SkillDefinition ChainBind(ChainBindTuning t, RootPolicyTuning root)
        {
            var effects = new List<SkillEffect>();
            SkillBuildUtil.AddOptionalDamage(effects, t.Damage);
            effects.Add(new ApplyStatusEffect(StatusLibrary.Root(1, root.BlocksExternalMoves)));

            return new SkillDefinition(
                ChainBindId, "사슬 속박", t.ApCost,
                new UnitTargeting(TargetFilter.Enemy, t.Range),
                PatternArea.SingleCell,
                effects,
                requiredTuning: SkillBuildUtil.Require(("Root." + nameof(root.BlocksExternalMoves), root.BlocksExternalMoves)));
        }
    }

    public class ChainGuardTuning
    {
        public GuardTuning Guard { get; } = new GuardTuning();
        public ChainBindTuning ChainBind { get; } = new ChainBindTuning();
    }

    /// <summary>호위(명세 12.2).</summary>
    public class GuardTuning
    {
        public int? ApCost { get; set; }

        /// <summary>호위를 걸 수 있는 거리.</summary>
        public int? Range { get; set; }

        /// <summary>피해 시점에 대신 받을 수 있는 호위자-대상 최대 거리(체비셰프).</summary>
        public int? MaxDistance { get; set; }
    }

    /// <summary>사슬 속박(명세 12.3). 지속은 "한 턴"으로 확정. 속박의 외부 이동 정책은 SkillTuning.Root 를 따른다.</summary>
    public class ChainBindTuning
    {
        public int? ApCost { get; set; }
        public int? Range { get; set; }

        /// <summary>피해. 0 이면 없음.</summary>
        public int? Damage { get; set; }
    }
}
