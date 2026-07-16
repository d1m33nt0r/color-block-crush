using Game.BlockCrush.Cannon;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Grid.Shared;
using Game.Scripts.Shared;

namespace Game.BlockCrush.Cannon.Factory
{
    public sealed class CannonGridCellFactory : IFactory<GridPosition, Transform, CannonGridCell>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;

        public CannonGridCellFactory(DiContainer container, FactoryConfig config)
        {
            _container = container;
            _config = config;
        }

        public CannonGridCell Create(GridPosition position, Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;
            CannonGridCell cell;

            if (_config.CannonCellPrefab != null)
            {
                cell = _container.InstantiatePrefabForComponent<CannonGridCell>(_config.CannonCellPrefab, targetParent);
            }
            else
            {
                GameObject gameObject = new GameObject("Cannon Cell");
                gameObject.transform.SetParent(targetParent, false);
                cell = _container.InstantiateComponent<CannonGridCell>(gameObject);
            }

            cell.Initialize(position, _config.AnimationSettings);
            return cell;
        }
    }
}
