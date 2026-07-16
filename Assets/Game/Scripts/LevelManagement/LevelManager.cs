using Game.Scripts.Data.GameData.Levels.Data;
using Game.Scripts.Data.GameData.Levels.Shared;
using Game.Scripts.Data.UserData.Levels.Shared;
using Game.Scripts.LevelManagement.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace Game.Scripts.LevelManagement
{
    public sealed class LevelManager : ILevelManager, IInitializable
    {
        private const int FirstLoopLevelIndex = 2;
        private const int ExclusiveLoopLevelEndIndex = 5;

        private readonly ILevelDatabase _levelDatabase;
        private readonly ILevelsUserData _levelsUserData;

        public LevelManager(
            ILevelDatabase levelDatabase,
            ILevelsUserData levelsUserData)
        {
            _levelDatabase = levelDatabase;
            _levelsUserData = levelsUserData;
        }

        public int CurrentLevelIndex
        {
            get { return _levelsUserData.CurrentLevelIndex; }
        }

        public LevelData CurrentLevel
        {
            get
            {
                TryGetCurrentLevel(out LevelData levelData);
                return levelData;
            }
        }

        public void Initialize()
        {
            int levelCount = LevelCount;
            _levelsUserData.EnsureLevelCount(levelCount);

            if (!IsValidLevelIndex(_levelsUserData.CurrentLevelIndex))
            {
                SetCurrentLevel(ResolveNextLevelIndex());
            }
        }

        public bool TryGetCurrentLevel(out LevelData levelData)
        {
            levelData = null;

            if (!IsValidLevelIndex(CurrentLevelIndex))
            {
                return false;
            }

            levelData = _levelDatabase.Levels[CurrentLevelIndex];
            return levelData != null;
        }

        public void CompleteCurrentLevel()
        {
            if (IsValidLevelIndex(CurrentLevelIndex))
            {
                _levelsUserData.SetLevelCompleted(CurrentLevelIndex, true);
            }

            SetCurrentLevel(ResolveNextLevelIndex());
        }

        public void LoadCurrentLevel()
        {
            ReloadActiveScene();
        }

        public void LoadNextLevel()
        {
            LoadCurrentLevel();
        }

        public void RestartCurrentLevel()
        {
            ReloadActiveScene();
        }

        private int ResolveNextLevelIndex()
        {
            int levelCount = LevelCount;
            if (levelCount <= 0)
            {
                return -1;
            }

            int tutorialEnd = Mathf.Min(FirstLoopLevelIndex, levelCount);
            for (int i = 0; i < tutorialEnd; i++)
            {
                if (!_levelsUserData.IsLevelCompleted(i))
                {
                    return i;
                }
            }

            int loopStart = levelCount > FirstLoopLevelIndex ? FirstLoopLevelIndex : 0;
            int loopEnd = Mathf.Min(ExclusiveLoopLevelEndIndex, levelCount);
            for (int i = loopStart; i < loopEnd; i++)
            {
                if (!_levelsUserData.IsLevelCompleted(i))
                {
                    return i;
                }
            }

            if (loopEnd > loopStart)
            {
                return Random.Range(loopStart, loopEnd);
            }

            return Random.Range(0, levelCount);
        }

        private void SetCurrentLevel(int levelIndex)
        {
            _levelsUserData.SetCurrentLevelIndex(levelIndex);
            _levelsUserData.Save();
        }

        private int LevelCount
        {
            get { return _levelDatabase != null && _levelDatabase.Levels != null ? _levelDatabase.Levels.Count : 0; }
        }

        private bool IsValidLevelIndex(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex < LevelCount;
        }

        private static void ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
                return;
            }

            SceneManager.LoadScene(activeScene.name);
        }
    }
}
