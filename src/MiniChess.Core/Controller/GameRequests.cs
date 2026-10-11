using System;
using System.Collections.Generic;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Skills.Areas;

namespace MiniChess.Core.Controller
{
    /// <summary>
    /// 컨트롤러 요청의 공통 형식. 유닛은 Id 로 가리키고, 요청한 팀(Team)을 함께 보낸다.
    /// 코어는 요청한 팀의 유닛이 아니면 거절한다(봇/클라/네트워크 요청이 같은 형식).
    /// </summary>
    public abstract class GameRequest
    {
        public Team Team { get; }

        protected GameRequest(Team team)
        {
            Team = team;
        }
    }

    /// <summary>한 팀의 배치를 한 번에 확정한다.</summary>
    public class PlaceUnitsRequest : GameRequest
    {
        public IReadOnlyList<UnitPlacement> Placements { get; }

        public PlaceUnitsRequest(Team team, IReadOnlyList<UnitPlacement> placements) : base(team)
        {
            Placements = placements ?? throw new ArgumentNullException(nameof(placements));
        }
    }

    /// <summary>유닛을 상하좌우 직선으로 이동한다.</summary>
    public class MoveRequest : GameRequest
    {
        public int UnitId { get; }
        public Position Destination { get; }

        public MoveRequest(Team team, int unitId, Position destination) : base(team)
        {
            UnitId = unitId;
            Destination = destination;
        }
    }

    /// <summary>적 유닛을 기본 공격한다.</summary>
    public class AttackRequest : GameRequest
    {
        public int UnitId { get; }
        public int TargetUnitId { get; }

        public AttackRequest(Team team, int unitId, int targetUnitId) : base(team)
        {
            UnitId = unitId;
            TargetUnitId = targetUnitId;
        }
    }

    /// <summary>장애물이 있는 칸을 기본 공격한다.</summary>
    public class AttackObstacleRequest : GameRequest
    {
        public int UnitId { get; }
        public Position Target { get; }

        public AttackObstacleRequest(Team team, int unitId, Position target) : base(team)
        {
            UnitId = unitId;
            Target = target;
        }
    }

    /// <summary>스킬을 사용한다. 지정 칸은 스킬이 요구하는 개수만큼(대부분 1개), 회전은 회전 범위 스킬에만 의미가 있다.</summary>
    public class UseSkillRequest : GameRequest
    {
        public int UnitId { get; }
        public string SkillId { get; }
        public IReadOnlyList<Position> Targets { get; }
        public AreaRotation Rotation { get; }

        public UseSkillRequest(Team team, int unitId, string skillId, IReadOnlyList<Position> targets, AreaRotation rotation = AreaRotation.R0)
            : base(team)
        {
            UnitId = unitId;
            SkillId = skillId ?? throw new ArgumentNullException(nameof(skillId));
            Targets = targets ?? throw new ArgumentNullException(nameof(targets));
            if (targets.Count == 0) throw new ArgumentException("지정 칸이 비어 있음", nameof(targets));
            Rotation = rotation;
        }

        public UseSkillRequest(Team team, int unitId, string skillId, Position target, AreaRotation rotation = AreaRotation.R0)
            : this(team, unitId, skillId, new[] { target }, rotation)
        {
        }
    }

    /// <summary>요청한 팀의 턴을 끝낸다.</summary>
    public class EndTurnRequest : GameRequest
    {
        public EndTurnRequest(Team team) : base(team)
        {
        }
    }
}
