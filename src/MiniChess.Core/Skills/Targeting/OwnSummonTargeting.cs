using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Movement;
using MiniChess.Core.State;
using MiniChess.Core.Summons;

namespace MiniChess.Core.Skills.Targeting
{
    /// <summary>
    /// 시전자 자신의 살아 있는 소환물 하나를 지정한다(거리 무관).
    /// MovedBy 를 지정하면 그 방식의 외부 이동이 상태효과로 막힌 소환물은 뺀다.
    /// </summary>
    public class OwnSummonTargeting : SkillTargeting
    {
        public MoveKind? MovedBy { get; }

        public OwnSummonTargeting(MoveKind? movedBy = null)
        {
            MovedBy = movedBy;
        }

        public override IEnumerable<Position> GetCandidates(GameState state, Unit caster)
        {
            foreach (Unit summon in SummonSystem.GetLivingSummons(state, caster))
            {
                if (!summon.IsTargetable)
                    continue;

                if (MovedBy == null || MovementControl.IsAllowedByStatuses(state, summon, caster, MovedBy.Value))
                    yield return summon.Position.Value;
            }
        }
    }
}
