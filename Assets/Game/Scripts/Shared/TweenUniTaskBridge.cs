using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Game.BlockCrush.Shared
{
    public static class TweenUniTaskBridge
    {
        public static async UniTask AwaitCompletionAsync(Tween tween, CancellationToken cancellationToken = default)
        {
            if (tween == null || !tween.IsActive())
            {
                return;
            }

            UniTaskCompletionSource source = new UniTaskCompletionSource();
            TweenCallback complete = () => source.TrySetResult();

            tween.OnComplete(complete);
            tween.OnKill(complete);

            // Do not kill DOTween from the cancellation callback. OnDestroy can cancel
            // while DOTween is already unlinking the tween, which may corrupt its active list.
            using (cancellationToken.Register(() => source.TrySetResult()))
            {
                await source.Task;
            }
        }
    }
}
