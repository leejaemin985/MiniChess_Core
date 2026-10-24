using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Capture;
using MiniChess.Core.Characters;
using MiniChess.Core.Characters.Pieces;
using MiniChess.Core.Common;
using MiniChess.Core.Data;
using MiniChess.Core.Skills;
using MiniChess.Core.Statuses.Library;

namespace MiniChess.Core.Presets
{
    /// <summary>
    /// 플레이 테스트용 프리셋. 확정 밸런스가 아니다.
    ///   - 규칙/맵: 기획 v0.7 의 [프로토타입] 값
    ///   - 캐릭터 수치: 명세상 TBD 이며 이 프리셋에서만 임시로 채운다
    /// 임시로 정한 값과 근거는 저장소 루트의 ASSUMPTIONS.md 에 기록한다. 값을 바꾸면 그 문서도 함께 갱신한다.
    /// </summary>
    public static class PrototypeTestPreset
    {
        public static GameRuleData CreateRules()
        {
            return new GameRuleData
            {
                // [가정] 명세 v0.1 3.2 의 [프로토타입] 값(6/4/4)에서 플레이 테스트를 위해 12/8/8 로 변경. 나머지는 [프로토타입] 값
                Ap = new ApRuleData
                {
                    MaxAp = 12,
                    StartAp = 8,
                    TurnRecoveryAp = 8,
                    CarryOver = true,
                },
                ActionCost = new ActionCostData
                {
                    MoveCostPerCell = 1,
                    BasicAttackCost = 2,
                },
                // [프로토타입] 명세 v0.1 3.1 (4 vs 4)
                Match = new MatchRuleData
                {
                    UnitsPerTeam = 4,
                    FirstTeam = Team.Player1,
                    // 사용자 확정: 소환물(분신)은 전멸 판정에서 제외
                    SummonElimination = SummonEliminationPolicy.ExcludeSummons,
                    // 사용자 확정: 전체 턴 번호 200. [가정] 수치는 플레이 테스트로 조정
                    MaxTurns = 200,
                },
                // [프로토타입] 명세 v0.1 3.5. 판정 시점(자기 Turn Start)과 보상 종류는 사용자 확정, 보상 수치는 [가정]
                Capture = new CaptureRuleData
                {
                    RequiredTurnStartCount = 2,
                    ActiveFromStart = true,
                    RewardPool = new List<CaptureReward>
                    {
                        new ShieldReward(amount: 3, regenerate: true),
                        new BasicAttackStatusReward(StatusLibrary.Burn(damagePerTrigger: 1, triggers: 2)),
                    },
                },
            };
        }

        /// <summary>
        /// 기획서 3.1 테스트 맵. 벽 배치는 기획서 그대로이며,
        /// 시작 위치는 기획서에 없어 임의로 지정했다. [가정]
        /// </summary>
        public static MapData CreateTestMap()
        {
            return new MapData
            {
                Name = "TestMap_7x7",
                Rows = new[]
                {
                    "2222222",
                    "2222222",
                    "...#...",
                    "#..C..#",
                    "...#...",
                    "1111111",
                    "1111111",
                },
            };
        }

        /// <summary>모든 캐릭터에 임시 수치를 채운 정의. [가정] ASSUMPTIONS.md 참고.</summary>
        public static List<CharacterDefinition> CreateCharacters()
        {
            var temporaryStats = new Dictionary<string, (int Hp, int Attack, int Range)>
            {
                { ScythePiece.PieceId,     (10, 3, 1) },
                { MortarPiece.PieceId,     (9,  2, 3) },
                { GardenerPiece.PieceId,   (16, 2, 1) },
                { WarriorPiece.PieceId,    (12, 3, 1) },
                { ArcherPiece.PieceId,     (8,  2, 3) },
                { ChemistPiece.PieceId,    (9,  2, 2) },
                { FlamePiece.PieceId,      (9,  3, 2) },
                { SeamstressPiece.PieceId, (9,  2, 2) },
                { ChainGuardPiece.PieceId, (15, 2, 1) },
                { AssassinPiece.PieceId,   (9,  4, 1) },
                { SculptorPiece.PieceId,   (9,  2, 2) },
                { InquisitorPiece.PieceId, (14, 2, 1) },
            };

            return PieceModules.CreateDefinitions()
                .Select(definition =>
                {
                    (int hp, int attack, int range) = temporaryStats[definition.Id];
                    definition.MaxHp = hp;
                    definition.Attack = attack;
                    definition.AttackRange = range;
                    return definition;
                })
                .ToList();
        }

        /// <summary>구현된 캐릭터 스킬을 임시 수치로 만든 스킬 목록.</summary>
        public static SkillCatalog CreateSkills()
        {
            return PieceModules.CreateSkillCatalog(CreateSkillTuning());
        }

        /// <summary>스킬 임시 수치. [가정] 모두 명세상 TBD. ASSUMPTIONS.md 참고.</summary>
        public static SkillTuning CreateSkillTuning()
        {
            var t = new SkillTuning();

            t.Root.BlocksExternalMoves = false;

            t.Warrior.Smash.ApCost = 3;
            t.Warrior.Smash.Damage = 5;

            t.Warrior.Dash.ApCost = 3;
            t.Warrior.Dash.Range = 3;
            t.Warrior.Dash.Damage = 3;

            t.Archer.AimedShot.ApCost = 3;
            t.Archer.AimedShot.Range = 5;
            t.Archer.AimedShot.Damage = 3;

            t.Flame.FlameZone.ApCost = 5;
            t.Flame.FlameZone.Range = 3;
            t.Flame.FlameZone.Area.Shape = new[]
            {
                ".#.",
                "#@#",
                ".#.",
            };
            t.Flame.FlameZone.ImpactDamage = 2;
            t.Flame.FlameZone.Damage = 3;
            t.Flame.FlameZone.Lifetime = 2;
            t.Flame.FlameZone.MaxActive = 1;

            t.Flame.CompressedShell.ApCost = 3;
            t.Flame.CompressedShell.Range = 3;
            t.Flame.CompressedShell.ImpactDamage = 1;
            t.Flame.CompressedShell.BurnDamage = 1;
            t.Flame.CompressedShell.BurnTriggers = 2;

            t.Chemist.RootTrap.ApCost = 3;
            t.Chemist.RootTrap.Range = 2;
            t.Chemist.RootTrap.Damage = 1;
            t.Chemist.RootTrap.RootTurns = 1;
            t.Chemist.RootTrap.MaxActive = 2;
            t.Chemist.RootTrap.Lifetime = 3;
            t.Chemist.RootTrap.SingleUse = true;

            t.Chemist.PoisonGas.ApCost = 3;
            t.Chemist.PoisonGas.Range = 3;
            t.Chemist.PoisonGas.Area.Shape = new[]
            {
                "##",
                "@#",
            };
            t.Chemist.PoisonGas.PoisonDamage = 1;
            t.Chemist.PoisonGas.PoisonTurns = 2;
            t.Chemist.PoisonGas.Lifetime = 2;
            t.Chemist.PoisonGas.MaxActive = 1;

            t.ChainGuard.Guard.ApCost = 3;
            t.ChainGuard.Guard.Range = 2;
            t.ChainGuard.Guard.MaxDistance = 2;

            t.ChainGuard.ChainBind.ApCost = 3;
            t.ChainGuard.ChainBind.Range = 2;
            t.ChainGuard.ChainBind.Damage = 0;

            t.Gardener.HealingMeadow.ApCost = 4;
            t.Gardener.HealingMeadow.Range = 2;
            t.Gardener.HealingMeadow.Area.Shape = new[]
            {
                "###",
                "#@#",
                "###",
            };
            t.Gardener.HealingMeadow.Heal = 1;
            t.Gardener.HealingMeadow.Lifetime = 2;

            t.Gardener.SingleShield.ApCost = 3;
            t.Gardener.SingleShield.Range = 2;
            t.Gardener.SingleShield.Amount = 3;

            t.Mortar.Bombard.ApCost = 4;
            t.Mortar.Bombard.Range = 4;
            t.Mortar.Bombard.Area.Shape = new[]
            {
                ".#.",
                "#@#",
                ".#.",
            };
            t.Mortar.Bombard.Damage = 5;
            t.Mortar.Bombard.MaxActive = 1;
            t.Mortar.Bombard.InstallShieldAmount = 2;

            t.Mortar.Uninstall.ApCost = 2;

            t.Mortar.ShockShell.ApCost = 3;
            t.Mortar.ShockShell.Range = 3;
            t.Mortar.ShockShell.Damage = 1;
            t.Mortar.ShockShell.PushCells = 2;
            t.Mortar.ShockShell.LimitCells = 1;
            t.Mortar.ShockShell.LimitTurns = 1;

            t.Scythe.Slash.ApCost = 3;
            t.Scythe.Slash.Range = 2;
            t.Scythe.Slash.Area.Shape = new[] { "#@##" };
            t.Scythe.Slash.Area.Rotate = true;
            t.Scythe.Slash.Damage = 3;

            t.Scythe.ShadowClone.ApCost = 3;
            t.Scythe.ShadowClone.CloneHp = 1;
            t.Scythe.ShadowClone.CloneAttackRange = 1;
            t.Scythe.ShadowClone.MarkBonusDamage = 2;
            t.Scythe.CloneSwap.ApCost = 1;

            t.Seamstress.Warp.ApCost = 3;
            t.Seamstress.Warp.Range = 3;

            t.Seamstress.Obstacle.ApCost = 3;
            t.Seamstress.Obstacle.Range = 3;
            t.Seamstress.Obstacle.Area.Shape = new[] { "@#" };
            t.Seamstress.Obstacle.Area.Rotate = true;
            t.Seamstress.Obstacle.Hits = 1;
            t.Seamstress.Obstacle.Lifetime = 2;
            t.Seamstress.Obstacle.MaxActive = 1;

            t.Assassin.Leap.ApCost = 3;
            t.Assassin.Leap.Range = 4;
            t.Assassin.Charge.ApCost = 2;
            t.Assassin.Charge.BonusDamage = 4;

            t.Sculptor.Seal.ApCost = 4;
            t.Sculptor.Seal.Range = 3;
            t.Sculptor.Correction.ApCost = 2;
            t.Sculptor.Correction.Range = 3;
            t.Sculptor.Correction.Damage = 1;
            t.Sculptor.Correction.PushCells = 2;

            t.Inquisitor.Purify.ApCost = 3;
            t.Inquisitor.Purify.Range = 2;
            t.Inquisitor.Judgment.ApCost = 3;
            t.Inquisitor.Judgment.Range = 1;
            t.Inquisitor.Judgment.Damage = 3;
            t.Inquisitor.Judgment.PushCells = 2;

            t.Archer.Bola.ApCost = 3;
            t.Archer.Bola.Range = 3;
            t.Archer.Bola.Damage = 1;
            t.Archer.Bola.MaxCells = 1;
            t.Archer.Bola.TargetTurns = 1;

            return t;
        }

        /// <summary>QuickBattle 에서 양 팀이 쓰는 4인 구성. [가정] 기존 샘플(전사/궁수/수호/암살)과 비슷한 역할 분포.</summary>
        public static string[] CreateStartingLineup()
        {
            return new[]
            {
                WarriorPiece.PieceId,
                ArcherPiece.PieceId,
                GardenerPiece.PieceId,
                ScythePiece.PieceId,
            };
        }
    }
}
