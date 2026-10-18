using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Snapshots;

namespace MiniChess.Protocol
{
    /// <summary>서버 → 클라이언트 메시지.</summary>
    public abstract class ServerMessage
    {
    }

    /// <summary>
    /// 상대가 정해져 경기가 시작되었다(배치 단계). 클라이언트는 받은 팀과 스냅샷으로 화면을 만든다(OnlineMatch.Restore).
    /// </summary>
    public class MatchStartedMessage : ServerMessage
    {
        public Team YourTeam { get; set; }

        /// <summary>받는 팀 시점으로 거른 경기 상태.</summary>
        public GameSnapshot Snapshot { get; set; }
    }

    /// <summary>
    /// 명령 하나의 처리 결과. 보낸 쪽에게만 간다.
    /// 성공이면 이 메시지보다 먼저 StateUpdateMessage 로 바뀐 상태가 와 있다.
    /// </summary>
    public class CommandResultMessage : ServerMessage
    {
        public int RequestId { get; set; }
        public bool Success { get; set; }

        /// <summary>실패 사유(코어 실패 enum 의 이름, 또는 StaleTurn/InvalidRequest 등 서버 사유). 성공이면 null.</summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// 경기 상태가 바뀌었다(누군가의 명령 성공, 연결 끊김으로 인한 종료). 양쪽 모두에게 각자의 시점으로 걸러서 보낸다.
    /// 클라이언트는 Snapshot 으로 경기를 다시 만들고, Events 로 연출(행동 아이콘 등)을 한다.
    /// </summary>
    public class StateUpdateMessage : ServerMessage
    {
        /// <summary>이번 변화로 생긴 일(받는 팀 시점으로 걸러진 것). 순번 순이다.</summary>
        public List<EventEnvelope> Events { get; set; }

        /// <summary>변화가 끝난 뒤의 경기 상태(받는 팀 시점으로 걸러진 것).</summary>
        public GameSnapshot Snapshot { get; set; }

        /// <summary>현재 턴의 남은 시간(밀리초). 시간이 다 되면 서버가 그 팀의 턴을 끝낸다. 턴이 없으면(배치 단계, 경기 종료) null.</summary>
        public int? TurnTimeLeftMs { get; set; }
    }

    /// <summary>이벤트 한 건. Payload 는 Type 이름의 이벤트 클래스(EventTypes.Find)로 읽는다.</summary>
    public class EventEnvelope
    {
        /// <summary>경기 이벤트 로그의 순번.</summary>
        public int Sequence { get; set; }

        /// <summary>이벤트 클래스 이름(예: "UnitMovedEvent").</summary>
        public string Type { get; set; }

        /// <summary>이벤트 본문. 직렬화 쪽이 Type 에 맞는 클래스로 쓰고 읽는다.</summary>
        public object Payload { get; set; }
    }

    /// <summary>PingMessage 에 대한 답.</summary>
    public class PongMessage : ServerMessage
    {
    }

    /// <summary>경기와 무관한 오류(프로토콜 버전 불일치, 해석할 수 없는 메시지 등).</summary>
    public class ErrorMessage : ServerMessage
    {
        public string Message { get; set; }
    }
}
