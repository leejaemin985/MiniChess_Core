using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Capture;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Scheduled;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Statuses.Library;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Snapshots
{
    /// <summary>
    /// 스냅샷(한 팀 시점의 값)으로 경기 상태를 다시 만든다. GameController.Restore 가 쓴다.
    /// 만든 상태는 표시와 조회 전용이다: 숨겨진 정보가 빠져 있고, 효과의 발동 동작(장판 피해, 호위 가로채기 등)과
    /// 이벤트 기록은 복원하지 않는다. 명령은 서버의 경기에서 실행한다.
    /// </summary>
    internal static class SnapshotRestorer
    {
        public static GameState Restore(GameConfig config, GameSnapshot snapshot)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var characters = new CharacterCatalog(config.Characters ?? Array.Empty<CharacterDefinition>());
            var state = new GameState(config.Rules, config.Map, config.Skills, characters);
            if (state.Board.Width != snapshot.BoardWidth || state.Board.Height != snapshot.BoardHeight)
                throw new ArgumentException($"맵 크기가 스냅샷과 다름: {state.Board.Width}x{state.Board.Height} != {snapshot.BoardWidth}x{snapshot.BoardHeight}");

            state.Phase = snapshot.Phase;
            state.CurrentTeam = snapshot.CurrentTeam;
            state.TurnNumber = snapshot.TurnNumber;
            state.Winner = snapshot.Winner;
            state.EndReason = snapshot.EndReason;

            foreach (PlayerSnapshot player in snapshot.Players)
            {
                state.GetPlayer(player.Team).Ap.Restore(player.Ap);
                state.GetPlayer(player.Team).HasPlaced = player.HasPlaced;
            }

            foreach (CellSnapshot cell in snapshot.Cells)
                state.Board.GetCell(cell.Position).IsCaptureTile = cell.IsCaptureTile;

            RestoreUnits(state, snapshot);
            RestoreCellEffects(state, snapshot);
            RestoreStrikes(state, snapshot);
            RestoreCapture(state, config, snapshot.Capture);
            return state;
        }

        /// <summary>유닛을 먼저 모두 만든 뒤, 서로를 가리키는 값(소환자, 상태를 건 유닛)을 잇는다.</summary>
        private static void RestoreUnits(GameState state, GameSnapshot snapshot)
        {
            foreach (UnitSnapshot value in snapshot.Units)
            {
                var stats = new UnitBaseStats
                {
                    Id = value.CharacterId,
                    Name = value.Name,
                    MaxHp = value.MaxHp,
                    Attack = value.Attack,
                    AttackRange = value.AttackRange,
                    SkillIds = value.SkillIds.ToArray(),
                };

                var unit = new Unit(value.Id, value.Team, stats) { IsSummon = value.IsSummon };
                unit.Stats.RestoreHp(value.Hp);
                unit.TurnState.Restore(value.CombatActionUsed, value.VoluntaryMoveLocked, value.ActionsEnded, value.VoluntaryCellsMoved);
                state.RestoreUnit(unit);

                if (value.Position.HasValue && !state.Board.TrySpawn(unit, value.Position.Value))
                    throw new ArgumentException($"유닛 {value.Id} 를 {value.Position.Value} 에 놓을 수 없음");
            }

            foreach (UnitSnapshot value in snapshot.Units)
            {
                Unit unit = state.FindUnit(value.Id);
                unit.SummonOwner = Find(state, value.SummonOwnerId);

                foreach (ShieldSnapshot shield in value.Shields.Where(shield => shield.Amount > 0))
                    unit.Stats.SetShield(shield.ShieldId, shield.Amount, source: null);

                foreach (StatusSnapshot status in value.Statuses)
                {
                    var effect = new StatusEffect(Definition(status), unit, Find(state, status.SourceId), status.SourceTeam, status.OriginId, state.TurnNumber)
                    {
                        Remaining = status.Remaining,
                    };
                    unit.AddStatus(effect);
                }
            }
        }

        /// <summary>상태 정의. 라이브러리에 없는 상태는 이름과 남은 횟수만 보이도록 동작 없는 정의로 만든다.</summary>
        private static StatusDefinition Definition(StatusSnapshot status)
        {
            StatusDefinition definition = StatusLibrary.Restore(status.StatusId, status.Duration, status.Parameters);
            if (definition != null)
                return definition;

            StatusTiming decrement = status.IsPermanent
                ? null
                : new StatusTiming(TurnStep.EndDurationTick, TimingOwner.TargetOwner, includeApplicationTurn: false);
            return new StatusDefinition(status.StatusId, Math.Max(1, status.Duration), StatusStackPolicy.Refresh, decrement);
        }

        private static void RestoreCellEffects(GameState state, GameSnapshot snapshot)
        {
            var zones = new Dictionary<int, FieldZone>();

            foreach (CellEffectSnapshot value in snapshot.CellEffects)
            {
                if (value.ZoneId == null || value.OwnerTeam == null)
                    throw new ArgumentException($"{value.Position} 의 {value.Kind}: 구역이 없는 칸 효과는 복원할 수 없음");

                if (!zones.TryGetValue(value.ZoneId.Value, out FieldZone zone))
                {
                    zone = new FieldZone(value.SkillId, Find(state, value.SourceId), value.OwnerTeam.Value, value.Remaining ?? 1, placedTurnNumber: -1)
                    {
                        Id = value.ZoneId.Value,
                    };
                    zones.Add(zone.Id, zone);
                    state.ReserveZoneId(zone.Id);
                }

                ZoneCellEffect effect = value.Kind == CellEffectKinds.Obstacle
                    ? new Obstacle(zone, value.HitsRemaining ?? 1)
                    : (ZoneCellEffect)new RestoredZoneEffect(zone, value.Layer, value.Kind);

                state.Board.GetCell(value.Position).SetEffect(effect);
                zone.Register(value.Position, effect);
            }
        }

        private static void RestoreStrikes(GameState state, GameSnapshot snapshot)
        {
            foreach (StrikeSnapshot value in snapshot.Strikes)
            {
                Unit source = Find(state, value.SourceId)
                    ?? throw new ArgumentException($"포격 {value.Id} 의 시전자 {value.SourceId} 가 스냅샷에 없음");

                var strike = new ScheduledStrike(value.SkillId, source, value.Cells.ToList(), value.Damage, value.ScheduledTurnNumber)
                {
                    Id = value.Id,
                };
                state.RestoreScheduledStrike(strike);
            }
        }

        private static void RestoreCapture(GameState state, GameConfig config, CaptureSnapshot value)
        {
            CaptureState capture = state.Capture;
            capture.IsActive = value.IsActive;
            capture.CapturedBy = value.CapturedBy;
            capture.SetProgress(Team.Player1, value.Player1Progress);
            capture.SetProgress(Team.Player2, value.Player2Progress);

            capture.Reward = config.Rules.Capture?.RewardPool?.FirstOrDefault(reward => reward.Id == value.RewardId);

            // 팀 버프 보상은 플레이어에 남는다. 보호막 보상은 이미 유닛 보호막으로 복원되었다.
            if (capture.Reward is BasicAttackStatusReward statusReward && value.CapturedBy.HasValue)
                state.GetPlayer(value.CapturedBy.Value).AddBasicAttackStatus(statusReward.Status);
        }

        private static Unit Find(GameState state, int? unitId)
        {
            return unitId.HasValue ? state.FindUnit(unitId.Value) : null;
        }
    }
}
