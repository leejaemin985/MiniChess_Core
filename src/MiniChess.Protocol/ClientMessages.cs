using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Skills.Areas;

namespace MiniChess.Protocol
{
    /// <summary>클라이언트 → 서버 메시지. 직렬화 형식(JSON 등)은 각 쪽이 정하고, 여기는 내용만 정의한다.</summary>
    public abstract class ClientMessage
    {
    }

    /// <summary>경기 참가 요청. 서버는 대기 중인 상대와 짝지어 MatchStartedMessage 를 보낸다.</summary>
    public class JoinMatchMessage : ClientMessage
    {
        /// <summary>클라이언트의 ProtocolInfo.Version. 서버와 다르면 거절한다.</summary>
        public int ProtocolVersion { get; set; }
    }

    /// <summary>
    /// 경기 명령. 요청 팀은 보내지 않는다(서버가 연결 기준으로 정한다).
    /// </summary>
    public abstract class CommandMessage : ClientMessage
    {
        /// <summary>클라이언트가 정하는 요청 번호(연결마다 1씩 증가). 결과 메시지에 그대로 돌려준다. 이미 처리한 번호 이하면 무시한다.</summary>
        public int RequestId { get; set; }

        /// <summary>클라이언트가 알고 있는 경기 턴 번호(GameSnapshot.TurnNumber). 서버의 턴 번호와 다르면 오래된 요청으로 보고 거절한다.</summary>
        public int TurnNumber { get; set; }

        /// <summary>코어 요청으로 바꾼다. 필수 값이 비어 있는 등 형식이 잘못되었으면 null.</summary>
        public abstract GameRequest ToRequest(Team team);
    }

    public class PlacementEntry
    {
        public string CharacterId { get; set; }
        public Position Position { get; set; }
    }

    public class PlaceUnitsMessage : CommandMessage
    {
        public List<PlacementEntry> Placements { get; set; }

        public override GameRequest ToRequest(Team team)
        {
            if (Placements == null || Placements.Any(entry => entry == null || entry.CharacterId == null))
                return null;

            return new PlaceUnitsRequest(team, Placements.Select(entry => new UnitPlacement(entry.CharacterId, entry.Position)).ToList());
        }
    }

    public class MoveMessage : CommandMessage
    {
        public int UnitId { get; set; }
        public Position Destination { get; set; }

        public override GameRequest ToRequest(Team team) => new MoveRequest(team, UnitId, Destination);
    }

    public class AttackMessage : CommandMessage
    {
        public int UnitId { get; set; }
        public int TargetUnitId { get; set; }

        public override GameRequest ToRequest(Team team) => new AttackRequest(team, UnitId, TargetUnitId);
    }

    public class AttackObstacleMessage : CommandMessage
    {
        public int UnitId { get; set; }
        public Position Target { get; set; }

        public override GameRequest ToRequest(Team team) => new AttackObstacleRequest(team, UnitId, Target);
    }

    public class UseSkillMessage : CommandMessage
    {
        public int UnitId { get; set; }
        public string SkillId { get; set; }

        /// <summary>지정 칸 전부(지정 순).</summary>
        public List<Position> Targets { get; set; }

        public AreaRotation Rotation { get; set; }

        public override GameRequest ToRequest(Team team)
        {
            if (SkillId == null || Targets == null || Targets.Count == 0)
                return null;

            return new UseSkillRequest(team, UnitId, SkillId, Targets.ToList(), Rotation);
        }
    }

    public class EndTurnMessage : CommandMessage
    {
        public override GameRequest ToRequest(Team team) => new EndTurnRequest(team);
    }
}
