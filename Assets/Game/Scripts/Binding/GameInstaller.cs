using Game.BlockCrush.Block;
using Game.BlockCrush.Cannon;
using Game.BlockCrush.Effects;
using Game.BlockCrush.Grid;
using Game.BlockCrush.Level;
using Game.BlockCrush.Shared;
using Game.Scripts.GameState;
using Game.Scripts.GameState.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Grid.Shared;
using Game.BlockCrush.Level.Shared;
using Game.BlockCrush.Block.Factory;
using Game.BlockCrush.Grid.Factory;
using Game.BlockCrush.Cannon.Factory;
using Game.BlockCrush.Effects.Factory;
using Game.Scripts.Shared;

namespace Game.BlockCrush.Binding
{
    public sealed class GameInstaller : MonoInstaller
    {
        [Header("Scene")]
        [SerializeField] private BlockGrid _grid;
        [SerializeField] private CannonGrid _cannonGrid;
        [SerializeField] private LevelBootstrapper _bootstrapper;
        [SerializeField] private Transform _runtimeRoot;

        [Header("Data")]
        [SerializeField] private ColorBlockDatabase _blockDatabase;
        [SerializeField] private AnimationSettings _animationSettings;

        [Header("Prefabs")]
        [SerializeField] private BlockCell _cellPrefab;
        [SerializeField] private ColorBlock _defaultBlockPrefab;
        [SerializeField] private CannonGridCell _cannonCellPrefab;
        [SerializeField] private CannonSlot _cannonSlotPrefab;
        [SerializeField] private ColorCannon _defaultCannonPrefab;
        [SerializeField] private ColorBullet _defaultBulletPrefab;
        [SerializeField] private BlockDestroyEffect _blockDestroyEffectPrefab;

        public override void InstallBindings()
        {
            AnimationSettings animationSettings = _animationSettings != null
                ? _animationSettings
                : ScriptableObject.CreateInstance<AnimationSettings>();

            FactoryConfig factoryConfig = new FactoryConfig(
                _cellPrefab,
                _defaultBlockPrefab,
                _cannonCellPrefab,
                _cannonSlotPrefab,
                _defaultCannonPrefab,
                _defaultBulletPrefab,
                _blockDestroyEffectPrefab,
                _runtimeRoot,
                animationSettings);

            Container.Bind<AnimationSettings>().FromInstance(animationSettings).AsSingle();
            Container.Bind<FactoryConfig>().FromInstance(factoryConfig).AsSingle();
            Container.Bind<LevelJsonParser>().AsSingle();
            Container.Bind<CannonJsonParser>().AsSingle();
            Container.Bind<BlockDestroyEffectPool>().AsSingle();
            Container.Bind<BlockDestroyEffectSpawner>().AsSingle();
            InstallGameState();

            if (_blockDatabase != null)
            {
                Container.Bind<ColorBlockDatabase>().FromInstance(_blockDatabase).AsSingle();
            }

            if (_grid != null)
            {
                Container.Bind<BlockGrid>().FromInstance(_grid).AsSingle();
                Container.QueueForInject(_grid);
            }
            else
            {
                Container.Bind<BlockGrid>().FromComponentInHierarchy().AsSingle();
            }

            if (_bootstrapper != null)
            {
                Container.QueueForInject(_bootstrapper);
            }

            if (_cannonGrid != null)
            {
                Container.Bind<CannonGrid>().FromInstance(_cannonGrid).AsSingle();
                Container.QueueForInject(_cannonGrid);
            }
            else
            {
                Container.Bind<CannonGrid>().FromComponentInHierarchy().AsSingle();
            }

            Container
                .BindFactory<GridPosition, Transform, BlockCell, BlockCell.Factory>()
                .FromFactory<GridCellFactory>();

            Container
                .BindFactory<ColorBlockDefinition, Transform, ColorBlock, ColorBlock.Factory>()
                .FromFactory<ColorBlockFactory>();

            Container
                .BindFactory<GridPosition, Transform, CannonGridCell, CannonGridCell.Factory>()
                .FromFactory<CannonGridCellFactory>();

            Container
                .BindFactory<int, Transform, CannonSlot, CannonSlot.Factory>()
                .FromFactory<CannonSlotFactory>();

            Container
                .BindFactory<CannonSpawnData, Transform, ColorCannon, ColorCannon.Factory>()
                .FromFactory<ColorCannonFactory>();

            Container
                .BindFactory<BlockColorKey, Transform, ColorBullet, ColorBullet.Factory>()
                .FromFactory<ColorBulletFactory>();

            Container
                .BindFactory<Transform, BlockDestroyEffect, BlockDestroyEffect.Factory>()
                .FromFactory<BlockDestroyEffectFactory>();
        }

        private void InstallGameState()
        {
            Container
                .Bind<GameResultEvents>()
                .AsSingle();

            Container
                .Bind<IGameResultEvents>()
                .To<GameResultEvents>()
                .FromResolve();

            Container
                .BindInterfacesTo<GameStateController>()
                .AsSingle();

            Container
                .BindInterfacesTo<GameResultUIController>()
                .AsSingle();
        }
    }
}
