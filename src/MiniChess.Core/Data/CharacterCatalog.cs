using System;
using System.Collections.Generic;

namespace MiniChess.Core.Data
{
    /// <summary>경기에서 고를 수 있는 캐릭터 정의 모음(Id → 정의). 배치 요청의 캐릭터 Id 를 해석한다.</summary>
    public class CharacterCatalog
    {
        private readonly Dictionary<string, CharacterDefinition> _characters = new Dictionary<string, CharacterDefinition>();

        public static CharacterCatalog Empty => new CharacterCatalog();

        public IEnumerable<CharacterDefinition> All => _characters.Values;

        public CharacterCatalog()
        {
        }

        public CharacterCatalog(IEnumerable<CharacterDefinition> characters)
        {
            foreach (CharacterDefinition character in characters)
                Add(character);
        }

        public CharacterCatalog Add(CharacterDefinition character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (_characters.ContainsKey(character.Id)) throw new ArgumentException($"캐릭터 Id 중복: {character.Id}", nameof(character));

            _characters.Add(character.Id, character);
            return this;
        }

        public CharacterDefinition Find(string id)
        {
            return id != null && _characters.TryGetValue(id, out CharacterDefinition character) ? character : null;
        }
    }
}
