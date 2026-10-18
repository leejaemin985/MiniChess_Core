using MiniChess.Core.Combat;
using MiniChess.Core.Effects;

namespace MiniChess.Core.Tests.Support
{
    /// <summary>밟으면 피해를 주고, 설정에 따라 이동을 멈추게 하는 테스트용 칸 효과.</summary>
    public class TestCellEffect : ICellEffect
    {
        public CellEffectLayer Layer { get; init; } = CellEffectLayer.Trap;
        public string Kind { get; init; } = "TEST";
        public int Damage { get; init; }
        public bool StopOnEnter { get; init; }

        public int EnterCount { get; private set; }
        public int StopCount { get; private set; }

        public CellEnterResult OnEnter(CellEffectContext context)
        {
            EnterCount++;

            if (Damage > 0)
                DamageSystem.Apply(context.State, new DamageRequest(null, context.Unit, Damage, DamageType.Area));

            return StopOnEnter ? CellEnterResult.Stop : CellEnterResult.Continue;
        }

        public void OnStop(CellEffectContext context)
        {
            StopCount++;
        }
    }
}
