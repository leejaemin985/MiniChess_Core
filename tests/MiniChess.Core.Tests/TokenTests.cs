using System.Collections.Generic;
using System.Linq;
using MiniChess.Core.Actions;
using MiniChess.Core.Common;
using MiniChess.Core.Controller;
using MiniChess.Core.Data;
using MiniChess.Core.Effects;
using MiniChess.Core.Effects.Zones;
using MiniChess.Core.Events;
using MiniChess.Core.Statuses.Library;
using MiniChess.Core.Presets;
using MiniChess.Core.Snapshots;
using MiniChess.Core.State;
using MiniChess.Core.Statuses;
using MiniChess.Core.Tests.Support;
using MiniChess.Core.Tokens;
using MiniChess.Protocol;
using Xunit;

namespace MiniChess.Core.Tests
{
    /// <summary>
    /// 토큰(사용자 확정 규칙):
    /// 토큰 라운드의 각 팀 턴 시작에 빈 칸(벽/유닛/장애물/점령 칸 제외) 하나에 토큰이 생기고, 그 턴 종료에 사라진다.
    /// 그 팀 유닛이 칸에 들어가면(지나가기 포함) 먹는다. 힐은 유닛 회복, AP 는 팀 AP. 최대치에 막혀도 토큰은 사라진다.
    /// </summary>
    public class TokenTests
    {
        private sealed class FixedRandom : IRandomSource
        {
            private readonly int _value;

            public FixedRandom(int value) => _value = value;

            public int LastMaxExclusive { get; private set; }

            public int Next(int maxExclusive)
            {
                LastMaxExclusive = maxExclusive;
                return _value;
            }
        }

        /// <summary>2 라운드(전체 턴 3·4)부터 매 라운드 힐 → AP. AP 최대치는 넉넉히.</summary>
        private static TestGame CreateGame(string[] map, TokenKind first = TokenKind.Heal)
        {
            return new TestGame()
                .WithMap(map)
                .WithRules(rules =>
                {
                    rules.Ap = new ApRuleData { MaxAp = 20, StartAp = 4, TurnRecoveryAp = 4, CarryOver = true };
                    rules.Token = new TokenRuleData
                    {
                        StartRound = 2,
                        PeriodRounds = 1,
                        Sequence = new List<TokenKind> { first, first == TokenKind.Heal ? TokenKind.Ap : TokenKind.Heal },
                        ApAmount = 4,
                        HealAmount = 3,
                    };
                });
        }

        private static void PassRound(GameState state)
        {
            new EndTurnAction(Team.Player1).Execute(state);
            new EndTurnAction(Team.Player2).Execute(state);
        }

        // 아래 줄 P1 (0,0), P2 (4,0). 후보는 (1,0) (2,0) (3,0) 과 위 줄 전부.
        private static readonly string[] Corridor =
        {
            "#####",
            ".....",
        };

        #region 주기

        // 사용자 예시: 3 라운드 힐, 4 없음, 5 AP, 7 힐 ...
        [Theory]
        [InlineData(1, null)]
        [InlineData(2, null)]
        [InlineData(3, TokenKind.Heal)]
        [InlineData(4, null)]
        [InlineData(5, TokenKind.Ap)]
        [InlineData(6, null)]
        [InlineData(7, TokenKind.Heal)]
        [InlineData(9, TokenKind.Ap)]
        public void KindForRound_FollowsStartPeriodAndSequence(int round, TokenKind? expected)
        {
            Assert.Equal(expected, TokenSystem.KindForRound(PrototypeTestPreset.CreateRules().Token, round));
        }

        [Theory]
        [InlineData(1, new[] { 3, 4, 5 })]
        [InlineData(3, new[] { 3, 6, 9 })]
        public void KindForRound_PeriodIsAdjustable(int period, int[] tokenRounds)
        {
            var rule = new TokenRuleData { StartRound = 3, PeriodRounds = period, Sequence = new List<TokenKind> { TokenKind.Heal, TokenKind.Ap } };

            int[] rounds = Enumerable.Range(1, tokenRounds.Last()).Where(round => TokenSystem.KindForRound(rule, round) != null).ToArray();

            Assert.Equal(tokenRounds, rounds);
            Assert.Equal(TokenKind.Ap, TokenSystem.KindForRound(rule, tokenRounds[1]));
        }

        [Fact]
        public void NoRule_NoTokens()
        {
            var (state, _) = new TestGame().WithMap(Corridor).Place(Team.Player1, 0, 0).Place(Team.Player2, 4, 0).Start();
            for (int i = 0; i < 4; i++)
                PassRound(state);

            Assert.Empty(state.Events.All.OfType<TokenSpawnedEvent>());
        }

        #endregion

        #region 생성과 소멸

        [Fact]
        public void TokenRound_EachTeamGetsItsOwnToken_ThatExpiresAtItsTurnEnd()
        {
            var (state, _) = CreateGame(Corridor).Place(Team.Player1, 0, 0).Place(Team.Player2, 4, 0).Start();
            Assert.Null(state.Token);

            new EndTurnAction(Team.Player1).Execute(state);
            Assert.Null(state.Token);
            new EndTurnAction(Team.Player2).Execute(state);

            // 2 라운드 P1 턴
            Assert.Equal(3, state.TurnNumber);
            Assert.NotNull(state.Token);
            Assert.Equal(TokenKind.Heal, state.Token.Kind);
            Assert.Equal(Team.Player1, state.Token.Team);

            new EndTurnAction(Team.Player1).Execute(state);

            var expired = Assert.Single(state.Events.All.OfType<TokenExpiredEvent>());
            Assert.Equal(Team.Player1, expired.Team);
            // 2 라운드 P2 턴: 같은 종류의 새 토큰
            Assert.Equal(Team.Player2, state.Token.Team);
            Assert.Equal(TokenKind.Heal, state.Token.Kind);

            new EndTurnAction(Team.Player2).Execute(state);
            Assert.Equal(TokenKind.Ap, state.Token.Kind); // 3 라운드는 다음 종류
            Assert.Equal(3, state.Events.All.OfType<TokenSpawnedEvent>().Count());
        }

        // 벽(#), 점령 칸(C), 유닛, 장애물이 있는 칸은 후보가 아니다. 장판과 덫이 있는 칸은 후보다.
        [Fact]
        public void Spawn_PicksOnlyFreeCells_ExcludingCaptureTileAndObstacles()
        {
            string[] map =
            {
                "#C..",
                "....",
            };
            var (state, u) = CreateGame(map).Place(Team.Player1, 0, 0).Place(Team.Player2, 1, 0).Start();
            var obstacleZone = new FieldZone(null, u[0], 99, state.TurnNumber);
            var trapZone = new FieldZone(null, u[1], 99, state.TurnNumber);
            CellEffectSystem.Place(state, new Position(2, 0), new Obstacle(obstacleZone, 1));
            CellEffectSystem.Place(state, new Position(3, 0), new RootTrap(trapZone, 0, StatusLibrary.Root(1, false), singleUse: true));
            Assert.True(state.Board.GetCell(new Position(2, 0)).HasObstacle);
            Assert.NotNull(state.Board.GetCell(new Position(3, 0)).GetEffect(CellEffectLayer.Trap));
            var random = new FixedRandom(1);
            state.Random = random;

            PassRound(state);

            // 후보: (3,0) 덫, (2,1), (3,1)
            Assert.Equal(3, random.LastMaxExclusive);
            Assert.Equal(new Position(2, 1), state.Token.Position);
        }

        [Fact]
        public void Spawn_NoFreeCell_SkipsThatTurn()
        {
            var (state, _) = CreateGame(new[] { "##", ".." }).Place(Team.Player1, 0, 0).Place(Team.Player2, 1, 0).Start();

            PassRound(state);

            Assert.Null(state.Token);
            Assert.Empty(state.Events.All.OfType<TokenSpawnedEvent>());
        }

        #endregion

        #region 먹기

        /// <summary>2 라운드 P1 턴까지 진행하고 토큰을 (2,0)에 둔다. 위 줄은 벽.</summary>
        private static (GameState State, Unit[] Units) TokenAtMiddle(TokenKind kind, UnitBaseStats p1Stats = null)
        {
            var (state, units) = CreateGame(Corridor, kind).Place(Team.Player1, 0, 0, p1Stats).Place(Team.Player2, 4, 0).Start();
            state.Random = new FixedRandom(1); // 후보 (1,0) (2,0) (3,0) 중 (2,0)
            PassRound(state);
            Assert.Equal(new Position(2, 0), state.Token.Position);
            return (state, units);
        }

        [Fact]
        public void PassingThrough_ConsumesApToken_ForTheTeam()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Ap);
            int apBefore = state.GetPlayer(Team.Player1).Ap.Current;

            new MoveAction(u[0], new Position(3, 0)).Execute(state); // 3칸 이동(AP 3), (2,0)을 지나감

            Assert.Null(state.Token);
            Assert.Equal(apBefore - 3 + 4, state.GetPlayer(Team.Player1).Ap.Current);
            var consumed = Assert.Single(state.Events.All.OfType<TokenConsumedEvent>());
            Assert.Equal(u[0].Id, consumed.UnitId);
            Assert.Equal(4, consumed.Amount);
            Assert.Equal(state.GetPlayer(Team.Player1).Ap.Current, consumed.TeamAp);
            Assert.DoesNotContain(state.Events.All.OfType<TokenExpiredEvent>(), e => e.Team == Team.Player1);
        }

        [Fact]
        public void ApToken_AtMaxAp_IsStillConsumed_ExcessDiscarded()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Ap);
            ApPool ap = state.GetPlayer(Team.Player1).Ap;
            ap.Restore(ap.Max);

            new MoveAction(u[0], new Position(2, 0)).Execute(state); // 이동 비용 2 를 먼저 내고 4 를 얻어 최대치에서 2 가 버려짐

            Assert.Null(state.Token);
            Assert.Equal(ap.Max, ap.Current);
            Assert.Equal(2, Assert.Single(state.Events.All.OfType<TokenConsumedEvent>()).Amount);
        }

        // 덫에 걸려 도중에 멈추면 못 간 칸의 비용은 돌려받는다(이동 비용을 먼저 내도록 바꾼 뒤에도 기존 규칙 유지).
        [Fact]
        public void MoveStoppedByTrap_RefundsUnmovedCells_AndKeepsTokenAp()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Ap);
            var trapZone = new FieldZone(null, u[1], 99, state.TurnNumber);
            CellEffectSystem.Place(state, new Position(2, 0), new RootTrap(trapZone, 0, StatusLibrary.Root(1, false), singleUse: true));
            int apBefore = state.GetPlayer(Team.Player1).Ap.Current;

            new MoveAction(u[0], new Position(3, 0)).Execute(state); // (2,0) 덫에서 멈춤: 2칸 이동, 토큰은 먹음

            Assert.Equal(new Position(2, 0), u[0].Position);
            Assert.Null(state.Token);
            Assert.Equal(apBefore - 2 + 4, state.GetPlayer(Team.Player1).Ap.Current);
        }

        [Fact]
        public void HealToken_HealsTheUnit_UpToMaxHp()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Heal, TestGame.Stats(hp: 10));
            Combat.DamageSystem.Apply(state, new Combat.DamageRequest(null, u[0], 5, Combat.DamageType.Direct));

            new MoveAction(u[0], new Position(2, 0)).Execute(state);

            Assert.Null(state.Token);
            Assert.Equal(8, u[0].Stats.CurrentHp);
            var healed = Assert.Single(state.Events.All.OfType<UnitHealedEvent>());
            Assert.Equal(3, healed.AppliedAmount);
            Assert.True(state.Events.All.ToList().IndexOf(state.Events.All.OfType<TokenConsumedEvent>().Single()) < state.Events.All.ToList().IndexOf(healed));
        }

        [Fact]
        public void HealToken_AtFullHp_IsStillConsumed()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Heal);

            new MoveAction(u[0], new Position(2, 0)).Execute(state);

            Assert.Null(state.Token);
            Assert.Equal(u[0].Stats.MaxHp, u[0].Stats.CurrentHp);
            Assert.Single(state.Events.All.OfType<TokenConsumedEvent>());
        }

        [Fact]
        public void OtherTeamUnit_DoesNotConsume()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Ap);
            state.Board.Relocate(u[1], new Position(2, 0));

            TokenSystem.OnUnitEntered(state, u[1]);

            Assert.NotNull(state.Token);
            Assert.Empty(state.Events.All.OfType<TokenConsumedEvent>());
        }

        [Fact]
        public void ConsumedToken_DoesNotExpireAtTurnEnd()
        {
            var (state, u) = TokenAtMiddle(TokenKind.Ap);
            new MoveAction(u[0], new Position(2, 0)).Execute(state);

            new EndTurnAction(Team.Player1).Execute(state);

            Assert.DoesNotContain(state.Events.All.OfType<TokenExpiredEvent>(), e => e.Team == Team.Player1);
        }

        #endregion

        #region 복사/스냅샷/통신

        [Fact]
        public void Fork_CopiesTheToken()
        {
            var (state, _) = TokenAtMiddle(TokenKind.Heal);

            GameState fork = state.Fork(Team.Player2);

            Assert.Equal(state.Token.Position, fork.Token.Position);
            Assert.Equal(state.Token.Kind, fork.Token.Kind);
            Assert.Equal(state.Token.Team, fork.Token.Team);
        }

        [Fact]
        public void Snapshot_CarriesTheToken_ThroughProtocolJson_AndRestore()
        {
            var config = new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
                Seed = 7,
            };
            GameController game = GameController.Create(config);
            string[] lineup = PrototypeTestPreset.CreateCharacters().Take(config.Rules.Match.UnitsPerTeam).Select(c => c.Id).ToArray();
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player1, lineup.Select((id, x) => new UnitPlacement(id, new Position(x, 1))).ToList()));
            game.PlaceUnits(new PlaceUnitsRequest(Team.Player2, lineup.Select((id, x) => new UnitPlacement(id, new Position(x, 5))).ToList()));
            for (int i = 0; i < 4; i++)
                game.EndTurn(new EndTurnRequest(game.State.CurrentTeam));

            // 프리셋: 3 라운드 첫 턴(전체 턴 5)에 힐 토큰
            Assert.Equal(5, game.State.TurnNumber);
            Assert.NotNull(game.State.Token);

            var update = new StateUpdateMessage
            {
                Events = game.State.Events.All.Select(ProtocolJson.Wrap).ToList(),
                Snapshot = game.GetSnapshot(Team.Player2),
            };
            var back = Assert.IsType<StateUpdateMessage>(ProtocolJson.DecodeServer(ProtocolJson.Encode(update)));

            TokenSnapshot token = back.Snapshot.Token;
            Assert.Equal(TokenKind.Heal, token.Kind);
            Assert.Equal(game.State.Token.Position, token.Position);
            Assert.Equal(Team.Player1, token.Team);
            var spawned = Assert.IsType<TokenSpawnedEvent>(back.Events.Select(ProtocolJson.ReadEvent).Single(e => e is TokenSpawnedEvent));
            Assert.Equal(token.Position, spawned.Position);

            GameController restored = GameController.Restore(config, back.Snapshot);
            Assert.Equal(token.Position, restored.State.Token.Position);
            Assert.Equal(token.Kind, restored.State.Token.Kind);
        }

        #endregion
    }
}
