namespace MiniChess.Core.Actions
{
    public enum PlaceFailReason
    {
        None,
        NotSetupPhase,

        /// <summary>이 팀은 이미 배치했다(다시 배치 불가).</summary>
        AlreadyPlaced,

        /// <summary>배치 유닛 수가 규칙의 팀당 유닛 수와 다르다.</summary>
        WrongUnitCount,

        /// <summary>경기 캐릭터 목록에 없는 Id.</summary>
        UnknownCharacter,

        /// <summary>캐릭터의 필수 수치가 비어 있다.</summary>
        CharacterNotConfigured,

        /// <summary>한 팀에 같은 캐릭터를 둘 이상 넣었다.</summary>
        DuplicateCharacter,

        /// <summary>자기 팀 시작 위치가 아닌 칸.</summary>
        NotSpawnCell,

        /// <summary>같은 칸에 둘 이상 배치했다.</summary>
        DuplicatePosition,
    }
}
