using System;
using System.Collections.Generic;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Statuses;

namespace MiniChess.Core.State
{
    /// <summary>
    /// 경기 상태를 복사할 때(GameState.Fork) 원본 객체 → 복사본 객체 대응표.
    /// 서로를 가리키는 객체(상태를 건 유닛, 구역의 설치자, 호위 가로채기의 상태 등)를 복사본끼리 다시 잇는 데 쓴다.
    /// </summary>
    internal class ForkMap
    {
        private readonly Dictionary<Unit, Unit> _units = new Dictionary<Unit, Unit>();
        private readonly Dictionary<StatusEffect, StatusEffect> _statuses = new Dictionary<StatusEffect, StatusEffect>();
        private readonly Dictionary<FieldZone, FieldZone> _zones = new Dictionary<FieldZone, FieldZone>();
        private readonly Dictionary<ZoneCellEffect, ZoneCellEffect> _effects = new Dictionary<ZoneCellEffect, ZoneCellEffect>();

        public void Add(Unit original, Unit copy) => _units.Add(original, copy);

        public void Add(StatusEffect original, StatusEffect copy) => _statuses.Add(original, copy);

        public void Add(ZoneCellEffect original, ZoneCellEffect copy) => _effects.Add(original, copy);

        /// <summary>원본 유닛의 복사본. null 이면 null.</summary>
        public Unit Unit(Unit original)
        {
            if (original == null)
                return null;

            return _units.TryGetValue(original, out Unit copy)
                ? copy
                : throw new InvalidOperationException($"복사되지 않은 유닛 {original.Id} 를 가리킴");
        }

        public StatusEffect Status(StatusEffect original)
        {
            return _statuses.TryGetValue(original, out StatusEffect copy)
                ? copy
                : throw new InvalidOperationException($"복사되지 않은 상태 {original.Definition.Id} 를 가리킴");
        }

        /// <summary>원본 칸 효과의 복사본. 처음 보는 구역이면 구역째(등록된 모든 칸 효과 포함) 복사한다.</summary>
        public ZoneCellEffect Effect(ZoneCellEffect original)
        {
            if (!_zones.ContainsKey(original.Zone))
                _zones.Add(original.Zone, original.Zone.Fork(this));

            return _effects[original];
        }
    }
}
