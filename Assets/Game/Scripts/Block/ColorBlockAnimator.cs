using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.BlockCrush.Shared;
using UnityEngine;

namespace Game.BlockCrush.Block
{
    public sealed class ColorBlockAnimator : MonoBehaviour
    {
        [SerializeField] private AnimationSettings _settingsOverride;

        private Vector3 _initialScale;
        private bool _isShakingPosition;

        private void Awake()
        {
            CaptureInitialScale();
        }

        public void CaptureInitialScale()
        {
            _initialScale = transform.localScale;
        }

        public async UniTask PlayDestroyAsync(AnimationSettings fallbackSettings, CancellationToken cancellationToken = default)
        {
            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            float duration = settings != null ? settings.destroyDuration : 0.2f;
            float scaleMultiplier = settings != null ? settings.destroyScaleMultiplier : 0.05f;
            Ease ease = settings != null ? settings.destroyEase : Ease.InBack;

            transform.DOKill(false);

            if (duration <= 0f)
            {
                transform.localScale = _initialScale * scaleMultiplier;
                return;
            }

            Tween tween = transform
                .DOScale(_initialScale * scaleMultiplier, duration)
                .SetEase(ease)
                .SetLink(gameObject);

            await TweenUniTaskBridge.AwaitCompletionAsync(tween, cancellationToken);
        }

        public async UniTask PlayNeighborShakeAsync(
            AnimationSettings fallbackSettings,
            CancellationToken cancellationToken = default)
        {
            if (_isShakingPosition)
            {
                return;
            }

            AnimationSettings settings = _settingsOverride != null ? _settingsOverride : fallbackSettings;
            float duration = settings != null ? settings.neighborShakeDuration : 0.14f;
            float horizontalStrength = settings != null ? settings.neighborShakeHorizontalStrength : 0.04f;
            float verticalStrength = settings != null ? settings.neighborShakeVerticalStrength : 0.01f;
            int vibrato = settings != null ? Mathf.Max(1, settings.neighborShakeVibrato) : 6;
            Ease ease = settings != null ? settings.neighborShakeEase : Ease.OutQuad;

            if (duration <= 0f || (horizontalStrength <= 0f && verticalStrength <= 0f))
            {
                return;
            }

            _isShakingPosition = true;

            try
            {
                float stepDuration = duration / (vibrato + 1);
                Vector3 currentOffset = Vector3.zero;
                Sequence sequence = DOTween.Sequence().SetLink(gameObject);

                for (int i = 0; i < vibrato; i++)
                {
                    Vector2 horizontal = UnityEngine.Random.insideUnitCircle * horizontalStrength;
                    float vertical = UnityEngine.Random.Range(-verticalStrength, verticalStrength);
                    Vector3 nextOffset = new Vector3(horizontal.x, vertical, horizontal.y);
                    Vector3 delta = nextOffset - currentOffset;
                    currentOffset = nextOffset;

                    sequence.Append(transform
                        .DOBlendableLocalMoveBy(delta, stepDuration)
                        .SetEase(ease));
                }

                sequence.Append(transform
                    .DOBlendableLocalMoveBy(-currentOffset, stepDuration)
                    .SetEase(ease));

                await TweenUniTaskBridge.AwaitCompletionAsync(sequence, cancellationToken);
            }
            finally
            {
                _isShakingPosition = false;
            }
        }
    }
}
