using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Snapshots;

namespace MiniChess.Protocol
{
    /// <summary>서버 → 클라이언트 메시지.</summary>
    public abstract class ServerMessage
    {
    }

    /// <summary>상대가 정해져 경기가 시작되었다(배치 단계). 받은 팀으로 이후 화면을 그린다.</summary>
    public class MatchStartedMessage : ServerMessage
    {
        public Team YourTeam { get; set; }
        public GameSnapshot Snapshot { get; set; }

        /// <summary>
        /// [임시: 클라 로컬 복제 방식] 경기 시드. 클라이언트는 같은 프리셋과 이 시드로 경기를 만들고, CommandAppliedMessage 를 그대로 실행해 서버와 같은 상태를 유지한다.
        /// 스냅샷 복원 방식으로 바꾸면 없앤다(시드를 알면 무작위 결과를 미리 알 수 있다).
        /// </summary>
        public int Seed { get; set; }
    }

    /// <summary>명령 하나의 처리 결과. 성공이면 이어서(또는 먼저) EventsMessage 로 변화가 온다.</summary>
    public class CommandResultMessage : ServerMessage
    {
        public int RequestId { get; set; }
        public bool Success { get; set; }

        /// <summary>실패 사유(코어 실패 enum 의 이름, 또는 StaleTurn/InvalidRequest 등 서버 사유). 성공이면 null.</summary>
        public string Reason { get; set; }
    }

    /// <summary>
    /// [임시: 클라 로컬 복제 방식] 서버가 명령 하나를 실행했다(양쪽 모두에게, 보낸 쪽 포함). 클라이언트는 같은 명령을 자기 경기에 실행한다.
    /// 실행 후 이벤트 로그의 마지막 순번이 LastEventSequence 와 다르면 서버와 어긋난 것이다.
    /// </summary>
    public class CommandAppliedMessage : ServerMessage
    {
        public Team Team { get; set; }

        /// <summary>명령 메시지 이름(MessageTypes 이름, 예: "Move").</summary>
        public string CommandType { get; set; }

        /// <summary>명령 본문. 직렬화 쪽이 CommandType 에 맞는 클래스로 쓰고 읽는다.</summary>
        public object Command { get; set; }

        public int LastEventSequence { get; set; }
    }

    /// <summary>경기에서 일어난 일(받는 팀 시점으로 걸러진 것). 순번 순이다.</summary>
    public class EventsMessage : ServerMessage
    {
        public List<EventEnvelope> Events { get; set; }
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

    /// <summary>현재 상태 전체. 동기화가 어긋났을 때 등 서버가 필요할 때 보낸다.</summary>
    public class SnapshotMessage : ServerMessage
    {
        public GameSnapshot Snapshot { get; set; }
    }

    /// <summary>경기와 무관한 오류(프로토콜 버전 불일치, 해석할 수 없는 메시지 등).</summary>
    public class ErrorMessage : ServerMessage
    {
        public string Message { get; set; }
    }
}
