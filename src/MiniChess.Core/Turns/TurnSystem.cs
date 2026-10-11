using System;
using MiniChess.Core.Capture;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Scheduled;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Turns
{
    /// <summary>
    /// Battle 단계의 턴 진행(시작/종료/교대)을 담당한다.
    /// 각 TurnStep 을 순서대로 처리하며, 단계마다 상태효과 → 칸 효과 → 예약 포격 순으로 훅을 실행한다.
    /// 처리 중 경기가 끝나면 남은 단계와 다음 턴 시작을 진행하지 않는다.
    /// </summary>
    public static class TurnSystem
    {
        private static readonly TurnStep[] StartSteps =
        {
            TurnStep.StartPositiveEffects,
            TurnStep.StartRestrictionCheck,
            TurnStep.StartScheduledEffects,
        };

        private static readonly TurnStep[] EndSteps =
        {
            TurnStep.EndHeal,
            TurnStep.EndDamageOverTime,
            TurnStep.EndAreaEffects,
            TurnStep.EndDurationTick,
        };

        /// <summary>Setup 단계를 끝내고 Battle 단계로 넘어가 선공 팀의 첫 턴을 시작한다.</summary>
        internal static void StartBattle(GameState state)
        {
            if (state.Phase != GamePhase.Setup)
                throw new InvalidOperationException($"Battle 은 Setup 단계에서만 시작할 수 있음. 현재: {state.Phase}");

            state.Phase = GamePhase.Battle;
            state.CurrentTeam = state.Rules.Match.FirstTeam;
            state.TurnNumber = 1;

            BeginTurn(state);
        }

        /// <summary>현재 팀의 턴을 끝내고(종료 단계 처리) 상대 팀의 턴을 시작한다.</summary>
        internal static void EndTurn(GameState state)
        {
            if (state.Phase != GamePhase.Battle)
                throw new InvalidOperationException($"턴 종료는 Battle 단계에서만 가능. 현재: {state.Phase}");

            Team endingTeam = state.CurrentTeam;

            foreach (TurnStep step in EndSteps)
            {
                RunHooks(state, step, endingTeam);
                if (state.IsGameOver)
                    return;
            }

            state.Events.Record(new TurnEndedEvent(endingTeam, state.TurnNumber));

            state.CurrentTeam = GetOpponent(endingTeam);
            state.TurnNumber++;

            BeginTurn(state);
        }

        public static Team GetOpponent(Team team)
        {
            return team == Team.Player1 ? Team.Player2 : Team.Player1;
        }

        /// <summary>
        /// 현재 팀의 턴 시작 처리.
        /// AP 회복(첫 턴은 StartAp 만 적용) → 유닛 턴 상태 초기화 → 시작 단계 훅 → 점령 판정.
        /// </summary>
        private static void BeginTurn(GameState state)
        {
            PlayerState player = state.CurrentPlayer;

            state.Events.Record(new TurnStartedEvent(state.CurrentTeam, state.TurnNumber));

            if (player.TurnsStarted > 0)
                RecoverAp(player.Ap, state.Rules.Ap);

            player.TurnsStarted++;

            foreach (Unit unit in player.Units)
                unit.ResetTurnState();

            foreach (TurnStep step in StartSteps)
            {
                RunHooks(state, step, state.CurrentTeam);
                if (state.IsGameOver)
                    return;
            }

            // TurnStep.StartCapture: 시작 효과(예약 포격 등)로 점령 칸의 유닛이 사라졌을 수 있어 마지막에 판정한다.
            CaptureSystem.OnTurnStart(state, state.CurrentTeam);
        }

        private static void RunHooks(GameState state, TurnStep step, Team activeTeam)
        {
            var context = new TurnContext(state, step, activeTeam);

            StatusSystem.RunStep(context);
            if (state.IsGameOver)
                return;

            CellEffectSystem.RunStep(context);
            if (state.IsGameOver)
                return;

            ScheduledStrikeSystem.RunStep(context);
        }

        private static void RecoverAp(ApPool ap, ApRuleData rule)
        {
            if (!rule.CarryOver)
                ap.Clear();

            ap.Recover(rule.TurnRecoveryAp);
        }
    }
}
