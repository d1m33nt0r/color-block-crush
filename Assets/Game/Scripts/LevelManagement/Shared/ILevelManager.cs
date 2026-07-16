using Game.Scripts.Data.GameData.Levels.Data;

namespace Game.Scripts.LevelManagement.Shared
{
    public interface ILevelManager
    {
        int CurrentLevelIndex { get; }
        LevelData CurrentLevel { get; }

        bool TryGetCurrentLevel(out LevelData levelData);
        void CompleteCurrentLevel();
        void LoadCurrentLevel();
        void LoadNextLevel();
        void RestartCurrentLevel();
    }
}
