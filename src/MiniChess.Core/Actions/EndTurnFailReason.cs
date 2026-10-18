namespace MiniChess.Core.Actions
{
    public enum EndTurnFailReason
    {
        None,
        NotBattlePhase,
        NotYourTurn,

        /// <summary>요청 형식이 잘못됨(요청 없음, 존재하지 않는 팀 등).</summary>
        InvalidRequest,
    }
}
