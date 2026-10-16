using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.State;

namespace MiniChess.Core.Capture
{
    /// <summary>
    /// 점령 판정(명세 3.5, 사용자 확정 규칙).
    ///   - 자기 턴 시작(시작 단계의 마지막)에 자기 유닛(소환물 포함)이 점령 칸에 서 있으면 진행도 +1.
    ///   - 점령 칸에서 그 팀의 유닛이 사라지는 순간 그 팀의 진행도는 0 으로 돌아간다(연속으로 올라가 있어야 함).
    ///   - 진행도는 팀별로 따로 센다. Required 에 도달하면 점령 완료.
    ///   - 점령 완료 시 보상 풀에서 하나를 무작위로 지급하고, 점령 칸은 소멸한다(이 경기에서 더 이상 점령 없음).
    /// </summary>
    public static class CaptureSystem
    {
        /// <summary>자기 턴 시작 판정. TurnSystem 이 시작 단계를 모두 처리한 뒤 호출한다.</summary>
        internal static void OnTurnStart(GameState state, Team team)
        {
            CaptureState capture = state.Capture;
            if (!capture.IsActive)
                return;

            if (!IsHeldBy(state, team))
            {
                ResetProgress(state, team);
                return;
            }

            int progress = capture.GetProgress(team) + 1;
            capture.SetProgress(team, progress);
            state.Events.Record(new CaptureProgressChangedEvent(team, progress, capture.Required));

            if (progress >= capture.Required)
                Complete(state, team);
        }

        /// <summary>
        /// 유닛 배치가 바뀐 뒤(이동, 사망) 호출한다. 점령 칸에 자기 유닛이 없는 팀의 진행도를 0 으로 돌린다.
        /// </summary>
        internal static void OnOccupancyChanged(GameState state)
        {
            if (!state.Capture.IsActive)
                return;

            ResetIfNotHeld(state, Team.Player1);
            ResetIfNotHeld(state, Team.Player2);
        }

        private static void ResetIfNotHeld(GameState state, Team team)
        {
            if (!IsHeldBy(state, team))
                ResetProgress(state, team);
        }

        private static void ResetProgress(GameState state, Team team)
        {
            CaptureState capture = state.Capture;
            if (capture.GetProgress(team) == 0)
                return;

            capture.SetProgress(team, 0);
            state.Events.Record(new CaptureProgressChangedEvent(team, 0, capture.Required));
        }

        private static bool IsHeldBy(GameState state, Team team)
        {
            // 소환물(분신)과 봉인된 유닛은 점령을 진행하거나 유지하지 못한다(사용자 확정).
            Unit occupant = state.Board.GetLivingUnit(state.Capture.Tile.Value);
            return occupant != null && !occupant.IsSummon && occupant.IsTargetable && occupant.Team == team;
        }

        private static void Complete(GameState state, Team team)
        {
            CaptureState capture = state.Capture;
            capture.IsActive = false;
            capture.CapturedBy = team;
            capture.SetProgress(Team.Player1, 0);
            capture.SetProgress(Team.Player2, 0);
            state.Board.GetCell(capture.Tile.Value).IsCaptureTile = false;

            CaptureReward reward = PickReward(state);
            capture.Reward = reward;
            state.Events.Record(new CaptureCompletedEvent(team, reward));

            reward?.Grant(state, team);
        }

        private static CaptureReward PickReward(GameState state)
        {
            CaptureRuleData rule = state.Rules.Capture;
            if (rule?.RewardPool == null || rule.RewardPool.Count == 0)
                return null;

            return rule.RewardPool[state.Random.Next(rule.RewardPool.Count)];
        }
    }
}
