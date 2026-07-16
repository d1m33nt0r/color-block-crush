using Game.BlockCrush.Effects;
using Game.Scripts.Shared;
using UnityEngine;
using Zenject;

namespace Game.BlockCrush.Effects.Factory
{
    public sealed class BlockDestroyEffectFactory : IFactory<Transform, BlockDestroyEffect>
    {
        private readonly DiContainer _container;
        private readonly FactoryConfig _config;

        public BlockDestroyEffectFactory(DiContainer container, FactoryConfig config)
        {
            _container = container;
            _config = config;
        }

        public BlockDestroyEffect Create(Transform parent)
        {
            Transform targetParent = parent != null ? parent : _config.RuntimeRoot;

            if (_config.BlockDestroyEffectPrefab != null)
            {
                return _container.InstantiatePrefabForComponent<BlockDestroyEffect>(
                    _config.BlockDestroyEffectPrefab,
                    targetParent);
            }

            return CreateFallbackEffect(targetParent);
        }

        private BlockDestroyEffect CreateFallbackEffect(Transform parent)
        {
            GameObject gameObject = new GameObject("Block Destroy Effect");
            gameObject.transform.SetParent(parent, false);

            ParticleSystem particleSystem = gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.loop = false;
            main.duration = 0.18f;
            main.startLifetime = 0.22f;
            main.startSpeed = 1.4f;
            main.startSize = 0.12f;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;

            BlockDestroyEffect effect = _container.InstantiateComponent<BlockDestroyEffect>(gameObject);
            effect.SetParticleSystems(new[] { particleSystem });
            return effect;
        }
    }
}
