using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Movement;
using MiniChess.Core.Skills;
using MiniChess.Core.Skills.Areas;
using MiniChess.Core.State;

namespace MiniChess.Core.Controller
{
    /// <summary>
    /// 코어의 단일 진입점. 클라/봇/서버는 이 클래스만 사용한다.
    ///   - 명령: 요청(GameRequest)을 받아 검사 후 실행하고 응답(GameResponse)을 돌려준다. 실패하면 상태는 바뀌지 않는다.
    ///   - 조회: 상태를 바꾸지 않는 표시용 정보(배치/이동/공격/스킬 가능 칸, 범위 미리보기).
    ///   - State: 읽기 전용 경기 상태(보드, 유닛, AP, 턴, 이벤트 로그). 변경은 명령으로만 한다.
    /// PvP 에서 상대 정보를 숨기는 처리는 서버가 응답/상태를 걸러서 한다.
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
            return new GameController(new GameState(config.Rules, config.Map, config.Skills, characters));
        }

        #region Commands

        public GameResponse<PlaceFailReason, PlaceUnitsResult> PlaceUnits(PlaceUnitsRequest request)
        {
            var action = new PlaceUnitsAction(request.Team, request.Placements);

            PlaceFailReason reason = action.Validate(State);
            if (reason != PlaceFailReason.None)
                return GameResponse<PlaceFailReason, PlaceUnitsResult>.Fail(reason);

            PlaceUnitsResult result = action.Execute(State);
            return GameResponse<PlaceFailReason, PlaceUnitsResult>.Ok(result, result.Events);
        }

        public GameResponse<MoveFailReason, MovementResult> Move(MoveRequest request)
        {
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
            var action = new EndTurnAction(request.Team);

            EndTurnFailReason reason = action.Validate(State);
            if (reason != EndTurnFailReason.None)
                return GameResponse<EndTurnFailReason, EndTurnResult>.Fail(reason);

            EndTurnResult result = action.Execute(State);
            return GameResponse<EndTurnFailReason, EndTurnResult>.Ok(result, result.Events);
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

        #endregion
    }
}
