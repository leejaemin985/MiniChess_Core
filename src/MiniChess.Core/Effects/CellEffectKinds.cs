namespace MiniChess.Core.Effects
{
    /// <summary>칸 효과 종류(ICellEffect.Kind) 값. 스냅샷/이벤트로 전송되므로 바꾸면 클라이언트 표시도 맞춰야 한다.</summary>
    public static class CellEffectKinds
    {
        public const string DamageField = "DAMAGE_FIELD";
        public const string HealField = "HEAL_FIELD";
        public const string PoisonField = "POISON_FIELD";
        public const string RootTrap = "ROOT_TRAP";
        public const string Obstacle = "OBSTACLE";
    }
}
