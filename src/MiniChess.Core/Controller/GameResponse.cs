using System;
using System.Collections.Generic;
using MiniChess.Core.Events;

namespace MiniChess.Core.Controller
{
    /// <summary>
    /// 컨트롤러 명령의 응답. 실패면 Reason 에 사유가 있고 상태는 바뀌지 않는다.
    /// 성공이면 Result 에 결과가, Events 에 이 요청으로 발생한 이벤트(로그/연출용, 발생 순)가 담긴다.
    /// </summary>
    /// <typeparam name="TReason">실패 사유 enum. 기본값(None)이 성공이다.</typeparam>
    public class GameResponse<TReason, TResult>
        where TReason : struct, Enum
        where TResult : class
    {
        public bool Success => EqualityComparer<TReason>.Default.Equals(Reason, default);

        public TReason Reason { get; }

        /// <summary>성공 시 결과. 실패면 null.</summary>
        public TResult Result { get; }

        public IReadOnlyList<GameEvent> Events { get; }

        private GameResponse(TReason reason, TResult result, IReadOnlyList<GameEvent> events)
        {
            Reason = reason;
            Result = result;
            Events = events;
        }

        internal static GameResponse<TReason, TResult> Fail(TReason reason)
        {
            return new GameResponse<TReason, TResult>(reason, null, Array.Empty<GameEvent>());
        }

        internal static GameResponse<TReason, TResult> Ok(TResult result, IReadOnlyList<GameEvent> events)
        {
            return new GameResponse<TReason, TResult>(default, result, events);
        }
    }
}
