using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Characters.Shared;

namespace MiniChess.Core.Characters
{
    /// <summary>
    /// 캐릭터 스킬의 수치 묶음. 캐릭터별 수치 항목은 각 Piece 파일에 정의한다.
    /// 명세상 모두 TBD 이며 기본값은 전부 null(미설정)이다. 값은 프리셋 등 외부 데이터에서 채운다.
    /// 0 은 "없음"을 뜻한다(예: 즉시 피해 0 = 즉시 피해 없음).
    /// </summary>
    public class SkillTuning
    {
        /// <summary>속박 공통 정책(화학 덫 전문가, 사슬 수호기사).</summary>
        public RootPolicyTuning Root { get; } = new RootPolicyTuning();

        public GardenerTuning Gardener { get; } = new GardenerTuning();
        public WarriorTuning Warrior { get; } = new WarriorTuning();
        public ArcherTuning Archer { get; } = new ArcherTuning();
        public ChemistTuning Chemist { get; } = new ChemistTuning();
        public FlameTuning Flame { get; } = new FlameTuning();
        public ChainGuardTuning ChainGuard { get; } = new ChainGuardTuning();
        public SeamstressTuning Seamstress { get; } = new SeamstressTuning();
        public ScytheTuning Scythe { get; } = new ScytheTuning();
        public MortarTuning Mortar { get; } = new MortarTuning();
        public AssassinTuning Assassin { get; } = new AssassinTuning();
        public SculptorTuning Sculptor { get; } = new SculptorTuning();
        public InquisitorTuning Inquisitor { get; } = new InquisitorTuning();
    }
}
