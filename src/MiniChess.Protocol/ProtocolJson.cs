using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MiniChess.Core.Common;
using MiniChess.Core.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace MiniChess.Protocol
{
    /// <summary>
    /// 메시지 JSON 형식. 서버와 클라이언트(Unity)가 이 코드 하나를 같이 쓴다.
    ///   - 봉투: { "Type": "Move", "Payload": { ... } }. Type 은 MessageTypes 이름이다.
    ///   - enum 은 이름 문자열만 받는다(정수는 거절). Position 은 { "X": 0, "Y": 0 }.
    ///   - 읽기는 형식이 잘못된 입력에 예외를 던지지 않고 null 을 돌려준다(네트워크 입력은 믿지 않는다).
    /// </summary>
    public static class ProtocolJson
    {
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Converters = { new PositionConverter(), new StringEnumConverter { AllowIntegerValues = false } },
            MaxDepth = 32,
        };

        private static readonly JsonSerializer Serializer = JsonSerializer.Create(Settings);

        public static string Encode(ClientMessage message) => EncodeEnvelope(message);

        public static string Encode(ServerMessage message) => EncodeEnvelope(message);

        /// <summary>클라이언트 메시지를 읽는다. 모르는 종류이거나 형식이 잘못되었으면 null.</summary>
        public static ClientMessage DecodeClient(string json) => Decode<ClientMessage>(json);

        /// <summary>서버 메시지를 읽는다. 모르는 종류이거나 형식이 잘못되었으면 null.</summary>
        public static ServerMessage DecodeServer(string json) => Decode<ServerMessage>(json);

        /// <summary>이벤트를 봉투로 감싼다. 본문은 이벤트의 실제 클래스로 직렬화된다.</summary>
        public static EventEnvelope Wrap(GameEvent gameEvent)
        {
            return new EventEnvelope { Sequence = gameEvent.Sequence, Type = EventTypes.NameOf(gameEvent), Payload = gameEvent };
        }

        /// <summary>이벤트 봉투의 본문을 코어 이벤트로 읽는다. 모르는 종류이거나 읽을 수 없으면 null.</summary>
        public static GameEvent ReadEvent(EventEnvelope envelope)
        {
            if (envelope?.Payload is GameEvent gameEvent)
                return gameEvent;

            Type type = EventTypes.Find(envelope?.Type);
            if (type == null || !(envelope.Payload is JObject body))
                return null;

            try
            {
                return (GameEvent)body.ToObject(type, Serializer);
            }
            catch (Exception exception) when (IsMalformedInput(exception))
            {
                return null;
            }
        }

        /// <summary>
        /// 값 객체(설정 데이터 등)의 지문: 속성 이름 순으로 정렬한 JSON 의 SHA-256(16진수).
        /// 서버와 클라이언트가 같은 데이터를 쓰는지 비교하는 데 쓴다(OnlineMatch.ConfigHash).
        /// </summary>
        public static string Fingerprint(object value)
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new SortedContractResolver(),
                Converters = { new PositionConverter(), new StringEnumConverter() },
                ReferenceLoopHandling = ReferenceLoopHandling.Error,
            };

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value, settings));
            using (SHA256 sha = SHA256.Create())
                return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
        }

        private static string EncodeEnvelope(object message)
        {
            var envelope = new JObject
            {
                ["Type"] = MessageTypes.NameOf(message.GetType()),
                ["Payload"] = JObject.FromObject(message, Serializer),
            };
            return envelope.ToString(Formatting.None);
        }

        private static T Decode<T>(string json) where T : class
        {
            try
            {
                JObject envelope = JObject.Parse(json);
                Type type = MessageTypes.Find(envelope.Value<string>("Type"));
                if (type == null || !typeof(T).IsAssignableFrom(type) || !(envelope["Payload"] is JObject payload))
                    return null;

                return (T)payload.ToObject(type, Serializer);
            }
            catch (Exception exception) when (IsMalformedInput(exception))
            {
                return null;
            }
        }

        /// <summary>잘못된 입력 때문에 읽기 중에 날 수 있는 예외(형식, 누락 값, 범위 밖 값 등).</summary>
        private static bool IsMalformedInput(Exception exception)
        {
            return exception is JsonException
                || exception is ArgumentException
                || exception is FormatException
                || exception is InvalidCastException
                || exception is InvalidOperationException
                || exception is OverflowException
                || exception is NullReferenceException;
        }

        /// <summary>Position(읽기 전용 구조체)과 Position? 를 { "X", "Y" } 로 쓰고 읽는다. X/Y 가 없으면 형식 오류.</summary>
        private sealed class PositionConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(Position) || objectType == typeof(Position?);

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    if (objectType == typeof(Position))
                        throw new JsonSerializationException("Position 이 null");
                    return null;
                }

                JObject value = JObject.Load(reader);
                if (value["X"]?.Type != JTokenType.Integer || value["Y"]?.Type != JTokenType.Integer)
                    throw new JsonSerializationException("Position 에 정수 X, Y 가 없음");

                return new Position((int)value["X"], (int)value["Y"]);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                var position = (Position)value;
                writer.WriteStartObject();
                writer.WritePropertyName("X");
                writer.WriteValue(position.X);
                writer.WritePropertyName("Y");
                writer.WriteValue(position.Y);
                writer.WriteEndObject();
            }
        }

        /// <summary>지문용: 속성을 이름 순으로 쓴다(런타임마다 반사 순서가 달라도 같은 JSON). 대리자(람다)는 뺀다.</summary>
        private sealed class SortedContractResolver : DefaultContractResolver
        {
            protected override System.Collections.Generic.IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                return base.CreateProperties(type, memberSerialization)
                    .Where(property => !typeof(Delegate).IsAssignableFrom(property.PropertyType))
                    .OrderBy(property => property.PropertyName, StringComparer.Ordinal)
                    .ToList();
            }
        }
    }
}
