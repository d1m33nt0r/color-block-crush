using System.Collections.Generic;
using Game.Scripts.Shared;
using UnityEngine;

namespace Game.BlockCrush.Effects
{
    public sealed class BlockDestroyEffectPool
    {
        private readonly BlockDestroyEffect.Factory _factory;
        private readonly FactoryConfig _config;
        private readonly Stack<BlockDestroyEffect> _pool = new Stack<BlockDestroyEffect>();
        private Transform _root;

        public BlockDestroyEffectPool(
            BlockDestroyEffect.Factory factory,
            FactoryConfig config)
        {
            _factory = factory;
            _config = config;
        }

        public BlockDestroyEffect Get()
        {
            BlockDestroyEffect effect = null;

            while (_pool.Count > 0 && effect == null)
            {
                effect = _pool.Pop();
            }

            if (effect == null)
            {
                effect = _factory.Create(Root);
            }

            effect.transform.SetParent(Root, false);
            effect.gameObject.SetActive(true);
            return effect;
        }

        public void Release(BlockDestroyEffect effect)
        {
            if (effect == null)
            {
                return;
            }

            effect.StopAndClear();
            effect.transform.SetParent(Root, false);
            effect.gameObject.SetActive(false);
            _pool.Push(effect);
        }

        private Transform Root
        {
            get
            {
                if (_root != null)
                {
                    return _root;
                }

                GameObject rootObject = new GameObject("Block Destroy Effects Pool");
                Transform parent = _config != null ? _config.RuntimeRoot : null;
                rootObject.transform.SetParent(parent, false);
                _root = rootObject.transform;
                return _root;
            }
        }
    }
}
