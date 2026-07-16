using Game.BlockCrush.Cannon;
using Game.BlockCrush.Grid;
using Game.BlockCrush.Level;
using Zenject;

namespace Game.Scripts.GameState
{
    public sealed class GameStateController : IInitializable, ITickable
    {
        private readonly GameResultEvents _resultEvents;
        private readonly BlockGrid _blockGrid;
        private readonly CannonGrid _cannonGrid;
        private readonly LevelBootstrapper _bootstrapper;

        private bool _hasResult;

        public GameStateController(
            GameResultEvents resultEvents,
            [InjectOptional] BlockGrid blockGrid,
            [InjectOptional] CannonGrid cannonGrid,
            [InjectOptional] LevelBootstrapper bootstrapper)
        {
            _resultEvents = resultEvents;
            _blockGrid = blockGrid;
            _cannonGrid = cannonGrid;
            _bootstrapper = bootstrapper;
        }

        public void Initialize()
        {
            _hasResult = false;
        }

        public void Tick()
        {
            if (_hasResult || _blockGrid == null || !IsLevelReady())
            {
                return;
            }

            if (_blockGrid.ActiveBlockCount <= 0 && !HasPendingShots())
            {
                ResolveLevelCompleted();
                return;
            }

            if (_cannonGrid != null && _cannonGrid.IsGameOverState())
            {
                ResolveGameOver();
            }
        }

        private bool IsLevelReady()
        {
            return _bootstrapper == null || _bootstrapper.IsLoaded;
        }

        private bool HasPendingShots()
        {
            return _cannonGrid != null && _cannonGrid.HasPendingTargetedBlocks;
        }

        private void ResolveLevelCompleted()
        {
            _hasResult = true;
            _resultEvents.RaiseLevelCompleted();
        }

        private void ResolveGameOver()
        {
            _hasResult = true;
            _resultEvents.RaiseGameOver();
        }
    }
}
