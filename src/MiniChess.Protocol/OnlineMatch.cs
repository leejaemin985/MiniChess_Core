using MiniChess.Core.Controller;
using MiniChess.Core.Presets;

namespace MiniChess.Protocol
{
    /// <summary>
    /// 온라인 경기의 설정. 서버와 클라이언트가 같은 함수로 경기를 만들어, 같은 시드라면 같은 경기가 된다.
    /// [임시] 규칙/맵/캐릭터는 프로토타입 프리셋이다.
    /// </summary>
    public static class OnlineMatch
    {
        public static GameController Create(int seed)
        {
            return GameController.Create(new GameConfig
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                Skills = PrototypeTestPreset.CreateSkills(),
                Seed = seed,
            });
        }
    }
}
