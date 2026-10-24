using System.Linq;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using MiniChess.Core.Skills;
using MiniChess.Core.State;
using MiniChess.Core.Turns;
using MiniChess.Core.Snapshots;

namespace MiniChess.Core.Effects.Scheduled
{
    /// <summary>예약 포격의 등록/발사. 시전자가 죽어도 취소하지 않는다.</summary>
    public static class ScheduledStrikeSystem
    {
        internal static void Schedule(GameState state, ScheduledStrike strike)
        {
            state.AddScheduledStrike(strike);
            state.Events.Record(new StrikeScheduledEvent(SnapshotBuilder.Strike(strike)));
        }

        /// <summary>
        /// 예약 효과 단계에서 현재 팀 시전자의 예약을 발사한다(조준한 턴 제외).
        /// 발사: 예약 제거 → 범위 안 적에게 장판 피해. 시전자의 행동은 소모하지 않는다.
        /// 시전자가 착탄 전에 죽어도 예약은 남아 그 팀의 다음 턴 시작에 그대로 발사된다(사용자 확정).
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

        private static void Fire(GameState state, ScheduledStrike strike)
        {
            state.RemoveScheduledStrike(strike);
            state.Events.Record(new StrikeLandedEvent(SnapshotBuilder.Strike(strike)));

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
