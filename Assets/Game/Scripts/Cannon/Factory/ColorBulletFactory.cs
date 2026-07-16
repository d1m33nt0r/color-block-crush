using Game.BlockCrush.Cannon;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.Scripts.Shared;

namespace Game.BlockCrush.Cannon.Factory
{
    public sealed class ColorBulletFactory : IFactory<BlockColorKey, Transform, ColorBullet>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;
        private readonly ColorBlockDatabase _blockDatabase;

        public ColorBulletFactory(
            DiContainer container,
            FactoryConfig config,
            [InjectOptional] ColorBlockDatabase blockDatabase)
        {
            _container = container;
            _config = config;
            _blockDatabase = blockDatabase;
        }

        public ColorBullet Create(BlockColorKey colorKey, Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;
            ColorBullet bullet = _config.DefaultBulletPrefab != null
                ? _container.InstantiatePrefabForComponent<ColorBullet>(_config.DefaultBulletPrefab, targetParent)
                : CreatePrimitiveBullet(targetParent);

            ColorBlockDefinition definition = _blockDatabase != null
                ? _blockDatabase.GetDefinitionOrFallback(colorKey)
                : ColorBlockDefinition.CreateFallback(colorKey);

            bullet.Initialize(colorKey, definition, _config.AnimationSettings);
            return bullet;
        }

        private ColorBullet CreatePrimitiveBullet(Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.name = "Color Bullet";
            gameObject.transform.SetParent(parent, false);

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            return _container.InstantiateComponent<ColorBullet>(gameObject);
        }
    }
}
