using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.BlockCrush.Effects
{
    public sealed class BlockDestroyEffectSpawner
    {
        private readonly BlockDestroyEffectPool _pool;

        public BlockDestroyEffectSpawner(BlockDestroyEffectPool pool)
        {
            _pool = pool;
        }

        public void Spawn(
            Vector3 worldPosition,
            Color color,
            CancellationToken cancellationToken = default)
        {
            BlockDestroyEffect effect = _pool.Get();
            effect.transform.position = worldPosition;
            PlayAndReleaseAsync(effect, color, cancellationToken).Forget();
        }

        private async UniTask PlayAndReleaseAsync(
            BlockDestroyEffect effect,
            Color color,
            CancellationToken cancellationToken)
        {
            try
            {
                await effect.PlayAsync(color, cancellationToken);
            }
            catch (System.OperationCanceledException)
            {
            }
            finally
            {
                _pool.Release(effect);
            }
        }
    }
}
