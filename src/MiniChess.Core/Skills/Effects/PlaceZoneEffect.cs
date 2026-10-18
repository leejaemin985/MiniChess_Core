using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;

namespace MiniChess.Core.Skills.Effects
{
    /// <summary>
    /// 범위의 각 칸에 구역(FieldZone) 효과를 설치한다. 한 번의 시전이 하나의 구역이 되며 수명을 공유한다.
    /// 설치할 수 없는 칸(벽, 점령 칸의 덫 등)은 건너뛴다.
    /// </summary>
    public class PlaceZoneEffect : SkillEffect
    {
        /// <summary>구역 수명(설치한 쪽 소유자의 턴 종료 횟수). [TBD 가능]</summary>
        public int? Lifetime { get; }

        /// <summary>구역과 칸으로 칸 효과를 만든다. 효과 수치가 TBD 면 null 로 둔다.</summary>
        public Func<FieldZone, Position, ZoneCellEffect> Factory { get; }

        public PlaceZoneEffect(int? lifetime, Func<FieldZone, Position, ZoneCellEffect> factory)
        {
            Lifetime = lifetime;
            Factory = factory;
        }

        public override void CollectConfigIssues(string path, List<string> issues)
        {
            Require(Lifetime, path, nameof(Lifetime), issues);
            Require(Factory, path, nameof(Factory), issues);
        }

        public override void Apply(SkillContext context)
        {
            var zone = new FieldZone(context.Skill.Id, context.Caster, Lifetime.Value, context.State.TurnNumber)
            {
                Id = context.State.NextZoneId(),
            };

            foreach (Position position in context.AffectedCells)
            {
                ZoneCellEffect effect = Factory(zone, position);
                if (CellEffectSystem.Place(context.State, position, effect) == CellEffectPlaceFailReason.None)
                    zone.Register(position, effect);
            }
        }
    }
}
