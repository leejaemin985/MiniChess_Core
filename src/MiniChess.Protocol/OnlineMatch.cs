using MiniChess.Core.Controller;
using MiniChess.Core.Presets;
using MiniChess.Core.Snapshots;

namespace MiniChess.Protocol
{
    /// <summary>
    /// 온라인 경기의 설정. 서버는 Create 로 경기를 만들고, 클라이언트는 같은 설정과 서버가 보낸 스냅샷으로 Restore 한다.
    /// [임시] 규칙/맵/캐릭터는 프로토타입 프리셋이다.
    /// </summary>
    public static class OnlineMatch
    {
        public static GameConfig CreateConfig(int? seed = null)
        {
            return new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
                Seed = seed,
            };
        }

        /// <summary>서버의 경기.</summary>
        public static GameController Create(int seed)
        {
            return GameController.Create(CreateConfig(seed));
        }

        /// <summary>클라이언트의 경기: 서버가 보낸 스냅샷으로 다시 만든다(표시/조회 전용).</summary>
        public static GameController Restore(GameConfig config, GameSnapshot snapshot)
        {
            return GameController.Restore(config, snapshot);
        }
    }
}
