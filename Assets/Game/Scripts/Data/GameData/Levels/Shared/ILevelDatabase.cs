using System.Collections.Generic;
using Game.Scripts.Data.GameData.Levels.Data;

namespace Game.Scripts.Data.GameData.Levels.Shared
{
    public interface ILevelDatabase
    {
        IReadOnlyList<LevelData> Levels { get; }
    }
}