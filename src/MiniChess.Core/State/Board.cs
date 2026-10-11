using System;
using System.Collections.Generic;
using MiniChess.Core.Common;
using MiniChess.Core.Data;

namespace MiniChess.Core.State
{
    public class Board
    {
        private readonly BoardCell[,] _cells;
        private readonly Dictionary<Team, List<Position>> _spawnPositions = new Dictionary<Team, List<Position>>
        {
            { Team.Player1, new List<Position>() },
            { Team.Player2, new List<Position>() },
        };

        public int Width { get; }
        public int Height { get; }

        public Board(MapData map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            map.Validate();

            Width = map.Width;
            Height = map.Height;
            _cells = new BoardCell[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var position = new Position(x, y);
                    MapTileType tile = map.GetTile(position);

                    _cells[x, y] = CreateCell(position, tile);
                    RegisterSpawn(position, tile);
                }
            }
        }

        #region Construction

        private static BoardCell CreateCell(Position position, MapTileType tile)
        {
            TerrainType terrain = tile == MapTileType.Wall ? TerrainType.Wall : TerrainType.Ground;
            bool isCaptureTile = tile == MapTileType.Capture;

            return new BoardCell(position, terrain, isCaptureTile);
        }

        private void RegisterSpawn(Position position, MapTileType tile)
        {
            if (tile == MapTileType.SpawnPlayer1)
                _spawnPositions[Team.Player1].Add(position);
            else if (tile == MapTileType.SpawnPlayer2)
                _spawnPositions[Team.Player2].Add(position);
        }

        #endregion

        #region Query

        public bool IsInBounds(Position position)
        {
            return position.X >= 0 && position.X < Width
                && position.Y >= 0 && position.Y < Height;
        }

        public BoardCell GetCell(Position position)
        {
            if (!IsInBounds(position))
                throw new ArgumentOutOfRangeException(nameof(position), $"보드 범위 밖 좌표: {position}");

            return _cells[position.X, position.Y];
        }

        /// <summary>칸 위의 살아 있는 유닛. 보드 밖이거나 비었으면 null.</summary>
        public Unit GetLivingUnit(Position position)
        {
            if (!IsInBounds(position))
                return null;

            Unit occupant = GetCell(position).Occupant;
            return occupant != null && occupant.IsAlive ? occupant : null;
        }

        /// <summary>해당 팀의 시작 위치 후보 칸들.</summary>
        public IReadOnlyList<Position> GetSpawnPositions(Team team)
        {
            return _spawnPositions[team];
        }

        /// <summary>
        /// from 에서 to 까지 상하좌우 직선 경로의 칸들(from 제외, to 포함).
        /// 직선이 아니거나 같은 칸이면 빈 목록. 칸이 막혀 있는지는 검사하지 않는다.
        /// </summary>
        public IReadOnlyList<Position> GetStraightPath(Position from, Position to)
        {
            var path = new List<Position>();

            bool sameColumn = from.X == to.X;
            bool sameRow = from.Y == to.Y;
            if (sameColumn == sameRow)
                return path;

            int stepX = Math.Sign(to.X - from.X);
            int stepY = Math.Sign(to.Y - from.Y);

            var current = from;
            while (current != to)
            {
                current = new Position(current.X + stepX, current.Y + stepY);
                path.Add(current);
            }

            return path;
        }

        /// <summary>경로의 모든 칸에 유닛이 놓일 수 있는지(벽/유닛/범위 밖이 없는지).</summary>
        public bool IsPathClear(IReadOnlyList<Position> path)
        {
            foreach (Position position in path)
            {
                if (!CanPlace(position))
                    return false;
            }

            return true;
        }

        #endregion

        #region Placement

        /// <summary>해당 칸에 유닛이 놓일 수 있는지(벽/유닛/장애물이 없는지). 스폰/이동/순간이동 등 공통으로 사용.</summary>
        public bool CanPlace(Position position)
        {
            if (!IsInBounds(position))
                return false;

            BoardCell cell = GetCell(position);
            return !cell.IsWall && cell.IsEmpty && !cell.HasObstacle;
        }

        /// <summary>보드 밖에 있는 유닛을 해당 칸에 올릴 수 있는지.</summary>
        public bool CanSpawn(Unit unit, Position position)
        {
            return unit != null
                && !unit.IsPlaced
                && CanPlace(position);
        }

        /// <summary>보드 밖에 있는 유닛을 빈 칸에 올린다. 올릴 수 없으면 false.</summary>
        public bool TrySpawn(Unit unit, Position position)
        {
            if (!CanSpawn(unit, position))
                return false;

            GetCell(position).Occupant = unit;
            unit.Position = position;
            return true;
        }

        /// <summary>보드 위의 유닛을 다른 칸으로 옮긴다. 이동 방식(걷기/넉백/워프)과 무관한 공간 처리만 한다.</summary>
        internal void Relocate(Unit unit, Position to)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!unit.IsPlaced) throw new InvalidOperationException($"유닛 {unit.Id} 는 보드 위에 없음");
            if (!CanPlace(to)) throw new InvalidOperationException($"유닛 {unit.Id} 를 {to} 로 옮길 수 없음");

            GetCell(unit.Position.Value).Occupant = null;
            GetCell(to).Occupant = unit;
            unit.Position = to;
        }

        /// <summary>보드 위의 두 유닛 자리를 맞바꾼다. 공간 처리만 한다.</summary>
        internal void Swap(Unit a, Unit b)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));
            if (b == null) throw new ArgumentNullException(nameof(b));
            if (!a.IsPlaced || !b.IsPlaced) throw new InvalidOperationException("교환할 유닛이 보드 위에 없음");
            if (ReferenceEquals(a, b)) throw new InvalidOperationException("같은 유닛끼리는 교환할 수 없음");

            Position positionA = a.Position.Value;
            Position positionB = b.Position.Value;

            GetCell(positionA).Occupant = b;
            GetCell(positionB).Occupant = a;
            a.Position = positionB;
            b.Position = positionA;
        }

        /// <summary>유닛을 보드에서 뺀다(사망 등).</summary>
        internal void Remove(Unit unit)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (!unit.IsPlaced) throw new InvalidOperationException($"유닛 {unit.Id} 는 보드 위에 없음");

            GetCell(unit.Position.Value).Occupant = null;
            unit.Position = null;
        }

        #endregion
    }
}
