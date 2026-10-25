using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Scheduled;
using MiniChess.Core.Effects.Zones;

namespace MiniChess.Core.Snapshots
{
    /// <summary>
    /// 한 팀에게 숨기는 정보의 규칙(사용자 확정). 서버의 시점 투영(GameProjection)과, 실제 경기를 보는 봇이 같은 규칙을 쓰도록 한 곳에 둔다.
    ///   - 상대가 설치한 덫(ROOT_TRAP)의 위치
    ///   - 상대가 예약한 포격의 조준 칸
    /// 숨김 규칙을 바꾸면 온라인 화면, 로컬 봇전 화면, 봇의 판단에 함께 적용된다.
    /// </summary>
    public static class GameVisibility
    {
        /// <summary>viewer 에게 숨기는 칸 효과 종류인지(상대가 설치한 덫).</summary>
        public static bool IsHiddenEffect(string kind, Team? ownerTeam, Team viewer)
        {
            return kind == CellEffectKinds.RootTrap && ownerTeam.HasValue && ownerTeam.Value != viewer;
        }

        /// <summary>viewer 에게 숨기는 칸 효과인지.</summary>
        public static bool IsHidden(ICellEffect effect, Team viewer)
        {
            return effect != null && IsHiddenEffect(effect.Kind, (effect as ZoneCellEffect)?.Zone.OwnerTeam, viewer);
        }

        /// <summary>viewer 에게 숨기는 예약 포격인지(상대 포격의 조준 칸).</summary>
        public static bool IsHidden(ScheduledStrike strike, Team viewer)
        {
            return IsHiddenStrike(strike.SourceTeam, viewer);
        }

        /// <summary>viewer 에게 숨기는 예약 포격인지(팀으로 판정).</summary>
        public static bool IsHiddenStrike(Team sourceTeam, Team viewer)
        {
            return sourceTeam != viewer;
        }
    }
}
