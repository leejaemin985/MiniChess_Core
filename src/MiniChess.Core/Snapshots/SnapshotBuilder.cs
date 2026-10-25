using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Scheduled;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.Snapshots
{
    /// <summary>살아 있는 상태 객체를 그 시점의 값(스냅샷)으로 복사한다. 이벤트 기록과 GameController.GetSnapshot 이 쓴다.</summary>
    internal static class SnapshotBuilder
    {
        public static UnitSnapshot Unit(Unit unit)
        {
            return new UnitSnapshot(
                unit.Id,
                unit.Team,
                unit.Stats.Base.Id,
                unit.Stats.Base.Name,
                unit.Position,
                unit.Stats.CurrentHp,
                unit.Stats.MaxHp,
                unit.Stats.Attack,
                unit.Stats.AttackRange,
                unit.IsSummon,
                unit.SummonOwner?.Id,
                unit.Stats.Base.SkillIds.ToList(),
                unit.Stats.Shields.Select(shield => new ShieldSnapshot(shield.Id, shield.Amount)).ToList(),
                unit.Statuses.Select(Status).ToList(),
                unit.TurnState.ActionsEnded,
                unit.TurnState.VoluntaryCellsMoved,
                unit.TurnState.BasicAttackUsed,
                unit.TurnState.UsedSkillIds.ToList());
        }

        public static StatusSnapshot Status(StatusEffect status)
        {
            StatusDefinition definition = status.Definition;
            return new StatusSnapshot(
                definition.Id, status.Remaining, definition.IsPermanent, status.Source?.Id, status.SourceTeam,
                definition.Duration, new Dictionary<string, int>(definition.Behavior?.Parameters ?? new Dictionary<string, int>()),
                status.OriginId);
        }

        public static CellEffectSnapshot CellEffect(Position position, ICellEffect effect)
        {
            FieldZone zone = (effect as ZoneCellEffect)?.Zone;
            return new CellEffectSnapshot(
                position,
                effect.Layer,
                effect.Kind,
                zone?.OwnerTeam,
                zone?.SkillId,
                zone?.Id,
                zone?.Source?.Id,
                zone?.Remaining,
                (effect as Obstacle)?.HitsRemaining);
        }

        public static StrikeSnapshot Strike(ScheduledStrike strike)
        {
            return new StrikeSnapshot(
                strike.Id, strike.SkillId, strike.Source.Id, strike.Source.Team, strike.Cells.ToList(), strike.Damage, strike.ScheduledTurnNumber);
        }

        public static GameSnapshot Game(GameState state, Team viewer)
        {
            var cells = new List<CellSnapshot>();
            var effects = new List<CellEffectSnapshot>();
            foreach (BoardCell cell in state.Board.AllCells())
            {
                cells.Add(new CellSnapshot(cell.Position, cell.Terrain, cell.IsCaptureTile));
                effects.AddRange(cell.GetEffectsInOrder().Select(effect => CellEffect(cell.Position, effect)));
            }

            var players = new[] { Team.Player1, Team.Player2 }
                .Select(team => state.GetPlayer(team))
                .Select(player => new PlayerSnapshot(player.Team, player.Ap.Current, player.Ap.Max, player.HasPlaced, state.Board.GetSpawnPositions(player.Team).ToList()))
                .ToList();

            CaptureState capture = state.Capture;
            var captureSnapshot = new CaptureSnapshot(
                capture.Tile, capture.Required, capture.IsActive, capture.CapturedBy, capture.Reward?.Id,
                capture.GetProgress(Team.Player1), capture.GetProgress(Team.Player2));

            return new GameSnapshot(
                viewer,
                state.Phase,
                state.CurrentTeam,
                state.TurnNumber,
                state.Winner,
                state.EndReason,
                state.Events.Count - 1,
                state.Board.Width,
                state.Board.Height,
                cells,
                players,
                state.AllUnits().Where(unit => unit.IsAlive).Select(Unit).ToList(),
                effects,
                state.ScheduledStrikes.Select(Strike).ToList(),
                captureSnapshot);
        }
    }
}
