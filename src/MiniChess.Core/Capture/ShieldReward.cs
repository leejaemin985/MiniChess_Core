using System;
using MiniChess.Core.Combat;
using MiniChess.Core.Common;
using MiniChess.Core.State;

namespace MiniChess.Core.Capture
{
    /// <summary>
    /// 점령 시점에 보드 위에 살아 있는 아군 유닛 모두에게 보호막을 준다. 보호막은 소모될 때까지 유지된다.
    /// </summary>
    public class ShieldReward : CaptureReward
    {
        public const string RewardId = "SHIELD";

        /// <summary>이 보상이 주는 보호막의 Id. 다른 Id 의 보호막(스킬 등)과는 별개로 합산된다.</summary>
        public const string ShieldId = "CAPTURE_SHIELD";

        public int Amount { get; }

        public override string Id => RewardId;

        public ShieldReward(int amount)
        {
            if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

            Amount = amount;
        }

        internal override void Grant(GameState state, Team team)
        {
            // AddShield 가 보드 위에 살아 있는 유닛만 처리한다.
            foreach (Unit unit in state.GetPlayer(team).Units)
                DamageSystem.AddShield(state, unit, ShieldId, Amount, source: null);
        }
    }
}
