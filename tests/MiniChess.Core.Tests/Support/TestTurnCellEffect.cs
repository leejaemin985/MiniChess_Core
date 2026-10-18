using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Tests.Support
{
    /// <summary>지정한 턴 단계에, 칸 위 유닛의 소유자 턴일 때 동작하는 테스트용 칸 효과.</summary>
    public class TestTurnCellEffect : ICellTurnEffect
    {
        public CellEffectLayer Layer { get; init; } = CellEffectLayer.AreaEffect;
        public string Kind { get; init; } = "TEST";
        public TurnStep Step { get; init; }
        public Action<CellTurnContext> OnStep { get; init; }

        public List<(TurnStep Step, Team Team)> Calls { get; } = new();

        public CellEnterResult OnEnter(CellEffectContext context) => CellEnterResult.Continue;

        public void OnStop(CellEffectContext context) { }

        public void OnTurnStep(CellTurnContext context)
        {
            var occupant = context.Cell.Occupant;
            if (context.Turn.Step != Step || occupant == null || occupant.Team != context.Turn.ActiveTeam)
                return;

            Calls.Add((context.Turn.Step, context.Turn.ActiveTeam));
            OnStep?.Invoke(context);
        }
    }
}
