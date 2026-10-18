using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Events;

namespace MiniChess.Protocol
{
    /// <summary>
    /// 명령 하나의 결과. 실패면 상태는 바뀌지 않았고 Events 는 비어 있다.
    /// 예외: 서버 오류로 경기가 무효 처리되었으면(ServerError) 실패이지만 Events 에 경기 종료 이벤트가 담긴다.
    /// </summary>
    public class CommandOutcome
    {
        /// <summary>요청 턴 번호가 서버의 현재 턴과 다름(오래되었거나 중복된 요청).</summary>
        public const string StaleTurn = "StaleTurn";

        /// <summary>메시지 형식이 잘못됨(필수 값 누락 등).</summary>
        public const string InvalidRequest = "InvalidRequest";

        /// <summary>경기가 이미 끝남.</summary>
        public const string GameOver = "GameOver";

        /// <summary>처리 중 서버 오류가 나서 경기가 무효 처리됨.</summary>
        public const string ServerError = "ServerError";

        public bool Success { get; }

        /// <summary>실패 사유. 코어 실패 enum 의 이름이거나 위 상수. 성공이면 null.</summary>
        public string Reason { get; }

        public IReadOnlyList<GameEvent> Events { get; }

        private CommandOutcome(bool success, string reason, IReadOnlyList<GameEvent> events)
        {
            Success = success;
            Reason = reason;
            Events = events;
        }

        public static CommandOutcome Ok(IReadOnlyList<GameEvent> events) => new CommandOutcome(true, null, events);
        public static CommandOutcome Fail(string reason) => new CommandOutcome(false, reason, Array.Empty<GameEvent>());

        /// <summary>서버 오류로 경기를 무효 처리했다. abortEvents 는 GameController.Abort 가 돌려준 종료 이벤트.</summary>
        public static CommandOutcome Aborted(IReadOnlyList<GameEvent> abortEvents) => new CommandOutcome(false, ServerError, abortEvents);
    }

    /// <summary>
    /// 명령 메시지를 코어 컨트롤러에 실행한다. 요청 팀은 호출하는 쪽(서버: 연결의 팀)이 정한다.
    /// 메시지 종류별 분기는 이곳 한 군데에만 둔다.
    /// </summary>
    public static class CommandExecutor
    {
        public static CommandOutcome Execute(GameController game, Team team, CommandMessage command)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));

            if (command == null)
                return CommandOutcome.Fail(CommandOutcome.InvalidRequest);
            if (command.TurnNumber != game.State.TurnNumber)
                return CommandOutcome.Fail(CommandOutcome.StaleTurn);

            GameRequest request = command.ToRequest(team);
            switch (request)
            {
                case PlaceUnitsRequest place:
                    return From(game.PlaceUnits(place));
                case MoveRequest move:
                    return From(game.Move(move));
                case AttackRequest attack:
                    return From(game.Attack(attack));
                case AttackObstacleRequest attackObstacle:
                    return From(game.AttackObstacle(attackObstacle));
                case UseSkillRequest useSkill:
                    return From(game.UseSkill(useSkill));
                case EndTurnRequest endTurn:
                    return From(game.EndTurn(endTurn));
                default:
                    return CommandOutcome.Fail(CommandOutcome.InvalidRequest);
            }
        }

        private static CommandOutcome From<TReason, TResult>(GameResponse<TReason, TResult> response)
            where TReason : struct, Enum
            where TResult : class
        {
            return response.Success ? CommandOutcome.Ok(response.Events) : CommandOutcome.Fail(response.Reason.ToString());
        }
    }
}
