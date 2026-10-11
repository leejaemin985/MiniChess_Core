using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Effects.Scheduled
{
    /// <summary>예약 포격의 등록/발사/취소.</summary>
    public static class ScheduledStrikeSystem
    {
        internal static void Schedule(GameState state, ScheduledStrike strike)
        {
            state.AddScheduledStrike(strike);
            state.Events.Record(new StrikeScheduledEvent(strike));
        }

        /// <summary>
        /// 예약 효과 단계에서 현재 팀 시전자의 예약을 발사한다(조준한 턴 제외).
        /// 발사: 예약 제거 → 범위 안 적에게 장판 피해. 시전자의 행동은 소모하지 않는다.
        /// </summary>
        internal static void RunStep(TurnContext context)
        {
            if (context.Step != TurnStep.StartScheduledEffects)
                return;

            GameState state = context.State;
            var due = state.ScheduledStrikes
                .Where(s => s.Source.Team == context.ActiveTeam && s.ScheduledTurnNumber != state.TurnNumber)
                .ToList();

            foreach (ScheduledStrike strike in due)
            {
                if (state.IsGameOver)
                    return;

                Fire(state, strike);
            }
        }

        /// <summary>시전자가 사망하면 그 시전자의 예약을 모두 취소한다.</summary>
        internal static void CancelBySource(GameState state, Unit source)
        {
            foreach (ScheduledStrike strike in state.ScheduledStrikes.Where(s => s.Source == source).ToList())
            {
                state.RemoveScheduledStrike(strike);
                state.Events.Record(new StrikeCancelledEvent(strike));
            }
        }

        private static void Fire(GameState state, ScheduledStrike strike)
        {
            state.RemoveScheduledStrike(strike);
            state.Events.Record(new StrikeLandedEvent(strike));

            // 발사는 행동이 아니다: 시전자는 발사한 턴에도 행동할 수 있다(사용자 확정).
            foreach (Unit target in TargetFilter.Enemy.GetUnits(state.Board, strike.Cells, strike.Source))
            {
                if (state.IsGameOver)
                    return;

                DamageSystem.Apply(state, new DamageRequest(strike.Source, target, strike.Damage, DamageType.Area));
            }
        }
    }
}
