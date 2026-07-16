using Game.BlockCrush.Grid;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Grid.Shared;
using Game.Scripts.Shared;

namespace Game.BlockCrush.Grid.Factory
{
    public sealed class GridCellFactory : IFactory<GridPosition, Transform, BlockCell>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;

        public GridCellFactory(DiContainer container, FactoryConfig config)
        {
            _container = container;
            _config = config;
        }

        public BlockCell Create(GridPosition position, Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;
            BlockCell cell;

            if (_config.CellPrefab != null)
            {
                cell = _container.InstantiatePrefabForComponent<BlockCell>(_config.CellPrefab, targetParent);
            }
            else
            {
                GameObject gameObject = new GameObject("Grid Cell");
                gameObject.transform.SetParent(targetParent, false);
                cell = _container.InstantiateComponent<BlockCell>(gameObject);
            }

            cell.Initialize(position, _config.AnimationSettings);
            return cell;
        }
    }
}
