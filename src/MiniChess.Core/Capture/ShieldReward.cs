using System;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Capture
{
    /// <summary>
    /// 점령 시점에 보드 위에 살아 있는 아군 유닛 모두에게 보호막을 준다. 보호막은 소모될 때까지 유지된다.
    /// 재생(Regenerates)이면 이후 점령한 팀의 턴 시작마다 그때 보드 위에 살아 있는 아군의 이 보호막을 다시 Amount 로 채운다(쌓이지 않음, 사용자 확정).
    /// </summary>
    public class ShieldReward : CaptureReward
    {
        public const string RewardId = "SHIELD";

        /// <summary>이 보상이 주는 보호막의 Id. 다른 Id 의 보호막(스킬 등)과는 별개로 합산된다.</summary>
        public const string ShieldId = "CAPTURE_SHIELD";

        public int Amount { get; }

        /// <summary>점령한 팀의 턴 시작마다 이 보호막을 다시 Amount 로 채우는지(재생 보호막, 쌓이지 않음).</summary>
        public bool Regenerates { get; }

        public override string Id => RewardId;

        public ShieldReward(int amount, bool regenerate = false)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
            Regenerates = regenerate;
        }

        internal override void Grant(GameState state, Team team)
        {
            // AddShield 가 보드 위에 살아 있는 유닛만 처리한다.
            foreach (Unit unit in state.GetPlayer(team).Units)
                DamageSystem.AddShield(state, unit, ShieldId, Amount, source: null);
        }

        /// <summary>재생 보호막: 보드 위에 살아 있는 아군 중 이 보호막이 Amount 보다 적은 유닛만 Amount 로 다시 채운다.</summary>
        internal override void OnOwnerTurnStart(GameState state, Team team)
        {
            if (!Regenerates)
                return;

            foreach (Unit unit in state.GetPlayer(team).Units)
            {
                if ((unit.Stats.FindShield(ShieldId)?.Amount ?? 0) < Amount)
                    DamageSystem.AddShield(state, unit, ShieldId, Amount, source: null);
            }
        }
    }
}
