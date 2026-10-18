using MiniChess.Core.Controller;
using MiniChess.Core.Presets;
using MiniChess.Core.Snapshots;

namespace MiniChess.Protocol
{
    /// <summary>
    /// 온라인 경기의 설정. 서버는 Create 로 경기를 만들고, 클라이언트는 같은 설정과 서버가 보낸 스냅샷으로 Restore 한다.
    /// 두 쪽이 같은 설정인지는 참가할 때 ConfigHash 로 확인한다.
    /// [임시] 규칙/맵/캐릭터는 프로토타입 프리셋이다.
    /// </summary>
    public static class OnlineMatch
    {
        /// <summary>한 턴의 제한 시간(초). 다 되면 서버가 그 팀의 턴을 끝낸다(사용자 확정 2분). 배치 단계에는 제한이 없다.</summary>
        public const int TurnTimeSeconds = 120;

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

        /// <summary>
        /// 경기 설정의 지문. 스킬 정의에는 람다가 있어 그대로 비교할 수 없으므로, 스킬을 만드는 재료인 수치(SkillTuning)를 대신 넣는다.
        /// 수치가 아닌 코드(규칙 로직)가 바뀐 경우는 ProtocolInfo.Version 으로 막는다.
        /// </summary>
        public static string ConfigHash()
        {
            return ProtocolJson.Fingerprint(new
            {
                Rules = PrototypeTestPreset.CreateRules(),
                Map = PrototypeTestPreset.CreateTestMap(),
                Characters = PrototypeTestPreset.CreateCharacters(),
                SkillTuning = PrototypeTestPreset.CreateSkillTuning(),
            });
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
