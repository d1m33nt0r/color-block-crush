using Game.BlockCrush.Block;
using Game.BlockCrush.Shared;
using UnityEngine;
using Zenject;
using Game.BlockCrush.Block.Shared;
using Game.Scripts.Shared;

namespace Game.BlockCrush.Block.Factory
{
    public sealed class ColorBlockFactory : IFactory<ColorBlockDefinition, Transform, ColorBlock>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;

        public ColorBlockFactory(DiContainer container, FactoryConfig config)
        {
            _container = container;
            _config = config;
        }

        public ColorBlock Create(ColorBlockDefinition definition, Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;
            ColorBlock prefab = definition != null && definition.prefab != null
                ? definition.prefab
                : _config.DefaultBlockPrefab;

            ColorBlock block = prefab != null
                ? _container.InstantiatePrefabForComponent<ColorBlock>(prefab, targetParent)
                : CreatePrimitiveBlock(targetParent);

            block.Initialize(definition, _config.AnimationSettings);
            return block;
        }

        private ColorBlock CreatePrimitiveBlock(Transform parent)
        {
            GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gameObject.name = "Color Block";
            gameObject.transform.SetParent(parent, false);

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            _container.InstantiateComponent<ColorBlockAnimator>(gameObject);
            return _container.InstantiateComponent<ColorBlock>(gameObject);
        }
    }
}
