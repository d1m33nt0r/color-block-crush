using Game.BlockCrush.Cannon;
using Game.Scripts.Shared;
using UnityEngine;
using Zenject;

namespace Game.BlockCrush.Cannon.Factory
{
    public sealed class CannonSlotFactory : IFactory<int, Transform, CannonSlot>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;

        public CannonSlotFactory(DiContainer container, FactoryConfig config)
        {
            _container = container;
            _config = config;
        }

        public CannonSlot Create(int index, Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;
            CannonSlot slot;

            if (_config.CannonSlotPrefab != null)
            {
                slot = _container.InstantiatePrefabForComponent<CannonSlot>(_config.CannonSlotPrefab, targetParent);
            }
            else
            {
                GameObject gameObject = new GameObject("Cannon Slot");
                gameObject.transform.SetParent(targetParent, false);
                slot = _container.InstantiateComponent<CannonSlot>(gameObject);
            }

            slot.Initialize(index, _config.AnimationSettings);
            return slot;
        }
    }
}
