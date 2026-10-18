using System;
using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Events;

namespace MiniChess.Protocol
{
    public static class ProtocolInfo
    {
        /// <summary>메시지 형식 버전. 메시지/이벤트/스냅샷의 모양이 바뀌면 올린다.</summary>
        public const int Version = 2;
    }

    /// <summary>
    /// 메시지 봉투 { Type, Payload } 의 Type 이름 ↔ 클래스. 이름은 클래스 이름에서 "Message" 를 뗀 것(예: "Move").
    /// </summary>
    public static class MessageTypes
    {
        public static readonly IReadOnlyList<Type> Client = new[]
        {
            typeof(JoinMatchMessage),
            typeof(PlaceUnitsMessage),
            typeof(MoveMessage),
            typeof(AttackMessage),
            typeof(AttackObstacleMessage),
            typeof(UseSkillMessage),
            typeof(EndTurnMessage),
        };

        public static readonly IReadOnlyList<Type> Server = new[]
        {
            typeof(MatchStartedMessage),
            typeof(CommandResultMessage),
            typeof(StateUpdateMessage),
            typeof(ErrorMessage),
        };

        private static readonly Dictionary<string, Type> ByName = Client.Concat(Server).ToDictionary(NameOf);

        public static string NameOf(Type type)
        {
            const string suffix = "Message";
            return type.Name.EndsWith(suffix) ? type.Name.Substring(0, type.Name.Length - suffix.Length) : type.Name;
        }

        /// <summary>이름의 메시지 클래스. 모르는 이름이면 null.</summary>
        public static Type Find(string name)
        {
            return name != null && ByName.TryGetValue(name, out Type type) ? type : null;
        }
    }

    /// <summary>
    /// EventEnvelope.Type 이름 ↔ 코어 이벤트 클래스. 코어의 GameEvent 하위 클래스 전부를 자동으로 등록한다(새 이벤트를 빠뜨리지 않도록).
    /// </summary>
    public static class EventTypes
    {
        private static readonly Dictionary<string, Type> ByName = typeof(GameEvent).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(GameEvent).IsAssignableFrom(type))
            .ToDictionary(type => type.Name);

        public static IEnumerable<Type> All => ByName.Values;

        public static string NameOf(GameEvent gameEvent) => gameEvent.GetType().Name;

        /// <summary>이름의 이벤트 클래스. 모르는 이름이면 null.</summary>
        public static Type Find(string name)
        {
            return name != null && ByName.TryGetValue(name, out Type type) ? type : null;
        }
    }
}
