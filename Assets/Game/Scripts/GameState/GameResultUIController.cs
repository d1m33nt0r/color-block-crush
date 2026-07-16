using System;
using Game.Scripts.Core.Common.Sound.Data;
using Game.Scripts.Core.Common.Sound.Shared;
using Game.Scripts.GameState.Shared;
using Game.Scripts.LevelManagement.Shared;
using Game.Scripts.UI.GameOver.Shared;
using Game.Scripts.UI.LevelCompleted.Shared;
using Zenject;

namespace Game.Scripts.GameState
{
    public sealed class GameResultUIController : IInitializable, IDisposable
    {
        private readonly IGameResultEvents _resultEvents;
        private readonly ILevelManager _levelManager;
        private readonly IGameOverUI _gameOverUI;
        private readonly ILevelCompletedUI _levelCompletedUI;
        private readonly ISoundPlayer _levelCompletedSoundPlayer;
        private readonly ISoundPlayer _gameOverSoundPlayer;

        public GameResultUIController(
            IGameResultEvents resultEvents,
            [InjectOptional] ILevelManager levelManager,
            [InjectOptional] IGameOverUI gameOverUI,
            [InjectOptional] ILevelCompletedUI levelCompletedUI,
            [InjectOptional] ISoundPlayer levelCompletedSoundPlayer,
            [InjectOptional] ISoundPlayer gameOverSoundPlayer)
        {
            _resultEvents = resultEvents;
            _levelManager = levelManager;
            _gameOverUI = gameOverUI;
            _levelCompletedUI = levelCompletedUI;
            _levelCompletedSoundPlayer = levelCompletedSoundPlayer;
            _gameOverSoundPlayer = gameOverSoundPlayer;

            _levelCompletedSoundPlayer?.Initialize(SoundKey.CompleteLevel);
            _gameOverSoundPlayer?.Initialize(SoundKey.GameOver);
        }

        public void Initialize()
        {
            _resultEvents.LevelCompleted += OnLevelCompleted;
            _resultEvents.GameOver += OnGameOver;
        }

        public void Dispose()
        {
            _resultEvents.LevelCompleted -= OnLevelCompleted;
            _resultEvents.GameOver -= OnGameOver;
        }

        private void OnLevelCompleted()
        {
            _levelCompletedSoundPlayer?.Play();
            _levelManager?.CompleteCurrentLevel();
            _levelCompletedUI?.Show();
        }

        private void OnGameOver()
        {
            _gameOverSoundPlayer?.Play();
            _gameOverUI?.Show();
        }
    }
}
