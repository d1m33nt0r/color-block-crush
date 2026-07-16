using System;
using System.Collections.Generic;
using Game.Scripts.Data.GameData.Levels.Shared;
using Game.Scripts.Data.UserData.Levels.Shared;
using Game.Scripts.Data.UserData.Shared;
using UnityEngine;
using Zenject;

namespace Game.Scripts.Data.UserData.Levels
{
    [Serializable]
    public class LevelsUserData : UserData<LevelsUserData>, ILevelsUserData, IInitializable
    {
        protected override string FileName => "LevelsUserData.json";
        
        public IReadOnlyList<bool> CompletedLevels => _completedLevels;
        public int CurrentLevelIndex => _currentLevelIndex;
        
        [Inject] private ILevelDatabase _levelDatabase;
        
        [SerializeField] private List<bool> _completedLevels;
        [SerializeField] private int _currentLevelIndex = -1;
        
        public void Initialize()
        {
            if (!TryRead(out var data))
            {
                _completedLevels = new List<bool>();
                _currentLevelIndex = -1;
                EnsureLevelCount(GetLevelCount());
                Save();
                return;
            }
            
            _completedLevels = data.CompletedLevels != null
                ? new List<bool>(data.CompletedLevels)
                : new List<bool>();

            _currentLevelIndex = data.CurrentLevelIndex;
            EnsureLevelCount(GetLevelCount());
            Save();
        }

        public void EnsureLevelCount(int levelCount)
        {
            levelCount = Mathf.Max(0, levelCount);

            if (_completedLevels == null)
            {
                _completedLevels = new List<bool>();
            }

            while (_completedLevels.Count < levelCount)
            {
                _completedLevels.Add(false);
            }

            if (_completedLevels.Count > levelCount)
            {
                _completedLevels.RemoveRange(levelCount, _completedLevels.Count - levelCount);
            }

            if (_currentLevelIndex >= levelCount)
            {
                _currentLevelIndex = -1;
            }
        }

        public bool IsLevelCompleted(int levelIndex)
        {
            return levelIndex >= 0
                   && _completedLevels != null
                   && levelIndex < _completedLevels.Count
                   && _completedLevels[levelIndex];
        }

        public void SetLevelCompleted(int levelIndex, bool completed)
        {
            if (levelIndex < 0)
            {
                return;
            }

            EnsureLevelCount(Mathf.Max(GetLevelCount(), levelIndex + 1));
            _completedLevels[levelIndex] = completed;
        }

        public void SetCurrentLevelIndex(int levelIndex)
        {
            _currentLevelIndex = levelIndex;
        }

        private int GetLevelCount()
        {
            return _levelDatabase != null && _levelDatabase.Levels != null
                ? _levelDatabase.Levels.Count
                : 0;
        }
    }
}
