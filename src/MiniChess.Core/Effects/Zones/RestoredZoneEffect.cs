namespace MiniChess.Core.Effects.Zones
{
    /// <summary>
    /// 스냅샷으로 복원한 경기의 칸 효과(장애물 제외). 종류/레이어/구역만 있고 발동 동작은 없다.
    /// 복원한 경기는 표시와 조회 전용이라 효과가 발동할 일이 없다(GameController.Restore).
    /// </summary>
    internal sealed class RestoredZoneEffect : ZoneCellEffect
    {
        public override CellEffectLayer Layer { get; }
        public override string Kind { get; }

        public RestoredZoneEffect(FieldZone zone, CellEffectLayer layer, string kind) : base(zone)
        {
            Layer = layer;
            Kind = kind;
        }

        internal override ZoneCellEffect Fork(FieldZone zone) => new RestoredZoneEffect(zone, Layer, Kind);
    }
}
