using System;
using Game.Scripts.GameState.Shared;

namespace Game.Scripts.GameState
{
    public sealed class GameResultEvents : IGameResultEvents
    {
        public event Action LevelCompleted;
        public event Action GameOver;

        public void RaiseLevelCompleted()
        {
            LevelCompleted?.Invoke();
        }

        public void RaiseGameOver()
        {
            GameOver?.Invoke();
        }
    }
}
