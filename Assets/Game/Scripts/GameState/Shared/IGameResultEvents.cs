using System;

namespace Game.Scripts.GameState.Shared
{
    public interface IGameResultEvents
    {
        event Action LevelCompleted;
        event Action GameOver;
    }
}
