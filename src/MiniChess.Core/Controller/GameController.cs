using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Events;
using MiniChess.Core.Movement;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.Snapshots;
using MiniChess.Core.State;
using MiniChess.Core.Turns;

namespace MiniChess.Core.Controller
{
    /// <summary>
    /// 코어의 단일 진입점. 클라/봇/서버는 이 클래스만 사용한다.
    ///   - 명령: 요청(GameRequest)을 받아 검사 후 실행하고 응답(GameResponse)을 돌려준다. 실패하면 상태는 바뀌지 않는다.
    ///   - 조회: 상태를 바꾸지 않는 표시용 정보(배치/이동/공격/스킬 가능 칸, 범위 미리보기).
    ///   - State: 읽기 전용 경기 상태(보드, 유닛, AP, 턴, 이벤트 로그). 변경은 명령으로만 한다.
    ///   - 시점(View): 팀별로 거른 스냅샷/이벤트(GetSnapshot, GetVisibleEvents). 서버는 이것만 각 클라이언트에 보낸다.
    /// </summary>
    public class GameController
    {
        public GameState State { get; }

        internal GameController(GameState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>경기를 만든다. 유닛 없이 배치(Setup) 단계에서 시작한다.</summary>
        public static GameController Create(GameConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Rules == null) throw new ArgumentException("Rules 가 없음", nameof(config));
            if (config.Map == null) throw new ArgumentException("Map 이 없음", nameof(config));

            var characters = new CharacterCatalog(config.Characters ?? Array.Empty<CharacterDefinition>());
            int seed = config.Seed ?? (int)DateTime.UtcNow.Ticks;
            return new GameController(new GameState(config.Rules, config.Map, config.Skills, characters, seed));
        }

        /// <summary>
        /// 한 팀 시점의 스냅샷(GetSnapshot)으로 경기를 다시 만든다. 온라인 클라이언트가 서버 상태를 화면에 옮길 때 쓴다.
        /// config 는 서버 경기와 같은 규칙/맵/캐릭터/스킬이어야 한다(Seed 는 쓰지 않는다).
        /// 만든 경기는 표시와 조회(Get*) 전용이다. 숨겨진 정보가 빠져 있고 효과의 발동 동작과 이벤트 기록은 복원하지 않으므로,
        /// 이 경기에 명령을 실행하면 서버와 결과가 다를 수 있다. 명령은 서버로 보낸다.
        /// </summary>
        public static GameController Restore(GameConfig config, GameSnapshot snapshot)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.Rules == null) throw new ArgumentException("Rules 가 없음", nameof(config));
            if (config.Map == null) throw new ArgumentException("Map 이 없음", nameof(config));

            return new GameController(SnapshotRestorer.Restore(config, snapshot));
        }

        #region Commands

        public GameResponse<PlaceFailReason, PlaceUnitsResult> PlaceUnits(PlaceUnitsRequest request)
        {
            if (!IsWellFormed(request)) return GameResponse<PlaceFailReason, PlaceUnitsResult>.Fail(PlaceFailReason.InvalidRequest);

            var action = new PlaceUnitsAction(request.Team, request.Placements);

            PlaceFailReason reason = action.Validate(State);
            if (reason != PlaceFailReason.None)
                return GameResponse<PlaceFailReason, PlaceUnitsResult>.Fail(reason);

            PlaceUnitsResult result = action.Execute(State);
            return GameResponse<PlaceFailReason, PlaceUnitsResult>.Ok(result, result.Events);
        }

        public GameResponse<MoveFailReason, MovementResult> Move(MoveRequest request)
        {
            if (!IsWellFormed(request)) return GameResponse<MoveFailReason, MovementResult>.Fail(MoveFailReason.InvalidRequest);

            Unit unit = State.FindUnit(request.UnitId);
            if (unit == null) return GameResponse<MoveFailReason, MovementResult>.Fail(MoveFailReason.UnknownUnit);
            if (unit.Team != request.Team) return GameResponse<MoveFailReason, MovementResult>.Fail(MoveFailReason.NotYourUnit);

            var action = new MoveAction(unit, request.Destination);

            MoveFailReason reason = action.Validate(State);
            if (reason != MoveFailReason.None)
                return GameResponse<MoveFailReason, MovementResult>.Fail(reason);

            MovementResult result = action.Execute(State);
            return GameResponse<MoveFailReason, MovementResult>.Ok(result, result.Events);
        }

        public GameResponse<AttackFailReason, AttackResult> Attack(AttackRequest request)
        {
            if (!IsWellFormed(request)) return GameResponse<AttackFailReason, AttackResult>.Fail(AttackFailReason.InvalidRequest);

            Unit attacker = State.FindUnit(request.UnitId);
            if (attacker == null) return GameResponse<AttackFailReason, AttackResult>.Fail(AttackFailReason.UnknownUnit);
            if (attacker.Team != request.Team) return GameResponse<AttackFailReason, AttackResult>.Fail(AttackFailReason.NotYourUnit);

            Unit target = State.FindUnit(request.TargetUnitId);
            if (target == null) return GameResponse<AttackFailReason, AttackResult>.Fail(AttackFailReason.InvalidTarget);

            var action = new AttackAction(attacker, target);

            AttackFailReason reason = action.Validate(State);
            if (reason != AttackFailReason.None)
                return GameResponse<AttackFailReason, AttackResult>.Fail(reason);

            AttackResult result = action.Execute(State);
            return GameResponse<AttackFailReason, AttackResult>.Ok(result, result.Events);
        }

        public GameResponse<AttackFailReason, AttackObstacleResult> AttackObstacle(AttackObstacleRequest request)
        {
            if (!IsWellFormed(request)) return GameResponse<AttackFailReason, AttackObstacleResult>.Fail(AttackFailReason.InvalidRequest);

            Unit attacker = State.FindUnit(request.UnitId);
            if (attacker == null) return GameResponse<AttackFailReason, AttackObstacleResult>.Fail(AttackFailReason.UnknownUnit);
            if (attacker.Team != request.Team) return GameResponse<AttackFailReason, AttackObstacleResult>.Fail(AttackFailReason.NotYourUnit);

            var action = new AttackObstacleAction(attacker, request.Target);

            AttackFailReason reason = action.Validate(State);
            if (reason != AttackFailReason.None)
                return GameResponse<AttackFailReason, AttackObstacleResult>.Fail(reason);

            AttackObstacleResult result = action.Execute(State);
            return GameResponse<AttackFailReason, AttackObstacleResult>.Ok(result, result.Events);
        }

        public GameResponse<SkillFailReason, SkillResult> UseSkill(UseSkillRequest request)
        {
            if (!IsWellFormed(request)) return GameResponse<SkillFailReason, SkillResult>.Fail(SkillFailReason.InvalidRequest);

            Unit caster = State.FindUnit(request.UnitId);
            if (caster == null) return GameResponse<SkillFailReason, SkillResult>.Fail(SkillFailReason.UnknownUnit);
            if (caster.Team != request.Team) return GameResponse<SkillFailReason, SkillResult>.Fail(SkillFailReason.NotYourUnit);

            var action = new UseSkillAction(caster, request.SkillId, request.Targets, request.Rotation);

            SkillFailReason reason = action.Validate(State);
            if (reason != SkillFailReason.None)
                return GameResponse<SkillFailReason, SkillResult>.Fail(reason);

            SkillResult result = action.Execute(State);
            return GameResponse<SkillFailReason, SkillResult>.Ok(result, result.Events);
        }

        public GameResponse<EndTurnFailReason, EndTurnResult> EndTurn(EndTurnRequest request)
        {
            if (!IsWellFormed(request)) return GameResponse<EndTurnFailReason, EndTurnResult>.Fail(EndTurnFailReason.InvalidRequest);

            var action = new EndTurnAction(request.Team);

            EndTurnFailReason reason = action.Validate(State);
            if (reason != EndTurnFailReason.None)
                return GameResponse<EndTurnFailReason, EndTurnResult>.Fail(reason);

            EndTurnResult result = action.Execute(State);
            return GameResponse<EndTurnFailReason, EndTurnResult>.Ok(result, result.Events);
        }

        /// <summary>
        /// 한 팀의 연결이 끊겨 경기를 끝낸다(상대 승리, GameEndReason.Disconnect). 서버만 호출한다.
        /// 배치 단계에서도 끝낼 수 있다. 이미 끝났거나 팀이 잘못되었으면 아무것도 하지 않고 빈 목록을 돌려준다.
        /// </summary>
        public IReadOnlyList<GameEvent> EndByDisconnect(Team disconnected)
        {
            if (State.IsGameOver || !IsDefined(disconnected))
                return Array.Empty<GameEvent>();

            int eventStart = State.Events.Count;
            DeathSystem.EndGame(State, TurnSystem.GetOpponent(disconnected), GameEndReason.Disconnect);
            return State.Events.Since(eventStart);
        }

        /// <summary>요청이 있고 팀이 실제 팀인지. 네트워크로 들어온 잘못된 요청을 예외 대신 실패로 돌려주기 위한 검사.</summary>
        private static bool IsWellFormed(GameRequest request) => request != null && IsDefined(request.Team);

        private static bool IsDefined(Team team) => team == Team.Player1 || team == Team.Player2;

        #endregion

        #region Views

        /// <summary>viewer 팀 시점의 현재 상태(값 복사본). 접속 직후 화면 구성, 이벤트 재생 후 동기화에 쓴다.</summary>
        public GameSnapshot GetSnapshot(Team viewer)
        {
            return GameProjection.Snapshot(State, viewer);
        }

        /// <summary>events 중 viewer 팀에게 보여 줄 것. 응답/로그의 이벤트를 상대에게 보내기 전에 거른다.</summary>
        public IReadOnlyList<GameEvent> GetVisibleEvents(Team viewer, IReadOnlyList<GameEvent> events)
        {
            return GameProjection.FilterEvents(State, viewer, events);
        }

        #endregion

        #region Queries

        /// <summary>팀이 유닛을 배치할 수 있는 칸(시작 위치).</summary>
        public IReadOnlyList<Position> GetPlacementCells(Team team)
        {
            return State.Board.GetSpawnPositions(team);
        }

        /// <summary>배치할 때 고를 수 있는 캐릭터.</summary>
        public IReadOnlyList<CharacterDefinition> GetCharacters()
        {
            return State.Characters.All.ToList();
        }

        /// <summary>유닛이 지금 이동할 수 있는 도착 칸. 지금 이동할 수 없거나 유닛이 없으면 빈 목록.</summary>
        public List<Position> GetMoveDestinations(int unitId)
        {
            Unit unit = State.FindUnit(unitId);
            if (unit == null || !unit.IsOnBoard)
                return new List<Position>();

            int maxCells = Math.Max(State.Board.Width, State.Board.Height);
            return RangeCalculator.GetPositionsInRange(State.Board, unit.Position.Value, maxCells, RangeShape.Orthogonal)
                .Where(destination => new MoveAction(unit, destination).Validate(State) == MoveFailReason.None)
                .ToList();
        }

        /// <summary>유닛이 지금 기본 공격할 수 있는 적. 지금 공격할 수 없으면 빈 목록.</summary>
        public List<Unit> GetAttackTargets(int unitId)
        {
            Unit attacker = State.FindUnit(unitId);
            if (attacker == null)
                return new List<Unit>();

            return AttackAction.GetTargetsInRange(State, attacker)
                .Where(target => new AttackAction(attacker, target).Validate(State) == AttackFailReason.None)
                .ToList();
        }

        /// <summary>유닛이 지금 기본 공격할 수 있는 장애물 칸. 지금 공격할 수 없으면 빈 목록.</summary>
        public List<Position> GetAttackableObstacles(int unitId)
        {
            Unit attacker = State.FindUnit(unitId);
            if (attacker == null)
                return new List<Position>();

            return AttackObstacleAction.GetObstaclesInRange(State, attacker)
                .Where(position => new AttackObstacleAction(attacker, position).Validate(State) == AttackFailReason.None)
                .ToList();
        }

        /// <summary>유닛이 가진 스킬(슬롯 순 → 고유 기능)과 지금 사용 가능 여부.</summary>
        public List<SkillInfo> GetSkills(int unitId)
        {
            Unit unit = State.FindUnit(unitId);
            if (unit == null)
                return new List<SkillInfo>();

            IReadOnlyList<string> extraIds = State.Characters.Find(unit.Stats.Base.Id)?.ExtraSkillIds ?? new List<string>();

            return unit.Stats.Base.SkillIds
                .Select(id => new SkillInfo(
                    id,
                    State.Skills.Find(id),
                    extraIds.Contains(id),
                    UseSkillAction.ValidateUsable(State, unit, id),
                    SkillQueries.CanRotate(State, id)))
                .ToList();
        }

        /// <summary>
        /// 스킬의 다음 지정 가능 칸. chosen 은 이미 지정한 칸(다중 지정 스킬, 없으면 null).
        /// 지금 쓸 수 없거나 다 지정했으면 빈 목록.
        /// </summary>
        public List<Position> GetSkillTargets(int unitId, string skillId, IReadOnlyList<Position> chosen = null)
        {
            Unit caster = State.FindUnit(unitId);
            if (caster == null)
                return new List<Position>();

            return SkillQueries.GetValidTargets(State, caster, skillId, chosen ?? Array.Empty<Position>());
        }

        /// <summary>해당 칸을 지정했을 때 효과가 적용될 칸(범위 미리보기). 회전 범위는 회전할 때마다 다시 조회한다.</summary>
        public IReadOnlyList<Position> GetSkillArea(int unitId, string skillId, Position target, AreaRotation rotation = AreaRotation.R0)
        {
            Unit caster = State.FindUnit(unitId);
            if (caster == null || !caster.IsPlaced)
                return Array.Empty<Position>();

            return SkillQueries.GetAffectedCells(State, caster, skillId, target, rotation);
        }

        /// <summary>
        /// 방향을 골라 미는 스킬(자리 교정 등)의 예상 도착 칸. 밀리지 않으면 대상의 현재 칸.
        /// targets 는 모든 지정 칸(대상, 방향 칸). 지정이 유효하지 않거나 미는 스킬이 아니면 null.
        /// </summary>
        public Position? GetPushPreview(int unitId, string skillId, IReadOnlyList<Position> targets)
        {
            Unit caster = State.FindUnit(unitId);
            if (caster == null || targets == null)
                return null;

            return SkillQueries.PredictPush(State, caster, skillId, targets);
        }

        #endregion
    }
}
