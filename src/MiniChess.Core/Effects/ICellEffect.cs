namespace MiniChess.Core.Effects
{
    /// <summary>
    /// 칸 위에 설치되어 유닛에 반응하는 효과 (장판, 덫 등).
    /// 호출 순서: 경로상의 각 칸마다 OnEnter → 이동이 끝난 칸에서 OnStop.
    /// </summary>
    public interface ICellEffect
    {
        /// <summary>이 효과가 차지하는 레이어. 같은 레이어의 기존 효과를 덮어쓴다.</summary>
        CellEffectLayer Layer { get; }

        /// <summary>효과 종류(CellEffectKinds). 표시 계층이 구체 타입 대신 이 값으로 모양을 정한다.</summary>
        string Kind { get; }

        /// <summary>
        /// 유닛이 이 칸에 들어왔을 때 호출된다. 이동 방식과 관계없이 칸을 밟으면 발동한다.
        /// 지나가는 칸이든 도착 칸이든 동일하다. Stop 을 반환하면 이 칸에서 이동이 끝난다.
        /// </summary>
        CellEnterResult OnEnter(CellEffectContext context);

        /// <summary>
        /// 유닛이 이 칸에서 이동을 마쳤을 때 호출된다.
        /// 목적지에 도착했거나, OnEnter 가 Stop 을 반환해 도중에 멈춘 경우 모두 해당한다.
        /// </summary>
        void OnStop(CellEffectContext context);
    }
}
