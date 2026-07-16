using System.Collections.Generic;

namespace Game.Scripts.Data.UserData.Levels.Shared
{
    public interface ILevelsUserData
    {
        IReadOnlyList<bool> CompletedLevels { get; }
        int CurrentLevelIndex { get; }

        void EnsureLevelCount(int levelCount);
        bool IsLevelCompleted(int levelIndex);
        void SetLevelCompleted(int levelIndex, bool completed);
        void SetCurrentLevelIndex(int levelIndex);
        bool Save();
    }
}
