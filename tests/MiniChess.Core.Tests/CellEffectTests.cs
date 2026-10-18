using MiniChess.Core.Actions;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Turns;
using Xunit;

namespace MiniChess.Core.Tests
{
    public class CellEffectTests
    {
        private static readonly string[] CaptureMap =
        {
            ".......",
            ".......",
            ".......",
            "...C...",
            ".......",
            ".......",
            ".......",
        };

        private static readonly Position Center = new(3, 3);

        // 명세 14: "3×3 회복지대 일부에 독가스 설치" → 겹치는 타일의 회복지대가 교체됨
        [Fact]
        public void AreaEffect_OverwritesExistingAreaEffectOnSameTile()
        {
            var (state, _) = new TestGame().Place(Team.Player1, 0, 0).Start();
            var heal = new TestCellEffect { Layer = CellEffectLayer.AreaEffect };
            var gas = new TestCellEffect { Layer = CellEffectLayer.AreaEffect };
            CellEffectSystem.Place(state, new Position(2, 2), heal);

            CellEffectSystem.Place(state, new Position(2, 2), gas);

            Assert.Same(gas, state.Board.GetCell(new Position(2, 2)).GetEffect(CellEffectLayer.AreaEffect));
            var removed = state.Events.All.OfType<CellEffectRemovedEvent>().Single();
            Assert.Equal(heal.Kind, removed.Kind);
            Assert.Equal(CellEffectRemoveReason.Replaced, removed.Reason);
        }

        // 명세 14: "장판 위에 덫 존재" → 별도 레이어이므로 가능
        [Fact]
        public void TrapAndAreaEffect_Coexist()
        {
            var (state, _) = new TestGame().Place(Team.Player1, 0, 0).Start();
            var area = new TestCellEffect { Layer = CellEffectLayer.AreaEffect };
            var trap = new TestCellEffect { Layer = CellEffectLayer.Trap };

            CellEffectSystem.Place(state, new Position(2, 2), area);
            CellEffectSystem.Place(state, new Position(2, 2), trap);

            BoardCell cell = state.Board.GetCell(new Position(2, 2));
            Assert.Same(area, cell.GetEffect(CellEffectLayer.AreaEffect));
            Assert.Same(trap, cell.GetEffect(CellEffectLayer.Trap));
        }

        // 명세 14: "점령 타일에 덫 생성" → 거부
        [Fact]
        public void Trap_OnCaptureTile_IsRejected()
        {
            var (state, _) = new TestGame().WithMap(CaptureMap).Place(Team.Player1, 0, 0).Start();

            CellEffectPlaceFailReason reason = CellEffectSystem.Place(state, Center, new TestCellEffect { Layer = CellEffectLayer.Trap });

            Assert.Equal(CellEffectPlaceFailReason.CaptureTileRestricted, reason);
            Assert.Null(state.Board.GetCell(Center).GetEffect(CellEffectLayer.Trap));
        }

        // 명세 14: "점령 타일에 회복/독가스/화염 장판" → 허용
        [Fact]
        public void AreaEffect_OnCaptureTile_IsAllowed()
        {
            var (state, _) = new TestGame().WithMap(CaptureMap).Place(Team.Player1, 0, 0).Start();

            CellEffectPlaceFailReason reason = CellEffectSystem.Place(state, Center, new TestCellEffect { Layer = CellEffectLayer.AreaEffect });

            Assert.Equal(CellEffectPlaceFailReason.None, reason);
        }

        [Fact]
        public void Effect_OnWall_IsRejected()
        {
            var (state, _) = new TestGame()
                .WithMap(".......", ".......", ".......", ".......", ".......", ".......", "..#....")
                .Place(Team.Player1, 0, 0)
                .Start();

            Assert.Equal(CellEffectPlaceFailReason.Wall,
                CellEffectSystem.Place(state, new Position(2, 0), new TestCellEffect { Layer = CellEffectLayer.AreaEffect }));
        }

        // 명세 14: "턴 시작 회복 초원" → 지정한 Turn Start 에만 회복, 종료형으로 중복 발동 금지
        [Fact]
        public void TurnStartHealField_HealsOnlyAtOwnersTurnStart()
        {
            var (state, u) = new TestGame()
                .Place(Team.Player1, 0, 0, TestGame.Stats(hp: 10))
                .Place(Team.Player2, 6, 6)
                .Start();
            DamageSystem.Apply(state, new DamageRequest(null, u[0], 5, DamageType.Direct));
            var field = new TestTurnCellEffect
            {
                Step = TurnStep.StartPositiveEffects,
                OnStep = c => DamageSystem.Heal(c.State, null, c.Cell.Occupant, 1),
            };
            CellEffectSystem.Place(state, new Position(0, 0), field);

            new EndTurnAction(Team.Player1).Execute(state); // Player1 종료, Player2 시작
            Assert.Equal(5, u[0].Stats.CurrentHp);

            new EndTurnAction(Team.Player2).Execute(state); // Player2 종료, Player1 시작 → 회복
            Assert.Equal(6, u[0].Stats.CurrentHp);
            Assert.Equal(new[] { (TurnStep.StartPositiveEffects, Team.Player1) }, field.Calls);
        }

        [Fact]
        public void EndAreaEffect_RunsAfterDoTStep()
        {
            var (state, u) = new TestGame().Place(Team.Player1, 0, 0).Place(Team.Player2, 6, 6).Start();
            var area = new TestTurnCellEffect { Step = TurnStep.EndAreaEffects };
            CellEffectSystem.Place(state, new Position(0, 0), area);

            new EndTurnAction(Team.Player1).Execute(state);

            Assert.Equal(new[] { (TurnStep.EndAreaEffects, Team.Player1) }, area.Calls);
        }
    }
}
