using Game.BlockCrush.Cannon;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.BlockCrush.Level.Shared;
using Game.Scripts.Shared;

namespace Game.BlockCrush.Cannon.Factory
{
    public sealed class ColorCannonFactory : IFactory<CannonSpawnData, Transform, ColorCannon>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;
        private readonly ColorBlockDatabase _blockDatabase;

        public ColorCannonFactory(
            DiContainer container,
            FactoryConfig config,
            [InjectOptional] ColorBlockDatabase blockDatabase)
        {
            _container = container;
            _config = config;
            _blockDatabase = blockDatabase;
        }

        public ColorCannon Create(CannonSpawnData spawnData, Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;
            ColorCannon cannon = _config.DefaultCannonPrefab != null
                ? _container.InstantiatePrefabForComponent<ColorCannon>(_config.DefaultCannonPrefab, targetParent)
                : CreatePrimitiveCannon(targetParent);

            ColorBlockDefinition definition = _blockDatabase != null
                ? _blockDatabase.GetDefinitionOrFallback(spawnData.color)
                : ColorBlockDefinition.CreateFallback(spawnData.color);

            cannon.Initialize(
                spawnData.color,
                spawnData.shots,
                spawnData.shootRate,
                definition,
                _config.AnimationSettings);
            return cannon;
        }

        private ColorCannon CreatePrimitiveCannon(Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gameObject.name = "Color Cannon";
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localScale = new Vector3(0.75f, 0.5f, 0.75f);

            _container.InstantiateComponent<ColorCannonAnimator>(gameObject);
            return _container.InstantiateComponent<ColorCannon>(gameObject);
        }
    }
}
