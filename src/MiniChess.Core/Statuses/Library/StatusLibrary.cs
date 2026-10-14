using MiniChess.Core.Turns;

namespace MiniChess.Core.Statuses.Library
{
    /// <summary>
    /// 공용 상태효과 정의 모음. 스킬, 덫, 보상 등 어디서든 같은 상태를 이 정의로 건다.
    /// 수치가 하나라도 null 이면 null 을 반환한다(그 상태를 쓰는 기능은 설정 누락으로 사용 불가).
    /// </summary>
    public static class StatusLibrary
    {
        public const string RootId = "ROOT";
        public const string BurnId = "BURN";
        public const string DistanceLimitId = "DISTANCE_LIMIT";
        public const string GuardId = "GUARD";
        public const string InstalledId = "INSTALLED";
        public const string CurseMarkId = "CURSE_MARK";
        public const string ChargedId = "CHARGED";

        // 대상 소유자의 턴 종료 단계. 걸린 그 턴은 세지 않아, 1 이면 대상의 다음 자기 턴까지 유지된다.
        private static readonly StatusTiming TargetDurationTick =
            new StatusTiming(TurnStep.EndDurationTick, TimingOwner.TargetOwner, includeApplicationTurn: false);

        private static readonly StatusTiming TargetDamageOverTime =
            new StatusTiming(TurnStep.EndDamageOverTime, TimingOwner.TargetOwner, includeApplicationTurn: false);

        // 건 쪽 소유자의 턴 시작 첫 효과 단계. 1 이면 상대 턴 동안 유지되고 건 쪽의 다음 턴 시작에 풀린다.
        private static readonly StatusTiming SourceTurnStart =
            new StatusTiming(TurnStep.StartPositiveEffects, TimingOwner.SourceOwner, includeApplicationTurn: false);

        /// <summary>
        /// 속박: 자발적 이동 불가. 공격/스킬은 가능하다.
        /// 다시 걸면 남은 횟수를 갱신한다.
        /// </summary>
        /// <param name="targetTurns">지속 횟수(대상 소유자의 턴 종료 횟수).</param>
        /// <param name="blocksExternalMoves">넉백/당기기/워프 같은 외부 강제 이동도 막는지.</param>
        public static StatusDefinition Root(int? targetTurns, bool? blocksExternalMoves)
        {
            if (targetTurns == null || blocksExternalMoves == null)
                return null;

            return new StatusDefinition(
                RootId,
                targetTurns.Value,
                StatusStackPolicy.Refresh,
                TargetDurationTick,
                behavior: new RootBehavior(blocksExternalMoves.Value));
        }

        /// <summary>
        /// 화상: 대상 소유자의 턴 종료마다 DoT 피해. 출처는 상태를 건 유닛.
        /// 다시 걸면 피해를 쌓지 않고 남은 횟수만 갱신한다.
        /// </summary>
        /// <param name="damagePerTrigger">1 회 발동 피해량.</param>
        /// <param name="triggers">발동 횟수.</param>
        public static StatusDefinition Burn(int? damagePerTrigger, int? triggers)
        {
            if (damagePerTrigger == null || triggers == null)
                return null;

            return new StatusDefinition(
                BurnId,
                triggers.Value,
                StatusStackPolicy.Refresh,
                TargetDurationTick,
                TargetDamageOverTime,
                new BurnBehavior(damagePerTrigger.Value));
        }

        /// <summary>
        /// 이동 거리 제한: 한 턴의 일반 이동을 누적 maxCells 칸으로 제한. 스킬 이동과 외부 강제 이동은 막지 않는다.
        /// 다시 걸면 남은 횟수를 갱신한다.
        /// </summary>
        /// <param name="maxCells">한 턴에 자발적으로 이동할 수 있는 최대 칸 수(누적).</param>
        /// <param name="targetTurns">지속 횟수(대상 소유자의 턴 종료 횟수).</param>
        public static StatusDefinition DistanceLimit(int? maxCells, int? targetTurns)
        {
            if (maxCells == null || targetTurns == null)
                return null;

            return new StatusDefinition(
                DistanceLimitId,
                targetTurns.Value,
                StatusStackPolicy.Refresh,
                TargetDurationTick,
                behavior: new DistanceLimitBehavior(maxCells.Value));
        }

        /// <summary>
        /// 호위: 받은 유닛의 직접 피해를 건 유닛이 대신 받는다(피해 시점에 maxDistance 이내일 때만).
        /// 건 쪽의 다음 턴 시작에 풀리며, 건 유닛이 죽으면 즉시 풀린다.
        /// 한 대상에는 호위자 한 명(새로 걸면 교체), 한 호위자는 한 대상만 호위한다.
        /// </summary>
        /// <param name="maxDistance">호위자와 대상 사이 최대 거리(체비셰프).</param>
        public static StatusDefinition Guard(int? maxDistance)
        {
            if (maxDistance == null)
                return null;

            return new StatusDefinition(
                GuardId,
                duration: 1,
                StatusStackPolicy.Replace,
                SourceTurnStart,
                behavior: new GuardBehavior(maxDistance.Value),
                removeOnSourceDeath: true);
        }

        /// <summary>
        /// 설치: 영구 상태(해체할 때까지). 자발적 이동과 기본 공격 불가, 외부 강제 이동은 허용. 다시 걸어도 아무 일 없다.
        /// 설치형 캐릭터 공용이며, 설치/해체 시 부가 효과(보호막 등)는 각 캐릭터의 스킬이 정한다.
        /// </summary>
        public static StatusDefinition Installed()
        {
            return new StatusDefinition(
                InstalledId,
                duration: 1,
                StatusStackPolicy.Ignore,
                decrement: null,
                behavior: new InstalledBehavior());
        }

        /// <summary>
        /// 저주 표식: 표식 소유자(건 유닛의 소환자)의 다음 직접 피해에 추가 피해 1회, 발동하면 소모.
        /// 스택하지 않으며 다시 걸면 남은 횟수를 갱신한다. 건 유닛(분신)이 죽어도 유지된다.
        /// </summary>
        /// <param name="targetTurns">지속 횟수(대상 소유자의 턴 종료 횟수).</param>
        /// <param name="bonusDamage">발동 시 추가 피해.</param>
        public static StatusDefinition CurseMark(int? targetTurns, int? bonusDamage)
        {
            if (targetTurns == null || bonusDamage == null)
                return null;

            return new StatusDefinition(
                CurseMarkId,
                targetTurns.Value,
                StatusStackPolicy.Refresh,
                TargetDurationTick,
                behavior: new CurseMarkBehavior(bonusDamage.Value));
        }

        /// <summary>
        /// 차징: 대상의 다음 기본 공격에 추가 피해, 공격하면 소모. 걸린 턴은 세지 않아 대상의 다음 자기 턴 끝까지 유지된다.
        /// 다시 걸면 남은 횟수를 갱신한다.
        /// </summary>
        /// <param name="bonusDamage">기본 공격 추가 피해.</param>
        public static StatusDefinition Charged(int? bonusDamage)
        {
            if (bonusDamage == null)
                return null;

            return new StatusDefinition(
                ChargedId,
                duration: 1,
                StatusStackPolicy.Refresh,
                TargetDurationTick,
                behavior: new ChargedBehavior(bonusDamage.Value));
        }
    }
}
