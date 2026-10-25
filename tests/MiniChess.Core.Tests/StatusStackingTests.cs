using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Characters;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.Presets;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Skills.Effects;
using MiniChess.Core.Skills.Targeting;
using MiniChess.Core.Snapshots;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Statuses.Library;
using MiniChess.Core.Tests.Support;
using MiniChess.Protocol;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 상태 쌓임 범위(사용자 확정): 출처별 상태(중독/화상/속박/기절/이동 제한)는 건 유닛 + 원인 스킬이 같을 때만 같은 건이라 갱신되고,
    /// 다르면 따로 걸려 각자 횟수를 세고 각자 발동한다. 대상당 하나인 상태(저주 표식 등)는 누가 걸어도 하나다.
    /// </summary>
    public class StatusStackingTests
    {
        private const string PoisonA = "TEST_POISON_A";
        private const string PoisonB = "TEST_POISON_B";

        // 같은 정의를 쓰는 두 스킬(1 × 2턴). 원인(스킬 Id)만 다르다.
        private static readonly StatusDefinition Poison = StatusLibrary.Poison(damagePerTrigger: 1, triggers: 2);

        private static SkillDefinition PoisonSkill(string id) =>
            new SkillDefinition(id, id, 1, new UnitTargeting(TargetFilter.Enemy, 6), PatternArea.SingleCell,
                new SkillEffect[] { new ApplyStatusEffect(Poison) });

        private static TestGame Game() => new TestGame()
            .WithSkills(PieceModules.CreateSkillCatalog(PrototypeTestPreset.CreateSkillTuning()).All.ToArray())
            .WithSkills(PoisonSkill(PoisonA), PoisonSkill(PoisonB));

        private static void EndTurn(GameState state) => new EndTurnAction(state.CurrentTeam).Execute(state);

        private static void Use(GameState state, Unit caster, string skill, int x, int y)
        {
            var action = new UseSkillAction(caster, skill, new Position(x, y));
            Assert.Equal(SkillFailReason.None, action.Validate(state));
            action.Execute(state);
        }

        [Fact]
        public void DifferentCasters_SameSkill_StackSeparately_AndBothDamage()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, TestGame.Stats("a", skills: PoisonA))
                .Place(Team.Player1, 1, 0, TestGame.Stats("b", skills: PoisonA))
                .Place(Team.Player2, 3, 3, TestGame.Stats(hp: 20))
                .Start();

            Use(state, u[0], PoisonA, 3, 3);
            Use(state, u[1], PoisonA, 3, 3);
            Assert.Equal(2, u[2].Statuses.Count(s => s.Definition.Id == StatusLibrary.PoisonId));

            EndTurn(state);
            EndTurn(state); // P2 종료: 두 건 각각 1
            Assert.Equal(18, u[2].Stats.CurrentHp);
            Assert.All(u[2].Statuses, s => Assert.Equal(1, s.Remaining));
        }

        [Fact]
        public void SameCaster_SameSkill_Refreshes()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, TestGame.Stats("a", skills: PoisonA))
                .Place(Team.Player2, 3, 3, TestGame.Stats(hp: 20))
                .Start();

            Use(state, u[0], PoisonA, 3, 3);
            EndTurn(state);
            EndTurn(state); // P2 종료: 1 피해, 남은 1
            Use(state, u[0], PoisonA, 3, 3);

            StatusEffect poison = Assert.Single(u[1].Statuses);
            Assert.Equal(2, poison.Remaining);
            Assert.Equal(PoisonA, poison.OriginId);
        }

        [Fact]
        public void SameCaster_DifferentSkill_StackSeparately()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, TestGame.Stats("a", skills: new[] { PoisonA, PoisonB }))
                .Place(Team.Player2, 3, 3, TestGame.Stats(hp: 20))
                .Start();

            Use(state, u[0], PoisonA, 3, 3);
            EndTurn(state);
            EndTurn(state);
            Use(state, u[0], PoisonB, 3, 3);

            Assert.Equal(new[] { PoisonA, PoisonB }, u[1].Statuses.Select(s => s.OriginId));
            Assert.Equal(new[] { 1, 2 }, u[1].Statuses.Select(s => s.Remaining));
        }

        // 대상당 하나인 상태는 다른 유닛/원인이 걸어도 하나로 갱신된다
        [Fact]
        public void PerTargetStatus_StaysSingle_AcrossSources()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0)
                .Place(Team.Player1, 1, 0)
                .Place(Team.Player2, 3, 3)
                .Start();
            StatusDefinition mark = StatusLibrary.CurseMark(2);

            StatusSystem.Apply(state, mark, u[2], u[0], "SKILL_X");
            StatusSystem.Apply(state, mark, u[2], u[1], "SKILL_Y");

            StatusEffect only = Assert.Single(u[2].Statuses);
            Assert.Same(u[1], only.Source);
        }

        // 정화는 상대 팀이 건 건을 모두 지운다
        [Fact]
        public void Purify_RemovesEverySeparateInstance()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, TestGame.Stats("a", skills: PoisonA))
                .Place(Team.Player1, 1, 0, TestGame.Stats("b", skills: PoisonA))
                .Place(Team.Player2, 3, 3, TestGame.Stats("inquisitor", skills: new[] { InquisitorPiece.PurifyId, InquisitorPiece.JudgmentId }))
                .Start();
            Use(state, u[0], PoisonA, 3, 3);
            Use(state, u[1], PoisonA, 3, 3);
            EndTurn(state);

            Use(state, u[2], InquisitorPiece.PurifyId, 3, 3);

            Assert.Empty(u[2].Statuses);
        }

        // 온라인: 원인과 건 유닛이 이벤트로 전달된다(클라이언트가 같은 Id 의 여러 건을 구분)
        [Fact]
        public void StatusEvents_CarryOriginOverTheWire()
        {
            var (state, u) = Game()
                .Place(Team.Player1, 0, 0, TestGame.Stats("a", skills: PoisonA))
                .Place(Team.Player2, 3, 3)
                .Start();
            Use(state, u[0], PoisonA, 3, 3);

            var applied = (StatusAppliedEvent)ProtocolJson.ReadEvent(ProtocolJson.Wrap(state.Events.All.OfType<StatusAppliedEvent>().Last()));
            Assert.Equal(PoisonA, applied.Status.OriginId);
            Assert.Equal(u[0].Id, applied.Status.SourceId);

            StatusSystem.Remove(state, u[1].Statuses.Single(), StatusRemoveReason.Removed);
            var removed = (StatusRemovedEvent)ProtocolJson.ReadEvent(ProtocolJson.Wrap(state.Events.All.OfType<StatusRemovedEvent>().Last()));
            Assert.Equal(PoisonA, removed.OriginId);
            Assert.Equal(u[0].Id, removed.SourceId);
        }
    }
}
