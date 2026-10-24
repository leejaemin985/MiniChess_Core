using MiniChess.Core.Capture;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Summons;

namespace MiniChess.Core.Combat
{
    /// <summary>
    /// 유닛 사망과 경기 종료를 처리한다. 피해 원인(공격/덫/장판 등)과 무관하게 이곳 한 곳에서 처리한다.
    /// </summary>
    public static class DeathSystem
    {
        /// <summary>
        /// 유닛이 죽었으면 보드에서 제거하고 경기 종료 여부를 판정한다.
        /// 죽은 유닛이면 true. 플레이어의 유닛 목록에는 남겨둔다(부활 대상 보존).
        /// 이미 처리된 사망(보드에 없음)은 다시 기록하지 않는다.
        /// </summary>
        internal static bool HandleIfDead(GameState state, Unit unit)
        {
            if (unit.IsAlive)
                return false;

            if (unit.IsPlaced)
            {
                Position position = unit.Position.Value;
                state.Board.Remove(unit);
                state.Events.Record(new UnitDiedEvent(unit.Id, position));
                StatusSystem.RemoveAll(state, unit, StatusRemoveReason.TargetDied);
                StatusSystem.RemoveLinkedTo(state, unit);
                SummonSystem.RemoveSummonsOf(state, unit);
                CaptureSystem.OnOccupancyChanged(state);
            }

            CheckGameEnd(state);
            return true;
        }

        /// <summary>
        /// 전멸한 팀이 있으면 경기를 끝낸다.
        /// 한쪽만 전멸 → 상대 승리, 양쪽 동시 전멸 → 무승부(Winner = null).
        /// </summary>
        internal static void CheckGameEnd(GameState state)
        {
            if (state.Phase != GamePhase.Battle)
                return;

            SummonEliminationPolicy policy = state.Rules.Match.SummonElimination;
            bool player1Eliminated = state.GetPlayer(Team.Player1).IsEliminated(policy);
            bool player2Eliminated = state.GetPlayer(Team.Player2).IsEliminated(policy);

            if (!player1Eliminated && !player2Eliminated)
                return;

            Team? winner = null;
            if (player1Eliminated != player2Eliminated)
                winner = player1Eliminated ? Team.Player2 : Team.Player1;

            EndGame(state, winner, GameEndReason.Elimination);
        }

        /// <summary>경기를 끝낸다. 전멸/턴 제한 등 모든 종료가 이곳을 거친다.</summary>
        internal static void EndGame(GameState state, Team? winner, GameEndReason reason)
        {
            state.Phase = GamePhase.Ended;
            state.Winner = winner;
            state.EndReason = reason;
            state.Events.Record(new GameEndedEvent(winner, reason));
        }
    }
}
