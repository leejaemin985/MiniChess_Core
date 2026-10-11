using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Capture;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Summons
{
    /// <summary>
    /// 소환물(분신 등)의 생성과 정리. 소환물은 소환자와 같은 팀의 별개 유닛이며 칸을 차지한다.
    /// 소환한 턴부터 행동할 수 있고(새 유닛이라 행동 기록이 비어 있음), 행동 비용은 팀 AP 를 쓴다.
    /// </summary>
    public static class SummonSystem
    {
        /// <summary>owner 가 소환해 살아 있는 소환물들.</summary>
        public static IEnumerable<Unit> GetLivingSummons(GameState state, Unit owner)
        {
            return state.GetPlayer(owner.Team).Units.Where(unit => unit.SummonOwner == owner && unit.IsOnBoard);
        }

        /// <summary>
        /// owner 의 소환물을 position 에 만든다. 칸에 유닛을 놓을 수 없으면 예외.
        /// onHitStatuses 는 소환물의 기본 공격이 적중하면 대상에게 걸 상태다.
        /// </summary>
        internal static Unit Summon(
            GameState state, Unit owner, IReadOnlyUnitBaseStats baseStats, Position position, IEnumerable<StatusDefinition> onHitStatuses)
        {
            if (!state.Board.CanPlace(position))
                throw new InvalidOperationException($"{position} 에 소환할 수 없음");

            Unit summon = state.CreateUnit(owner.Team, baseStats);
            summon.IsSummon = true;
            summon.SummonOwner = owner;
            foreach (StatusDefinition status in onHitStatuses)
                summon.AddBasicAttackStatus(status);

            state.Board.TrySpawn(summon, position);
            state.Events.Record(new UnitSummonedEvent(owner, summon, position));
            CaptureSystem.OnOccupancyChanged(state);
            return summon;
        }

        /// <summary>소환자가 죽으면 그 소환물도 함께 사라진다(사용자 확정). DeathSystem 이 호출한다.</summary>
        internal static void RemoveSummonsOf(GameState state, Unit owner)
        {
            foreach (Unit summon in GetLivingSummons(state, owner).ToList())
            {
                summon.Stats.ApplyDamage(summon.Stats.CurrentHp);
                DeathSystem.HandleIfDead(state, summon);
            }
        }
    }
}
