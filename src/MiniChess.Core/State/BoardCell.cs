using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Effects;

namespace MiniChess.Core.State
{
    public class BoardCell
    {
        private static readonly int LayerCount = Enum.GetValues(typeof(CellEffectLayer)).Length;

        /// <summary>레이어별 효과 슬롯. 인덱스 = (int)CellEffectLayer.</summary>
        private readonly ICellEffect[] _effects = new ICellEffect[LayerCount];

        public Position Position { get; }
        public TerrainType Terrain { get; }

        /// <summary>중앙 점령 목표 칸인지. 지형과 별개이며 장판 설치 등은 허용된다. 점령이 완료되면 소멸한다(false).</summary>
        public bool IsCaptureTile { get; internal set; }

        /// <summary>이 칸에 배치된 유닛. 없으면 null. 변경은 Board 를 통해서만 한다.</summary>
        public Unit Occupant { get; internal set; }

        public bool IsWall => Terrain == TerrainType.Wall;
        public bool IsEmpty => Occupant == null;

        /// <summary>장애물이 있어 유닛이 들어갈 수 없는 칸인지.</summary>
        public bool HasObstacle => HasEffect(CellEffectLayer.Obstacle);

        public BoardCell(Position position, TerrainType terrain, bool isCaptureTile)
        {
            Position = position;
            Terrain = terrain;
            IsCaptureTile = isCaptureTile;
        }

        #region Cell Effects

        /// <summary>해당 레이어의 효과. 없으면 null.</summary>
        public ICellEffect GetEffect(CellEffectLayer layer)
        {
            return _effects[(int)layer];
        }

        public bool HasEffect(CellEffectLayer layer)
        {
            return GetEffect(layer) != null;
        }

        /// <summary>이 효과가 아직 이 칸에 설치되어 있는지(제거/교체되지 않았는지).</summary>
        public bool Contains(ICellEffect effect)
        {
            return effect != null && GetEffect(effect.Layer) == effect;
        }

        /// <summary>효과를 자신의 레이어에 설치한다. 같은 레이어의 기존 효과는 덮어쓴다. 규칙 검사/이벤트는 CellEffectSystem 이 한다.</summary>
        internal void SetEffect(ICellEffect effect)
        {
            if (effect == null) throw new ArgumentNullException(nameof(effect));

            _effects[(int)effect.Layer] = effect;
        }

        internal void RemoveEffect(CellEffectLayer layer)
        {
            _effects[(int)layer] = null;
        }

        /// <summary>설치된 효과를 레이어 순서(= 발동 순서)대로 반환한다.</summary>
        public IEnumerable<ICellEffect> GetEffectsInOrder()
        {
            foreach (ICellEffect effect in _effects)
            {
                if (effect != null)
                    yield return effect;
            }
        }

        #endregion
    }
}
